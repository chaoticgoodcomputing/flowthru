using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage.Strategies;
using Flowthru.Core.Flows;
using Flowthru.Core.Meta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Services;

/// <summary>
/// Builder interface for configuring Flowthru service registration.
/// </summary>
/// <remarks>
/// <para>
/// Implement extension methods on this interface to add optional Flowthru features.
/// Access <see cref="Services"/> to register components and <see cref="Configuration"/>
/// to bind strongly-typed options:
/// </para>
/// <code>
/// public static IFlowthruBuilder UseSpark(this IFlowthruBuilder builder)
/// {
///     builder.Services.AddOptions&lt;SparkConnectOptions&gt;()
///         .Configure&lt;IConfiguration&gt;((opts, config) =&gt;
///             config.GetSection("Flowthru:Spark").Bind(opts))
///         .ValidateOnStart();
///     builder.Services.AddSingleton&lt;SparkFrameProvider&gt;();
///     return builder;
/// }
/// </code>
/// </remarks>
public interface IFlowthruBuilder
{
  /// <summary>
  /// The underlying DI service collection.
  /// Use this to register services, options, and validators.
  /// </summary>
  IServiceCollection Services { get; }

  /// <summary>
  /// The application configuration passed to <c>AddFlowthru</c>.
  /// Available to extensions that need to read config values at registration time.
  /// </summary>
  IConfiguration Configuration { get; }

  /// <summary>
  /// Configures service-level default execution behaviour for all flows.
  /// </summary>
  /// <remarks>
  /// Code-first overrides take effect after config-file binding. Values set here
  /// override anything set in the <c>Flowthru:Execution</c> appsettings section.
  /// Per-call <see cref="ExecutionOptions"/> passed to
  /// <see cref="IFlowthruService.ExecuteFlowAsync"/> take precedence over both.
  /// </remarks>
  IFlowthruBuilder ConfigureExecution(Action<ExecutionOptions> configure);

  /// <summary>
  /// Convenience escape hatch for registering additional services with the underlying
  /// <see cref="Services"/> collection. Prefer using <see cref="Services"/> directly.
  /// </summary>
  IFlowthruBuilder ConfigureServices(Action<IServiceCollection> configure);

  /// <summary>Registers a catalog type with DI constructor injection.</summary>
  IFlowthruBuilder RegisterCatalog<TCatalog>()
    where TCatalog : CatalogAbstract;

  /// <summary>Registers a catalog instance directly.</summary>
  IFlowthruBuilder RegisterCatalog(CatalogAbstract catalog);

  /// <summary>Registers a catalog via a factory that receives the service provider.</summary>
  IFlowthruBuilder RegisterCatalog<TCatalog>(Func<IServiceProvider, TCatalog> catalogFactory)
    where TCatalog : CatalogAbstract;

  /// <summary>
  /// Registers a collection of pre-built catalog instances (fan-out pattern).
  /// </summary>
  IFlowthruBuilder RegisterCatalogs(IEnumerable<CatalogAbstract> catalogs);

  /// <summary>
  /// Registers multiple catalogs via a factory that receives the service provider.
  /// </summary>
  IFlowthruBuilder RegisterCatalogs(
    Func<IServiceProvider, IEnumerable<CatalogAbstract>> catalogsFactory
  );

  /// <summary>
  /// Escape-hatch for registering flows via a full-access service provider factory.
  /// Prefer <see cref="RegisterFlow"/> for standard flow registration.
  /// </summary>
  IFlowthruBuilder RegisterFlows(Func<IServiceProvider, Dictionary<string, Flow>> flowFactory);

  /// <summary>
  /// Registers a flow by inspecting the delegate's parameter types.
  /// Catalog parameters are resolved from DI; all others are resolved from DI as services.
  /// </summary>
  /// <param name="label">Unique flow name.</param>
  /// <param name="flow">Delegate whose parameters are catalogs, services, or config objects.</param>
  /// <param name="configurationSection">
  /// Optional configuration section path. When provided, the first non-catalog,
  /// non-interface parameter is bound from <see cref="Configuration"/> instead of DI.
  /// </param>
  IFlowthruBuilder RegisterFlow(string label, Delegate flow, string? configurationSection = null);

  /// <summary>Adds a description to the most recently registered flow.</summary>
  IFlowthruBuilder WithDescription(string description);

  /// <summary>Registers a storage entry factory type.</summary>
  IFlowthruBuilder UseStorageStrategy<TStrategy>()
    where TStrategy : class, IStorageEntryFactory;

  /// <summary>Registers a storage entry factory instance.</summary>
  IFlowthruBuilder UseStorageStrategy(IStorageEntryFactory strategy);

  /// <summary>Registers a storage entry factory via a service-provider factory.</summary>
  IFlowthruBuilder UseStorageStrategy(Func<IServiceProvider, IStorageEntryFactory> strategyFactory);

  /// <summary>Configures metadata export (DAG diagrams, JSON manifests, etc.).</summary>
  IFlowthruBuilder ConfigureMetadata(Action<FlowthruMetadataBuilder> configure);
}
