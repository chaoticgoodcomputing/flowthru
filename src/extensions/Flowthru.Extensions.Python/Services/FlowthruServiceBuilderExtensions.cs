using Flowthru.Core.Graph.Validation;
using Flowthru.Core.Services;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Python.Services;

/// <summary>
/// Extension methods for integrating Python support with <see cref="FlowthruServiceBuilder"/>.
/// </summary>
public static class FlowthruServiceBuilderExtensions
{
  /// <summary>
  /// Registers Python runtime and executor with default configuration.
  /// </summary>
  /// <param name="builder">The Flowthru service builder</param>
  /// <returns>The builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Uses auto-detection for all configuration:
  /// <list type="bullet">
  /// <item>Python DLL: <c>PYTHONNET_PYDLL</c> → <c>.venv/</c> → system Python</item>
  /// <item>Virtual environment: <c>FLOWTHRU_PYTHON_VENV</c> → <c>.venv/</c> → none</item>
  /// <item>Module search paths: <c>FLOWTHRU_PYTHON_PATH</c> → project root</item>
  /// </list>
  /// </para>
  /// <para>
  /// <strong>Example (auto-detection):</strong>
  /// <code>
  /// services.AddFlowthru(flowthru =>
  /// {
  ///     flowthru
  ///         .RegisterCatalog&lt;MyCatalog&gt;()
  ///         .UsePython();  // Auto-detects .venv/, project root, etc.
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  public static IFlowthruBuilder UsePython(this IFlowthruBuilder builder)
  {
    return builder.UsePython(options => { });
  }

  /// <summary>
  /// Registers Python runtime and executor with custom configuration.
  /// </summary>
  /// <param name="builder">The Flowthru service builder</param>
  /// <param name="configure">Action to configure Python runtime options</param>
  /// <returns>The builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Explicit configuration overrides auto-detection.
  /// Use this for:
  /// <list type="bullet">
  /// <item>Container deployments with non-standard Python paths</item>
  /// <item>Custom module search paths</item>
  /// <item>Multiple Python versions (explicit DLL path)</item>
  /// </list>
  /// </para>
  /// <para>
  /// <strong>Example (explicit configuration):</strong>
  /// <code>
  /// services.AddFlowthru(flowthru =>
  /// {
  ///     flowthru
  ///         .RegisterCatalog&lt;MyCatalog&gt;()
  ///         .UsePython(python =>
  ///         {
  ///             python.PythonDll = "/usr/lib/x86_64-linux-gnu/libpython3.12.so";
  ///             python.ModuleSearchPaths.Add("Flows");
  ///             python.ModuleSearchPaths.Add("SharedSteps");
  ///         });
  /// });
  /// </code>
  /// </para>
  /// <para>
  /// <strong>Example (environment-variable driven, for containers):</strong>
  /// <code>
  /// services.AddFlowthru(flowthru =>
  /// {
  ///     flowthru
  ///         .RegisterCatalog&lt;MyCatalog&gt;()
  ///         .UsePython(python =>
  ///         {
  ///             // Reads PYTHONNET_PYDLL, FLOWTHRU_PYTHON_VENV, FLOWTHRU_PYTHON_PATH
  ///             // Auto-detection still active for unset properties
  ///         });
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  public static IFlowthruBuilder UsePython(
    this IFlowthruBuilder builder,
    Action<PythonRuntimeOptions> configure
  )
  {
    if (builder == null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    if (configure == null)
    {
      throw new ArgumentNullException(nameof(configure));
    }

    // Create and configure options
    var options = new PythonRuntimeOptions();
    configure(options);

    // Register options as singleton
    builder.Services.AddSingleton(options);

    if (options.ExecutionMode == PythonExecutionMode.InProcess)
    {
      // In-process mode: shared PythonEngine, GIL-guarded execution.
      // PythonRuntime may be pre-registered for testing.
      if (!builder.Services.Any(sd => sd.ServiceType == typeof(PythonRuntime)))
      {
        builder.Services.AddSingleton<PythonRuntime>();
      }
      builder.Services.AddSingleton<IPythonExecutor, PythonNetExecutor>();
      // Register pre-flight decorator + dry-run dtype validation (in-process only)
      builder.Services.AddSingleton<
        Flowthru.Core.Graph.Validation.IFlowValidationHook,
        PythonStepValidator
      >();
    }
    else
    {
      // Subprocess mode (default): each service has an isolated Python worker process.
      // No shared PythonEngine or GIL — isolation is at the OS process boundary.
      builder.Services.AddSingleton<IPythonExecutor, SubprocessPythonExecutor>();
    }

    return builder;
  }
}
