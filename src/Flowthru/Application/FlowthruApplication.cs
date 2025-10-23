using Flowthru.Data;
using Flowthru.Pipelines;
using Microsoft.Extensions.Logging;

namespace Flowthru.Application;

/// <summary>
/// Main application class for running Flowthru pipelines.
/// </summary>
/// <remarks>
/// <para>
/// FlowthruApplication is the primary entry point for executing data pipelines.
/// It handles:
/// - Command-line argument parsing
/// - Pipeline selection
/// - Service injection
/// - Pipeline execution
/// - Result formatting
/// - Exit code generation
/// </para>
/// <para>
/// <strong>Inline Registration Example:</strong>
/// <code>
/// public static async Task&lt;int&gt; Main(string[] args)
/// {
///     var app = FlowthruApplication.Create(args, builder =>
///     {
///         builder.UseCatalog(new MyCatalog("Data"));
///         builder.RegisterPipeline&lt;MyCatalog&gt;("my_pipeline", MyPipeline.Create);
///     });
///     
///     return await app.RunAsync();
/// }
/// </code>
/// </para>
/// <para>
/// <strong>Registry Class Example:</strong>
/// <code>
/// public static async Task&lt;int&gt; Main(string[] args)
/// {
///     var app = FlowthruApplication.Create(args, builder =>
///     {
///         builder.UseCatalog(new MyCatalog("Data"));
///         builder.RegisterPipelines&lt;MyPipelineRegistry&gt;();
///     });
///     
///     return await app.RunAsync();
/// }
/// </code>
/// </para>
/// </remarks>
public class FlowthruApplication : IFlowthruApplication {
  private readonly string[] _args;
  private readonly DataCatalogBase _catalog;
  private readonly Dictionary<string, Pipeline> _pipelines;
  private readonly IServiceProvider _services;
  private readonly ExecutionOptions _executionOptions;
  private readonly ILogger<FlowthruApplication> _logger;

  /// <summary>
  /// Initializes a new instance of FlowthruApplication.
  /// </summary>
  /// <remarks>
  /// This constructor is internal - use the static Create() method instead.
  /// </remarks>
  internal FlowthruApplication(
    string[] args,
    DataCatalogBase catalog,
    Dictionary<string, Pipeline> pipelines,
    IServiceProvider services,
    ExecutionOptions executionOptions,
    ILogger<FlowthruApplication> logger) {
    _args = args;
    _catalog = catalog;
    _pipelines = pipelines;
    _services = services;
    _executionOptions = executionOptions;
    _logger = logger;
  }

  /// <summary>
  /// Creates a new Flowthru application with the specified configuration.
  /// </summary>
  /// <param name="args">Command-line arguments</param>
  /// <param name="configure">Action to configure the application</param>
  /// <returns>The configured application</returns>
  /// <remarks>
  /// This is the primary factory method for creating Flowthru applications.
  /// </remarks>
  public static IFlowthruApplication Create(
    string[] args,
    Action<FlowthruApplicationBuilder> configure) {
    var builder = new FlowthruApplicationBuilder(args);
    configure(builder);
    return builder.Build();
  }

  /// <inheritdoc />
  public Task<int> RunAsync() {
    return RunAsync(CancellationToken.None);
  }

  /// <inheritdoc />
  public async Task<int> RunAsync(CancellationToken cancellationToken) {
    try {
      // 1. Parse command-line arguments to select pipeline
      var pipelineName = SelectPipeline(_args, _pipelines.Keys);

      if (pipelineName == null) {
        // No pipeline specified - merge and run all pipelines
        _logger.LogInformation("No pipeline specified. Running all pipelines in dependency order.");
        _logger.LogInformation("Available pipelines: {Pipelines}",
          string.Join(", ", _pipelines.Keys));

        // Merge all pipelines into a single DAG
        var mergedPipeline = Pipeline.Merge(_pipelines);

        // Inject services and logger
        mergedPipeline.Logger = _logger;
        mergedPipeline.ServiceProvider = _services;

        // Build and execute merged pipeline
        var mergedResult = await RunMergedPipelineAsync(mergedPipeline);

        // Return appropriate exit code
        return mergedResult.Success ? 0 : 1;
      }

      // 2. Run the selected pipeline
      var result = await RunPipelineAsync(pipelineName);

      // 3. Return appropriate exit code
      return result.Success ? 0 : 1;
    } catch (Exception ex) {
      _logger.LogCritical(ex, "Application failed: {Message}", ex.Message);
      return 1;
    }
  }

  /// <inheritdoc />
  public async Task<PipelineResult> RunPipelineAsync(string pipelineName) {
    // 1. Get pipeline
    if (!_pipelines.TryGetValue(pipelineName, out var pipeline)) {
      _logger.LogError("Pipeline '{Name}' not found. Available pipelines: {Available}",
        pipelineName,
        string.Join(", ", _pipelines.Keys));
      throw new KeyNotFoundException($"Pipeline '{pipelineName}' not found");
    }

    _logger.LogInformation("Running pipeline: {Name}", pipelineName);

    // 2. Inject services into pipeline
    pipeline.Logger = _logger;
    pipeline.ServiceProvider = _services;

    // 3. Build the pipeline
    if (!pipeline.IsBuilt) {
      pipeline.Build();
    }

    // 4. Validate external inputs if configured
    var validationResult = await pipeline.ValidateExternalInputsAsync();
    if (!validationResult.IsValid) {
      validationResult.ThrowIfInvalid();
    }

    // 5. Execute pipeline
    var result = await pipeline.RunAsync();

    // 6. Format results
    var formatter = _executionOptions.GetFormatter();
    formatter.Format(result, _logger);

    return result;
  }

  /// <summary>
  /// Runs a merged pipeline containing nodes from all registered pipelines.
  /// </summary>
  /// <param name="mergedPipeline">The merged pipeline to execute</param>
  /// <returns>Pipeline execution result</returns>
  private async Task<PipelineResult> RunMergedPipelineAsync(Pipeline mergedPipeline) {
    _logger.LogInformation("Running merged pipeline: {Name}", mergedPipeline.Name);

    // Build the pipeline
    if (!mergedPipeline.IsBuilt) {
      mergedPipeline.Build();
    }

    // Validate external inputs if configured
    var validationResult = await mergedPipeline.ValidateExternalInputsAsync();
    if (!validationResult.IsValid) {
      validationResult.ThrowIfInvalid();
    }

    // Execute pipeline
    var result = await mergedPipeline.RunAsync();

    // Format results
    var formatter = _executionOptions.GetFormatter();
    formatter.Format(result, _logger);

    return result;
  }

  /// <summary>
  /// Selects a pipeline name from command-line arguments.
  /// </summary>
  /// <param name="args">Command-line arguments</param>
  /// <param name="availablePipelines">Available pipeline names</param>
  /// <returns>Selected pipeline name, or null to run all</returns>
  private string? SelectPipeline(string[] args, IEnumerable<string> availablePipelines) {
    // Simple implementation: first argument is pipeline name
    // Future: add --list, --help, --dry-run flags

    if (args.Length == 0) {
      // No arguments - should run all pipelines (Phase 2)
      return null;
    }

    var pipelineName = args[0];

    // Check if it's a valid pipeline name
    if (!availablePipelines.Contains(pipelineName)) {
      _logger.LogWarning("Pipeline '{Name}' not found. Available: {Available}",
        pipelineName,
        string.Join(", ", availablePipelines));
      // Return it anyway - let RunPipelineAsync throw the proper exception
    }

    return pipelineName;
  }
}
