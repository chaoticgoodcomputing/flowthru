using Flowthru.Data;
using Flowthru.Meta;
using Flowthru.Parameters;
using Flowthru.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Application;

/// <summary>
/// Fluent builder for configuring a Flowthru application.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to configure all aspects of your Flowthru application:
/// catalog, pipelines, services, logging, and execution options.
/// </para>
/// <para>
/// <strong>Inline Registration (Recommended):</strong>
/// <code>
/// var app = FlowthruApplication.Create(args, builder =>
/// {
///     builder.UseCatalog(new MyCatalog("Data"));
///     builder
///         .RegisterPipeline&lt;MyCatalog&gt;("pipeline_name", MyPipeline.Create)
///         .WithDescription("Pipeline description")
///         .WithTags("tag1", "tag2");
///     builder.ConfigureLogging(logging => logging.AddConsole());
/// });
/// </code>
/// </para>
/// <para>
/// <strong>Registry Class Alternative:</strong>
/// <code>
/// var app = FlowthruApplication.Create(args, builder =>
/// {
///     builder.UseCatalog(new MyCatalog("Data"));
///     builder.RegisterPipelines&lt;MyPipelineRegistry&gt;();
///     builder.ConfigureLogging(logging => logging.AddConsole());
/// });
/// </code>
/// </para>
/// </remarks>
public class FlowthruApplicationBuilder {
  private readonly string[] _args;
  private DataCatalogBase? _catalog;
  private Func<IServiceProvider, DataCatalogBase>? _catalogFactory;
  private Type? _pipelineRegistryType;
  private readonly List<Action<PipelineRegistrar<DataCatalogBase>>> _inlineRegistrations = new();
  private readonly ServiceCollection _services = new();
  private readonly ParameterStore _parameters = new();
  private readonly ExecutionOptions _executionOptions = new();
  private FlowthruMetadataConfiguration? _metadataConfiguration;

