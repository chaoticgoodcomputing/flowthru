using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage.Strategies;
using Flowthru.Core.Flows;
using Flowthru.Core.Meta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Services;

/// <summary>
/// Fluent builder for configuring Flowthru service registration.
/// </summary>
/// <remarks>
/// <para>
/// This builder configures the service layer without CLI coupling.
/// Use it to register catalogs, flows, and optional features.
/// </para>
/// <para>
/// <strong>Basic Usage:</strong>
/// <code>
/// services.AddFlowthru(configuration, flowthru =>
/// {
///     flowthru.RegisterCatalog(_ => new MyCatalog(dataPath));
///     flowthru.RegisterFlow("my_flow", MyFlow.Create);
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class FlowthruServiceBuilder : IFlowthruBuilder
{
  private readonly List<Type> _registeredCatalogTypes = new();
  private readonly List<
    Func<IServiceProvider, IEnumerable<CatalogAbstract>>
  > _dynamicCatalogFactories = new();
  private readonly List<FlowRegistrationEntry> _registrations = new();
  private Func<IServiceProvider, Dictionary<string, Flow>>? _flowFactory;
  private FlowRegistrationEntry? _lastRegistration;

  /// <inheritdoc />
  public IServiceCollection Services { get; }

  /// <inheritdoc />
  public IConfiguration Configuration { get; }

  internal FlowthruServiceBuilder(IServiceCollection services, IConfiguration configuration)
  {
    Services = services ?? throw new ArgumentNullException(nameof(services));
    Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <summary>
  /// Configures default execution behaviour for all pipelines run through this service.
  /// </summary>
  /// <remarks>
  /// Settings applied here act as the service-level default. They are overridden by any
  /// value explicitly supplied in the <see cref="ExecutionOptions"/> passed to
  /// <see cref="IFlowthruService.ExecuteFlowAsync"/> (e.g. from the CLI <c>--parallelism</c>
  /// flag). Properties left <c>null</c> or at their default in the per-call options fall
  /// back to these values.
  /// </remarks>
  /// <example>
  /// <code>
  /// services.AddFlowthru(flowthru =>
  /// {
  ///     flowthru.ConfigureExecution(opts =>
  ///     {
  ///         opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
  ///     });
  /// });
  /// </code>
  /// </example>
  public IFlowthruBuilder ConfigureExecution(Action<ExecutionOptions> configure)
  {
    if (configure == null)
    {
      throw new ArgumentNullException(nameof(configure));
    }

    Services.PostConfigure<ExecutionOptions>(configure);
    return this;
  }

  /// <summary>
  /// Convenience method for registering additional services with <see cref="Services"/>.
  /// </summary>
  /// <param name="configure">Action that receives the service collection.</param>
  /// <returns>This builder for method chaining.</returns>
  public IFlowthruBuilder ConfigureServices(Action<IServiceCollection> configure)
  {
    if (configure == null)
    {
      throw new ArgumentNullException(nameof(configure));
    }

    configure(Services);
    return this;
  }

  /// <summary>
  /// Internal entry type that carries a Flow factory and its associated metadata.
  /// Replaces the FlowRegistrar indirection for cleaner multi-catalog support.
  /// </summary>
  internal sealed class FlowRegistrationEntry
  {
    public string Label { get; }
    public Func<IServiceProvider, Flow> Factory { get; }
    public string Description { get; set; } = "";

    internal FlowRegistrationEntry(string label, Func<IServiceProvider, Flow> factory)
    {
      Label = label;
      Factory = factory;
    }
  }

  /// <summary>
  /// Registers a catalog type with constructor injection.
  /// </summary>
  /// <typeparam name="TCatalog">The catalog type</typeparam>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// The catalog will be resolved from the DI container, allowing constructor
  /// parameter injection (e.g., IConfiguration, IOptions).
  /// </remarks>
  public IFlowthruBuilder RegisterCatalog<TCatalog>()
    where TCatalog : CatalogAbstract
  {
    Services.AddSingleton<TCatalog>();
    _registeredCatalogTypes.Add(typeof(TCatalog));
    return this;
  }

  /// <summary>
  /// Registers a catalog instance directly.
  /// </summary>
  /// <param name="catalog">The catalog instance</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this when the catalog doesn't require dependency injection.
  /// </remarks>
  public IFlowthruBuilder RegisterCatalog(CatalogAbstract catalog)
  {
    if (catalog == null)
    {
      throw new ArgumentNullException(nameof(catalog));
    }

    Services.AddSingleton(catalog.GetType(), catalog);
    _registeredCatalogTypes.Add(catalog.GetType());
    return this;
  }

  /// <summary>
  /// Registers a catalog factory that receives the service provider.
  /// </summary>
  /// <typeparam name="TCatalog">The concrete catalog type</typeparam>
  /// <param name="catalogFactory">Factory function to create the catalog</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this when the catalog needs to resolve services during construction,
  /// or when construction requires parameters unavailable at the call site.
  /// </remarks>
  public IFlowthruBuilder RegisterCatalog<TCatalog>(Func<IServiceProvider, TCatalog> catalogFactory)
    where TCatalog : CatalogAbstract
  {
    if (catalogFactory == null)
    {
      throw new ArgumentNullException(nameof(catalogFactory));
    }

    Services.AddSingleton<TCatalog>(catalogFactory);
    _registeredCatalogTypes.Add(typeof(TCatalog));
    return this;
  }

  /// <summary>
  /// Registers a collection of pre-built catalog instances produced by iterative or dynamic
  /// construction — the fan-out pattern where N identical-shaped catalogs differ only by
  /// their construction parameters (e.g., one catalog per US state).
  /// </summary>
  /// <param name="catalogs">The catalog instances to register</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// All registered catalogs will receive DI service injection and appear in
  /// <see cref="IFlowthruService.Catalogs"/>. Use with <see cref="RegisterFlows"/> to
  /// wire per-catalog flows in a loop.
  /// </remarks>
  public IFlowthruBuilder RegisterCatalogs(IEnumerable<CatalogAbstract> catalogs)
  {
    if (catalogs == null)
    {
      throw new ArgumentNullException(nameof(catalogs));
    }

    var snapshot = catalogs.ToList();
    _dynamicCatalogFactories.Add(_ => snapshot);
    return this;
  }

  /// <summary>
  /// Registers catalogs produced by a factory that receives the service provider —
  /// useful when catalog construction itself requires DI resolution.
  /// </summary>
  /// <param name="catalogsFactory">Factory that returns the catalog collection</param>
  /// <returns>This builder for method chaining</returns>
  public IFlowthruBuilder RegisterCatalogs(
    Func<IServiceProvider, IEnumerable<CatalogAbstract>> catalogsFactory
  )
  {
    if (catalogsFactory == null)
    {
      throw new ArgumentNullException(nameof(catalogsFactory));
    }

    _dynamicCatalogFactories.Add(catalogsFactory);
    return this;
  }

  /// <summary>
  /// Escape-hatch for registering flows via a full-access service provider factory.
  /// </summary>
  /// <param name="flowFactory">Factory function that receives the service provider and returns the Flow dictionary</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Prefer <see cref="RegisterFlow(string, Delegate, string?)"/> for standard Flow registration.
  /// Use this only when you need full service provider access during Flow construction.
  /// </remarks>
  public IFlowthruBuilder RegisterFlows(
    Func<IServiceProvider, Dictionary<string, Flow>> flowFactory
  )
  {
    if (flowFactory == null)
    {
      throw new ArgumentNullException(nameof(flowFactory));
    }

    _flowFactory = flowFactory;

    return this;
  }

  /// <summary>
  /// Registers a Flow by inspecting the delegate's parameter types at runtime.
  /// Parameters that extend <see cref="CatalogAbstract"/> are resolved from DI as catalogs.
  /// All other parameters are resolved from DI as services.
  /// </summary>
  /// <param name="label">Unique Flow name</param>
  /// <param name="flow">A method group or delegate whose parameters are catalogs or DI-registered services.</param>
  /// <returns>This builder for method chaining</returns>
  public IFlowthruBuilder RegisterFlow(string label, Delegate flow)
  {
    if (string.IsNullOrWhiteSpace(label))
    {
      throw new ArgumentException("Flow label cannot be null or empty.", nameof(label));
    }

    if (flow == null)
    {
      throw new ArgumentNullException(nameof(flow));
    }

    var method = flow.Method;
    var parameters = method.GetParameters();

    // Validate return type
    if (method.ReturnType != typeof(Flow))
    {
      throw new ArgumentException(
        $"Flow delegate must return Flow, but '{method.Name}' returns {method.ReturnType.Name}.",
        nameof(flow)
      );
    }

    // Build the resolver: for each parameter, determine how to resolve it at flow-build time
    var resolvers = new Func<IServiceProvider, object?>[parameters.Length];
    for (int i = 0; i < parameters.Length; i++)
    {
      var capturedType = parameters[i].ParameterType;
      resolvers[i] = sp => sp.GetRequiredService(capturedType);
    }

    var capturedDelegate = flow;
    var entry = new FlowRegistrationEntry(
      label,
      sp =>
      {
        var args = new object?[resolvers.Length];
        for (int i = 0; i < resolvers.Length; i++)
        {
          args[i] = resolvers[i](sp);
        }

        return (Flow)capturedDelegate.DynamicInvoke(args)!;
      }
    );
    _registrations.Add(entry);
    _lastRegistration = entry;
    return this;
  }

  /// <summary>
  /// Adds a description to the most recently registered flow.
  /// </summary>
  /// <param name="description">Human-readable description of what the Flow does</param>
  /// <returns>This builder for method chaining</returns>
  public IFlowthruBuilder WithDescription(string description)
  {
    if (_lastRegistration == null)
    {
      throw new InvalidOperationException(
        "WithDescription() can only be used after RegisterFlow()."
      );
    }

    _lastRegistration.Description = description;
    return this;
  }

  /// <summary>
  /// Registers a storage entry factory (for environment-specific entries).
  /// </summary>
  /// <typeparam name="TStrategy">The storage strategy type</typeparam>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Storage strategies enable environment-specific catalog entries:
  /// </para>
  /// <code>
  /// if (env.IsDevelopment())
  /// {
  ///     flowthru.UseStorageStrategy&lt;CsvStorageEntryFactory&gt;();
  /// }
  /// else if (env.IsProduction())
  /// {
  ///     flowthru.UseStorageStrategy&lt;DatabaseStorageEntryFactory&gt;();
  /// }
  /// else if (env.IsTest())
  /// {
  ///     flowthru.UseStorageStrategy&lt;MemoryStorageEntryFactory&gt;();
  /// }
  /// </code>
  /// </remarks>
  public IFlowthruBuilder UseStorageStrategy<TStrategy>()
    where TStrategy : class, IStorageEntryFactory
  {
    Services.AddSingleton<IStorageEntryFactory, TStrategy>();
    return this;
  }

  /// <summary>
  /// Registers a storage entry factory instance.
  /// </summary>
  /// <param name="strategy">The storage strategy instance</param>
  /// <returns>This builder for method chaining</returns>
  public IFlowthruBuilder UseStorageStrategy(IStorageEntryFactory strategy)
  {
    if (strategy == null)
    {
      throw new ArgumentNullException(nameof(strategy));
    }

    Services.AddSingleton(strategy);
    return this;
  }

  /// <summary>
  /// Registers a storage entry factory using a factory function.
  /// </summary>
  /// <param name="strategyFactory">Factory function to create the strategy</param>
  /// <returns>This builder for method chaining</returns>
  public IFlowthruBuilder UseStorageStrategy(
    Func<IServiceProvider, IStorageEntryFactory> strategyFactory
  )
  {
    if (strategyFactory == null)
    {
      throw new ArgumentNullException(nameof(strategyFactory));
    }

    Services.AddSingleton(strategyFactory);
    return this;
  }

  /// <summary>
  /// Configures metadata export.
  /// </summary>
  /// <param name="configure">Action to configure the metadata builder</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Metadata export is optional. If not configured, flows will execute
  /// without generating DAG diagrams or metadata files.
  /// </para>
  /// </remarks>
  public IFlowthruBuilder ConfigureMetadata(Action<FlowthruMetadataBuilder> configure)
  {
    if (configure == null)
    {
      throw new ArgumentNullException(nameof(configure));
    }

    var builder = new FlowthruMetadataBuilder();

    // Apply programmatic configuration
    configure(builder);

    Services.AddSingleton(builder);

    return this;
  }

  /// <summary>
  /// Internal method called by AddFlowthru to register the catalog collection and
  /// Flow dictionary into the DI container. Must be called after all RegisterCatalog
  /// and RegisterFlow calls have been made.
  /// </summary>
  internal void RegisterFlowDictionary()
  {
    // Always register the catalog collection so FlowthruService can inject all catalogs.
    // Merges both type-registered catalogs (RegisterCatalog) and dynamically constructed
    // catalog collections (RegisterCatalogs).
    var catalogTypes = _registeredCatalogTypes.ToList();
    var dynamicFactories = _dynamicCatalogFactories.ToList();
    Services.AddSingleton<IReadOnlyList<CatalogAbstract>>(sp =>
    {
      var typedCatalogs = catalogTypes.Select(t => (CatalogAbstract)sp.GetRequiredService(t));
      var dynamicCatalogs = dynamicFactories.SelectMany(f => f(sp));
      return typedCatalogs.Concat(dynamicCatalogs).ToList().AsReadOnly();
    });

    var snapshot = _registrations.ToList();
    var factory = _flowFactory;

    if (snapshot.Count == 0 && factory == null)
    {
      throw new InvalidOperationException(
        "No flows were registered. Call RegisterFlow() or RegisterFlows() before building the service."
      );
    }

    Services.AddSingleton<Dictionary<string, Flow>>(sp =>
    {
      var result = new Dictionary<string, Flow>();

      // Source 1: RegisterFlows factory (registered first so inline registrations can override)
      if (factory != null)
      {
        foreach (var (key, flow) in factory(sp))
        {
          result[key] = flow;
        }
      }

      // Source 2: RegisterFlow calls (wins on key collision)
      foreach (var reg in snapshot)
      {
        var flow = reg.Factory(sp);
        flow.Name = reg.Label;
        flow.Description = reg.Description;
        result[reg.Label] = flow;
      }

      return result;
    });
  }
}
