using Flowthru.Step.Python;

namespace Flowthru.Step.Python;

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
/// </remarks>
public sealed class PythonRuntimeOptions
{
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
  /// explicitly.
  /// </para>
  /// </remarks>
  public string UvPath { get; set; } = "uv";

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
  /// Name of the <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
  /// section to flatten into env vars at subprocess spawn. Empty (default)
  /// disables the bridge — no IConfiguration values are exported to Python.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Set to a top-level section name (e.g. <c>"Diarization"</c>) to enable
  /// the bridge. The flattener walks that section recursively, joining
  /// nested keys with .NET's native <c>__</c> separator. Python-side
  /// consumers — <c>flowthru.config</c>, <c>pydantic-settings</c>, plain
  /// <c>os.environ</c> — see the resulting env vars and re-nest as needed.
  /// </para>
  /// <para>
  /// Use a path like <c>"Flowthru:Python:Services"</c> to scope a deeper
  /// section instead of the project's root config.
  /// </para>
  /// </remarks>
  public string ConfigurationSection { get; set; } = string.Empty;

  /// <summary>
  /// Service ↔ sidecar-inspector registrations populated by
  /// <see cref="RegisterService(string, Action{PythonServiceBuilder})"/>.
  /// Consumed by <see cref="IPythonServiceInspectorRegistry"/> at
  /// preflight time. Internal — users should not mutate this directly.
  /// </summary>
  internal Dictionary<string, PythonServiceRegistration> ServiceRegistrations { get; } =
    new(StringComparer.Ordinal);

  /// <summary>
  /// Registers a Python service for preflight inspection via a separately-
  /// defined sidecar inspector module. Mirrors .NET's
  /// <c>AddFlowServiceInspector&lt;TService, TInspector&gt;()</c>: the service
  /// class is unmodified user code; the inspector is a Python module that
  /// exports an <c>inspect(svc)</c> function returning a
  /// <c>flowthru.ValidationResult</c>.
  /// </summary>
  /// <param name="serviceClassPath">
  /// Fully-qualified Python class path of the service — e.g.
  /// <c>"Services.PyannoteDiarizer"</c>. Must match the value emitted by
  /// the corresponding Python <c>@step(services=[...])</c> decorator.
  /// </param>
  /// <param name="configure">
  /// Builder lambda; must call
  /// <see cref="PythonServiceBuilder.WithInspector(string, string)"/> to
  /// declare the sidecar inspector module.
  /// </param>
  /// <returns>This options instance, for chaining.</returns>
  /// <example>
  /// <code>
  /// flowthru.UsePython(python =>
  /// {
  ///     python.ConfigurationSection = "Diarization";
  ///     python.RegisterService("Services.PyannoteDiarizer", svc => svc
  ///         .WithInspector("Services.pyannote_diarizer_inspector"));
  /// });
  /// </code>
  /// </example>
  public PythonRuntimeOptions RegisterService(
    string serviceClassPath,
    Action<PythonServiceBuilder> configure
  )
  {
    if (string.IsNullOrWhiteSpace(serviceClassPath))
    {
      throw new ArgumentException(
        "Service class path cannot be null or whitespace.",
        nameof(serviceClassPath)
      );
    }
    if (configure is null)
    {
      throw new ArgumentNullException(nameof(configure));
    }

    var builder = new PythonServiceBuilder(serviceClassPath);
    configure(builder);
    ServiceRegistrations[serviceClassPath] = builder.Build();
    return this;
  }
}
