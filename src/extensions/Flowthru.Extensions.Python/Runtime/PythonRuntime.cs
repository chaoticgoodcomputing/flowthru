using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Python.Runtime;
using PythonEngineRuntime = Python.Runtime.Runtime;

namespace Flowthru.Extensions.Python.Runtime;

/// <summary>
/// Manages the Python runtime lifecycle and GIL context.
/// </summary>
/// <remarks>
/// <para>
/// Wraps Python.NET's <see cref="PythonEngine"/> initialization and shutdown.
/// Registered as a singleton in DI — only one Python runtime per application.
/// </para>
/// <para>
/// Thread-safety: Python.NET manages GIL acquisition internally.
/// Use <see cref="AcquireGil"/> to ensure thread-safe access to Python objects.
/// </para>
/// </remarks>
public sealed class PythonRuntime : IDisposable
{
  /// <summary>
  /// Static lock to serialize PythonEngine.Initialize() calls across all PythonRuntime instances.
  /// PythonEngine is process-global; concurrent initialization from multiple threads causes crashes.
  /// </summary>
  private static readonly object _initializationLock = new();

  private readonly PythonRuntimeOptions _options;
  private readonly ILogger<PythonRuntime> _logger;
  private bool _initialized;
  private bool _disposed;

  /// <summary>
  /// Initializes a new instance of <see cref="PythonRuntime"/>.
  /// </summary>
  public PythonRuntime(IOptions<PythonRuntimeOptions> options, ILogger<PythonRuntime> logger)
  {
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <summary>
  /// Initializes the Python runtime if not already initialized.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Idempotent — safe to call multiple times.
  /// Applies configuration from <see cref="PythonRuntimeOptions"/> on first call.
  /// </para>
  /// <para>
  /// Sets:
  /// <list type="bullet">
  /// <item><c>PYTHONNET_PYDLL</c> from resolved DLL path</item>
  /// <item><c>PYTHONHOME</c> from resolved venv path (if applicable)</item>
  /// <item><c>sys.path</c> from resolved module search paths</item>
  /// </list>
  /// </para>
  /// </remarks>
  /// <exception cref="InvalidOperationException">
  /// Thrown if Python runtime cannot be initialized (missing DLL, ABI mismatch, etc.).
  /// </exception>
  public void Initialize()
  {
    if (_initialized)
    {
      return;
    }

    if (_disposed)
    {
      throw new ObjectDisposedException(nameof(PythonRuntime));
    }

    // Serialize PythonEngine initialization across all PythonRuntime instances
    // (PythonEngine is process-global and not thread-safe during initialization)
    lock (_initializationLock)
    {
      // Double-check after acquiring lock — another thread may have initialized while we waited
      if (_initialized)
      {
        return;
      }

      try
      {
        // ── Phase 1: Engine initialization (process-global, first-writer-wins) ──────────
        // PythonEngine.Initialize() is idempotent at the process level: whichever
        // FlowthruService calls this first locks in the Python DLL and home directory.
        // Subsequent instances skip engine init but MUST still configure sys.path below.

        bool engineAlreadyInitialized = false;

        var pythonDll = PythonEnvironmentResolver.ResolvePythonDll(_options);
        _logger.LogInformation("Resolved Python DLL path: {PythonDll}", pythonDll);

        if (!File.Exists(pythonDll))
        {
          throw new InvalidOperationException($"Python DLL not found at path: {pythonDll}");
        }

        try
        {
          PythonEngineRuntime.PythonDLL = pythonDll;
          _logger.LogInformation("Set Runtime.PythonDLL to: {PythonDll}", pythonDll);
        }
        catch (InvalidOperationException ex)
          when (ex.Message.Contains("must be set before runtime is initialized"))
        {
          // Another instance already initialized the engine — that's fine.
          // We still need to configure sys.path below.
          engineAlreadyInitialized = true;
          _logger.LogDebug("Python engine already initialized globally by another instance");
        }

        if (!engineAlreadyInitialized)
        {
          var pythonHome = Path.GetDirectoryName(Path.GetDirectoryName(pythonDll));
          if (!string.IsNullOrEmpty(pythonHome))
          {
            PythonEngine.PythonHome = pythonHome;
            _logger.LogInformation("Set PythonEngine.PythonHome to: {PythonHome}", pythonHome);
          }

          _logger.LogInformation("Calling PythonEngine.Initialize()...");
          try
          {
            PythonEngine.Initialize();
            _logger.LogInformation("Python runtime initialized successfully");
          }
          catch (ArgumentException ex) when (ex.Message.Contains("__name__"))
          {
            // Duplicate-key collision — engine was already initialized concurrently.
            engineAlreadyInitialized = true;
            _logger.LogDebug("Python engine was already initialized globally");
          }

          if (!engineAlreadyInitialized)
          {
            // Enable multi-threaded access once, after the first successful Initialize().
            PythonEngine.BeginAllowThreads();
          }
        }

        // ── Phase 2: sys.path configuration (per-instance) ───────────────────────────────
        // Each FlowthruService has its own PythonRuntimeOptions with its own module search
        // paths and venv site-packages. These are cumulative across instances — intentional,
        // because the GIL serialises all Python execution in this process anyway.
        // Different venv site-packages being on sys.path simultaneously is a documented
        // constraint (same-named packages resolve to whichever was imported first).

        var venvPath = PythonEnvironmentResolver.ResolveVenvPath(_options);
        if (venvPath != null)
        {
          _logger.LogInformation("Using Python virtual environment: {VenvPath}", venvPath);
        }

        var searchPaths = PythonEnvironmentResolver.ResolveModuleSearchPaths(_options);
        using (Py.GIL())
        {
          dynamic sys = Py.Import("sys");

          if (venvPath != null)
          {
            var sitePackagesPath = FindSitePackagesPath(venvPath);
            if (sitePackagesPath != null)
            {
              _logger.LogDebug("Adding venv site-packages: {Path}", sitePackagesPath);
              sys.path.insert(0, sitePackagesPath);
            }
          }

          foreach (var path in searchPaths)
          {
            var absPath = Path.GetFullPath(path);
            _logger.LogDebug("Adding module search path: {Path}", absPath);
            sys.path.insert(0, absPath);
          }
        }

        _initialized = true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to initialize Python runtime");
        throw new InvalidOperationException(
          "Failed to initialize Python runtime. See inner exception for details.",
          ex
        );
      }
    }
  }

  /// <summary>
  /// Acquires the Python GIL (Global Interpreter Lock).
  /// </summary>
  /// <remarks>
  /// <para>
  /// Use this to bracket any Python.NET interop code:
  /// <code>
  /// using (runtime.AcquireGil())
  /// {
  ///     dynamic module = Py.Import("my_module");
  ///     var result = module.my_function(42);
  /// }
  /// </code>
  /// </para>
  /// <para>
  /// Python.NET's GIL management is thread-safe — multiple threads can acquire/release
  /// the GIL, but only one thread executes Python code at a time.
  /// </para>
  /// </remarks>
  /// <returns>A disposable GIL token. Dispose to release the GIL.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if <see cref="Initialize"/> has not been called.
  /// </exception>
  public IDisposable AcquireGil()
  {
    if (!_initialized)
    {
      throw new InvalidOperationException(
        "Python runtime is not initialized. Call Initialize() first."
      );
    }

    if (_disposed)
    {
      throw new ObjectDisposedException(nameof(PythonRuntime));
    }

    return Py.GIL();
  }

  /// <summary>
  /// Disposes the Python runtime wrapper.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Does not call <c>PythonEngine.Shutdown()</c> — Python.NET initializes globally once per process,
  /// and explicit shutdown during process teardown is redundant. The OS reclaims all resources on exit.
  /// </para>
  /// <para>
  /// Additionally, <c>PythonEngine.Shutdown()</c> attempts to serialize runtime state using
  /// <c>BinaryFormatter</c>, which has been removed in .NET 10+ (see https://aka.ms/binaryformatter).
  /// Since the serialized state is never restored, shutdown is both unnecessary and incompatible.
  /// </para>
  /// <para>
  /// After disposal, <see cref="AcquireGil"/> will throw <see cref="ObjectDisposedException"/>.
  /// </para>
  /// </remarks>
  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    _disposed = true;
  }

  /// <summary>
  /// Finds the site-packages directory within a virtual environment.
  /// </summary>
  private static string? FindSitePackagesPath(string venvPath)
  {
    // Virtual environment structure:
    // .venv/lib/pythonX.Y/site-packages/ (Linux/macOS)
    // .venv/Lib/site-packages/ (Windows)

    var libPath = Path.Combine(venvPath, "lib");
    if (Directory.Exists(libPath))
    {
      // Look for python3.X subdirectory
      foreach (var pythonDir in Directory.GetDirectories(libPath, "python3.*"))
      {
        var sitePackages = Path.Combine(pythonDir, "site-packages");
        if (Directory.Exists(sitePackages))
        {
          return sitePackages;
        }
      }
    }

    // Windows: .venv/Lib/site-packages
    var windowsLibPath = Path.Combine(venvPath, "Lib", "site-packages");
    if (Directory.Exists(windowsLibPath))
    {
      return windowsLibPath;
    }

    return null;
  }
}
