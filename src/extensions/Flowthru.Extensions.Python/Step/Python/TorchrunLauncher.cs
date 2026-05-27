using System.Diagnostics;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;

namespace Flowthru.Step.Python;

/// <summary>
/// PyTorch-native distributed launcher — spawns the Python worker via
/// <c>torchrun --nproc_per_node=N</c> so PyTorch DDP and any framework
/// that builds on it (HuggingFace <c>Trainer</c>, Lightning, etc.) can
/// fan out across multiple GPUs on a single box without forking the
/// executor.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Slice 4 limitation — protocol interleaving.</strong> torchrun
/// pipes every rank's stdout to the parent by default, which can
/// interleave with the JSON protocol the worker speaks on stdout. For
/// flows where this matters, set <see cref="RedirectsFlag"/> to
/// whatever torchrun mapping suits the user — e.g. <c>"1:3,2:3,3:3"</c>
/// to redirect ranks 1+ to per-rank log files while keeping rank 0
/// connected to the parent. Slice 5 will move the protocol off stdout
/// onto a dedicated fd so this caveat goes away.
/// </para>
/// <para>
/// <strong>Rank-aware step bodies.</strong> The worker treats
/// non-rank-0 processes as silent participants — they import the user
/// module and let the distributed-training framework
/// (<c>Trainer.train()</c>, Lightning's <c>fit()</c>, etc.) drive the
/// coordination. Only rank 0 returns a result to .NET. Slice 5
/// formalises this contract; in slice 4 it works because every
/// modern distributed-training framework already handles
/// rank-coordination internally — torchrun just spawns the processes
/// and sets the env vars.
/// </para>
/// </remarks>
public sealed class TorchrunLauncher : IPythonLauncher
{
  /// <summary>
  /// Number of processes (and conventionally GPUs) per node.
  /// Translated to <c>--nproc_per_node=N</c>. Required for any
  /// distributed run; default 1 mirrors a single-rank torchrun
  /// invocation, which is functionally equivalent to a direct python
  /// launch but exercises the seam.
  /// </summary>
  public int NProcPerNode { get; init; } = 1;

  /// <summary>
  /// Override the torchrun binary path. Default: the venv's
  /// <c>bin/torchrun</c> alongside the resolved <c>pyExe</c>. Set
  /// this to an absolute path for site-specific renames
  /// (<c>lab-torchrun</c>, <c>mycorp-torchrun-wrapper</c>) without
  /// subclassing.
  /// </summary>
  public string? BinaryPath { get; init; }

  /// <summary>
  /// torchrun's <c>--redirects</c> argument controlling per-rank
  /// stdout/stderr redirection. Default null lets the launcher
  /// auto-compute <c>1:3,2:3,...,(N-1):3</c> for <c>NProcPerNode &gt; 1</c>,
  /// which sends ranks 1..N-1's stdout+stderr to per-rank log files
  /// in <c>$TORCHELASTIC_LOGS_DIR</c> (typically <c>/tmp/torchelastic_*</c>)
  /// while keeping rank 0 connected to the parent — the
  /// configuration the rank-aware worker (slice 5) needs. Set this
  /// explicitly to override the default with a custom per-rank
  /// mapping; set to the empty string to disable redirection
  /// entirely.
  /// </summary>
  public string? RedirectsFlag { get; init; }

  /// <inheritdoc/>
  /// <remarks>
  /// Identity carries <see cref="NProcPerNode"/> so a launcher change
  /// invalidates the cache. DDP outputs are not bitwise-reproducible
  /// across <c>nproc_per_node</c> values; treating them as
  /// cache-equivalent would be wrong.
  /// </remarks>
  public string Identity => $"TorchrunLauncher(nproc_per_node={NProcPerNode})";

  /// <inheritdoc/>
  public IReadOnlyList<PythonPackageRequirement> Requirements { get; } = new[]
  {
    new PythonPackageRequirement(
      Package: "torch",
      VersionConstraint: ">=2.0",
      Reason: "Required by TorchrunLauncher (PyTorch DDP entry point)"
    ),
  };

