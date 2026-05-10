using System.Collections.Concurrent;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Platform-detection utilities for locating the Python runtime and virtual environment.
/// </summary>
/// <remarks>
/// <para>
/// Separated from <see cref="PythonRuntimeOptions"/> so that options remains a plain data bag
/// while resolution logic (which involves filesystem I/O and process spawning) stays in
/// the runtime layer where it belongs.
/// </para>
/// <para>
/// All public methods are intentionally static — they operate on the values stored in
/// <see cref="PythonRuntimeOptions"/> and have no instance state of their own.
/// </para>
/// </remarks>
internal static class PythonEnvironmentResolver
{
  private static readonly ConcurrentDictionary<string, SemaphoreSlim> _uvSyncLocks =
    new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Resolves the Python executable path for subprocess execution.
  /// </summary>
  public static string ResolvePythonExe(PythonRuntimeOptions options)
  {
    if (!string.IsNullOrWhiteSpace(options.VenvPath))
    {
      var exe = FindPythonExeInVenv(options.VenvPath!);
      if (exe != null)
      {
        return exe;
      }

      var uvVenvPath = EnsureVenvViaUv(options.VenvPath!, options.UvPath);
      if (uvVenvPath != null)
      {
        exe = FindPythonExeInVenv(uvVenvPath);
        if (exe != null)
        {
          return exe;
        }
      }
    }

    var appBaseVenv = EnsureVenvViaUv(AppContext.BaseDirectory, options.UvPath);
    if (appBaseVenv != null)
    {
      var exe = FindPythonExeInVenv(appBaseVenv);
      if (exe != null)
      {
        return exe;
      }
    }

    var virtualEnv = Environment.GetEnvironmentVariable("VIRTUAL_ENV");
    if (!string.IsNullOrWhiteSpace(virtualEnv))
    {
      var exe = FindPythonExeInVenv(virtualEnv!);
      if (exe != null)
      {
        return exe;
      }
    }

    return OperatingSystem.IsWindows() ? "python" : "python3";
  }

  /// <summary>
  /// Resolves the module search paths from an options snapshot.
  /// </summary>
  public static List<string> ResolveModuleSearchPaths(PythonRuntimeOptions options)
  {
    if (options.ModuleSearchPaths.Count > 0)
    {
      return options.ModuleSearchPaths;
    }

    return new List<string> { AppContext.BaseDirectory };
  }

  // ── Internal helpers ─────────────────────────────────────────────────────────────────

  private static string? FindPythonExeInVenv(string venvPath)
  {
    var exe = OperatingSystem.IsWindows()
      ? Path.Combine(venvPath, "Scripts", "python.exe")
      : Path.Combine(venvPath, "bin", "python");
    return File.Exists(exe) ? exe : null;
  }

  internal static string? EnsureVenvViaUv(string directory, string uvPath = "uv")
  {
    var pyprojectPath = Path.Combine(directory, "pyproject.toml");
    var uvLockPath = Path.Combine(directory, "uv.lock");
    var venvPath = Path.Combine(directory, ".venv");
    var pyvenvCfg = Path.Combine(venvPath, "pyvenv.cfg");

    if (!File.Exists(pyprojectPath) || !File.Exists(uvLockPath))
    {
      return null;
    }

    if (File.Exists(pyvenvCfg))
    {
      return venvPath;
    }

    var semaphore = _uvSyncLocks.GetOrAdd(directory, _ => new SemaphoreSlim(1, 1));
    semaphore.Wait();
    try
    {
      if (File.Exists(pyvenvCfg))
      {
        return venvPath;
      }

      if (Directory.Exists(venvPath))
      {
        try
        {
          Directory.Delete(venvPath, recursive: true);
        }
        catch
        {
          // Deletion failed — let uv sync handle it
        }
      }

      try
      {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
          FileName = uvPath,
          Arguments = "sync --frozen --python-preference only-managed",
          WorkingDirectory = directory,
          UseShellExecute = false,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          CreateNoWindow = true,
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
        {
          return null;
        }

        process.WaitForExit();

        if (process.ExitCode == 0 && File.Exists(pyvenvCfg))
        {
          return venvPath;
        }

        var stderr = process.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(stderr))
        {
          Console.Error.WriteLine($"uv sync failed: {stderr}");
        }
        return null;
      }
      catch
      {
        return null;
      }
    }
    finally
    {
      semaphore.Release();
    }
  }
}
