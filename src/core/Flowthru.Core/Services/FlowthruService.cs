using System.Diagnostics;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Graph.Validation;
using Flowthru.Core.Meta;
using Flowthru.Core.Meta.Providers;
using Flowthru.Core.Results;
using Flowthru.Core.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
  private readonly IOptions<ExecutionOptions> _executionDefaults;

  /// <summary>
  /// Initializes a new instance of FlowthruService.
  /// </summary>
  public FlowthruService(
    IReadOnlyList<CatalogAbstract> catalogs,
    Dictionary<string, Flow> pipelines,
    IServiceProvider services,
    ILogger<FlowthruService> logger,
    IOptions<ExecutionOptions> executionDefaults,
    FlowthruMetadataBuilder? metadataBuilder = null
  )
  {
    _catalogs = catalogs ?? throw new ArgumentNullException(nameof(catalogs));
    _pipelines = pipelines ?? throw new ArgumentNullException(nameof(pipelines));
    _services = services ?? throw new ArgumentNullException(nameof(services));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _executionDefaults =
      executionDefaults ?? throw new ArgumentNullException(nameof(executionDefaults));
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
      options.MaxDegreeOfParallelism ?? _executionDefaults.Value.MaxDegreeOfParallelism ?? 1;

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

    // Catalog-level applicative validation — every catalog's Validate() runs;
    // failures across catalogs are accumulated into one report.
    var executionContext = new FlowExecutionContext(
      FlowLabel: "Pipeline",
      Options: options,
      Services: _services
    );

    _logger.LogInformation("→ Running catalog-level validation...");
    var compositeValidation = FlowValidation.Combine(
      _catalogs.Select(c => c.Validate(executionContext))
    );
    if (!compositeValidation.IsValid)
    {
      _logger.LogError(
        "  ✗ {Count} catalog validation failure(s)",
        compositeValidation.Failures.Count
      );
      foreach (var f in compositeValidation.Failures)
      {
        _logger.LogError("    [{Source}] {Message}", f.Source, f.Message);
      }
      throw new FlowValidationException(compositeValidation);
    }
    _logger.LogInformation("  ✓ Catalog validation passed");

    // Catalog-attached resource acquisition. Skipped on default dry run to
    // preserve the "zero side effects" promise; enabled by
    // ExecutionOptions.AcquireResourcesOnDryRun for thorough probing.
    var resources = _catalogs
      .Select(c => c.Resource)
      .Where(r => r is not null)
      .Cast<IFlowResource>()
      .ToList();

    var shouldAcquire = !options.DryRun.Enabled || options.AcquireResourcesOnDryRun;
    var acquiredResources = new Stack<(IFlowResource Resource, object? Scope)>();
    var teardownErrors = new List<Exception>();
    Exception? bodyException = null;
    FlowResult? result = null;

    try
    {
      if (shouldAcquire && resources.Count > 0)
      {
        _logger.LogInformation(
          "→ Acquiring {Count} catalog resource(s)...",
          resources.Count
        );
        foreach (var resource in resources)
        {
          var scope = await resource.AcquireUntyped().Run(cancellationToken).ConfigureAwait(false);
          acquiredResources.Push((resource, scope));
        }
        _logger.LogInformation("  ✓ Resources acquired");
      }
      else if (resources.Count > 0)
      {
        _logger.LogInformation(
          "→ Skipping {Count} catalog resource(s) (dry run, AcquireResourcesOnDryRun=false)",
          resources.Count
        );
      }

      result = await ExecuteFlowBodyAsync(
        mergedPipeline,
        options,
        exportMetadata,
        preFlightStopwatch,
        totalStopwatch,
        cancellationToken
      ).ConfigureAwait(false);

      // Propagate step-level failure into bodyException so release closures
      // (e.g., PreserveOnFailure) can observe the run failed even when the
      // body returned a failed FlowResult instead of throwing.
      if (result is not null && !result.Success)
      {
        bodyException =
          result.Exception
          ?? new InvalidOperationException(
            "Flow returned a failed FlowResult without an exception."
          );
      }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      // User-initiated cancellation. Release still runs in finally; rethrow clean.
      throw;
    }
    catch (Exception ex)
    {
      bodyException = ex;
      throw;
    }
    finally
    {
      if (acquiredResources.Count > 0)
      {
        _logger.LogInformation(
          "→ Releasing {Count} catalog resource(s) (LIFO)...",
          acquiredResources.Count
        );
        while (acquiredResources.Count > 0)
        {
          var (resource, scope) = acquiredResources.Pop();
          try
          {
            await resource
              .ReleaseUntyped(scope, bodyException)
              .Run(cancellationToken)
              .ConfigureAwait(false);
          }
          catch (Exception releaseEx)
          {
            teardownErrors.Add(releaseEx);
            _logger.LogWarning(
              releaseEx,
              "  ⚠ Resource release threw: {Message}",
              releaseEx.Message
            );
          }
        }

        if (teardownErrors.Count == 0)
        {
          _logger.LogInformation("  ✓ Resources released");
        }
        else
        {
          _logger.LogWarning(
            "  ⚠ {Count} teardown error(s) captured",
            teardownErrors.Count
          );
        }
      }
    }

    // Successful path. Attach any teardown errors to the result; primary
    // result.Success / result.Exception are unchanged so the primary outcome
    // wins for "what caused the flow to (not) succeed" reporting.
    if (teardownErrors.Count > 0 && result is not null)
    {
      result = result.WithTeardownErrors(teardownErrors);
    }

    return result!;
  }

  /// <summary>
  /// Pre-flight (post-validation, post-acquire) plus execution. Extracted from
  /// <see cref="ExecuteFlowAsync"/> so the resource lifecycle wrapper stays
  /// readable. Returns the <see cref="FlowResult"/>; teardown errors are
  /// folded in by the caller after release completes.
  /// </summary>
  private async Task<FlowResult> ExecuteFlowBodyAsync(
    Flow mergedPipeline,
    ExecutionOptions options,
    bool exportMetadata,
    Stopwatch preFlightStopwatch,
    Stopwatch totalStopwatch,
    CancellationToken cancellationToken
  )
  {
    // Validate external inputs — skipped when:
    //  - StructureOnly dry runs (explicit no-data-access mode), OR
    //  - Default dry runs against catalogs with FlowResources (resources
    //    weren't acquired, so items behind them can't be inspected; don't
    //    pretend to validate what we can't reach).
    var validatedInputCount = 0;
    var hasUnacquiredResources =
      options.DryRun.Enabled
      && !options.AcquireResourcesOnDryRun
      && _catalogs.Any(c => c.Resource is not null);
    var skipDataValidation =
      (options.DryRun.Enabled && options.DryRun.Depth == ValidationDepth.StructureOnly)
      || hasUnacquiredResources;

    if (skipDataValidation)
    {
      var reason =
        options.DryRun.Enabled && options.DryRun.Depth == ValidationDepth.StructureOnly
          ? "StructureOnly dry run"
          : "dry run with unacquired catalog resources";
      _logger.LogInformation("→ Skipping data source validation ({Reason})", reason);
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
    DagMetadata? dag = null;
    if (exportMetadata && _metadataBuilder != null)
    {
      try
      {
        _logger.LogInformation("→ Exporting DAG metadata...");
        dag = mergedPipeline.ExportDag();
        ExportMetadata(dag, "Pipeline");
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

    // Execute merged pipeline.
    //
    // Service-level safety net: anything that escapes Flow.RunAsync past the
    // FlowResult contract is, by definition, an unexpected runtime escape. The
    // wrap below ensures the user-facing formatter still gets a chance to fire
    // (with the "please file an issue" framing) before the exception reaches
    // the host. Caller-initiated cancellation propagates clean — that's a
    // user-requested abort, not a Flowthru failure.
    FlowResult result;
    try
    {
      result = await mergedPipeline.RunAsync(options, cancellationToken);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      throw;
    }
    catch (Exception ex)
    {
      var wrapped =
        ex as FlowExecutionEscapedException
        ?? new FlowExecutionEscapedException(
          "Flow execution failed with an exception that escaped the FlowResult "
            + "contract. This is unexpected — pre-flight should have caught the "
            + "underlying issue before runtime. The original exception is "
            + "preserved as the inner exception.",
          ex
        );
      var synthetic = FlowResult.CreateFailure(
        executionTime: totalStopwatch.Elapsed,
        exception: wrapped,
        stepResults: new Dictionary<string, StepResult>(),
        flowName: "Pipeline"
      );
      options.GetFormatter().Format(synthetic, _logger);
      throw;
    }

    // Export post-run metadata — fires only after real executions (dry run returns above)
    if (_metadataBuilder != null)
    {
      var postRunProviders = _metadataBuilder.Providers.OfType<IPostRunMetadataProvider>().ToList();

      if (postRunProviders.Count > 0)
      {
        try
        {
          _logger.LogInformation("→ Exporting post-run metadata...");
          dag ??= mergedPipeline.ExportDag();
          var runMetadata = new RunMetadata { Dag = dag, Result = result };
          ExportRunMetadata(runMetadata);
          _logger.LogInformation("  ✓ Post-run metadata exported successfully");
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "  ⚠ Failed to export post-run metadata: {Message}", ex.Message);
        }
      }
    }

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

  private void ExportMetadata(DagMetadata dag, string pipelineName)
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

  private void ExportRunMetadata(RunMetadata run)
  {
    if (_metadataBuilder == null)
    {
      return;
    }

    foreach (var provider in _metadataBuilder.Providers.OfType<IPostRunMetadataProvider>())
    {
      var name = (provider as IMetadataProvider)?.Name ?? provider.GetType().Name;
      try
      {
        _logger.LogInformation("Exporting post-run metadata using {Provider}", name);
        provider.Consume(run, _services);
        _logger.LogInformation("{Provider} post-run export completed successfully", name);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(
          ex,
          "Error during {Provider} post-run export: {Message}",
          name,
          ex.Message
        );
      }
    }
  }
}
