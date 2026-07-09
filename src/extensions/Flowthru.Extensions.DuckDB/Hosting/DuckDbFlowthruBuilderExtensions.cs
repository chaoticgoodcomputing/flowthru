using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.DuckDb;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.DuckDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Flowthru.Hosting;

/// <summary>
/// <c>UseDuckDb()</c> extension methods on <see cref="IFlowthruBuilder"/>.
/// Registers DuckDB transform support — engine options (bound from
/// <c>Flowthru:DuckDb</c>), the embedded <see cref="IDuckDbEngine"/>
/// singleton, the <see cref="IServiceProfileContributor"/> that tells
/// the scheduler how many transforms may run concurrently, and the
/// hermetic pre-flight <see cref="IFlowValidationHook"/> that
/// schema-checks every transform's SQL against its declared input and
/// output schemas.
/// </summary>
public static class DuckDbFlowthruBuilderExtensions
{
  /// <summary>
  /// Register DuckDB transform support with configuration bound from
  /// the <c>Flowthru:DuckDb</c> section.
  /// </summary>
  /// <example>
  /// <code>
  /// services.AddFlowthru(configuration, b =>
  /// {
  ///   b.RegisterCatalog&lt;Catalog&gt;();
  ///   b.UseDuckDb();
  ///   b.RegisterFlow&lt;Catalog, IDuckDbEngine&gt;("Analytics", AnalyticsFlow.Create);
  /// });
  /// </code>
  /// </example>
  public static IFlowthruBuilder UseDuckDb(this IFlowthruBuilder builder)
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));

    builder.Services
      .AddOptions<DuckDbEngineOptions>()
      .Configure<IConfiguration>((opts, cfg) => cfg.GetSection("Flowthru:DuckDb").Bind(opts))
      .ValidateOnStart();

    // The embedded engine. TryAddSingleton semantics let test doubles
    // or user overrides registered earlier take precedence.
    builder.Services.TryAddSingleton<IDuckDbEngine>(sp =>
      new InProcessDuckDbEngine(sp.GetRequiredService<IOptions<DuckDbEngineOptions>>().Value)
    );

    // Conflict profile: every DuckDB transform step depends on the
    // shared engine, and each transform may use the engine's full
    // memory/disk budget. This contributor reports the engine's
    // MaxConcurrency as the capacity of the engine's conflict key, so
    // the ParallelFlowScheduler holds concurrent transforms to it. It's
    // cache-neutral (AffectsOutputs=false) — a transform's determinism
    // lives in its SQL and inputs, not the engine instance's identity.
    builder.Services.AddSingleton<IServiceProfileContributor>(sp =>
      new DuckDbEngineProfileContributor(sp.GetRequiredService<IDuckDbEngine>())
    );

    // Pre-flight hook: the hermetic SQL schema check for every DuckDB
    // transform in the registered flows — empty in-memory tables from
    // the declared input schemas, DESCRIBE the SQL against them, verify
    // the result against the declared output schema. Classified
    // Hermetic (reaches nothing outside the process), so a
    // schema-breaking SQL edit fails even an offline smoke test.
    // TryAddEnumerable keeps repeated UseDuckDb() calls from stacking
    // duplicate hooks (and duplicate findings).
    builder.Services.TryAddEnumerable(
      ServiceDescriptor.Singleton<IFlowValidationHook, DuckDbTransformValidationHook>());

    return builder;
  }

  /// <summary>
  /// Register DuckDB transform support with code-first option overrides.
  /// The configure callback runs after the <c>Flowthru:DuckDb</c>
  /// section binding, so it can selectively override individual values.
  /// </summary>
  /// <example>
  /// <code>
  /// b.UseDuckDb(opts =>
  /// {
  ///   opts.MemoryLimit = "4GB";
  ///   opts.MaxConcurrentTransforms = 2;
  /// });
  /// </code>
  /// </example>
  public static IFlowthruBuilder UseDuckDb(
    this IFlowthruBuilder builder,
    Action<DuckDbEngineOptions> configure
  )
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (configure is null) throw new ArgumentNullException(nameof(configure));

    builder.UseDuckDb();
    builder.Services.PostConfigure(configure);
    return builder;
  }
}
