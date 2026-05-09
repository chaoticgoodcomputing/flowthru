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
  /// Resolves the Python shared-library path from an options snapshot.
  /// </summary>
  /// <remarks>
  /// Resolution order:
  /// <list type="number">
  /// <item>Explicit <see cref="PythonRuntimeOptions.PythonDll"/> value (already set by PostConfigure)</item>
  /// <item>Explicit <see cref="PythonRuntimeOptions.VenvPath"/> — try as existing .venv, then as project dir for uv sync</item>
  /// <item>Auto-initialize <c>.venv/</c> via <c>uv sync</c> in <see cref="AppContext.BaseDirectory"/></item>
  /// <item><c>VIRTUAL_ENV</c> environment variable (compatibility with <c>uv run</c>)</item>
  /// </list>
  /// </remarks>
  public static string ResolvePythonDll(PythonRuntimeOptions options)
  {
    if (!string.IsNullOrWhiteSpace(options.PythonDll))
    {
      return options.PythonDll!;
    }

    if (!string.IsNullOrWhiteSpace(options.VenvPath))
    {
      var venvDll = FindPythonDllInVenv(options.VenvPath!);
      if (venvDll != null)
      {
        return venvDll;
      }

      var uvVenvPath = EnsureVenvViaUv(options.VenvPath!, options.UvPath);
      if (uvVenvPath != null)
      {
        venvDll = FindPythonDllInVenv(uvVenvPath);
        if (venvDll != null)
        {
          return venvDll;
        }
      }
    }

    var appBaseVenv = EnsureVenvViaUv(AppContext.BaseDirectory, options.UvPath);
    if (appBaseVenv != null)
    {
      var venvDll = FindPythonDllInVenv(appBaseVenv);
      if (venvDll != null)
      {
        return venvDll;
      }
    }

    var virtualEnv = Environment.GetEnvironmentVariable("VIRTUAL_ENV");
    if (!string.IsNullOrWhiteSpace(virtualEnv))
    {
      var venvDll = FindPythonDllInVenv(virtualEnv!);
      if (venvDll != null)
      {
        return venvDll;
      }
    }

    throw new InvalidOperationException(
      "Python runtime not found. Ensure 'pyproject.toml' and 'uv.lock' exist in the output directory, "
        + "or set the PYTHONNET_PYDLL environment variable, or configure PythonDll explicitly."
    );
  }

  /// <summary>
  /// Resolves the virtual environment path from an options snapshot.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="PythonRuntimeOptions.VenvPath"/> may point to the venv directory itself
  /// (contains <c>pyvenv.cfg</c>) OR to a project root directory that contains a
  /// <c>.venv/</c> subdirectory. Both forms are normalized to the actual venv directory.
  /// </para>
  /// </remarks>
  public static string? ResolveVenvPath(PythonRuntimeOptions options)
  {
    if (!string.IsNullOrWhiteSpace(options.VenvPath))
    {
      // Direct venv path: contains pyvenv.cfg
      if (File.Exists(Path.Combine(options.VenvPath!, "pyvenv.cfg")))
      {
        return options.VenvPath;
      }

      // Project root path: .venv/ subdirectory is the actual venv
      var subVenv = Path.Combine(options.VenvPath!, ".venv");
      if (File.Exists(Path.Combine(subVenv, "pyvenv.cfg")))
      {
        return subVenv;
      }
    }

    var outputVenv = Path.Combine(AppContext.BaseDirectory, ".venv");
    if (Directory.Exists(outputVenv) && File.Exists(Path.Combine(outputVenv, "pyvenv.cfg")))
    {
      return outputVenv;
    }

    var envVenv = Environment.GetEnvironmentVariable("VIRTUAL_ENV");
    if (!string.IsNullOrWhiteSpace(envVenv))
    {
      return envVenv;
    }

    return null;
  }

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

  internal static string? FindPythonDllInVenv(string venvPath)
  {
    var pyvenvCfg = Path.Combine(venvPath, "pyvenv.cfg");
    if (!File.Exists(pyvenvCfg))
    {
      return null;
    }

    string? homeDir = null;
    string? version = null;

    foreach (var line in File.ReadAllLines(pyvenvCfg))
    {
      var parts = line.Split('=', 2);
      if (parts.Length != 2)
      {
        continue;
      }

      var key = parts[0].Trim();
      var value = parts[1].Trim();

      if (key == "home")
      {
        homeDir = value;
      }
      else if (key == "version" || key == "version_info")
      {
        version = value;
      }
    }

    if (homeDir == null || version == null)
    {
      return null;
    }

    var versionParts = version.Split('.');
    if (versionParts.Length < 2)
    {
      return null;
    }

    var majorMinor = $"{versionParts[0]}.{versionParts[1]}";

    if (OperatingSystem.IsWindows())
    {
      var dllName = $"python{versionParts[0]}{versionParts[1]}.dll";
      var dllPath = Path.Combine(homeDir, dllName);
      return File.Exists(dllPath) ? dllPath : null;
    }
    else if (OperatingSystem.IsMacOS())
    {
      var frameworkPath = Path.Combine(homeDir, "..", "Python");
      if (File.Exists(frameworkPath))
      {
        return Path.GetFullPath(frameworkPath);
      }

      var libPath = Path.Combine(homeDir, "..", "lib", $"libpython{majorMinor}.dylib");
      return File.Exists(libPath) ? Path.GetFullPath(libPath) : null;
    }
    else
    {
      foreach (var libDir in new[] { "lib", "lib64" })
      {
        var libPath = Path.Combine(homeDir, "..", libDir, $"libpython{majorMinor}.so");
        if (File.Exists(libPath))
        {
          return Path.GetFullPath(libPath);
        }
      }
      return null;
    }
  }

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
