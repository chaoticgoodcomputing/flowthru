using System.Diagnostics;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Graph.Validation;
using Flowthru.Core.Meta;
using Flowthru.Core.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Services;

/// <summary>
/// Default implementation of <see cref="IFlowthruService"/>.
/// </summary>
/// <remarks>
/// This service wraps pipeline execution logic in a CLI-agnostic interface.
/// It delegates to existing <see cref="Flow"/> execution methods while
/// providing a cleaner API for programmatic use.
/// </remarks>
internal sealed class FlowthruService : IFlowthruService
{
  private readonly IReadOnlyList<CatalogAbstract> _catalogs;
  private readonly Dictionary<string, Flow> _pipelines;
  private readonly IServiceProvider _services;
  private readonly ILogger<FlowthruService> _logger;
  private readonly FlowthruMetadataBuilder? _metadataBuilder;
  private readonly FlowthruExecutionDefaults _executionDefaults;

  /// <summary>
  /// Initializes a new instance of FlowthruService.
  /// </summary>
  public FlowthruService(
    IReadOnlyList<CatalogAbstract> catalogs,
    Dictionary<string, Flow> pipelines,
    IServiceProvider services,
    ILogger<FlowthruService> logger,
    FlowthruExecutionDefaults executionDefaults,
    FlowthruMetadataBuilder? metadataBuilder = null
  )
  {
    _catalogs = catalogs ?? throw new ArgumentNullException(nameof(catalogs));
    _pipelines = pipelines ?? throw new ArgumentNullException(nameof(pipelines));
    _services = services ?? throw new ArgumentNullException(nameof(services));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _executionDefaults = executionDefaults ?? new FlowthruExecutionDefaults();
    _metadataBuilder = metadataBuilder;

    // Inject services into all registered catalogs
    foreach (var catalog in _catalogs)
    {
      catalog.Services = _services;
    }

    // Resolve validation hooks from DI (Phase 4: extensions can register hooks)
    var validationHooks = _services.GetServices<IFlowValidationHook>().ToList();

    // Inject services into each pipeline and build
    foreach (var pipeline in _pipelines.Values)
    {
      pipeline.Logger = _logger;
      pipeline.ServiceProvider = _services;

      // Register validation hooks (e.g., PythonStepValidator from Python extension)
      foreach (var hook in validationHooks)
      {
        pipeline.ValidationHooks.Add(hook);
      }

      pipeline.Build();
    }
  }

  /// <inheritdoc />
  public IReadOnlyCollection<string> FlowNames => _pipelines.Keys;

  /// <inheritdoc />
  public IReadOnlyList<CatalogAbstract> Catalogs => _catalogs;

  /// <inheritdoc />
  public async Task<FlowResult> ExecuteFlowAsync(
    ExecutionOptions? options = null,
    bool exportMetadata = true,
    string? metadataOutputDirectory = null,
    CancellationToken cancellationToken = default
  )
  {
    var totalStopwatch = Stopwatch.StartNew();

    _logger.LogInformation("Merging all pipelines into unified DAG.");
    _logger.LogInformation("Available pipelines: {Pipelines}", string.Join(", ", FlowNames));

    // Merge all pipelines into a single DAG
    var mergedPipeline = Flow.Merge(_pipelines);

    // Inject services and logger
    mergedPipeline.Logger = _logger;
    mergedPipeline.ServiceProvider = _services;

    options ??= new ExecutionOptions();

    // Resolve MaxDegreeOfParallelism: CLI/caller value wins; service default is fallback; 1 is the floor.
    options.MaxDegreeOfParallelism =
      options.MaxDegreeOfParallelism ?? _executionDefaults.MaxDegreeOfParallelism ?? 1;

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
      "  ✓ {StepCount} nodes organized into {LayerCount} execution layers",
      mergedPipeline.Steps.Count,
      mergedPipeline.ExecutionLayers!.Count
    );

    // Validate external inputs — skipped for StructureOnly dry runs (no data source access)
    var validatedInputCount = 0;
    var skipDataValidation =
      options.DryRun.Enabled && options.DryRun.Depth == ValidationDepth.StructureOnly;

    if (skipDataValidation)
    {
      _logger.LogInformation("→ Skipping data source validation (StructureOnly dry run)");
    }
    else
    {
      _logger.LogInformation("→ Validating external data sources...");
      var validationResult = await mergedPipeline.ValidateExternalInputsAsync(
        options.MaxDegreeOfParallelism!.Value,
        cancellationToken
      );
      if (!validationResult.IsValid)
      {
        _logger.LogError("  ✗ Validation failed");
        validationResult.ThrowIfInvalid();
      }

      var allMergedSteps = mergedPipeline.Steps.ToList();
      var mergedProducedLabels = new HashSet<string>(
        allMergedSteps.SelectMany(s => s.Outputs.Select(o => o.Label)),
        StringComparer.OrdinalIgnoreCase
      );
      validatedInputCount = allMergedSteps
        .SelectMany(s => s.Inputs)
        .Where(i => !mergedProducedLabels.Contains(i.Label))
        .Select(i => i.Label)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();
      _logger.LogInformation("  ✓ {Count} external data sources validated", validatedInputCount);
    }

