using System.Diagnostics;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Shared hermetic-venv provisioning for the Python extension's
/// integration test fixtures. Each fixture asks for its own
/// (tempdir, pyproject) tuple — the helper writes the
/// <c>pyproject.toml</c>, runs <c>uv lock</c> + <c>uv sync --frozen</c>,
/// and returns a <see cref="HermeticVenv"/> handle. Missing
/// dependencies (no <c>uv</c>, no network, no <c>flowthru_worker.py</c>
/// in <c>AppContext.BaseDirectory</c>) surface as
/// <c>Assert.Ignore</c> per the <see cref="TestCapability"/>-style
/// gating discipline in
/// <see href="/tests/extensions/CONTRIBUTING.md"/>.
/// </summary>
/// <remarks>
/// Lives in this test project (not <c>Flowthru.Tests.Kits</c>) because
/// the venv-provisioning shape is currently used only by Python — a
/// laws-kit promotion is only warranted if another extension needs
/// the same hermetic-Python pattern.
/// </remarks>
internal static class PythonHermeticVenv
{
  /// <summary>
  /// Path to the worker script as copied by the consumer csproj's
  /// <c>&lt;Content&gt;</c> entry. Fixtures use this to fail fast when
  /// build wiring has drifted (e.g., <c>PreserveNewest</c> dropped on
  /// the Python extension package).
  /// </summary>
  public static string WorkerScriptPath =>
    Path.Combine(AppContext.BaseDirectory, "flowthru_worker.py");

  /// <summary>
  /// Provision a hermetic <c>uv</c>-managed venv against the supplied
  /// <paramref name="dependencies"/>. The temp project dir lives under
  /// <see cref="Path.GetTempPath"/> with the <paramref name="tempDirPrefix"/>
  /// + a fresh GUID — fixture <c>OneTimeTearDown</c> is responsible
  /// for deleting it.
  /// </summary>
  /// <param name="tempDirPrefix">
  /// Short prefix for the temp directory name — keeps stray failed
  /// runs identifiable in <c>/tmp</c>. Use a stable per-fixture
  /// prefix (e.g., <c>"flowthru-pytest"</c>).
  /// </param>
  /// <param name="dependencies">
  /// Per-fixture <c>pyproject.toml</c> dependency list. Empty array
  /// is permitted (fixtures that only exercise stdlib paths can skip
  /// the heavy Arrow/pandas install).
  /// </param>
  public static HermeticVenv Provision(string tempDirPrefix, params string[] dependencies)
  {
    if (!File.Exists(WorkerScriptPath))
    {
      Assert.Ignore(
        $"flowthru_worker.py not found at '{WorkerScriptPath}'. "
        + "Check that the test csproj's <Content> items propagated to the output directory."
      );
    }

    var tempDir = Path.Combine(Path.GetTempPath(), $"{tempDirPrefix}-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempDir);

    var deps = string.Join(", ", dependencies.Select(d => $"\"{d}\""));
    File.WriteAllText(
      Path.Combine(tempDir, "pyproject.toml"),
      $"""
      [project]
      name = "{tempDirPrefix}"
      version = "0.0.1"
      description = "Probe project for SubprocessPythonExecutor tests"
      requires-python = ">=3.10"
      dependencies = [{deps}]
      """
    );

    if (!RunUv("lock", tempDir, out var lockErr))
    {
      Assert.Ignore($"`uv lock` failed: {lockErr}");
    }
    if (!RunUv("sync --frozen", tempDir, out var syncErr))
    {
      Assert.Ignore($"`uv sync --frozen` failed: {syncErr}");
    }

    var venvPath = Path.Combine(tempDir, ".venv");
    if (!Directory.Exists(venvPath))
    {
      Assert.Ignore($"venv was not created at '{venvPath}' after uv sync.");
    }

    return new HermeticVenv(tempDir, venvPath);
  }

  /// <summary>
  /// Best-effort delete of the temp project directory. Swallows
  /// failures — temp cleanup is advisory and a leaked dir doesn't
  /// affect correctness.
  /// </summary>
  public static void TryDelete(string? tempDir)
  {
    if (string.IsNullOrEmpty(tempDir) || !Directory.Exists(tempDir))
    {
      return;
    }
    try { Directory.Delete(tempDir, recursive: true); }
    catch { /* advisory */ }
  }

  /// <summary>
  /// Runs <c>uv</c> with the given args in the given directory and
  /// captures stderr for diagnostic surfacing. Returns true on
  /// exit-code 0. 120-second timeout — long enough for a cold
  /// <c>uv sync</c> over the network, short enough that a hung
  /// invocation surfaces in CI rather than hanging forever.
  /// </summary>
  private static bool RunUv(string args, string workingDirectory, out string stderr)
  {
    try
    {
      var psi = new ProcessStartInfo
      {
        FileName = "uv",
        Arguments = args,
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
      };
      using var process = Process.Start(psi);
      if (process == null) { stderr = "Process.Start returned null"; return false; }
      stderr = process.StandardError.ReadToEnd();
      _ = process.StandardOutput.ReadToEnd();
      process.WaitForExit(120_000);
      return process.HasExited && process.ExitCode == 0;
    }
    catch (Exception ex)
    {
      stderr = ex.Message;
      return false;
    }
  }
}

/// <summary>
/// Per-fixture handle to a provisioned hermetic venv. Fixtures pass
/// <see cref="VenvPath"/> and <see cref="TempProjectDir"/> into
/// <c>PythonRuntimeOptions</c> + <c>ModuleSearchPaths</c>.
/// </summary>
internal sealed record HermeticVenv(string TempProjectDir, string VenvPath);
