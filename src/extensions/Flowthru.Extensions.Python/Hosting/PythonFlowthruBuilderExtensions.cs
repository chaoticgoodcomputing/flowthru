using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowthru.Hosting;

/// <summary>
/// <c>UsePython()</c> extension methods on <see cref="IFlowthruBuilder"/>.
/// Registers Python step support — runtime options (bound from
/// <c>Flowthru:Python</c>), the subprocess <see cref="IPythonExecutor"/>,
/// the <see cref="IPythonServiceInspectorRegistry"/>, the
/// <see cref="IServiceRefDispatcher"/> for
/// <see cref="Flowthru.Validation.Runtime.Python.PythonServiceRef"/>,
/// and the pre-flight <see cref="IFlowValidationHook"/> that audits
/// every Python step's decorator-vs-typed-shape agreement.
/// </summary>
public static class PythonFlowthruBuilderExtensions
{
  /// <summary>
  /// Register Python step support with configuration bound from the
  /// <c>Flowthru:Python</c> section.
  /// </summary>
  /// <example>
  /// <code>
  /// services.AddFlowthru(configuration, b =>
  /// {
  ///   b.RegisterCatalog&lt;Catalog&gt;();
  ///   b.UsePython();
  /// });
  /// </code>
  /// </example>
  public static IFlowthruBuilder UsePython(this IFlowthruBuilder builder)
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));

    builder.Services
      .AddOptions<PythonRuntimeOptions>()
      .Configure<IConfiguration>((opts, cfg) => cfg.GetSection("Flowthru:Python").Bind(opts))
      .ValidateOnStart();

    // Cross-cutting Python-extension singletons. TryAddSingleton
    // semantics let test doubles or user overrides registered
    // earlier in the pipeline take precedence.
    builder.Services.TryAddSingleton<IPythonConfigurationFlattener, PythonConfigurationFlattener>();
    builder.Services.TryAddSingleton<IPythonServiceInspectorRegistry, PythonServiceInspectorRegistry>();
    // Shared "Flowthru"-category ILogger for the Python executor.
    // Mirrors AddFlowthru's fallback so UsePython() can
    // stand alone in tests that don't also call AddFlowthru. The
    // resolver lazily picks the host's ILoggerFactory if AddLogging
    // ran, otherwise falls back to NullLoggerFactory.Instance.
    builder.Services.TryAddSingleton<ILogger>(sp =>
      (sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance).CreateLogger("Flowthru")
    );
    // Default launcher — preserves historical [pyExe, workerScript]
    // behaviour. Registered via TryAddSingleton so users / tests that
    // wire an alternative (TorchrunLauncher, AccelerateLauncher, a
    // bespoke IPythonLauncher) earlier in the pipeline take precedence.
    builder.Services.TryAddSingleton<IPythonLauncher, DirectPythonLauncher>();
    builder.Services.TryAddSingleton<IPythonExecutor, SubprocessPythonExecutor>();
    builder.Services.AddSingleton<IFlowResourceProvider>(
      sp => (IFlowResourceProvider)sp.GetRequiredService<IPythonExecutor>());

    // Base Python-side capability declaration — the floor of what
    // SubprocessPythonExecutor's worker assumes is in the venv.
    // Registered via AddSingleton (NOT TryAddSingleton): the
    // requirements algebra folds *every* registered capability, so a
    // user who supplies an additional IPythonCapability shouldn't
    // displace the base, they should compose with it.
    builder.Services.AddSingleton<IPythonCapability, BasePythonExtensionCapability>();

    // Installed-package probe — default subprocess implementation
    // shells out to `python -m pip list --format=json`. TryAddSingleton
    // so tests / users can substitute a stub.
    builder.Services.TryAddSingleton<IInstalledPackageProbe, SubprocessInstalledPackageProbe>();

    // Service-ref dispatch: matches Category="python".
    builder.Services.AddSingleton<IServiceRefDispatcher, PythonServiceRefDispatcher>();

    // Pre-flight hook: the single authoritative site for
    // module-import / decorator-presence / schema-agreement / arity
    // checks against every PythonStep<,> in the registered flows.
    builder.Services.AddSingleton<IFlowValidationHook, PythonStepValidationHook>();

    // Pre-flight hook: requirements-algebra enforcement.
    // Folds every IPythonCapability + the active IPythonLauncher's
    // Requirements, probes the configured venv via `pip list`, and
    // surfaces typed PythonPreFlightError variants for missing or
    // wrong-version packages.
    builder.Services.AddSingleton<IFlowValidationHook, PythonRequirementsValidationHook>();

    return builder;
  }

  /// <summary>
  /// Register Python step support with code-first option overrides.
  /// The configure callback runs after the <c>Flowthru:Python</c> section binding,
  /// so it can selectively override individual values.
  /// </summary>
  /// <example>
  /// <code>
  /// b.UsePython(opts =>
  /// {
  ///   opts.ModuleSearchPaths.Add("Flows");
  ///   opts.VenvPath = "/opt/venv";
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
