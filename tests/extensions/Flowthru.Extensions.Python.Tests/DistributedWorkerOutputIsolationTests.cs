using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Regression coverage for the distributed-worker output contract:
/// when a fan-out launcher (torchrun, accelerate, …) runs N workers,
/// only rank 0 may write the step's result to the shared transit
/// directory. Non-rank-0 workers still execute the step body — every
/// rank must participate in collective coordination (DDP grad sync,
/// barriers) — but they must NOT encode their return value, because
/// the broadcast protocol hands every rank the same invoke message and
/// therefore the same <c>transit_dir</c>. A non-rank-0 <c>_encode</c>
/// <c>open(..., "wb")</c>s the same path rank 0 wrote and truncates it,
/// clobbering the real result with that rank's return (typically
/// <c>b""</c>) — surfacing one step later as a corrupt / empty input
/// (e.g. <c>pickle.loads(b"") → EOFError</c>).
/// </summary>
/// <remarks>
/// <para>
/// Deterministic and torch-free: the worker's non-rank-0 path
/// (<c>_main_non_rank_zero_distributed</c>) reads its protocol messages
/// from the broadcast session dir rather than stdin, so we pre-populate
/// that dir with the init / invoke / shutdown messages rank 0 would
/// have published and run the real worker as rank 1. No torchrun, no
/// DDP, no race window — we assert the *invariant* (non-rank-0 leaves
/// the transit output untouched) directly.
/// </para>
/// <para>
/// Gated by <c>Category("RequiresPython")</c> like the sibling
/// integration fixture: needs a real <c>python3</c> + the copied
/// <c>flowthru_worker.py</c>. Missing prerequisites surface as
/// <c>Assert.Ignore</c>, never a failure.
/// </para>
/// </remarks>
[TestFixture]
[Category("Python")]
[Category("RequiresPython")]
public class DistributedWorkerOutputIsolationTests
{
  // The non-rank-0 return value the buggy worker would write over rank
  // 0's real output. Non-empty on purpose: makes "file must not exist"
  // a sharp assertion (pre-fix the file exists with these bytes), and
  // doubles as the single-rank control's expected payload.
  private const string FixturePayload = "rank-1-must-not-write-this-to-the-shared-transit-file";
  private static readonly byte[] FixtureBytes = Encoding.ASCII.GetBytes(FixturePayload);

  private string _python = string.Empty;
  private readonly List<string> _tempDirs = new();

  [OneTimeSetUp]
  public void ResolvePython()
  {
    if (OperatingSystem.IsWindows())
    {
      Assert.Ignore("Worker isolation tests rely on POSIX tmpdir + /dev/null semantics.");
    }

    if (!File.Exists(PythonHermeticVenv.WorkerScriptPath))
    {
      Assert.Ignore(
        $"flowthru_worker.py not found at '{PythonHermeticVenv.WorkerScriptPath}'. "
        + "Check the test csproj's <Content> wiring."
      );
    }

    _python = ResolvePython3();
    if (string.IsNullOrEmpty(_python))
    {
      Assert.Ignore("python3 not found on PATH — the worker's non-rank-0 path needs only stdlib.");
    }
  }

  [OneTimeTearDown]
  public void Cleanup()
  {
    foreach (var dir in _tempDirs)
    {
      PythonHermeticVenv.TryDelete(dir);
    }
  }

  // ── The regression ────────────────────────────────────────────────────

