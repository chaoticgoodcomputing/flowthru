using System.Diagnostics;
using Flowthru.Data;
using Flowthru.Data.Validation;
using Flowthru.Meta;
using Flowthru.Meta.Models;
using Flowthru.Pipelines;
using Flowthru.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Services;

/// <summary>
/// Default implementation of <see cref="IFlowthruService"/>.
/// </summary>
/// <remarks>
/// This service wraps pipeline execution logic in a CLI-agnostic interface.
/// It delegates to existing <see cref="Pipeline"/> execution methods while
/// providing a cleaner API for programmatic use.
/// </remarks>
internal sealed class FlowthruService : IFlowthruService
{
  private readonly DataCatalogBase _catalog;
  private readonly Dictionary<string, Pipeline> _pipelines;
  private readonly IServiceProvider _services;
  private readonly ILogger<FlowthruService> _logger;
  private readonly FlowthruMetadataBuilder? _metadataBuilder;

  /// <summary>
  /// Initializes a new instance of FlowthruService.
  /// </summary>
  public FlowthruService(
    DataCatalogBase catalog,
    Dictionary<string, Pipeline> pipelines,
    IServiceProvider services,
    ILogger<FlowthruService> logger,
    FlowthruMetadataBuilder? metadataBuilder = null
  )
  {
    _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    _pipelines = pipelines ?? throw new ArgumentNullException(nameof(pipelines));
    _services = services ?? throw new ArgumentNullException(nameof(services));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _metadataBuilder = metadataBuilder;

    // Inject services into catalog
    _catalog.Services = _services;

    // Resolve validation hooks from DI (Phase 4: extensions can register hooks)
    var validationHooks = _services
      .GetServices<Pipelines.Validation.IPipelineValidationHook>()
      .ToList();

    // Inject services into each pipeline and build
    foreach (var pipeline in _pipelines.Values)
    {
      pipeline.Logger = _logger;
      pipeline.ServiceProvider = _services;

      // Register validation hooks (e.g., PythonNodeValidator from Python extension)
      foreach (var hook in validationHooks)
      {
        pipeline.ValidationHooks.Add(hook);
      }

      pipeline.Build();
    }
  }

  /// <inheritdoc />
  public IReadOnlyCollection<string> PipelineNames => _pipelines.Keys;

  /// <inheritdoc />
  public DataCatalogBase Catalog => _catalog;

  /// <inheritdoc />
  public async Task<PipelineResult> ExecutePipelineAsync(
    ExecutionOptions? options = null,
    bool exportMetadata = true,
    string? metadataOutputDirectory = null,
    CancellationToken cancellationToken = default
  )
  {
    var totalStopwatch = Stopwatch.StartNew();

    _logger.LogInformation("Merging all pipelines into unified DAG.");
    _logger.LogInformation("Available pipelines: {Pipelines}", string.Join(", ", PipelineNames));

    // Merge all pipelines into a single DAG
    var mergedPipeline = Pipeline.Merge(_pipelines);

    // Inject services and logger
    mergedPipeline.Logger = _logger;
    mergedPipeline.ServiceProvider = _services;

    options ??= new ExecutionOptions();

    // ════════════════════════════════════════
    // PRE-FLIGHT CHECKS
    // ════════════════════════════════════════
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("PRE-FLIGHT CHECKS");
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("");

    var preFlightStopwatch = Stopwatch.StartNew();

    // Build merged pipeline with optional slice strategy
    _logger.LogInformation("→ Building pipeline and analyzing dependencies...");
    mergedPipeline.Build(options.SliceStrategy);
    _logger.LogInformation(
      "  ✓ {NodeCount} nodes organized into {LayerCount} execution layers",
      mergedPipeline.Nodes.Count,
      mergedPipeline.ExecutionLayers!.Count
    );

    // Export DAG metadata if requested
    if (exportMetadata && _metadataBuilder != null && _metadataBuilder.AutoExport)
    {
      try
      {
        _logger.LogInformation("→ Exporting DAG metadata...");
        var dag = mergedPipeline.ExportDag();
        ExportMetadata(dag, "Pipeline", metadataOutputDirectory);
        _logger.LogInformation("  ✓ Metadata exported successfully");
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "  ⚠ Failed to export DAG metadata: {Message}", ex.Message);
      }
    }

    // Validate external inputs
    _logger.LogInformation("→ Validating external data sources...");
    var validationResult = await mergedPipeline.ValidateExternalInputsAsync(cancellationToken);
    if (!validationResult.IsValid)
    {
      _logger.LogError("  ✗ Validation failed");
      validationResult.ThrowIfInvalid();
    }

    // Count validated inputs
    var layer0Nodes = mergedPipeline.ExecutionLayers![0];
    var validatedInputCount = layer0Nodes.SelectMany(node => node.Inputs).Distinct().Count();

