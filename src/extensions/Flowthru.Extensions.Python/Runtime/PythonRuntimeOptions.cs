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
/// Resolution order: explicit value → environment variable → auto-detected default.
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
}