  [Test]
  public void NonRankZeroWorker_RunsStepBody_ButDoesNotWriteTransitOutput()
  {
    var workspace = NewTempDir("flowthru-dist-iso");
    var tmpDir = CreateSubdir(workspace, "tmp");
    var moduleDir = CreateSubdir(workspace, "modules");
    var transitDir = CreateSubdir(workspace, "invoke-0001");
    var sentinel = Path.Combine(workspace, "rank1.ran");
    WriteFixtureModule(moduleDir);

    // MASTER_ADDR/PORT/RUN_ID drive the broadcast session dir the
    // worker derives via _broadcast_session_dir(); pinning TMPDIR makes
    // tempfile.gettempdir() resolve to a dir we control so we can
    // pre-seed the same path the worker will poll.
    const string addr = "127.0.0.1";
    const string port = "29555";
    const string runId = "isolation-test-run";
    var bcastDir = CreateSubdir(tmpDir, $"flowthru-bcast-{addr}-{port}-{runId}");

    // The sequenced messages rank 0 would have published.
    File.WriteAllText(Path.Combine(bcastDir, "0001.json"), InitMessage(moduleDir).ToJsonString());
    File.WriteAllText(Path.Combine(bcastDir, "0002.json"), InvokeMessage(transitDir).ToJsonString());
    File.WriteAllText(Path.Combine(bcastDir, "0003.json"), ShutdownMessage().ToJsonString());

    var env = BaseEnv(tmpDir, sentinel);
    env["RANK"] = "1";
    env["WORLD_SIZE"] = "2";
    env["MASTER_ADDR"] = addr;
    env["MASTER_PORT"] = port;
    env["TORCHELASTIC_RUN_ID"] = runId;

    var (exitCode, _, stderr) = RunWorker(env, stdinPayload: null);

    var outputFile = Path.Combine(transitDir, "output.bin");
    Assert.Multiple(() =>
    {
      Assert.That(exitCode, Is.EqualTo(0), $"worker exited non-zero.\nstderr:\n{stderr}");
      Assert.That(File.Exists(sentinel), Is.True,
        "non-rank-0 step body must still execute for collective coordination — sentinel missing.");
      Assert.That(File.Exists(outputFile), Is.False,
        "non-rank-0 worker must NOT write the shared transit output file — it races rank 0's real result.");
    });
  }

  // ── Positive control ──────────────────────────────────────────────────
  // Guards against a false pass: proves the same fixture *does* produce
  // a transit output file when encoding isn't suppressed, so the
  // regression's "file absent" assertion is meaningful rather than
  // trivially true.

  [Test]
  public void SingleRankWorker_WritesTransitOutput()
  {
    var workspace = NewTempDir("flowthru-dist-iso-control");
    var tmpDir = CreateSubdir(workspace, "tmp");
    var moduleDir = CreateSubdir(workspace, "modules");
    var transitDir = CreateSubdir(workspace, "invoke-0001");
    var sentinel = Path.Combine(workspace, "single.ran");
    WriteFixtureModule(moduleDir);

    // Single-rank path speaks the stdin protocol: init → ready, then
    // invoke → ok, then shutdown.
    var stdin = new StringBuilder()
      .Append(InitMessage(moduleDir).ToJsonString()).Append('\n')
      .Append(InvokeMessage(transitDir).ToJsonString()).Append('\n')
      .Append(ShutdownMessage().ToJsonString()).Append('\n')
      .ToString();

    var env = BaseEnv(tmpDir, sentinel); // no RANK/WORLD_SIZE → single-rank mode

    var (exitCode, _, stderr) = RunWorker(env, stdin);

    var outputFile = Path.Combine(transitDir, "output.bin");
    Assert.Multiple(() =>
    {
      Assert.That(exitCode, Is.EqualTo(0), $"worker exited non-zero.\nstderr:\n{stderr}");
      Assert.That(File.Exists(sentinel), Is.True, "single-rank step body must execute.");
      Assert.That(File.Exists(outputFile), Is.True, "single-rank worker must write its output normally.");
      Assert.That(File.ReadAllBytes(outputFile), Is.EqualTo(FixtureBytes));
    });
  }

  // ── Protocol message builders ───────────────────────────────────────

  private static JsonObject InitMessage(string moduleDir) => new()
  {
    ["type"] = "init",
    ["sys_path"] = new JsonArray(moduleDir),
  };