    _logger.LogInformation("  ✓ {Count} external data sources validated", validatedInputCount);

    preFlightStopwatch.Stop();
    _logger.LogInformation("");
    _logger.LogInformation(
      "✅ Pre-flight completed in {Ms}ms",
      preFlightStopwatch.ElapsedMilliseconds
    );
    _logger.LogInformation("");

    // Check if dry run
    if (options.DryRun)
    {
      _logger.LogInformation("════════════════════════════════════════");
      _logger.LogInformation("DRY RUN SUCCESSFUL");
      _logger.LogInformation("════════════════════════════════════════");
      _logger.LogInformation("");
      _logger.LogInformation(
        "Nodes: {Count} nodes across {Layers} layers",
        mergedPipeline.Nodes.Count,
        mergedPipeline.ExecutionLayers!.Count
      );
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
        "Pipeline"
      );
    }

    // ════════════════════════════════════════
    // PIPELINE EXECUTION
    // ════════════════════════════════════════
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("PIPELINE EXECUTION");
    _logger.LogInformation("════════════════════════════════════════");
    _logger.LogInformation("");

    // Execute merged pipeline
    var result = await mergedPipeline.RunAsync(cancellationToken);

    // Format results
    var formatter = options.GetFormatter();
    formatter.Format(result, _logger);

    totalStopwatch.Stop();
    return result;
  }

  /// <inheritdoc />
  public PipelineMetadata GetPipelineMetadata(string pipelineName)
  {
    if (!_pipelines.TryGetValue(pipelineName, out var pipeline))
    {
      throw new KeyNotFoundException(
        $"Pipeline '{pipelineName}' not found. " + $"Available: {string.Join(", ", PipelineNames)}"
      );
    }

    var externalInputs =
      pipeline
        .ExecutionLayers?[0].SelectMany(node => node.Inputs)
        .Select(e => e.Label)
        .Distinct()
        .ToList() ?? new List<string>();

    return new PipelineMetadata
    {
      Name = pipeline.Name ?? pipelineName,
      Description = pipeline.Description,
      NodeCount = pipeline.Nodes.Count,
      LayerCount = pipeline.ExecutionLayers?.Count ?? 0,
      ExternalInputs = externalInputs,
      IsBuilt = pipeline.IsBuilt,
    };
  }

  /// <inheritdoc />
  public DagMetadata GetDagMetadata(
    string? pipelineName = null,
    PipelineSliceStrategy? sliceStrategy = null
  )
  {
    Dictionary<string, Pipeline> toMerge;

    if (pipelineName is not null)
    {
      if (!_pipelines.TryGetValue(pipelineName, out var namedPipeline))
      {
        throw new KeyNotFoundException(
          $"Pipeline '{pipelineName}' not found. "
            + $"Available: {string.Join(", ", PipelineNames)}"
        );
      }

      toMerge = new Dictionary<string, Pipeline> { [pipelineName] = namedPipeline };
    }
    else
    {
      toMerge = _pipelines;
    }

    // Always merge to produce a fresh pipeline instance — avoids mutating
    // the registered pipelines' Build/Slice state as a side effect.
    var pipeline = Pipeline.Merge(toMerge);
    pipeline.Logger = _logger;
    pipeline.ServiceProvider = _services;
    pipeline.Build(sliceStrategy);

    return pipeline.ExportDag();
  }

  /// <inheritdoc />
  public async Task<ValidationResult> ValidatePipelineAsync(
    string pipelineName,
    CancellationToken cancellationToken = default
  )
  {
    if (!_pipelines.TryGetValue(pipelineName, out var pipeline))
    {
      throw new KeyNotFoundException(
        $"Pipeline '{pipelineName}' not found. " + $"Available: {string.Join(", ", PipelineNames)}"
      );
    }

    return await pipeline.ValidateExternalInputsAsync(cancellationToken);
  }

  private async Task ExportPipelineMetadataAsync(
    Pipeline pipeline,
    string pipelineName,
    string? outputDirectory
  )
  {
    if (_metadataBuilder == null)
    {
      return;
    }

    var dag = pipeline.ExportDag();
    await Task.Run(() => ExportMetadata(dag, pipelineName, outputDirectory));
  }

  private void ExportMetadata(
    Meta.Models.DagMetadata dag,
    string pipelineName,
    string? outputDirectory = null
  )
  {
    if (_metadataBuilder == null)
    {
      return;
    }

    // Execute each provider
    foreach (var provider in _metadataBuilder.Providers)
    {
      try
      {
        _logger.LogInformation("Exporting DAG metadata using {Provider}", provider.Name);

        provider.Consume(dag);

        _logger.LogInformation("{Provider} export completed successfully", provider.Name);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(
          ex,
          "Error during {Provider} export: {Message}",
          provider.Name,
          ex.Message
        );
      }
    }
  }
}
