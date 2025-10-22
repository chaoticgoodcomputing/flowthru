using Flowthru.Data;
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
/// <strong>Usage:</strong>
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
public class FlowthruApplicationBuilder
{
  private readonly string[] _args;
  private DataCatalogBase? _catalog;
  private Func<IServiceProvider, DataCatalogBase>? _catalogFactory;
  private Type? _pipelineRegistryType;
  private readonly ServiceCollection _services = new();
  private readonly ParameterStore _parameters = new();
  private ExecutionOptions _executionOptions = new();

  /// <summary>
  /// Initializes a new instance of FlowthruApplicationBuilder.
  /// </summary>
  /// <param name="args">Command-line arguments</param>
  internal FlowthruApplicationBuilder(string[] args)
  {
    _args = args;

    // Set up default logging
    _services.AddLogging(logging =>
    {
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
  public FlowthruApplicationBuilder UseCatalog(DataCatalogBase catalog)
  {
    _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    _catalogFactory = null; // Clear factory if set
    return this;
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
  public FlowthruApplicationBuilder UseCatalog(Func<IServiceProvider, DataCatalogBase> catalogFactory)
  {
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
    where TRegistry : class, new()
  {
    _pipelineRegistryType = typeof(TRegistry);
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
  public FlowthruApplicationBuilder ConfigureServices(Action<IServiceCollection> configure)
  {
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
  public FlowthruApplicationBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
  {
    // Remove existing logging configuration
    var loggingDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(ILoggingBuilder));
    if (loggingDescriptor != null)
    {
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
  public FlowthruApplicationBuilder ConfigureExecution(Action<ExecutionOptions> configure)
  {
    configure?.Invoke(_executionOptions);
    return this;
  }

  /// <summary>
  /// Builds the configured application.
  /// </summary>
  /// <returns>The configured Flowthru application</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if catalog or pipeline registry is not configured
  /// </exception>
  internal IFlowthruApplication Build()
  {
    // 1. Build service provider
    var services = _services.BuildServiceProvider();

    // 2. Resolve or create catalog
    var catalog = _catalog ?? _catalogFactory?.Invoke(services);
    if (catalog == null)
      throw new InvalidOperationException(
        "No catalog configured. Call UseCatalog() before building the application.");

    // Inject services into catalog
    catalog.Services = services;

    // 3. Create pipeline registry and get pipelines
    if (_pipelineRegistryType == null)
      throw new InvalidOperationException(
        "No pipeline registry configured. Call RegisterPipelines<T>() before building the application.");

    var registry = Activator.CreateInstance(_pipelineRegistryType);
    if (registry == null)
      throw new InvalidOperationException(
        $"Failed to create instance of pipeline registry type {_pipelineRegistryType.Name}");

    // Use reflection to call GetPipelines with the catalog
    var getPipelinesMethod = _pipelineRegistryType.GetMethod(
      "GetPipelines",
      System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

    if (getPipelinesMethod == null)
      throw new InvalidOperationException(
        $"Pipeline registry type {_pipelineRegistryType.Name} does not have a GetPipelines method");

    var pipelines = getPipelinesMethod.Invoke(registry, new object[] { catalog })
      as Dictionary<string, Pipelines.Pipeline>;

    if (pipelines == null)
      throw new InvalidOperationException("Failed to get pipelines from registry");

    // 4. Create and return application
    return new FlowthruApplication(
      _args,
      catalog,
      pipelines,
      services,
      _executionOptions,
      services.GetRequiredService<ILogger<FlowthruApplication>>());
  }
}
