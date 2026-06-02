using Flowthru.Caching;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowthru.Hosting;

/// <summary>
/// DI registration entry point. <c>services.AddFlowthru(b =&gt; …)</c>
/// is the canonical authoring shape for hosting Flowthru inside a
/// .NET app — the lambda receives an
/// <see cref="IFlowthruBuilder"/> and registers a catalog factory,
/// flow factories, validation hooks, and metadata providers.
/// </summary>
public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Register Flowthru in <paramref name="services"/>. Inside the
  /// configuration callback, register at least one catalog and one
  /// flow; <see cref="IFlowthruService"/> is bound as a singleton
  /// you can resolve from DI.
  /// </summary>
  public static IServiceCollection AddFlowthru(
    this IServiceCollection services,
    Action<IFlowthruBuilder> configure
  )
  {
    if (services is null) throw new ArgumentNullException(nameof(services));
    if (configure is null) throw new ArgumentNullException(nameof(configure));

    var builder = new FlowthruServiceBuilder(services);
    configure(builder);

    // Logging registration — the engine and every step share one
    // ILogger (category "Flowthru"). The shared
    // ILogger resolves to whatever ILoggerFactory the host wired via
    // AddLogging(); if no factory is registered, the lambda falls
    // back to NullLoggerFactory.Instance and calls are silently
    // dropped. Avoid TryAdd<NullLoggerFactory> here — AddLogging()
    // also uses TryAdd, so whichever runs first wins, and an eager
    // NullLoggerFactory registration would block a real factory the
    // host registers afterward.
    services.TryAddSingleton<ILogger>(sp =>
      (sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance).CreateLogger("Flowthru")
    );

    // Default scheduler — TryAdd lets a host register its own
    // IFlowScheduler before AddFlowthru and have that win, the same
    // way TryAddSingleton works for any DI default.
    services.TryAddSingleton<IFlowScheduler, ParallelFlowScheduler>();

    // Default service-profile provider — permissive (every service is
    // unbounded + cache-affecting), so the scheduler's conflict gating is
    // a no-op until a resource declares a capacity. TryAdd lets an
    // extension or host register a composing provider ahead of it.
    services.TryAddSingleton<IServiceProfileProvider, DefaultServiceProfileProvider>();

    // Execution defaults (Parallelism, …) flow through the standard
    // Options pipeline so they can be set via ConfigureExecution(...)
    // or bound from appsettings. The validator turns a nonsensical
    // value into a pre-flight failure (when the options are first
    // resolved, before any flow logic runs) instead of the scheduler
    // silently clamping it at runtime. ValidateOnStart upgrades that to
    // host-startup time under a generic host.
    services.AddOptions<ExecutionDefaults>()
      .Validate(d => d.Parallelism >= 1, "Flowthru: ExecutionDefaults.Parallelism must be >= 1.")
      .ValidateOnStart();

    // Storage-medium resolver — composes any IStorageMediumProvider
    // registered by extensions (UseHttp, UseS3, …) into a single
    // dispatcher that format extensions consume. Bare paths and
    // file:// URIs always resolve via the built-in fallback, so a
    // host that doesn't register any provider still gets a working
    // resolver. TryAdd preserves the override-before-AddFlowthru
    // pattern.
    services.TryAddSingleton<IStorageMediumResolver>(sp =>
      new StorageMediumResolver(sp.GetServices<IStorageMediumProvider>())
    );

    // Default cache manifest storage — TryAdd preserves any prior
    // UseCacheStorage(...) registration from the configure callback.
    // Phase 6 of the smart-caching RFC: framework-managed item, not
    // part of any user-visible DAG.
    services.TryAddSingleton<IItem<CacheManifest>>(_ =>
      Item.Of<CacheManifest>("flowthru.cache")
        .Json()
        .AtPath(".flowthru/cache.json")
        .Build()
    );

    services.AddSingleton(builder);
    services.AddSingleton<IFlowthruService>(sp =>
      new FlowthruService(
        sp,
        sp.GetRequiredService<FlowthruServiceBuilder>(),
        sp.GetRequiredService<ILogger>()
      )
    );
    return services;
  }
}