  /// <summary>
  /// Initializes a new instance of FlowthruApplicationBuilder.
  /// </summary>
  /// <param name="args">Command-line arguments</param>
  internal FlowthruApplicationBuilder(string[] args) {
    _args = args;

    // Set up default logging
    _services.AddLogging(logging => {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }

  /// <summary>
  /// Configures the data catalog to use for pipelines.
  /// </summary>
  /// <param name="catalog">The catalog instance</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this overload when your catalog doesn't require dependency injection.
  /// </remarks>
  public FlowthruApplicationBuilder UseCatalog(DataCatalogBase catalog) {
    _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    _catalogFactory = null; // Clear factory if set
    return this;
  }

  /// <summary>
  /// Gets the configured catalog instance.
  /// </summary>
  /// <typeparam name="TCatalog">The catalog type</typeparam>
  /// <returns>The catalog instance</returns>
  /// <exception cref="InvalidOperationException">Thrown if catalog hasn't been configured yet</exception>
  /// <remarks>
  /// Use this to reference catalog entries when configuring validation options.
  /// </remarks>
  public TCatalog GetCatalog<TCatalog>() where TCatalog : DataCatalogBase {
    if (_catalog == null) {
      throw new InvalidOperationException(
        "Catalog has not been configured yet. Call UseCatalog() first.");
    }

    if (_catalog is not TCatalog typedCatalog) {
      throw new InvalidOperationException(
        $"Catalog is of type {_catalog.GetType().Name}, not {typeof(TCatalog).Name}");
    }

    return typedCatalog;
  }

  /// <summary>
  /// Configures the data catalog using a factory function that receives services.
  /// </summary>
  /// <param name="catalogFactory">Factory function to create the catalog</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this overload when your catalog needs to resolve services from DI container.
  /// Example:
  /// <code>
  /// builder.UseCatalog(services =>
  /// {
  ///     var config = services.GetRequiredService&lt;IConfiguration&gt;();
  ///     return new MyCatalog(config["DataPath"]);
  /// });
  /// </code>
  /// </remarks>
  public FlowthruApplicationBuilder UseCatalog(Func<IServiceProvider, DataCatalogBase> catalogFactory) {
    _catalogFactory = catalogFactory ?? throw new ArgumentNullException(nameof(catalogFactory));
    _catalog = null; // Clear instance if set
    return this;
  }

  /// <summary>
  /// Registers pipelines using a pipeline registry class.
  /// </summary>
  /// <typeparam name="TRegistry">The pipeline registry type</typeparam>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// The registry type must inherit from PipelineRegistry&lt;TCatalog&gt; and have
  /// a parameterless constructor.
  /// </remarks>
  public FlowthruApplicationBuilder RegisterPipelines<TRegistry>()
    where TRegistry : class, new() {
    _pipelineRegistryType = typeof(TRegistry);
    _inlineRegistrations.Clear(); // Clear inline registrations if using registry class
    return this;
  }

  /// <summary>
  /// Registers a pipeline with a parameterless factory function (inline registration).
  /// </summary>
  /// <typeparam name="TCatalog">The catalog type</typeparam>
  /// <param name="label">Unique pipeline name</param>
  /// <param name="creator">Factory function that creates the pipeline from catalog</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this for inline pipeline registration without a separate registry class.
  /// Fluent chaining with WithDescription() and WithTags() is supported.
  /// </remarks>
  public FlowthruApplicationBuilder RegisterPipeline<TCatalog>(
    string label,
    Func<TCatalog, Pipelines.Pipeline> creator)
    where TCatalog : DataCatalogBase {
    _inlineRegistrations.Add(registrar =>
      registrar.Register(label, catalog => creator((TCatalog)catalog)));
    _pipelineRegistryType = null; // Clear registry type if using inline registration
    return this;
  }

  /// <summary>
  /// Registers a pipeline with a parameterized factory function (inline registration).
  /// </summary>
  /// <typeparam name="TCatalog">The catalog type</typeparam>
  /// <typeparam name="TParams">The type of parameters the pipeline requires</typeparam>
  /// <param name="label">Unique pipeline name</param>
  /// <param name="creator">Factory function that creates the pipeline from catalog and parameters</param>
  /// <param name="parameters">Parameter instance to pass to the pipeline</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this for inline pipeline registration with parameters.
  /// Fluent chaining with WithDescription() and WithTags() is supported.
  /// </remarks>
  public FlowthruApplicationBuilder RegisterPipeline<TCatalog, TParams>(
    string label,
    Func<TCatalog, TParams, Pipelines.Pipeline> creator,
    TParams parameters)
    where TCatalog : DataCatalogBase {
    _inlineRegistrations.Add(registrar =>
      registrar.Register(label, (catalog, p) => creator((TCatalog)catalog, (TParams)p), parameters));
    _pipelineRegistryType = null; // Clear registry type if using inline registration
    return this;
  }

  /// <summary>
  /// Adds a description to the most recently registered pipeline.
  /// </summary>
  /// <param name="description">Human-readable description of what the pipeline does</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this after RegisterPipeline() to add metadata.
  /// </remarks>
  public FlowthruApplicationBuilder WithDescription(string description) {
    if (_inlineRegistrations.Count == 0) {
      throw new InvalidOperationException(
        "WithDescription() can only be used after RegisterPipeline(). " +
        "If using RegisterPipelines<T>(), use WithDescription() in the registry class instead.");
    }

    _inlineRegistrations.Add(registrar => registrar.WithDescription(description));
    return this;
  }

  /// <summary>
  /// Adds tags to the most recently registered pipeline.
  /// </summary>
  /// <param name="tags">Tags for categorizing the pipeline</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this after RegisterPipeline() to add metadata.
  /// </remarks>
  public FlowthruApplicationBuilder WithTags(params string[] tags) {
    if (_inlineRegistrations.Count == 0) {
      throw new InvalidOperationException(
        "WithTags() can only be used after RegisterPipeline(). " +
        "If using RegisterPipelines<T>(), use WithTags() in the registry class instead.");
    }

    _inlineRegistrations.Add(registrar => registrar.WithTags(tags));
    return this;
  }

  /// <summary>
  /// Configures validation options for the most recently registered pipeline.
  /// </summary>
  /// <param name="configure">Action to configure validation behavior</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Use this after RegisterPipeline() to opt into deep inspection for critical
  /// external data sources or to explicitly disable inspection for specific inputs.
  /// </para>
  /// <para>
  /// <strong>Example:</strong>
  /// </para>
  /// <code>
  /// builder.RegisterPipeline&lt;MyCatalog&gt;("data_processing", ProcessingPipeline.Create)
  ///   .WithValidation(validation => {
  ///     validation.Inspect(catalog.Companies, InspectionLevel.Deep);
  ///     validation.Inspect(catalog.Shuttles, InspectionLevel.Deep);
  ///   });
  /// </code>
  /// </remarks>
  public FlowthruApplicationBuilder WithValidation(Action<Pipelines.Validation.ValidationOptions> configure) {
    if (_inlineRegistrations.Count == 0) {
      throw new InvalidOperationException(
        "WithValidation() can only be used after RegisterPipeline(). " +
        "If using RegisterPipelines<T>(), use WithValidation() in the registry class instead.");
    }

    _inlineRegistrations.Add(registrar => registrar.WithValidation(configure));
    return this;
  }

  /// <summary>
  /// Configures services for dependency injection.
  /// </summary>
  /// <param name="configure">Action to configure the service collection</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Services registered here are available to:
  /// - Catalog entries (via catalog.Services)
  /// - Pipelines (via pipeline.ServiceProvider)
  /// - Nodes (via property injection)
  /// </remarks>
  public FlowthruApplicationBuilder ConfigureServices(Action<IServiceCollection> configure) {
    configure?.Invoke(_services);
    return this;
  }

  /// <summary>
  /// Configures logging for the application.
  /// </summary>
  /// <param name="configure">Action to configure logging</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// By default, console logging at Information level is configured.
  /// Use this method to override or enhance the default configuration.
  /// </remarks>
  public FlowthruApplicationBuilder ConfigureLogging(Action<ILoggingBuilder> configure) {
    // Remove existing logging configuration
    var loggingDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(ILoggingBuilder));
    if (loggingDescriptor != null) {
      _services.Remove(loggingDescriptor);
    }

    _services.AddLogging(configure);
    return this;
  }

  /// <summary>
  /// Configures execution options for pipeline execution.
  /// </summary>
  /// <param name="configure">Action to configure execution options</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// Use this to configure result formatting, error handling, and other execution settings.
  /// </remarks>
  public FlowthruApplicationBuilder ConfigureExecution(Action<ExecutionOptions> configure) {
    configure?.Invoke(_executionOptions);
    return this;
  }

  /// <summary>
  /// Enables metadata collection and export for Flowthru.Viz.
  /// </summary>
  /// <param name="configure">Action to configure metadata options</param>
  /// <returns>This builder for method chaining</returns>
  /// <remarks>
  /// <para>
  /// Metadata collection captures pipeline DAG structure (nodes, catalog entries, edges)
  /// and exports it to JSON files for visualization in Flowthru.Viz.
  /// </para>
  /// <para>
  /// <strong>Usage:</strong>
  /// </para>
  /// <code>
  /// builder.IncludeMetadata(metadata => {
  ///     metadata
  ///         .WithOutputDirectory("Data/Metadata")
  ///         .EnableAutoExport();
  /// });
  /// </code>
  /// <para>
  /// By default, DAG metadata is automatically exported after each pipeline build
  /// to the "Data/Metadata" directory.
  /// </para>
  /// </remarks>
  public FlowthruApplicationBuilder IncludeMetadata(Action<FlowthruMetadataConfiguration>? configure = null) {
    _metadataConfiguration = new FlowthruMetadataConfiguration();
    configure?.Invoke(_metadataConfiguration);
    return this;
  }

  /// <summary>
  /// Builds the configured application.
  /// </summary>
  /// <returns>The configured Flowthru application</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if catalog or pipeline registry is not configured
  /// </exception>
  internal IFlowthruApplication Build() {
    // 1. Build service provider
    var services = _services.BuildServiceProvider();

    // 2. Resolve or create catalog
    var catalog = _catalog ?? _catalogFactory?.Invoke(services);
    if (catalog == null) {
      throw new InvalidOperationException(
        "No catalog configured. Call UseCatalog() before building the application.");
    }

    // Inject services into catalog
    catalog.Services = services;

    // 3. Create pipeline registry and get pipelines
    Dictionary<string, Pipelines.Pipeline> pipelines;

    if (_inlineRegistrations.Count > 0) {
      // Use inline registration approach
      var registrar = new PipelineRegistrar<DataCatalogBase>(catalog);

      // Replay all registration actions
      foreach (var registration in _inlineRegistrations) {
        registration(registrar);
      }

      pipelines = registrar.Build();
    } else if (_pipelineRegistryType != null) {
      // Use registry class approach
      var registry = Activator.CreateInstance(_pipelineRegistryType);
      if (registry == null) {
        throw new InvalidOperationException(
          $"Failed to create instance of pipeline registry type {_pipelineRegistryType.Name}");
      }

      // Use reflection to call GetPipelines with the catalog
      var getPipelinesMethod = _pipelineRegistryType.GetMethod(
        "GetPipelines",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

      if (getPipelinesMethod == null) {
        throw new InvalidOperationException(
          $"Pipeline registry type {_pipelineRegistryType.Name} does not have a GetPipelines method");
      }

      pipelines = getPipelinesMethod.Invoke(registry, new object[] { catalog })
        as Dictionary<string, Pipelines.Pipeline>
        ?? throw new InvalidOperationException("Failed to get pipelines from registry");
    } else {
      throw new InvalidOperationException(
        "No pipelines configured. Call RegisterPipeline() or RegisterPipelines<T>() before building the application.");
    }

    // 3.5. Build all pipelines (metadata export happens after merge in RunAsync)
    var logger = services.GetRequiredService<ILogger<FlowthruApplication>>();

    foreach (var (name, pipeline) in pipelines) {
      pipeline.Name = name;
      pipeline.Logger = logger;
      pipeline.ServiceProvider = services;
      pipeline.Build();
    }

    // 4. Create and return application
    return new FlowthruApplication(
      _args,
      catalog,
      pipelines,
      services,
      _executionOptions,
      _metadataConfiguration,
      logger);
  }
}