  private static JsonObject InvokeMessage(string transitDir) => new()
  {
    ["type"] = "invoke",
    ["module"] = "regression_step",
    ["function"] = "make_bytes",
    ["input_type"] = "scalar",
    ["input"] = "null", // json.loads("null") → None; the step ignores its arg
    ["output_type"] = "bytes",
    ["transit_dir"] = transitDir,
  };

  private static JsonObject ShutdownMessage() => new() { ["type"] = "shutdown" };

  // ── Fixture / process plumbing ──────────────────────────────────────

  /// <summary>
  /// Plain stdlib step module — no <c>flowthru</c> import (invoke
  /// dispatch only needs a callable). Writes a sentinel as a side
  /// effect so the test can prove the body ran on this rank, and
  /// returns non-empty bytes so a stray encode would leave a
  /// detectable file.
  /// </summary>
  private static void WriteFixtureModule(string moduleDir) =>
    File.WriteAllText(Path.Combine(moduleDir, "regression_step.py"),
      $"""
      import os

      FIXTURE_BYTES = {ToPythonByteLiteral(FixturePayload)}


      def make_bytes(_payload):
          sentinel = os.environ.get("FLOWTHRU_TEST_SENTINEL")
          if sentinel:
              with open(sentinel, "w") as f:
                  f.write("ran")
          return FIXTURE_BYTES
      """);

  private static string ToPythonByteLiteral(string ascii) => $"b\"{ascii}\"";

  private Dictionary<string, string> BaseEnv(string tmpDir, string sentinel) => new()
  {
    // tempfile.gettempdir() consults TMPDIR/TEMP/TMP in order — pin all
    // three so the worker's broadcast dir lands where we seeded it.
    ["TMPDIR"] = tmpDir,
    ["TEMP"] = tmpDir,
    ["TMP"] = tmpDir,
    ["FLOWTHRU_TEST_SENTINEL"] = sentinel,
    ["PYTHONDONTWRITEBYTECODE"] = "1",
  };

  private (int exitCode, string stdout, string stderr) RunWorker(
    IReadOnlyDictionary<string, string> env,
    string? stdinPayload,
    int timeoutMs = 30_000)
  {
    var psi = new ProcessStartInfo
    {
      FileName = _python,
      UseShellExecute = false,
      RedirectStandardInput = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true,
    };
    psi.ArgumentList.Add(PythonHermeticVenv.WorkerScriptPath);
    foreach (var (key, value) in env)
    {
      psi.EnvironmentVariables[key] = value;
    }

    using var proc = Process.Start(psi)
      ?? throw new InvalidOperationException("Process.Start returned null for the worker.");

    if (stdinPayload is not null)
    {
      proc.StandardInput.Write(stdinPayload);
    }
    proc.StandardInput.Close();

    // Drain async so a full pipe buffer can't deadlock the wait.
    var stdoutTask = proc.StandardOutput.ReadToEndAsync();
    var stderrTask = proc.StandardError.ReadToEndAsync();

    if (!proc.WaitForExit(timeoutMs))
    {
      try { proc.Kill(entireProcessTree: true); } catch { /* best effort */ }
      Assert.Fail($"worker did not exit within {timeoutMs}ms.");
    }

    return (proc.ExitCode, stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult());
  }

  private string NewTempDir(string prefix)
  {
    var dir = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    _tempDirs.Add(dir);
    return dir;
  }

  private static string CreateSubdir(string parent, string name)
  {
    var dir = Path.Combine(parent, name);
    Directory.CreateDirectory(dir);
    return dir;
  }

  private static string ResolvePython3()
  {
    try
    {
      var psi = new ProcessStartInfo
      {
        FileName = "which",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
      };
      psi.ArgumentList.Add("python3");
      using var proc = Process.Start(psi);
      if (proc is null) return string.Empty;
      var path = proc.StandardOutput.ReadToEnd().Trim();
      if (!proc.WaitForExit(5_000)) return string.Empty;
      return proc.ExitCode == 0 && File.Exists(path) ? path : string.Empty;
    }
    catch
    {
      return string.Empty;
    }
  }
}
