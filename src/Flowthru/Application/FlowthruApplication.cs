using System.Diagnostics;
using Flowthru.Data;
using Flowthru.Meta;
using Flowthru.Meta.Builders;
using Flowthru.Meta.Models;
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
  private readonly FlowthruMetadataBuilder? _metadataBuilder;
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
    FlowthruMetadataBuilder? metadataBuilder,
    ILogger<FlowthruApplication> logger) {
    _args = args;
    _catalog = catalog;
    _pipelines = pipelines;
    _services = services;
    _executionOptions = executionOptions;
    _metadataBuilder = metadataBuilder;
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

        // Build merged pipeline
        mergedPipeline.Build();

        // Export merged DAG metadata if configured
        if (_metadataBuilder != null && _metadataBuilder.AutoExport) {
          try {
            var dag = mergedPipeline.ExportDag();
            ExportMetadata(dag, "Pipelines");
          } catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to export DAG metadata for merged pipeline");
          }
        }

        // Execute merged pipeline
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
    var totalStopwatch = Stopwatch.StartNew();

    // 1. Get pipeline
    if (!_pipelines.TryGetValue(pipelineName, out var pipeline)) {
      _logger.LogError("Pipeline '{Name}' not found. Available pipelines: {Available}",
        pipelineName,
        string.Join(", ", _pipelines.Keys));
      throw new KeyNotFoundException($"Pipeline '{pipelineName}' not found");
    }

    // ════════════════════════════════════════
    // PRE-FLIGHT CHECKS
    // ════════════════════════════════════════
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("PRE-FLIGHT CHECKS");
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("");

    var preFlightStopwatch = Stopwatch.StartNew();

    // 2. Inject services into pipeline
    _logger.LogInformation("→ Initializing pipeline: {Name}", pipelineName);
    pipeline.Logger = _logger;
    pipeline.ServiceProvider = _services;

    // 3. Build the pipeline
    if (!pipeline.IsBuilt) {
      _logger.LogInformation("→ Building pipeline and analyzing dependencies...");
      pipeline.Build();
      _logger.LogInformation("  ✓ Pipeline built successfully");
      _logger.LogInformation("  ✓ {NodeCount} nodes organized into {LayerCount} execution layers",
        pipeline.Nodes.Count,
        pipeline.ExecutionLayers!.Count);
    }

    // 3.5. Export DAG metadata if configured
    if (_metadataBuilder != null && _metadataBuilder.AutoExport) {
      try {
        _logger.LogInformation("→ Exporting DAG metadata...");
        var dag = pipeline.ExportDag();
        ExportMetadata(dag, pipelineName);
        _logger.LogInformation("  ✓ Metadata exported successfully");
      } catch (Exception ex) {
        _logger.LogWarning(ex, "  ⚠ Failed to export DAG metadata: {Message}", ex.Message);
      }
    }

    // 4. Validate external inputs if configured
    _logger.LogInformation("→ Validating external data sources...");
    var validationResult = await pipeline.ValidateExternalInputsAsync();
    if (!validationResult.IsValid) {
      _logger.LogError("  ✗ Validation failed");
      validationResult.ThrowIfInvalid();
    }

    // Count validated inputs
    var layer0Nodes = pipeline.ExecutionLayers![0];
    var validatedInputCount = layer0Nodes
      .SelectMany(node => node.Inputs)
      .Distinct()
      .Count();

    _logger.LogInformation("  ✓ {Count} external data sources validated", validatedInputCount);

    preFlightStopwatch.Stop();
    _logger.LogInformation("");
    _logger.LogInformation("✅ Pre-flight completed in {Ms}ms", preFlightStopwatch.ElapsedMilliseconds);
    _logger.LogInformation("");

    // Check if dry run
    if (_executionOptions.DryRun) {
      _logger.LogInformation("════════════════════════════════════════");
      _logger.LogInformation("DRY RUN SUCCESSFUL");
      _logger.LogInformation("════════════════════════════════════════");
      _logger.LogInformation("");
      _logger.LogInformation("Pipeline: {Name}", pipelineName);
      _logger.LogInformation("Nodes: {Count} nodes across {Layers} layers",
        pipeline.Nodes.Count,
        pipeline.ExecutionLayers!.Count);
      _logger.LogInformation("External Inputs: {Count} validated", validatedInputCount);
      _logger.LogInformation("Total Time: {Ms}ms", totalStopwatch.ElapsedMilliseconds);
      _logger.LogInformation("");
      _logger.LogInformation("✅ Pipeline is ready to execute");
      _logger.LogInformation("");

      totalStopwatch.Stop();
      return PipelineResult.CreateDryRunSuccess(
        totalStopwatch.Elapsed,
        pipeline.Nodes.Count,
        pipeline.ExecutionLayers!.Count,
        validatedInputCount,
        pipelineName);
    }

    // ════════════════════════════════════════
    // PIPELINE EXECUTION
    // ════════════════════════════════════════
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("PIPELINE EXECUTION");
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("");

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
    var totalStopwatch = Stopwatch.StartNew();
    var pipelineName = mergedPipeline.Name ?? "Pipelines";

    // ════════════════════════════════════════
    // PRE-FLIGHT CHECKS
    // ════════════════════════════════════════
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("PRE-FLIGHT CHECKS");
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("");

    var preFlightStopwatch = Stopwatch.StartNew();

    // Build the pipeline
    if (!mergedPipeline.IsBuilt) {
      _logger.LogInformation("→ Building merged pipeline and analyzing dependencies...");
      mergedPipeline.Build();
      _logger.LogInformation("  ✓ Pipeline built successfully");
      _logger.LogInformation("  ✓ {NodeCount} nodes organized into {LayerCount} execution layers",
        mergedPipeline.Nodes.Count,
        mergedPipeline.ExecutionLayers!.Count);
    }

    // Export DAG metadata if configured
    if (_metadataBuilder != null && _metadataBuilder.AutoExport) {
      try {
        _logger.LogInformation("→ Exporting DAG metadata...");
        var dag = mergedPipeline.ExportDag();
        ExportMetadata(dag, pipelineName);
        _logger.LogInformation("  ✓ Metadata exported successfully");
      } catch (Exception ex) {
        _logger.LogWarning(ex, "  ⚠ Failed to export DAG metadata: {Message}", ex.Message);
      }
    }

    // Validate external inputs if configured
    _logger.LogInformation("→ Validating external data sources...");
    var validationResult = await mergedPipeline.ValidateExternalInputsAsync();
    if (!validationResult.IsValid) {
      _logger.LogError("  ✗ Validation failed");
      validationResult.ThrowIfInvalid();
    }

    // Count validated inputs
    var layer0Nodes = mergedPipeline.ExecutionLayers![0];
    var validatedInputCount = layer0Nodes
      .SelectMany(node => node.Inputs)
      .Distinct()
      .Count();

    _logger.LogInformation("  ✓ {Count} external data sources validated", validatedInputCount);

    preFlightStopwatch.Stop();
    _logger.LogInformation("");
    _logger.LogInformation("✅ Pre-flight completed in {Ms}ms", preFlightStopwatch.ElapsedMilliseconds);
    _logger.LogInformation("");

    // Check if dry run
    if (_executionOptions.DryRun) {
      _logger.LogInformation("════════════════════════════════════════");
      _logger.LogInformation("DRY RUN SUCCESSFUL");
      _logger.LogInformation("════════════════════════════════════════");
      _logger.LogInformation("");
      _logger.LogInformation("Pipeline: {Name}", pipelineName);
      _logger.LogInformation("Nodes: {Count} nodes across {Layers} layers",
        mergedPipeline.Nodes.Count,
        mergedPipeline.ExecutionLayers!.Count);
      _logger.LogInformation("External Inputs: {Count} validated", validatedInputCount);
      _logger.LogInformation("Total Time: {Ms}ms", totalStopwatch.ElapsedMilliseconds);
      _logger.LogInformation("");
      _logger.LogInformation("✅ Pipeline is ready to execute");
      _logger.LogInformation("");

      totalStopwatch.Stop();
      return PipelineResult.CreateDryRunSuccess(
        totalStopwatch.Elapsed,
        mergedPipeline.Nodes.Count,
        mergedPipeline.ExecutionLayers!.Count,
        validatedInputCount,
        pipelineName);
    }

    // ════════════════════════════════════════
    // PIPELINE EXECUTION
    // ════════════════════════════════════════
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("PIPELINE EXECUTION");
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("");

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
    // Filter out flags and extract pipeline name
    var filteredArgs = new List<string>();

    foreach (var arg in args) {
      if (arg == "--dry-run") {
        _executionOptions.DryRun = true;
      } else {
        filteredArgs.Add(arg);
      }
    }

    if (filteredArgs.Count == 0) {
      // No arguments - should run all pipelines (Phase 2)
      return null;
    }

    var pipelineName = filteredArgs[0];

    // Check if it's a valid pipeline name
    if (!availablePipelines.Contains(pipelineName)) {
      _logger.LogWarning("Pipeline '{Name}' not found. Available: {Available}",
        pipelineName,
        string.Join(", ", availablePipelines));
      // Return it anyway - let RunPipelineAsync throw the proper exception
    }

    return pipelineName;
  }

  /// <summary>
  /// Exports DAG metadata using all registered providers.
  /// </summary>
  /// <param name="dag">The DAG metadata to export</param>
  /// <param name="name">Name for the exported files</param>
  private void ExportMetadata(DagMetadata dag, string name) {
    if (_metadataBuilder == null) {
      return;
    }

    var outputDirectory = _metadataBuilder.OutputDirectory;

    // Ensure output directory exists
    if (!Directory.Exists(outputDirectory)) {
      Directory.CreateDirectory(outputDirectory);
    }

    // Execute each provider
    foreach (var provider in _metadataBuilder.Providers) {
      try {
        _logger.LogInformation("Exporting DAG metadata using {Provider} to {Directory}",
          provider.Name, outputDirectory);

        var success = provider.Export(dag, outputDirectory, _metadataBuilder.TimestampConfig, _logger);

        if (success) {
          _logger.LogInformation("{Provider} export completed successfully", provider.Name);
        } else {
          _logger.LogWarning("{Provider} export failed", provider.Name);
        }
      } catch (Exception ex) {
        _logger.LogWarning(ex, "Error during {Provider} export: {Message}",
          provider.Name, ex.Message);
      }
    }
  }
}
