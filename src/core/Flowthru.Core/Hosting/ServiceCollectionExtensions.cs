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

    // Logging registration — per ADR-0005 / ADR-0006 the engine and
    // every step share one ILogger (category "Flowthru"). Hosts that
    // called AddLogging() before AddFlowthru see their real
    // ILoggerFactory; hosts that didn't get NullLoggerFactory via
    // the TryAdd-doesn't-overwrite pattern. The shared ILogger
    // resolves to loggerFactory.CreateLogger("Flowthru") in either
    // case. The open-generic ILogger<> shim stays for hosts that
    // opt into per-step categorization on their own — taking
    // ILoggerFactory in Create() and calling CreateLogger<T>() is
    // the escape hatch.
    services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>();
    services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));
    services.TryAddSingleton<ILogger>(sp =>
      sp.GetRequiredService<ILoggerFactory>().CreateLogger("Flowthru")
    );

    // Default scheduler — TryAdd lets a host register its own
    // IFlowScheduler before AddFlowthru and have that win, the same
    // way TryAddSingleton works for any DI default.
    services.TryAddSingleton<IFlowScheduler, ParallelFlowScheduler>();

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