    // Export DAG metadata if requested — runs after all pre-flight checks, before any execution
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

    preFlightStopwatch.Stop();
    _logger.LogInformation("");
    _logger.LogInformation(
      "✅ Pre-flight completed in {Ms}ms",
      preFlightStopwatch.ElapsedMilliseconds
    );
    _logger.LogInformation("");

    // Check if dry run
    if (options.DryRun.Enabled)
    {
      _logger.LogInformation("════════════════════════════════════════");
      _logger.LogInformation("DRY RUN SUCCESSFUL");
      _logger.LogInformation("════════════════════════════════════════");
      _logger.LogInformation("");
      _logger.LogInformation(
        "Steps: {Count} nodes across {Layers} layers",
        mergedPipeline.Steps.Count,
        mergedPipeline.ExecutionLayers!.Count
      );
      _logger.LogInformation("External Inputs: {Count} validated", validatedInputCount);
      _logger.LogInformation("Total Time: {Ms}ms", totalStopwatch.ElapsedMilliseconds);
      _logger.LogInformation("");
      _logger.LogInformation("✅ Pipeline is ready to execute");
      _logger.LogInformation("");

      totalStopwatch.Stop();
      return FlowResult.CreateDryRunSuccess(
        totalStopwatch.Elapsed,
        mergedPipeline.Steps.Count,
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
    var result = await mergedPipeline.RunAsync(options, cancellationToken);

    // Format results
    var formatter = options.GetFormatter();
    formatter.Format(result, _logger);

    totalStopwatch.Stop();
    return result;
  }

  /// <inheritdoc />
  public FlowMetadata GetFlowMetadata(string pipelineName)
  {
    if (!_pipelines.TryGetValue(pipelineName, out var pipeline))
    {
      throw new KeyNotFoundException(
        $"Pipeline '{pipelineName}' not found. " + $"Available: {string.Join(", ", FlowNames)}"
      );
    }

    var allSteps = pipeline.Steps.ToList();
    var producedLabels = new HashSet<string>(
      allSteps.SelectMany(s => s.Outputs.Select(o => o.Label)),
      StringComparer.OrdinalIgnoreCase
    );
    var externalInputs = allSteps
      .SelectMany(s => s.Inputs)
      .Where(i => !producedLabels.Contains(i.Label))
      .Select(i => i.Label)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();

    return new FlowMetadata
    {
      Name = pipeline.Name ?? pipelineName,
      Description = pipeline.Description,
      StepCount = pipeline.Steps.Count,
      LayerCount = pipeline.ExecutionLayers?.Count ?? 0,
      ExternalInputs = externalInputs,
      IsBuilt = pipeline.IsBuilt,
    };
  }

  /// <inheritdoc />
  public DagMetadata GetDagMetadata(
    string? pipelineName = null,
    FlowSliceStrategy? sliceStrategy = null
  )
  {
    Dictionary<string, Flow> toMerge;

    if (pipelineName is not null)
    {
      if (!_pipelines.TryGetValue(pipelineName, out var namedPipeline))
      {
        throw new KeyNotFoundException(
          $"Pipeline '{pipelineName}' not found. " + $"Available: {string.Join(", ", FlowNames)}"
        );
      }

      toMerge = new Dictionary<string, Flow> { [pipelineName] = namedPipeline };
    }
    else
    {
      toMerge = _pipelines;
    }

    // Always merge to produce a fresh pipeline instance — avoids mutating
    // the registered pipelines' Build/Slice state as a side effect.
    var pipeline = Flow.Merge(toMerge);
    pipeline.Logger = _logger;
    pipeline.ServiceProvider = _services;
    pipeline.Build(sliceStrategy);

    return pipeline.ExportDag();
  }

  /// <inheritdoc />
  public async Task<ValidationResult> ValidateFlowAsync(
    string pipelineName,
    CancellationToken cancellationToken = default
  )
  {
    if (!_pipelines.TryGetValue(pipelineName, out var pipeline))
    {
      throw new KeyNotFoundException(
        $"Pipeline '{pipelineName}' not found. " + $"Available: {string.Join(", ", FlowNames)}"
      );
    }

    return await pipeline.ValidateExternalInputsAsync(maxDegreeOfParallelism: 1, cancellationToken);
  }

  private async Task ExportPipelineMetadataAsync(
    Flow pipeline,
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

  private void ExportMetadata(DagMetadata dag, string pipelineName, string? outputDirectory = null)
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
