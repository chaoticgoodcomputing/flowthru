using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flowthru.Hosting;

/// <summary>
/// <c>UsePython()</c> extension methods on <see cref="IFlowthruBuilder"/>.
/// Registers Python step support — runtime options (bound from
/// <c>Flowthru:Python</c>), an <see cref="IPythonExecutor"/>
/// (subprocess by default; in-process opt-in via
/// <see cref="PythonExecutionMode"/>), the
/// <see cref="IPythonServiceInspectorRegistry"/>, the
/// <see cref="IServiceRefDispatcher"/> for
/// <see cref="Flowthru.Validation.Runtime.Python.PythonServiceRef"/>,
/// and the pre-flight <see cref="IFlowValidationHook"/> that audits
/// every Python step's decorator-vs-typed-shape agreement.
/// </summary>
public static class PythonFlowthruBuilderExtensions
{
  /// <summary>
  /// Register Python step support with configuration bound from the
  /// <c>Flowthru:Python</c> section. Platform defaults are applied
  /// after binding (e.g. <c>PYTHONNET_PYDLL</c> picked up from the
  /// process environment if present).
  /// </summary>
  /// <example>
  /// <code>
  /// services.AddFlowthru(configuration, b =>
  /// {
  ///   b.RegisterCatalog&lt;Catalog&gt;();
  ///   b.UsePython();  // subprocess executor by default
  /// });
  /// </code>
  /// </example>
  public static IFlowthruBuilder UsePython(this IFlowthruBuilder builder)
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));

    builder.Services
      .AddOptions<PythonRuntimeOptions>()
      .Configure<IConfiguration>((opts, cfg) => cfg.GetSection("Flowthru:Python").Bind(opts))
      .PostConfigure(opts =>
      {
        // PYTHONNET_PYDLL is a Python.NET convention that predates
        // Flowthru's config namespace — read it as a platform default
        // rather than via IConfiguration.
        if (string.IsNullOrWhiteSpace(opts.PythonDll))
        {
          var envDll = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
          if (!string.IsNullOrWhiteSpace(envDll)) opts.PythonDll = envDll;
        }
      })
      .ValidateOnStart();

    // Cross-cutting Python-extension singletons. TryAddSingleton
    // semantics let test doubles or user overrides registered
    // earlier in the pipeline take precedence.
    builder.Services.TryAddSingleton<IPythonConfigurationFlattener, PythonConfigurationFlattener>();
    builder.Services.TryAddSingleton<IPythonServiceInspectorRegistry, PythonServiceInspectorRegistry>();

    // PythonRuntime is needed only by the in-process executor, but
    // it's harmless to register unconditionally — initialization is
    // lazy on first use.
    builder.Services.TryAddSingleton<PythonRuntime>();

    // Executor: factory-resolved so the configured
    // PythonExecutionMode picks at runtime, not at registration time.
    // This lets the user register UsePython() unconditionally and
    // switch modes via configuration.
    builder.Services.TryAddSingleton<IPythonExecutor>(sp =>
    {
      var options = sp.GetRequiredService<IOptions<PythonRuntimeOptions>>().Value;
      return options.ExecutionMode switch
      {
        PythonExecutionMode.InProcess => new PythonNetExecutor(
          sp.GetRequiredService<PythonRuntime>(),
          sp.GetRequiredService<ILogger<PythonNetExecutor>>()
        ),
        _ => new SubprocessPythonExecutor(
          sp.GetRequiredService<IOptions<PythonRuntimeOptions>>(),
          sp.GetRequiredService<IPythonConfigurationFlattener>(),
          sp.GetRequiredService<ILogger<SubprocessPythonExecutor>>()
        ),
      };
    });

    // Service-ref dispatch: matches Category="python".
    builder.Services.AddSingleton<IServiceRefDispatcher, PythonServiceRefDispatcher>();

    // Pre-flight hook: the single authoritative site for
    // module-import / decorator-presence / schema-agreement / arity
    // checks against every PythonStep<,> in the registered flows.
    builder.Services.AddSingleton<IFlowValidationHook, PythonStepValidationHook>();

    return builder;
  }

  /// <summary>
  /// Register Python step support with code-first option overrides.
  /// The configure callback runs after the
  /// <c>Flowthru:Python</c> section binding and platform-default
  /// fallbacks, so it can selectively override individual values.
  /// </summary>
  /// <example>
  /// <code>
  /// b.UsePython(opts =>
  /// {
  ///   opts.ExecutionMode = PythonExecutionMode.InProcess;
  ///   opts.ModuleSearchPaths.Add("Flows");
  /// });
  /// </code>
  /// </example>
  public static IFlowthruBuilder UsePython(
    this IFlowthruBuilder builder,
    Action<PythonRuntimeOptions> configure
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (configure is null) throw new ArgumentNullException(nameof(configure));

    builder.UsePython();
    builder.Services.PostConfigure(configure);
    return builder;
  }
}
