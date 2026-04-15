using System.Collections.Concurrent;

namespace Flowthru.Extensions.Python.Runtime;

/// <summary>
/// Controls how Python step execution is isolated between FlowthruService instances.
/// </summary>
public enum PythonExecutionMode
{
    /// <summary>
    /// Executes Python steps in the same process via Python.NET.
    /// Fast (no IPC overhead), but all services share one Python interpreter,
    /// <c>sys.modules</c>, and GIL. Use when co-hosted flows are known to be compatible.
    /// </summary>
    InProcess,

    /// <summary>
    /// Executes Python steps in an isolated child process per FlowthruService.
    /// Each service gets its own Python interpreter, venv, <c>sys.path</c>, and module cache.
    /// Default for multi-service scenarios.
    /// </summary>
    Subprocess,
}

/// <summary>
/// Configuration options for the Python runtime.
/// </summary>
/// <remarks>
/// <para>
/// Follows the .NET Options pattern for environment-specific configuration.
/// Resolution order mirrors <see cref="Flowthru.Core.Configuration.FlowthruConfigurationOptions"/>.
/// </para>
/// <para>
/// <strong>Developer workflow:</strong>
/// Run <c>uv sync</c> in your project directory during development to create <c>.venv/</c>.
/// During build, <c>pyproject.toml</c>, <c>uv.lock</c>, and <c>.python-version</c> are copied
/// to the output directory. On first run, the application automatically executes <c>uv sync --frozen</c>
/// in the output directory to materialize <c>.venv/</c> in-place.
/// </para>
/// <para>
/// <strong>Auto-detection hierarchy:</strong>
/// <list type="number">
/// <item>Explicit value set via <c>UsePython(opts => opts.PythonDll = "...")</c></item>
/// <item>Environment variable (<c>PYTHONNET_PYDLL</c> for containers/CI)</item>
/// <item>Explicit <c>VenvPath</c> override</item>
/// <item>Auto-initialization via <c>uv sync --frozen</c> in output directory</item>
/// <item>Fallback to <c>VIRTUAL_ENV</c> if set (compatibility with <c>uv run</c>)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class PythonRuntimeOptions
{
    /// <summary>
    /// Path to the Python shared library (e.g., libpython3.12.so, python312.dll).
    /// </summary>
    /// <remarks>
    /// <para>
    /// If not set, resolved in order:
    /// <list type="number">
    /// <item><c>PYTHONNET_PYDLL</c> environment variable (explicit override)</item>
    /// <item>Explicit <c>VenvPath</c> override</item>
    /// <item>Auto-materialized <c>.venv/</c> via <c>uv sync --frozen</c> in output directory</item>
    /// <item><c>VIRTUAL_ENV</c> environment variable (compatibility with <c>uv run</c>)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Container deployments typically set <c>PYTHONNET_PYDLL</c> to point to system Python.
    /// Local development and deployables use <c>uv sync</c> to create <c>.venv/</c> in-place.
    /// </para>
    /// </remarks>
    public string? PythonDll { get; set; }

    /// <summary>
    /// Path to the Python virtual environment (e.g., <c>.venv/</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// If not set, resolved in order:
    /// <list type="number">
    /// <item>Auto-materialized via <c>uv sync --frozen</c> in output directory</item>
    /// <item><c>VIRTUAL_ENV</c> environment variable</item>
    /// <item>None (uses system Python packages)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Setting this property explicitly skips <c>uv sync</c> auto-initialization.
    /// Useful for pre-built containers or custom venv management.
    /// </para>
    /// </remarks>
    public string? VenvPath { get; set; }

    /// <summary>
    /// Path to the <c>uv</c> executable for virtual environment initialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <c>"uv"</c> (PATH lookup).
    /// Set this to an absolute path for non-standard installations.
    /// </para>
    /// <para>
    /// Used by auto-initialization when <c>pyproject.toml</c> and <c>uv.lock</c> exist in
    /// the output directory. To disable auto-initialization entirely, set <c>VenvPath</c>
    /// explicitly or set <c>PYTHONNET_PYDLL</c> to point to system Python.
    /// </para>
    /// </remarks>
    public string UvPath { get; set; } = "uv";

    /// <summary>
    /// Controls whether Python steps run in the same process or an isolated child process.
    /// Defaults to <see cref="PythonExecutionMode.Subprocess"/> for per-service isolation.
    /// Set to <see cref="PythonExecutionMode.InProcess"/> to opt in to shared-interpreter mode.
    /// </summary>
    public PythonExecutionMode ExecutionMode { get; set; } = PythonExecutionMode.Subprocess;

    /// <summary>
    /// Directories to add to Python's <c>sys.path</c> for module resolution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If empty, resolved in order:
    /// <list type="number">
    /// <item><c>FLOWTHRU_PYTHON_PATH</c> environment variable (colon/semicolon-separated)</item>
    /// <item>Project root (directory containing <c>.csproj</c>)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Python steps at <c>Flows/DataScience/Steps/train_model.py</c> are referenced as
    /// <c>"Flows.DataScience.Steps.train_model"</c> when the project root is in <c>sys.path</c>.
    /// </para>
    /// </remarks>
    public List<string> ModuleSearchPaths { get; set; } = new();

    /// <summary>
    /// Gets the resolved Python DLL path using the auto-detection hierarchy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Flowthru uses <c>uv</c> to manage Python environments. <c>pyproject.toml</c>, <c>uv.lock</c>,
    /// and <c>.python-version</c> are copied to the output directory during build. On first run,
    /// the runtime executes <c>uv sync --frozen</c> to materialize <c>.venv/</c> in-place.
    /// </para>
    /// <para>
    /// Attempts resolution in order: explicit value → <c>PYTHONNET_PYDLL</c> → explicit <c>VenvPath</c> →
    /// <c>uv sync</c> auto-init → <c>VIRTUAL_ENV</c>.
    /// Throws <see cref="InvalidOperationException"/> if no Python runtime is found.
    /// </para>
    /// </remarks>
    public string GetResolvedPythonDll()
    {
        // 1. Explicit value (intentional override via UsePython configuration)
        if (!string.IsNullOrWhiteSpace(PythonDll))
        {
            return PythonDll;
        }

        // 2. PYTHONNET_PYDLL environment variable (explicit override for containers/CI)
        var envDll = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
        if (!string.IsNullOrWhiteSpace(envDll))
        {
            return envDll;
        }

        // 3. Explicit VenvPath property (set programmatically, e.g., by test fixtures)
        //    Try as existing .venv first, then as project directory for uv sync
        if (!string.IsNullOrWhiteSpace(VenvPath))
        {
            // First try as existing .venv directory
            var venvDll = FindPythonDllInVenv(VenvPath);
            if (venvDll != null)
            {
                return venvDll;
            }

            // If not an existing .venv, treat as project directory and run uv sync
            var uvVenvPath = EnsureVenvViaUv(VenvPath);
            if (uvVenvPath != null)
            {
                venvDll = FindPythonDllInVenv(uvVenvPath);
                if (venvDll != null)
                {
                    return venvDll;
                }
            }
        }

        // 4. Auto-initialize .venv via uv sync in output directory
        //    The build copies pyproject.toml, uv.lock, and .python-version to output.
        //    If present, run `uv sync --frozen` to create .venv alongside the executable.
        var appBaseVenv = EnsureVenvViaUv(AppContext.BaseDirectory);
        if (appBaseVenv != null)
        {
            var venvDll = FindPythonDllInVenv(appBaseVenv);
            if (venvDll != null)
            {
                return venvDll;
            }
        }

        // 5. VIRTUAL_ENV (fallback for when uv run is used, compatibility only)
        var virtualEnv = Environment.GetEnvironmentVariable("VIRTUAL_ENV");
        if (!string.IsNullOrWhiteSpace(virtualEnv))
        {
            var venvDll = FindPythonDllInVenv(virtualEnv);
            if (venvDll != null)
            {
                return venvDll;
            }
        }

        throw new InvalidOperationException(
          "Python runtime not found. Ensure 'pyproject.toml' and 'uv.lock' exist in the output directory, "
            + "or set PYTHONNET_PYDLL environment variable explicitly."
        );
    }

    /// <summary>
    /// Gets the resolved virtual environment path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Checks in order:
    /// <list type="number">
    /// <item>Explicit <c>VenvPath</c> property</item>
    /// <item>Auto-materialized <c>.venv/</c> via <c>uv sync</c> in output directory</item>
    /// <item><c>VIRTUAL_ENV</c> environment variable</item>
    /// </list>
    /// </para>
    /// <para>
    /// Returns <c>null</c> if no virtual environment is configured.
    /// </para>
    /// </remarks>
    public string? GetResolvedVenvPath()
    {
        // 1. Explicit value
        if (!string.IsNullOrWhiteSpace(VenvPath))
        {
            return VenvPath;
        }

        // 2. Check for .venv in output directory (may have been created by GetResolvedPythonDll)
        var outputVenv = Path.Combine(AppContext.BaseDirectory, ".venv");
        if (Directory.Exists(outputVenv) && File.Exists(Path.Combine(outputVenv, "pyvenv.cfg")))
        {
            return outputVenv;
        }

        // 3. VIRTUAL_ENV environment variable (set by uv run or manual activation)
        var envVenv = Environment.GetEnvironmentVariable("VIRTUAL_ENV");
        if (!string.IsNullOrWhiteSpace(envVenv))
        {
            return envVenv;
        }

        return null;
    }

    /// <summary>
    /// Gets the Python executable path for subprocess execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resolves in the same order as <see cref="GetResolvedPythonDll"/> but returns the interpreter
    /// binary rather than the shared library. Used by <c>SubprocessPythonExecutor</c> to spawn
    /// the worker process.
    /// </para>
    /// <para>
    /// Falls back to <c>python3</c> (Unix) or <c>python</c> (Windows) on PATH if no venv is found.
    /// </para>
    /// </remarks>
    public string GetResolvedPythonExe()
    {
        // 1. Explicit VenvPath — try as existing .venv, then as project dir for uv sync
        if (!string.IsNullOrWhiteSpace(VenvPath))
        {
            var exe = FindPythonExeInVenv(VenvPath);
            if (exe != null)
            {
                return exe;
            }

            var uvVenvPath = EnsureVenvViaUv(VenvPath);
            if (uvVenvPath != null)
            {
                exe = FindPythonExeInVenv(uvVenvPath);
                if (exe != null)
                {
                    return exe;
                }
            }
        }

        // 2. Auto-init .venv via uv sync in output directory (same trigger as GetResolvedPythonDll)
        var appBaseVenv = EnsureVenvViaUv(AppContext.BaseDirectory);
        if (appBaseVenv != null)
        {
            var exe = FindPythonExeInVenv(appBaseVenv);
            if (exe != null)
            {
                return exe;
            }
        }

        // 3. VIRTUAL_ENV environment variable
        var virtualEnv = Environment.GetEnvironmentVariable("VIRTUAL_ENV");
        if (!string.IsNullOrWhiteSpace(virtualEnv))
        {
            var exe = FindPythonExeInVenv(virtualEnv);
            if (exe != null)
            {
                return exe;
            }
        }

        // Fallback: system interpreter on PATH
        return OperatingSystem.IsWindows() ? "python" : "python3";
    }

    private static string? FindPythonExeInVenv(string venvPath)
    {
        var exe = OperatingSystem.IsWindows()
          ? Path.Combine(venvPath, "Scripts", "python.exe")
          : Path.Combine(venvPath, "bin", "python");
        return File.Exists(exe) ? exe : null;
    }

    /// <summary>
    /// Gets the resolved Python module search paths.
    /// </summary>
    /// <remarks>
    /// Returns configured module search paths, or the executing assembly's base directory if none specified.
    /// Python automatically includes site-packages from <c>VIRTUAL_ENV</c> when set.
    /// </remarks>
    public List<string> GetResolvedModuleSearchPaths()
    {
        if (ModuleSearchPaths.Count > 0)
        {
            return ModuleSearchPaths;
        }

        // Python automatically adds sys.path entries from VIRTUAL_ENV
        // We only need to return project-specific search paths
        return new List<string> { AppContext.BaseDirectory };
    }

    private static string? FindPythonDllInVenv(string venvPath)
    {
        // Parse pyvenv.cfg to find Python home directory
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

        // Construct expected library name based on platform and version
        var versionParts = version.Split('.');
        if (versionParts.Length < 2)
        {
            return null;
        }

        var majorMinor = $"{versionParts[0]}.{versionParts[1]}";

        if (OperatingSystem.IsWindows())
        {
            // Windows: python3XX.dll in home directory
            var dllName = $"python{versionParts[0]}{versionParts[1]}.dll";
            var dllPath = Path.Combine(homeDir, dllName);
            return File.Exists(dllPath) ? dllPath : null;
        }
        else if (OperatingSystem.IsMacOS())
        {
            // macOS: Check framework structure first, then lib directory
            // Framework: /path/to/Python.framework/Versions/3.X/Python
            var frameworkPath = Path.Combine(homeDir, "..", "Python");
            if (File.Exists(frameworkPath))
            {
                return Path.GetFullPath(frameworkPath);
            }

            // Fallback: libpython3.X.dylib in ../lib
            var libPath = Path.Combine(homeDir, "..", "lib", $"libpython{majorMinor}.dylib");
            return File.Exists(libPath) ? Path.GetFullPath(libPath) : null;
        }
        else
        {
            // Linux: libpython3.X.so in ../lib or ../lib64
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

    /// <summary>
    /// Ensures a virtual environment is materialized in the specified directory using <c>uv sync</c>.
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _uvSyncLocks =
      new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// If the specified directory contains a valid <c>pyproject.toml</c> and <c>uv.lock</c>, ensures that
    /// a virtual environment is materialized using <c>uv sync</c>.
    /// </summary>
    /// <param name="directory">The directory containing the Python project.</param>
    /// <returns>The path to the virtual environment if successful, or <c>null</c> if not.</returns>
    private string? EnsureVenvViaUv(string directory)
    {
        var pyprojectPath = Path.Combine(directory, "pyproject.toml");
        var uvLockPath = Path.Combine(directory, "uv.lock");
        var venvPath = Path.Combine(directory, ".venv");
        var pyvenvCfg = Path.Combine(venvPath, "pyvenv.cfg");

        // Check if uv manifest files exist
        if (!File.Exists(pyprojectPath) || !File.Exists(uvLockPath))
        {
            return null; // Not a uv-managed project
        }

        // Fast path: venv already exists and is valid — no lock needed.
        if (File.Exists(pyvenvCfg))
        {
            return venvPath;
        }

        // Slow path: venv is missing or corrupt. One caller creates it; others wait and reuse.
        var semaphore = _uvSyncLocks.GetOrAdd(directory, _ => new SemaphoreSlim(1, 1));
        semaphore.Wait();
        try
        {
            // Re-check under the lock — another thread may have created it while we were waiting.
            if (File.Exists(pyvenvCfg))
            {
                return venvPath;
            }

            // If .venv exists but lacks pyvenv.cfg, it's corrupt — delete and recreate
            if (Directory.Exists(venvPath))
            {
                try
                {
                    Directory.Delete(venvPath, recursive: true);
                }
                catch
                {
                    // Deletion failed (permissions, locked files, etc.) — let uv sync handle it
                }
            }

            // Run uv sync to materialize .venv
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = UvPath,
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
                    return null; // Failed to start process
                }

                process.WaitForExit();

                // Check if .venv was created successfully
                if (process.ExitCode == 0 && File.Exists(pyvenvCfg))
                {
                    return venvPath;
                }

                // Sync failed — log stderr if available for diagnostics
                var stderr = process.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    Console.Error.WriteLine($"uv sync failed: {stderr}");
                }
                return null;
            }
            catch
            {
                // uv not found, permission denied, etc. — fall through to other resolution methods
                return null;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
}
