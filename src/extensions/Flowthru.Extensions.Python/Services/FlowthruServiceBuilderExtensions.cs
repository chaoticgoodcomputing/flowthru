using Flowthru.Core.Effects;
using Flowthru.Core.Graph.Validation;
using Flowthru.Core.Services;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Flowthru.Extensions.Python.Services;

/// <summary>
/// Extension methods for integrating Python support with <see cref="IFlowthruBuilder"/>.
/// </summary>
public static class FlowthruServiceBuilderExtensions
{
  /// <summary>
  /// Registers the Python runtime with configuration bound from <c>Flowthru:Python</c>.
  /// </summary>
  /// <param name="builder">The Flowthru service builder.</param>
  /// <returns>The builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Platform defaults are applied after configuration binding:
  /// <list type="bullet">
  /// <item>Python DLL: <c>PYTHONNET_PYDLL</c> → <c>.venv/</c> via <c>uv sync</c> → <c>VIRTUAL_ENV</c></item>
  /// <item>Virtual environment: <c>.venv/</c> in output directory</item>
  /// <item>Module search paths: output directory</item>
  /// </list>
  /// </para>
  /// <para>
  /// <strong>Example (auto-detection):</strong>
  /// <code>
  /// services.AddFlowthru(configuration, flowthru =>
  /// {
  ///     flowthru
  ///         .RegisterCatalog&lt;MyCatalog&gt;()
  ///         .UsePython();
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  public static IFlowthruBuilder UsePython(this IFlowthruBuilder builder)
  {
    builder
      .Services.AddOptions<PythonRuntimeOptions>()
      .Configure<IConfiguration>((opts, cfg) => cfg.GetSection("Flowthru:Python").Bind(opts))
      .PostConfigure(opts =>
      {
        // PYTHONNET_PYDLL is a Python.NET convention that predates Flowthru's config
        // namespace. Read it as a platform default rather than via IConfiguration.
        if (string.IsNullOrWhiteSpace(opts.PythonDll))
        {
          var envDll = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
          if (!string.IsNullOrWhiteSpace(envDll))
          {
            opts.PythonDll = envDll;
          }
        }
      })
      .ValidateOnStart();

    if (builder.Services.Any(sd => sd.ServiceType == typeof(PythonRuntimeOptions)))
    {
      // Already registered (e.g., test double injected before UsePython)
      return builder;
    }

    // ── Cross-cutting Python-extension singletons ──────────────────────────
    // These exist regardless of execution mode and are consumed by both the
    // subprocess and in-process executor paths (the latter wired in a later
    // phase). Registered with TryAddSingleton semantics so test doubles or
    // user overrides registered earlier in the pipeline take precedence.
    builder.Services.TryAddSingleton<IPythonConfigurationFlattener, PythonConfigurationFlattener>();
    builder.Services.TryAddSingleton<IPythonServiceInspectorRegistry, PythonServiceInspectorRegistry>();

    // Plug into Core's preflight dispatch: when the loop encounters a
    // ServiceRef.Python, this dispatcher resolves the matching registration
    // and forwards to IPythonExecutor.InvokeInspector. Multiple dispatcher
    // implementations co-exist via the IEnumerable<IServiceRefDispatcher>
    // resolution, so this Add is additive — not exclusive.
    builder.Services.AddSingleton<IServiceRefDispatcher, PythonServiceRefDispatcher>();

    // Determine execution mode: peek at options to decide which executor to register.
    // We read the bound section directly here because the DI container hasn't been built yet.
    // Use nullable so a missing config section stays null rather than defaulting to the
    // zero-value enum member (InProcess), which would silently override the intended default.
    var executionMode = builder
      .Configuration.GetSection("Flowthru:Python:ExecutionMode")
      .Get<PythonExecutionMode?>();

    // Default to Subprocess — safe for multi-service scenarios.
    if (executionMode == PythonExecutionMode.InProcess)
    {
      if (!builder.Services.Any(sd => sd.ServiceType == typeof(PythonRuntime)))
      {
        builder.Services.AddSingleton<PythonRuntime>();
      }
      builder.Services.AddSingleton<IPythonExecutor, PythonNetExecutor>();
      builder.Services.AddSingleton<IFlowValidationHook, PythonStepValidator>();
    }
    else
    {
      builder.Services.AddSingleton<IPythonExecutor, SubprocessPythonExecutor>();
    }

    return builder;
  }

  /// <summary>
  /// Registers the Python runtime with code-first configuration overrides.
  /// </summary>
  /// <param name="builder">The Flowthru service builder.</param>
  /// <param name="configure">Action to override Python options after config-file binding.</param>
  /// <returns>The builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// The <paramref name="configure"/> callback runs after <c>Flowthru:Python</c> section
  /// binding and platform env-var defaults, so it can selectively override specific values.
  /// </para>
  /// <para>
  /// <strong>Example (explicit configuration):</strong>
  /// <code>
  /// services.AddFlowthru(configuration, flowthru =>
  /// {
  ///     flowthru
  ///         .RegisterCatalog&lt;MyCatalog&gt;()
  ///         .UsePython(python =>
  ///         {
  ///             python.PythonDll = "/usr/lib/x86_64-linux-gnu/libpython3.12.so";
  ///             python.ModuleSearchPaths.Add("Flows");
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
    builder.UsePython();
    builder.Services.PostConfigure(configure);
    return builder;
  }
}
