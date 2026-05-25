using System.Diagnostics;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;

namespace Flowthru.Step.Python;

/// <summary>
/// HuggingFace Accelerate's distributed launcher — spawns the Python
/// worker via <c>accelerate launch</c>, which dispatches under the hood
/// to torchrun, deepspeed, MPI, or whichever backend the user's
/// <c>accelerate config</c> selects. The recommended distributed default
/// when a project is already using HuggingFace tooling because
/// Accelerate covers the launcher matrix HuggingFace maintains. Per
/// ADR-0014.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Slice 4 limitation — protocol interleaving.</strong> Same
/// stdout-multiplex caveat as <see cref="TorchrunLauncher"/>; non-rank-0
/// ranks share the parent's stdout pre-slice-5. If interleaving is
/// observed, configure redirection through the accelerate config file
/// pointed at by <see cref="ConfigFile"/>.
/// </para>
/// </remarks>
[PythonPackageRequirement(
  package: "accelerate",
  versionConstraint: ">=0.30",
  reason: "Required by AccelerateLauncher (HuggingFace meta-launcher entry point)"
)]
public sealed class AccelerateLauncher : IPythonLauncher
{
  /// <summary>
  /// Number of processes to launch. Maps to
  /// <c>--num_processes=N</c>. Null leaves the value unspecified and
  /// Accelerate falls back to its own auto-detection (typically the
  /// machine's GPU count from <c>nvidia-smi</c>).
  /// </summary>
  public int? NumProcesses { get; init; }

  /// <summary>
  /// Override the accelerate binary path. Default: the venv's
  /// <c>bin/accelerate</c> alongside the resolved <c>pyExe</c>. Set
  /// this for site-specific renames without subclassing.
  /// </summary>
  public string? BinaryPath { get; init; }

  /// <summary>
  /// Optional path to an Accelerate config YAML file
  /// (<c>--config_file</c>). When null, Accelerate uses
  /// <c>~/.cache/huggingface/accelerate/default_config.yaml</c> if
  /// present, otherwise its built-in defaults.
  /// </summary>
  public string? ConfigFile { get; init; }

  /// <inheritdoc/>
  /// <remarks>
  /// Identity carries both <see cref="NumProcesses"/> and
  /// <see cref="ConfigFile"/> so a launcher change invalidates the
  /// cache. Different process counts and different backend configs
  /// produce different execution paths and (especially with FSDP /
  /// DeepSpeed dispatch) different numerical outputs.
  /// </remarks>
  public string Identity =>
    $"AccelerateLauncher(num_processes={NumProcesses?.ToString() ?? "auto"},config={ConfigFile ?? "default"})";

  /// <inheritdoc/>
  /// <remarks>
  /// The runtime <c>Requirements</c> mirrors the
  /// <see cref="PythonPackageRequirementAttribute"/> on this class so
  /// the design-time analyzer (FTPY1501) and the pre-flight hook
  /// (FTPY3011) see the same declarations. Other launchers can choose
  /// to reflect attributes here for the same DRY guarantee — slice 4
  /// keeps it explicit for readability.
  /// </remarks>
  public IReadOnlyList<PythonPackageRequirement> Requirements { get; } = new[]
  {
    new PythonPackageRequirement(
      Package: "accelerate",
      VersionConstraint: ">=0.30",
      Reason: "Required by AccelerateLauncher (HuggingFace meta-launcher entry point)"
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
      "accelerate"
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

    // `accelerate launch` is the subcommand; everything after is the
    // launch-time args + the script.
    psi.ArgumentList.Add("launch");
    if (!string.IsNullOrWhiteSpace(ConfigFile))
    {
      psi.ArgumentList.Add($"--config_file={ConfigFile}");
    }
    if (NumProcesses is int n)
    {
      psi.ArgumentList.Add($"--num_processes={n}");
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
  /// Probe verifies the accelerate binary is reachable. Validating
  /// the <see cref="ConfigFile"/>'s contents (running
  /// <c>accelerate env</c>) is a heavier check deferred until a real
  /// case demonstrates the binary-presence check isn't enough.
  /// </remarks>
  public Validated<PreFlightError, FlowUnit> Probe()
  {
    var binary = BinaryPath;
    if (string.IsNullOrWhiteSpace(binary))
    {
      // No pyExe context here — assume PATH and let the launch
      // attempt surface a precise PreFlightError if accelerate isn't
      // installed. The FTPY3011 pre-flight check from the
      // requirements algebra already catches "accelerate not in venv"
      // — this probe is the binary-resolution backstop.
      binary = "accelerate";
    }

    if (!IsBinaryAvailable(binary))
    {
      return Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
          ServiceClassPath: "AccelerateLauncher",
          Detail: $"accelerate binary not found at '{binary}'. "
            + "Install via `uv add accelerate` or set AccelerateLauncher.BinaryPath."
        ))
      );
    }

    if (!string.IsNullOrWhiteSpace(ConfigFile) && !File.Exists(ConfigFile))
    {
      return Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
          ServiceClassPath: "AccelerateLauncher",
          Detail: $"Configured accelerate config file does not exist: '{ConfigFile}'."
        ))
      );
    }

    return Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);
  }

  private static bool IsBinaryAvailable(string path)
  {
    if (Path.IsPathRooted(path)) return File.Exists(path);

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
}