  /// <inheritdoc/>
  public ProcessStartInfo Build(
    string pyExe,
    string workerScript,
    IReadOnlyDictionary<string, string> envVars
  )
  {
    var binary = BinaryPath ?? Path.Combine(
      Path.GetDirectoryName(pyExe) ?? string.Empty,
      "torchrun"
    );

    var psi = new ProcessStartInfo
    {
      FileName = binary,
      UseShellExecute = false,
      RedirectStandardInput = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true,
    };

    psi.ArgumentList.Add($"--nproc_per_node={NProcPerNode}");
    var redirects = ResolveRedirectsFlag();
    if (!string.IsNullOrWhiteSpace(redirects))
    {
      psi.ArgumentList.Add($"--redirects={redirects}");
    }
    psi.ArgumentList.Add(workerScript);

    foreach (var (key, value) in envVars)
    {
      psi.EnvironmentVariables[key] = value;
    }

    return psi;
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// Two checks. First, the torchrun binary must exist at the
  /// resolved path — without it the launcher cannot start any
  /// subprocess and waiting for runtime to surface the failure is
  /// the exact anti-pattern Flowthru exists to avoid.
  /// </para>
  /// <para>
  /// Second, optional GPU-count guard: if <c>nvidia-smi -L</c> is
  /// available, count the lines (one GPU per line) and refuse if
  /// <see cref="NProcPerNode"/> exceeds the count. When
  /// <c>nvidia-smi</c> isn't available (CPU-only CI, ROCm box,
  /// macOS) we silently pass — better to skip the check than to
  /// emit false positives that block legitimate setups.
  /// </para>
  /// </remarks>
  public Validated<PreFlightError, FlowUnit> Probe()
  {
    var binary = BinaryPath;
    if (string.IsNullOrWhiteSpace(binary))
    {
      // Without a pyExe context we can't resolve the venv binary; assume
      // it's reachable on PATH and let the launch attempt surface a
      // PreFlightError if not.
      binary = "torchrun";
    }

    if (!IsBinaryAvailable(binary))
    {
      return Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
          ServiceClassPath: "TorchrunLauncher",
          Detail: $"torchrun binary not found at '{binary}'. Install torch (which ships torchrun) "
            + "via `uv add torch` or set TorchrunLauncher.BinaryPath."
        ))
      );
    }

    var gpuCount = TryProbeGpuCount();
    if (gpuCount is int n && NProcPerNode > n)
    {
      return Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
          ServiceClassPath: "TorchrunLauncher",
          Detail: $"NProcPerNode={NProcPerNode} exceeds available GPU count ({n}) reported by nvidia-smi."
        ))
      );
    }

    return Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);
  }

  /// <summary>
  /// Compute the <c>--redirects</c> argument from <see cref="NProcPerNode"/>
  /// and <see cref="RedirectsFlag"/>. Empty string disables the flag;
  /// null falls back to the auto-default (redirect ranks 1..N-1 to
  /// per-rank log files, leave rank 0 connected to the parent for the
  /// protocol stream).
  /// </summary>
  private string? ResolveRedirectsFlag()
  {
    if (RedirectsFlag is not null) return RedirectsFlag;
    if (NProcPerNode <= 1) return null;

    // `i:3` redirects rank i's stdout (1) + stderr (2) bits to a
    // per-rank log file. We leave rank 0 unredirected so its stdout
    // remains the parent's protocol channel.
    var clauses = Enumerable.Range(1, NProcPerNode - 1).Select(i => $"{i}:3");
    return string.Join(",", clauses);
  }

  private static bool IsBinaryAvailable(string path)
  {
    // Absolute paths just need to exist.
    if (Path.IsPathRooted(path))
    {
      return File.Exists(path);
    }

    // Bare name → PATH lookup. Use `which` (Unix) / `where` (Windows)
    // via a short-lived subprocess; cheaper than re-implementing PATH
    // traversal manually.
    var lookup = OperatingSystem.IsWindows() ? "where" : "which";
    try
    {
      var psi = new ProcessStartInfo
      {
        FileName = lookup,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
      };
      psi.ArgumentList.Add(path);
      using var proc = Process.Start(psi);
      if (proc is null) return false;
      if (!proc.WaitForExit(TimeSpan.FromSeconds(2))) return false;
      return proc.ExitCode == 0;
    }
    catch
    {
      return false;
    }
  }

  private static int? TryProbeGpuCount()
  {
    try
    {
      var psi = new ProcessStartInfo
      {
        FileName = "nvidia-smi",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
      };
      psi.ArgumentList.Add("-L");
      using var proc = Process.Start(psi);
      if (proc is null) return null;
      if (!proc.WaitForExit(TimeSpan.FromSeconds(5))) return null;
      if (proc.ExitCode != 0) return null;
      var output = proc.StandardOutput.ReadToEnd();
      // One GPU per non-blank line — "GPU 0: NVIDIA A100..." etc.
      return output
        .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
        .Count(l => l.StartsWith("GPU ", StringComparison.Ordinal));
    }
    catch
    {
      return null;
    }
  }
}
