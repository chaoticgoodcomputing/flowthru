using System.Collections.Concurrent;
using System.Diagnostics;
using Flowthru.Core.Data;
using Flowthru.Core.Effects;
using Flowthru.Core.Graph;
using Flowthru.Core.Graph.Meta;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Graph.Scheduling;
using Flowthru.Core.Graph.Validation;
using Flowthru.Core.Results;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Flows;

/// <summary>
/// Represents a complete data Flow with steps, dependencies, and execution order.
/// </summary>
/// <remarks>
/// <para>
/// A Flow is a directed acyclic graph (DAG) of transformation steps.
/// Each step reads data from catalog entries, performs transformations,
/// and writes results back to catalog entries.
/// </para>
/// <para>
/// <strong>Execution Model:</strong>
/// </para>
/// <list type="bullet">
/// <item>Steps are organized into layers via topological sort</item>
/// <item>Steps in layer 0 have no dependencies (read external data only)</item>
/// <item>Steps in layer N depend only on steps in layers 0..N-1</item>
/// <item>Sequential execution: Execute all steps in layer order</item>
/// <item>Parallel execution (Phase 2): Execute steps within same layer concurrently</item>
/// </list>
/// <para>
/// <strong>Single Producer Rule:</strong> Each catalog entry can be written by at most
/// one step. This ensures deterministic execution order and prevents race conditions.
/// </para>
/// </remarks>
public class Flow
{
  /// <summary>
  /// All steps in this flow, in the order they were added.
  /// </summary>
  /// <remarks>
  /// Exposed as public to enable validation hooks (Phase 4) to inspect steps.
  /// The collection is read-only - steps can only be added via FlowBuilder.
  /// </remarks>
  public IReadOnlyList<FlowStep> Steps => _steps.AsReadOnly();

  /// <summary>
  /// Internal accessor for the mutable step list. Used by Flow internals.
  /// </summary>
  internal List<FlowStep> StepsList => _steps;

  private readonly List<FlowStep> _steps = new();

  /// <summary>
  /// Subset of steps to execute after slicing is applied.
  /// Null if no slicing was applied (execute all steps).
  /// </summary>
  private List<FlowStep>? _slicedSteps;

  /// <summary>
  /// Steps grouped by execution layer.
  /// Populated after Build() is called.
  /// </summary>
  internal IReadOnlyList<List<FlowStep>>? ExecutionLayers { get; private set; }

  /// <summary>
  /// The slice strategy applied during the most recent Build() call, if any.
  /// </summary>
  /// <remarks>
  /// Cached to enable metadata export to include slice criteria.
  /// Null if Flow was built without slicing.
  /// </remarks>
  internal FlowSliceStrategy? AppliedSlice { get; private set; }

  /// <summary>
  /// Optional logger for Flow execution.
  /// </summary>
  public ILogger? Logger { get; set; }

  /// <summary>
  /// Optional service provider for dependency injection into steps.
  /// </summary>
  /// <remarks>
  /// Set by the service layer before Flow execution to enable steps
  /// to resolve services (e.g., database connections, external APIs).
  /// </remarks>
  public IServiceProvider? ServiceProvider { get; set; }

  /// <summary>
  /// Flow name for identification and logging.
  /// </summary>
  /// <remarks>
  /// Set by FlowRegistry during Flow registration.
  /// </remarks>
  public string? Name { get; internal set; }

  /// <summary>
  /// Optional description of what this Flow does.
  /// </summary>
  public string? Description { get; internal set; }

  /// <summary>
  /// Validation options for this flow.
  /// </summary>
  /// <remarks>
  /// Configures how external data sources (Layer 0 inputs) are validated
  /// before Flow execution begins.
  /// </remarks>
  public ValidationOptions ValidationOptions { get; internal set; } = ValidationOptions.Default();

  /// <summary>
  /// Validation hooks that run during pre-flight checks.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Extensions can register hooks to validate their own step types during pre-flight.
  /// Hooks are invoked after DAG analysis but before external input inspection.
  /// </para>
  /// <para>
  /// <strong>Hook execution order:</strong>
  /// </para>
  /// <list type="number">
  /// <item>Flow.Build() - DAG construction and layer assignment</item>
  /// <item>ValidationHooks.ValidateAsync() - Extension-specific validation</item>
  /// <item>Flow.ValidateExternalInputsAsync() - External input inspection</item>
  /// </list>
  /// <para>
  /// <strong>Example (Python extension):</strong>
  /// </para>
  /// <code>
  /// flow.ValidationHooks.Add(new PythonStepValidator(executor, runtime));
  /// </code>
  /// </remarks>
  public List<IFlowValidationHook> ValidationHooks { get; } = new();

  /// <summary>
  /// Indicates whether the Flow has been built (dependencies analyzed and layers assigned).
  /// </summary>
  public bool IsBuilt => ExecutionLayers != null;

  /// <summary>
  /// Gets the sliced subset of steps (if slicing was applied), otherwise null.
  /// </summary>
  /// <remarks>
  /// Used by metadata export to ensure only steps that will execute are included in the DAG.
  /// </remarks>
  internal List<FlowStep>? GetSlicedSteps() => _slicedSteps;

  /// <summary>
  /// Adds a step to the flow.
  /// </summary>
  /// <param name="step">The Flow step to add</param>
  /// <exception cref="InvalidOperationException">Thrown if Flow has already been built</exception>
  internal void AddStep(FlowStep step)
  {
    if (IsBuilt)
    {
      throw new InvalidOperationException(
        "Cannot add steps to a flow that has already been built. "
          + "Create a new flow or use FlowBuilder."
      );
    }

    _steps.Add(step);
  }

  /// <summary>
  /// Merges multiple flows into a single Flow by combining all their steps.
  /// </summary>
  /// <param name="flows">Dictionary of flow names to Flow instances</param>
  /// <returns>A new Flow containing all steps from all input flows</returns>
  /// <remarks>
  /// <para>
  /// This method creates a new Flow by combining all steps from the input flows.
  /// Step names are prefixed with their source Flow name (e.g., "data_processing.PreprocessCompanies")
  /// to ensure uniqueness and maintain traceability in logs.
  /// </para>
  /// <para>
  /// The existing DependencyAnalyzer will automatically resolve cross-flow dependencies
  /// based on catalog entries. The single producer rule is enforced - if multiple flows
  /// attempt to write to the same catalog entry, Build() will throw an InvalidOperationException.
  /// </para>
  /// </remarks>
  public static Flow Merge(Dictionary<string, Flow> flows)
  {
    var mergedFlows = new Flow
    {
      Name = "Flows",
      Description = $"Combined execution of: {string.Join(", ", flows.Keys)}",
    };

    // Combine all steps from all flows, prefixing step names with Flow name
    foreach (var (flowName, flow) in flows)
    {
      foreach (var step in flow.Steps)
      {
        // Create a new step with prefixed name
        var prefixedStep = new FlowStep(
          label: $"{flowName}.{step.Label}",
          description: step.Description,
          step: step.TransformFunction,
          inputs: step.Inputs,
          outputs: step.Outputs
        );

        mergedFlows.AddStep(prefixedStep);
      }
    }

    return mergedFlows;
  }

  /// <summary>
  /// Builds the Flow by analyzing dependencies and assigning execution layers.
  /// Must be called before executing the flow.
  /// </summary>
  /// <param name="sliceStrategy">Optional slicing strategy to filter steps before execution</param>
  /// <exception cref="InvalidOperationException">
  /// Thrown if:
  /// - Multiple steps write to the same catalog entry (single producer rule)
  /// - Circular dependency is detected
  /// - Slice strategy references non-existent steps or catalog entries
  /// </exception>
  /// <remarks>
  /// <para>
  /// <strong>Slicing:</strong> If a slicing strategy is provided, only steps matching
  /// the strategy will be included in the execution. The slice always forms a valid
  /// sub-DAG with all required dependencies.
  /// </para>
  /// </remarks>
  public void Build(FlowSliceStrategy? sliceStrategy = null)
  {
    if (IsBuilt)
    {
      Logger?.LogWarning("Flow.Build() called on already-built flow. Rebuilding...");
    }

    Logger?.LogInformation("Building flow with {StepCount} steps", _steps.Count);

    // Cache the slice strategy for metadata export
    AppliedSlice = sliceStrategy?.IsSliced == true ? sliceStrategy : null;

    // Step 1: Build dependency graph on the FULL step set
    // This must happen before slicing, as slicing logic traverses dependencies
    DependencyAnalyzer.BuildDependencyGraph(_steps);

    // Step 2: Apply slicing if requested
    if (sliceStrategy?.IsSliced == true)
    {
      Logger?.LogInformation("Applying flow slice strategy");
      _slicedSteps = DependencyAnalyzer.SliceSteps(_steps, sliceStrategy);
      Logger?.LogInformation(
        "Slice reduced flow from {OriginalCount} to {SlicedCount} steps",
        _steps.Count,
        _slicedSteps.Count
      );
    }
    else
    {
      _slicedSteps = null; // No slicing - execute all steps
    }

    // Step 3: Assign execution layers to the final step set (sliced or full)
    // This ensures Layer 0 correctly identifies external inputs in the execution context
    var stepsToExecute = _slicedSteps ?? _steps;
    DependencyAnalyzer.AssignLayers(stepsToExecute);

    // Step 4: Compute heights for critical-path scheduling
    // Height = longest path to a sink; used by CriticalPathSchedulingStrategy.
    DependencyAnalyzer.ComputeHeights(stepsToExecute);

    // Step 5: Group steps by layer for execution
    ExecutionLayers = DependencyAnalyzer.GroupByLayer(stepsToExecute).ToList();

    Logger?.LogInformation(
      "Flow built successfully. Execution will proceed in {LayerCount} layers",
      ExecutionLayers.Count
    );

    // Log layer details
    for (int i = 0; i < ExecutionLayers.Count; i++)
    {
      var layerSteps = ExecutionLayers[i];
      Logger?.LogDebug(
        "Layer {LayerIndex}: {StepCount} steps ({StepNames})",
        i,
        layerSteps.Count,
        string.Join(", ", layerSteps.Select(n => n.Label))
      );
    }
  }

  /// <summary>
  /// Exports DAG metadata for this Flow.
  /// </summary>
  /// <returns>Complete DAG metadata including steps, catalog entries, and edges</returns>
  /// <exception cref="InvalidOperationException">Thrown if Flow has not been built</exception>
  /// <remarks>
  /// <para>
  /// This method extracts structural metadata from the built Flow , creating
  /// a complete representation of the DAG (Directed Acyclic Graph) that can be
  /// serialized to JSON for visualization in Flowthru.Core.Viz.
  /// </para>
  /// <para>
  /// <strong>Prerequisites:</strong> Flow must be built before calling this method.
  /// Call Build() first if IsBuilt is false.
  /// </para>
  /// <para>
  /// <strong>Usage:</strong>
  /// </para>
  /// <code>
  /// var Flow = DataProcessingFlow.Create(catalog);
  /// flow.Build();
  ///
  /// var dag = flow.ExportDag();
  /// var json = dag.ToJson();
  /// File.WriteAllText("dag.json", json);
  /// </code>
  /// <para>
  /// This method is non-destructive and idempotent - it can be called multiple
  /// times without affecting the Flow state.
  /// </para>
  /// </remarks>
  public DagMetadata ExportDag()
  {
    if (!IsBuilt)
    {
      throw new InvalidOperationException(
        "Cannot export DAG metadata from an unbuilt flow. Call Build() first."
      );
    }

    Logger?.LogDebug("Exporting DAG metadata for flow '{FlowName}'", Name ?? "UnnamedFlow");

    return DagBuilder.Build(this);
  }

  /// <summary>
  /// Validates all external inputs and write destinations before Flow execution.
  /// </summary>
  /// <returns>ValidationResult containing any errors found</returns>
  /// <exception cref="InvalidOperationException">Thrown if Flow has not been built</exception>
  /// <remarks>
  /// <para>
  /// This method runs two validation passes:
  /// </para>
  /// <list type="number">
  /// <item>
  ///   <strong>Source validation:</strong> Inspects catalog entries consumed but not produced
  ///   by any step in the execution set. These are pre-existing external data sources
  ///   (files, databases, APIs) that must exist and be valid before the flow can execute.
  /// </item>
  /// <item>
  ///   <strong>Target validation:</strong> Calls <c>InspectTarget()</c> on all catalog entries
  ///   that steps will write to. This validates write destinations (directories, database tables,
  ///   API endpoints) are accessible before any step executes. Skipped for entries where
  ///   <c>Traits.CanInspect = false</c> or explicitly disabled via
  ///   <c>ValidationOptions.SkipTargetInspection()</c>.
  /// </item>
  /// </list>
  /// <para>
  /// <strong>Slicing Support:</strong> In sliced flows, catalog entries that were
  /// produced by steps outside the slice are correctly identified as external inputs
  /// and validated. This prevents runtime failures from missing intermediate data.
  /// </para>
  /// <para>
  /// <strong>Inspection Levels (source validation):</strong>
  /// </para>
  /// <list type="bullet">
  /// <item><strong>None:</strong> Skip inspection entirely</item>
  /// <item><strong>Shallow:</strong> Validate file exists, check headers/schema, deserialize sample rows</item>
  /// <item><strong>Deep:</strong> Validate all rows in the dataset (expensive!)</item>
  /// </list>
  /// <para>
  /// <strong>Default Behavior:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>If explicitly configured via WithValidation() → use that level</item>
  /// <item>If entry has PreferredInspectionLevel set → use that level</item>
  /// <item>Otherwise → Shallow (all storage adapters support inspection)</item>
  /// </list>
  /// <para>
  /// <strong>Usage:</strong>
  /// </para>
  /// <code>
  /// flow.Build();
  /// var validationResult = await flow.ValidateExternalInputsAsync();
  /// if (!validationResult.IsValid) {
  ///   // Handle validation errors before execution
  ///   validationResult.ThrowIfInvalid();
  /// }
  /// await flow.RunAsync();
  /// </code>
  /// </remarks>
  /// <param name="maxDegreeOfParallelism">
  /// Maximum number of external inputs inspected concurrently. Defaults to 1 (sequential).
  /// Pass the resolved <c>ExecutionOptions.MaxDegreeOfParallelism</c> to fan out I/O-bound
  /// inspections in parallel.
  /// </param>
  /// <param name="cancellationToken">Cancellation token for async operations.</param>
  public async Task<Data.Validation.ValidationResult> ValidateExternalInputsAsync(
    int maxDegreeOfParallelism = 1,
    CancellationToken cancellationToken = default
  )
  {
    if (!IsBuilt)
    {
      throw new InvalidOperationException(
        "Flow must be built before validation. Call Build() first."
      );
    }

    var result = Data.Validation.ValidationResult.Success();

    // No steps? No validation needed
    if (ExecutionLayers!.Count == 0)
    {
      Logger?.LogInformation("No steps in flow, nothing to validate");
      return result;
    }

    // Phase 4: Invoke validation hooks (e.g., Python step validation)
    if (ValidationHooks.Count > 0)
    {
      Logger?.LogInformation("Running {HookCount} validation hook(s)", ValidationHooks.Count);

      foreach (var hook in ValidationHooks)
      {
        try
        {
          var hookResult = await hook.ValidateAsync(this, cancellationToken);
          result.Merge(hookResult);

          if (!hookResult.IsValid)
          {
            Logger?.LogWarning(
              "Validation hook '{HookType}' found {ErrorCount} error(s)",
              hook.GetType().Name,
              hookResult.Errors.Count
            );
          }
        }
        catch (Exception ex)
        {
          Logger?.LogError(ex, "Validation hook '{HookType}' threw exception", hook.GetType().Name);
          result.AddError(
            new Data.Validation.ValidationError(
              "ValidationHook",
              Data.Validation.ValidationErrorType.InspectionFailure,
              $"Validation hook {hook.GetType().Name} threw exception: {ex.Message}",
              ex.ToString()
            )
          );
        }
      }

      // If hooks found errors, return early (no point checking external inputs)
      if (!result.IsValid)
      {
        Logger?.LogError("Validation hooks failed with {ErrorCount} error(s)", result.Errors.Count);
        return result;
      }
    }

    // Build a set of catalog entries produced by stepsin the execution set
    var stepsToExecute = ExecutionLayers.SelectMany(layer => layer).ToList();
    var producedEntries = new HashSet<string>(
      stepsToExecute.SelectMany(step => step.Outputs.Select(entry => entry.Label)),
      StringComparer.OrdinalIgnoreCase
    );

    // Find all catalog entries consumed by steps that are NOT produced by any steps
    // These are external inputs in the execution context (including sliced flows)
    var externalInputs = stepsToExecute
      .SelectMany(step => step.Inputs)
      .Where(entry => !producedEntries.Contains(entry.Label))
      .DistinctBy(entry => entry.Label)
      .ToList();

    Logger?.LogInformation(
      "Validating {ExternalInputCount} external input(s) (entries consumed but not produced)",
      externalInputs.Count
    );

    // Inspect each external input based on configured or default level.
    // Each inspection is independent and I/O-bound, so we fan them out up to
    // maxDegreeOfParallelism. Each task writes to its own local ValidationResult
    // to avoid shared-state contention; results are merged sequentially afterward.
    var inspectionResults = new ConcurrentBag<Data.Validation.ValidationResult>();

    await Parallel.ForEachAsync(
      externalInputs,
      new ParallelOptions
      {
        MaxDegreeOfParallelism = maxDegreeOfParallelism,
        CancellationToken = cancellationToken,
      },
      async (catalogEntry, token) =>
      {
        var entryResult = Data.Validation.ValidationResult.Success();
        var inspectionLevel = ValidationOptions.GetEffectiveInspectionLevel(catalogEntry);

        if (inspectionLevel == Data.Validation.InspectionLevel.None)
        {
          Logger?.LogDebug(
            "Skipping validation for '{CatalogKey}' (level: None)",
            catalogEntry.Label
          );
          inspectionResults.Add(entryResult);
          return;
        }

        Logger?.LogInformation(
          "Validating '{CatalogKey}' with {InspectionLevel} level",
          catalogEntry.Label,
          inspectionLevel
        );

        try
        {
          Data.Validation.ValidationResult inspectionResult;

          // For IItem nodes, use the two-level inspection dispatch
          if (catalogEntry is Data.IItem item)
          {
            inspectionResult =
              inspectionLevel == Data.Validation.InspectionLevel.Shallow
                ? await item.InspectShallow(sampleSize: 100).Run(token)
                : await item.InspectDeep().Run(token);
          }
          else
          {
            // For non-IItem nodes (effects, etc.), use the universal Validate()
            inspectionResult = await catalogEntry.Validate().Run(token);
          }

          entryResult.Merge(inspectionResult);

          if (!inspectionResult.IsValid)
          {
            Logger?.LogWarning(
              "Validation failed for '{CatalogKey}': {ErrorCount} error(s)",
              catalogEntry.Label,
              inspectionResult.Errors.Count
            );
          }
          else
          {
            Logger?.LogInformation(
              "'{CatalogKey}' passed {InspectionLevel} validation",
              catalogEntry.Label,
              inspectionLevel
            );
          }
        }
        catch (Exception ex)
        {
          Logger?.LogError(ex, "Exception during inspection of '{CatalogKey}'", catalogEntry.Label);
          entryResult.AddError(
            new Data.Validation.ValidationError(
              catalogEntry.Label,
              Data.Validation.ValidationErrorType.InspectionFailure,
              $"Inspection threw exception: {ex.Message}",
              ex.ToString()
            )
          );
        }

        inspectionResults.Add(entryResult);
      }
    );

    // Merge all per-entry results sequentially (ValidationResult is not thread-safe)
    foreach (var entryResult in inspectionResults)
    {
      result.Merge(entryResult);
    }

    // Return early if source validation already has errors — no point probing write targets
    if (!result.IsValid)
    {
      Logger?.LogError(
        "Source validation failed with {ErrorCount} error(s) across {CatalogCount} catalog entries",
        result.Errors.Count,
        result.Errors.Select(e => e.CatalogKey).Distinct().Count()
      );
      return result;
    }

    Logger?.LogInformation("All external inputs passed validation");

    // ── Step service inspection pass ───────────────────────────────────────
    // For each unique service type referenced by step.ServiceDependencies, look up a
    // registered IFlowthruInspector<T> sidecar and run the probe. Missing inspectors
    // are logged as warnings (non-fatal); inspector failures merge into the result.
    var serviceInspectionResult = await InspectStepServicesAsync(stepsToExecute, cancellationToken);
    result.Merge(serviceInspectionResult);

    if (!result.IsValid)
    {
      Logger?.LogError(
        "Step service inspection failed with {ErrorCount} error(s)",
        result.Errors.Count
      );
      return result;
    }

    // ── Target validation pass ──────────────────────────────────────────────
    // Validate write destinations for all items produced by steps in this execution set.
    // CanInspect = false means the adapter declared it cannot be probed cheaply (skip it).
    // SkipTargetInspection() provides an explicit per-entry escape hatch.
    var outputEntries = stepsToExecute
      .SelectMany(step => step.Outputs)
      .Where(entry => entry.Traits.CanInspect && ValidationOptions.ShouldInspectTarget(entry))
      .DistinctBy(entry => entry.Label)
      .ToList();

    Logger?.LogInformation("Validating {OutputCount} write destination(s)", outputEntries.Count);

    var targetResults = new ConcurrentBag<Data.Validation.ValidationResult>();

    await Parallel.ForEachAsync(
      outputEntries,
      new ParallelOptions
      {
        MaxDegreeOfParallelism = maxDegreeOfParallelism,
        CancellationToken = cancellationToken,
      },
      async (catalogEntry, token) =>
      {
        if (catalogEntry is not Data.IItem item)
        {
          return;
        }

        Logger?.LogInformation("Validating write destination '{CatalogKey}'", catalogEntry.Label);

        Data.Validation.ValidationResult targetResult;
        try
        {
          targetResult = await item.InspectTarget().Run(token);
        }
        catch (Exception ex)
        {
          Logger?.LogError(
            ex,
            "Exception during target inspection of '{CatalogKey}'",
            catalogEntry.Label
          );
          targetResult = Data.Validation.ValidationResult.Failure(
            catalogEntry.Label,
            Data.Validation.ValidationErrorType.InspectionFailure,
            $"Target inspection threw exception: {ex.Message}",
            ex.ToString()
          );
        }

        if (!targetResult.IsValid)
        {
          Logger?.LogWarning(
            "Write destination '{CatalogKey}' failed target validation: {ErrorCount} error(s)",
            catalogEntry.Label,
            targetResult.Errors.Count
          );
        }
        else
        {
          Logger?.LogInformation(
            "Write destination '{CatalogKey}' passed target validation",
            catalogEntry.Label
          );
        }

        targetResults.Add(targetResult);
      }
    );

    foreach (var targetResult in targetResults)
    {
      result.Merge(targetResult);
    }

    if (result.IsValid)
    {
      Logger?.LogInformation("All write destinations passed target validation");
    }
    else
    {
      Logger?.LogError(
        "Target validation failed with {ErrorCount} error(s) across {CatalogCount} catalog entries",
        result.Errors.Count,
        result.Errors.Select(e => e.CatalogKey).Distinct().Count()
      );
    }

    return result;
  }

  /// <summary>
  /// Runs preflight inspection on the union of services declared by
  /// <see cref="FlowStep.ServiceDependencies"/> across the given steps. Each unique
  /// service type is inspected at most once per execution.
  /// </summary>
  /// <remarks>
  /// <para>
  /// For each service type T:
  /// </para>
  /// <list type="number">
  /// <item>Resolve T from <see cref="ServiceProvider"/>. If null, log warning, skip.</item>
  /// <item>Resolve <c>IFlowthruInspector&lt;T&gt;</c> from <see cref="ServiceProvider"/>.
  /// If absent, log warning, skip — services without inspectors are non-fatal.</item>
  /// <item>If both are present, invoke the inspector. Exceptions are wrapped via
  /// <see cref="Data.Validation.ValidationResult.FromException"/>.</item>
  /// </list>
  /// <para>
  /// When <see cref="ServiceProvider"/> is null (e.g., flows constructed directly in
  /// tests without DI), service inspection is skipped entirely.
  /// </para>
  /// </remarks>
  private async Task<Data.Validation.ValidationResult> InspectStepServicesAsync(
    IReadOnlyList<FlowStep> stepsToExecute,
    CancellationToken cancellationToken
  )
  {
    var aggregate = Data.Validation.ValidationResult.Success();

    if (ServiceProvider is null)
    {
      // No DI container available — nothing to inspect.
      return aggregate;
    }

    // Deduplicate service types across steps so each unique service is probed once.
    var serviceGroups = stepsToExecute
      .SelectMany(step => step.ServiceDependencies.Select(t => (Step: step, ServiceType: t)))
      .GroupBy(pair => pair.ServiceType)
      .ToList();

    if (serviceGroups.Count == 0)
    {
      // No declared service deps — nothing to inspect.
      return aggregate;
    }

    Logger?.LogInformation(
      "Inspecting {ServiceCount} unique service dependency type(s) across {StepCount} step(s)",
      serviceGroups.Count,
      stepsToExecute.Count
    );

    foreach (var group in serviceGroups)
    {
      var serviceType = group.Key;
      var stepsUsingService = string.Join(
        ", ",
        group.Select(g => g.Step.Label).Distinct()
      );

      // Resolve the service instance.
      var serviceInstance = ServiceProvider.GetService(serviceType);
      if (serviceInstance is null)
      {
        Logger?.LogWarning(
          "Service '{ServiceType}' (used by step(s) '{Steps}') is not registered in DI; "
            + "preflight cannot inspect it.",
          serviceType.FullName,
          stepsUsingService
        );
        continue;
      }

      // Resolve the IFlowthruInspector<TService> sidecar.
      var inspectorType = typeof(Effects.IFlowthruInspector<>).MakeGenericType(serviceType);
      var inspector = ServiceProvider.GetService(inspectorType);
      if (inspector is null)
      {
        Logger?.LogWarning(
          "Service '{ServiceType}' (used by step(s) '{Steps}') has no registered "
            + "IFlowthruInspector<{ServiceTypeName}>. Use services.AddFlowthruInspect<{ServiceTypeName}>(...) "
            + "to enable preflight inspection.",
          serviceType.FullName,
          stepsUsingService,
          serviceType.Name,
          serviceType.Name
        );
        continue;
      }

      // Invoke the inspector reflectively. The inspector itself is strongly-typed;
      // only the lookup is reflective.
      try
      {
        var inspectMethod = inspectorType.GetMethod(
          nameof(Effects.IFlowthruInspector<object>.InspectAsync)
        )!;
        var flowIo = (FlowIO<Data.Validation.ValidationResult>)
          inspectMethod.Invoke(inspector, new[] { serviceInstance, (object)cancellationToken })!;
        var probeResult = await flowIo.Run(cancellationToken);
        aggregate.Merge(probeResult);

        if (probeResult.IsValid)
        {
          Logger?.LogInformation(
            "Service '{ServiceType}' passed preflight inspection",
            serviceType.FullName
          );
        }
        else
        {
          Logger?.LogWarning(
            "Service '{ServiceType}' failed preflight inspection: {ErrorCount} error(s)",
            serviceType.FullName,
            probeResult.Errors.Count
          );
        }
      }
      catch (Exception ex)
      {
        Logger?.LogError(
          ex,
          "Exception during preflight inspection of service '{ServiceType}'",
          serviceType.FullName
        );
        aggregate.Merge(
          Data.Validation.ValidationResult.FromException(serviceType.Name, ex)
        );
      }
    }

    return aggregate;
  }

  /// <summary>
  /// Builds and executes the flow, returning comprehensive execution results.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token to signal graceful shutdown</param>
  /// <returns>FlowResult containing execution status, timing, and step results</returns>
  /// <remarks>
  /// This is the primary high-level API for executing flows. It automatically
  /// calls Build() if the Flow hasn't been built yet, then executes via the
  /// task-graph scheduler with default options (sequential, stop on first error).
  /// </remarks>
  public Task<FlowResult> RunAsync(CancellationToken cancellationToken) =>
    RunAsync(new ExecutionOptions(), cancellationToken);

  /// <summary>
  /// Builds and executes the flow with the supplied execution options.
  /// </summary>
  /// <param name="options">Controls parallelism, error policy, and other execution behaviour.</param>
  /// <param name="cancellationToken">Cancellation token to signal graceful shutdown</param>
  /// <returns>FlowResult containing execution status, timing, and step results</returns>
  /// <remarks>
  /// <para>
  /// Steps are dispatched by the task-graph scheduler as soon as all their dependencies
  /// complete, up to <see cref="ExecutionOptions.MaxDegreeOfParallelism"/> concurrent steps.
  /// </para>
  /// <para>
  /// With <c>MaxDegreeOfParallelism = 1</c> (default) execution is sequential and
  /// behaviourally equivalent to the previous layer-by-layer loop.
  /// </para>
  /// </remarks>
  public async Task<FlowResult> RunAsync(
    ExecutionOptions options,
    CancellationToken cancellationToken
  )
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      // Ensure Flow is built
      if (!IsBuilt)
      {
        Logger?.LogInformation("Building flow before execution");
        Build();
      }

      var stepList = (IReadOnlyList<FlowStep>)(_slicedSteps ?? _steps);

      // Resolve MaxDegreeOfParallelism: null means "not specified", default to 1 (sequential).
      var parallelism = options.MaxDegreeOfParallelism ?? 1;

      Logger?.LogInformation(
        "Starting flow execution via RunAsync() ({StepCount} steps, parallelism={Parallelism})",
        stepList.Count,
        parallelism
      );

      var executor = new Graph.TaskGraphExecutor(
        stepList,
        parallelism,
        ExecuteStepWithTrackingAsync,
        options.SchedulingStrategy
          ?? (
            parallelism == 1 ? new FifoSchedulingStrategy() : new CriticalPathSchedulingStrategy()
          ),
        Logger
      );

      var stepResults = await executor.RunAsync(options.StopOnFirstError, cancellationToken);

      // Surface the first failure when StopOnFirstError is true.
      var firstFailure = stepResults.Values.FirstOrDefault(r => !r.Success);
      if (firstFailure != null)
      {
        stopwatch.Stop();
        return FlowResult.CreateFailure(
          stopwatch.Elapsed,
          firstFailure.Exception!,
          stepResults,
          Name
        );
      }

      stopwatch.Stop();
      Logger?.LogInformation(
        "Flow execution completed successfully in {ElapsedMs}ms",
        stopwatch.ElapsedMilliseconds
      );

      return FlowResult.CreateSuccess(stopwatch.Elapsed, stepResults, Name);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      // Caller-initiated abort — re-throw cleanly so hosts can distinguish a
      // requested cancellation from a runtime failure.
      stopwatch.Stop();
      throw;
    }
    catch (OperationCanceledException ex)
    {
      // Cancellation propagated past the executor without the caller asking
      // for it — i.e., an internal stop-on-first-error cascade leaked the
      // cancel out instead of cleanly returning partial results. By the
      // FlowResult contract this is an unexpected escape, so wrap it in
      // FlowExecutionEscapedException so the runtime-error classifier reports
      // it as a possible framework bug instead of an external cancellation.
      stopwatch.Stop();
      var wrapped = new FlowExecutionEscapedException(
        "Flow execution aborted by an unexpected cancellation that was not "
          + "caller-initiated. This indicates a possible bug in Flowthru's "
          + "execution path — the original cancellation is preserved as the "
          + "inner exception.",
        ex
      );
      Logger?.LogError(wrapped, "Flow execution aborted unexpectedly: {ErrorMessage}", ex.Message);
      return FlowResult.CreateFailure(
        stopwatch.Elapsed,
        wrapped,
        new Dictionary<string, StepResult>(),
        Name
      );
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      Logger?.LogError(ex, "Flow execution failed: {ErrorMessage}", ex.Message);
      return FlowResult.CreateFailure(
        stopwatch.Elapsed,
        ex,
        new Dictionary<string, StepResult>(),
        Name
      );
    }
  }

  /// <summary>
  /// Executes the flow in topological order, throwing on the first step failure.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token to signal graceful shutdown</param>
  /// <returns>Task representing the flow execution</returns>
  /// <exception cref="InvalidOperationException">Thrown if the flow has not been built</exception>
  /// <remarks>
  /// For structured result-based execution (including parallel), use <see cref="RunAsync(CancellationToken)"/>.
  /// </remarks>
  public async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    if (!IsBuilt)
    {
      throw new InvalidOperationException(
        "Flow must be built before execution. Call Build() first."
      );
    }

    Logger?.LogInformation("Starting flow execution");

    try
    {
      var result = await RunAsync(cancellationToken);

      if (!result.Success)
      {
        throw result.Exception
          ?? new InvalidOperationException("Flow execution failed with no exception details.");
      }

      Logger?.LogInformation("Flow execution completed successfully");
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      Logger?.LogError(ex, "Flow execution failed: {ErrorMessage}", ex.Message);
      throw;
    }
  }

  /// <summary>
  /// Determines whether a step's transformation function accepts a CancellationToken parameter.
  /// </summary>
  /// <param name="transformFunc">The transformation function delegate</param>
  /// <returns>True if the function accepts a CancellationToken as its last parameter</returns>
  /// <remarks>
  /// Supports optional cancellation awareness in step functions. Steps can opt-in to cancellation
  /// by accepting a CancellationToken as the last parameter:
  /// <list type="bullet">
  /// <item>Func&lt;CancellationToken, Task&gt; - zero-input with cancellation</item>
  /// <item>Func&lt;CancellationToken, Task&lt;TOut&gt;&gt; - zero-input, single-output with cancellation</item>
  /// <item>Func&lt;TIn, CancellationToken, Task&lt;TOut&gt;&gt; - single input with cancellation</item>
  /// <item>Func&lt;(TIn1, TIn2), CancellationToken, Task&lt;TOut&gt;&gt; - multi-input with cancellation</item>
  /// </list>
  /// </remarks>
  private static bool StepAcceptsCancellationToken(Delegate transformFunc)
  {
    var invokeMethod = transformFunc.GetType().GetMethod("Invoke");
    var parameters = invokeMethod!.GetParameters();

    // Check if last parameter is CancellationToken. Length >= 1 covers zero-input
    // transforms whose only parameter is the CT itself (Func<CancellationToken, Task>).
    return parameters.Length >= 1 && parameters[^1].ParameterType == typeof(CancellationToken);
  }

  /// <summary>
  /// Executes a single step with execution tracking and returns detailed results.
  /// </summary>
  /// <param name="flowStep">The step to execute</param>
  /// <param name="cancellationToken">Cancellation token for I/O operations</param>
  /// <returns>StepResult with execution details</returns>
  private async Task<StepResult> ExecuteStepWithTrackingAsync(
    FlowStep flowStep,
    CancellationToken cancellationToken
  )
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      // Get input counts for diagnostics (before loading data)
      var inputCountAffs = flowStep
        .Inputs.OfType<Data.IItem>()
        .Select(entry => entry.GetCountAsync());
      var inputCountTasks = inputCountAffs.Select(aff => aff.Run(cancellationToken).AsTask());
      var inputCountResults = await Task.WhenAll(inputCountTasks);
      var inputCounts = inputCountResults;
      var totalInputCount = inputCounts.Sum();

      Logger?.LogInformation(
        "Executing step: {StepName} (inputs: {InputCount} observations from {EntryCount} entries)",
        flowStep.Label,
        totalInputCount,
        flowStep.Inputs.Count
      );

      // Load inputs from catalog entries
      // ProduceUntyped() returns T directly (singleton or collection), no wrapping needed
      var inputAffs = flowStep.Inputs.Select(entry => entry.ProduceUntyped());
      var inputLoadTasks = inputAffs.Select(aff => aff.Run(cancellationToken).AsTask());
      var inputResults = await Task.WhenAll(inputLoadTasks);
      var inputs = inputResults;

      // Prepare input parameter for function invocation
      // For zero-input steps: no parameter (transform takes no input)
      // For single-input steps: pass data directly (T)
      // For multi-input steps: construct tuple (T1, T2, ...) or pass as object[] for fan-in
      object? inputParameter = null;
      if (flowStep.Inputs.Count == 1)
      {
        // Single input: pass data directly, unless this is a fan-in wrapper (Func<object[], TOut>)
        // in which case the step still expects an object[] even with a single entry.
        var singleFuncType = flowStep.TransformFunction.GetType();
        var singleParams = singleFuncType.GetMethod("Invoke")!.GetParameters();
        if (singleParams.Length == 1 && singleParams[0].ParameterType == typeof(object[]))
        {
          inputParameter = (object)inputs; // fan-in wrapper with single shard
        }
        else
        {
          inputParameter = inputs[0];
        }
      }
      else if (flowStep.Inputs.Count > 1)
      {
        // Multi-input: construct tuple from loaded values, or pass as object[] for fan-in steps.
        // Use the function's actual parameter type to ensure correct tuple signature.
        var funcType = flowStep.TransformFunction.GetType();
        var invokeMethod = funcType.GetMethod("Invoke");
        var parameters = invokeMethod!.GetParameters();

        // Strip trailing CancellationToken when present so we look at "data" parameters only.
        var dataParameterCount =
          parameters.Length >= 1 && parameters[^1].ParameterType == typeof(CancellationToken)
            ? parameters.Length - 1
            : parameters.Length;
        if (dataParameterCount != 1)
        {
          throw new InvalidOperationException(
            $"Transform function for step {flowStep.Label} should have exactly 1 data parameter (tuple), but has {dataParameterCount}"
          );
        }

        var paramType = parameters[0].ParameterType;

        if (paramType == typeof(object[]))
        {
          // Fan-in step: the FlowBuilder wraps Func<IReadOnlyList<TIn>, TOut> into
          // Func<object[], TOut>. Pass the loaded array as a single boxed object so
          // DynamicInvoke receives new object[]{ inputs } — no array-spreading occurs.
          inputParameter = (object)inputs;
        }
        else
        {
          // Standard multi-input: construct ValueTuple from loaded values
          var tupleType = paramType;

          try
          {
            inputParameter =
              Activator.CreateInstance(tupleType, inputs)
              ?? throw new InvalidOperationException(
                $"Activator returned null for tuple type {tupleType.Name}"
              );
          }
          catch (Exception ex)
          {
            throw new InvalidOperationException(
              $"Failed to create {inputs.Length}-tuple for step {flowStep.Label}. "
                + $"Expected tuple type: {tupleType.FullName}, Input types: [{string.Join(", ", inputs.Select(v => v?.GetType().Name ?? "null"))}]",
              ex
            );
          }
        }
      }

      // Invoke transformation function directly via DynamicInvoke
      // Pass cancellation token if the step signature accepts it
      var transformFunc = flowStep.TransformFunction;
      var hasCancellationToken = StepAcceptsCancellationToken(transformFunc);
      object? result;
      if (flowStep.Inputs.Count == 0)
      {
        // Zero-input: invoke with either (CT) or () depending on signature.
        result = hasCancellationToken
          ? transformFunc.DynamicInvoke(cancellationToken)
          : transformFunc.DynamicInvoke();
      }
      else if (hasCancellationToken)
      {
        result = transformFunc.DynamicInvoke(inputParameter, cancellationToken);
      }
      else
      {
        result = transformFunc.DynamicInvoke(inputParameter);
      }

      // Result handling. Zero-output transforms (Action / Func<Task>) may return null
      // (sync void) or a non-generic Task (async void); both are valid.
      object? output;
      if (flowStep.Outputs.Count == 0)
      {
        if (result is Task voidTask)
        {
          await voidTask.ConfigureAwait(false);
        }
        output = null;
      }
      else if (result == null)
      {
        throw new InvalidOperationException(
          $"Transform function for step {flowStep.Label} returned null"
        );
      }
      else if (result is Task resultTask)
      {
        // Async path with output: await and extract Task<T>.Result.
        await resultTask.ConfigureAwait(false);
        output = GetTaskResult(resultTask);
      }
      else
      {
        // Sync path with output: use result directly.
        output = result;
      }

      // Save outputs to catalog entries
      // ConsumeUntyped() accepts T directly (singleton or collection), no unwrapping needed
      if (output != null && flowStep.Outputs.Count > 0)
      {
        if (flowStep.Outputs.Count == 1)
        {
          // Single output: save directly
          var catalogEntry = flowStep.Outputs[0];
          await catalogEntry.ConsumeUntyped(output).Run(cancellationToken);
        }
        else
        {
          // Multi-output: deconstruct tuple
          var tupleType = output.GetType();
          if (!tupleType.IsGenericType || !tupleType.FullName!.StartsWith("System.ValueTuple"))
          {
            throw new InvalidOperationException(
              $"Multi-output step '{flowStep.Label}' must return tuple, got: {tupleType.Name}"
            );
          }

          // Get tuple fields (Item1, Item2, ...)
          var tupleFields = tupleType.GetFields();
          if (tupleFields.Length != flowStep.Outputs.Count)
          {
            throw new InvalidOperationException(
              $"Multi-output step '{flowStep.Label}': Tuple arity ({tupleFields.Length}) doesn't match output count ({flowStep.Outputs.Count})"
            );
          }

          // Save each output directly from tuple field
          for (int i = 0; i < flowStep.Outputs.Count; i++)
          {
            var catalogEntry = flowStep.Outputs[i];
            var field = tupleFields[i];
            var outputData = field.GetValue(output);

            await catalogEntry.ConsumeUntyped(outputData!).Run(cancellationToken);
          }
        }
      }

      stopwatch.Stop();

      // Get output counts for diagnostics (after saving data)
      var outputCountAffs = flowStep
        .Outputs.OfType<Data.IItem>()
        .Select(entry => entry.GetCountAsync());
      var outputCountTasks = outputCountAffs.Select(aff => aff.Run(cancellationToken).AsTask());
      var outputCountResults = await Task.WhenAll(outputCountTasks);
      var outputCounts = outputCountResults;
      var totalOutputCount = outputCounts.Sum();

      Logger?.LogInformation(
        "Step {StepName} completed: {InputCount} observations in → {OutputCount} observations out ({ElapsedMs}ms)",
        flowStep.Label,
        totalInputCount,
        totalOutputCount,
        stopwatch.ElapsedMilliseconds
      );

      return StepResult.CreateSuccess(
        flowStep.Label,
        stopwatch.Elapsed,
        totalInputCount,
        totalOutputCount
      );
    }
    catch (OperationCanceledException ex)
    {
      // Normalize all cancellation exceptions to OperationCanceledException for consistent API
      // TaskCanceledException is a subclass, but we want uniform exception types for consumers
      if (ex is TaskCanceledException)
      {
        throw new OperationCanceledException(ex.Message, ex.InnerException, ex.CancellationToken);
      }
      throw;
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      // Unwrap reflection's TargetInvocationException so the surfaced exception is the
      // one the user's transform actually threw. Sync transforms invoked via DynamicInvoke
      // are wrapped; async transforms unwrap automatically via await — this normalizes
      // the two paths so consumers always see the original.
      var surfacedException = ex is System.Reflection.TargetInvocationException tie
        && tie.InnerException is not null
        ? tie.InnerException
        : ex;
      Logger?.LogError(
        surfacedException,
        "Step {StepName} failed: {ErrorMessage}",
        flowStep.Label,
        surfacedException.Message
      );
      return StepResult.CreateFailure(flowStep.Label, stopwatch.Elapsed, surfacedException);
    }
  }

  /// <summary>
  /// Executes a single step by loading its inputs, invoking the transformation,
  /// and saving its outputs.
  /// </summary>
  /// <param name="flowStep">The step to execute</param>
  /// <param name="cancellationToken">Cancellation token for I/O operations</param>
  private async Task ExecuteStepAsync(FlowStep flowStep, CancellationToken cancellationToken)
  {
    Logger?.LogInformation("Executing step: {StepName}", flowStep.Label);

    try
    {
      // Load inputs from catalog entries
      var inputAffs = flowStep.Inputs.Select(entry => entry.ProduceUntyped());
      var inputLoadTasks = inputAffs.Select(aff => aff.Run(cancellationToken).AsTask());
      var inputResults = await Task.WhenAll(inputLoadTasks);
      var inputs = inputResults;

      // Prepare input parameter
      object? inputParameter = null;
      if (flowStep.Inputs.Count == 1)
      {
        // Single input, unless this is a fan-in wrapper (Func<object[], TOut>)
        var singleFuncType = flowStep.TransformFunction.GetType();
        var singleParams = singleFuncType.GetMethod("Invoke")!.GetParameters();
        if (singleParams.Length == 1 && singleParams[0].ParameterType == typeof(object[]))
        {
          inputParameter = (object)inputs;
        }
        else
        {
          inputParameter = inputs[0];
        }
      }
      else if (flowStep.Inputs.Count > 1)
      {
        // Use the function's actual parameter type to ensure correct tuple signature
        var funcType = flowStep.TransformFunction.GetType();
        var invokeMethod = funcType.GetMethod("Invoke");
        var parameters = invokeMethod!.GetParameters();
        var paramType = parameters[0].ParameterType;

        if (paramType == typeof(object[]))
        {
          // Fan-in step: pass the loaded array as a single boxed object.
          inputParameter = (object)inputs;
        }
        else
        {
          var tupleType = paramType;
          inputParameter =
            Activator.CreateInstance(tupleType, inputs)
            ?? throw new InvalidOperationException(
              $"Failed to create tuple for step {flowStep.Label}"
            );
        }
      }

      // Invoke transformation function
      // Pass cancellation token if the step signature accepts it
      var transformFunc = flowStep.TransformFunction;
      var hasCancellationToken = StepAcceptsCancellationToken(transformFunc);
      Task? resultTask;
      if (flowStep.Inputs.Count == 0)
      {
        // Zero-input: invoke with either (CT) or () depending on signature.
        resultTask = (Task?)(hasCancellationToken
          ? transformFunc.DynamicInvoke(cancellationToken)
          : transformFunc.DynamicInvoke());
      }
      else if (hasCancellationToken)
      {
        resultTask = (Task?)transformFunc.DynamicInvoke(inputParameter, cancellationToken);
      }
      else
      {
        resultTask = (Task?)transformFunc.DynamicInvoke(inputParameter);
      }

      if (resultTask == null)
      {
        throw new InvalidOperationException(
          $"Transform function for {flowStep.Label} returned null"
        );
      }

      await resultTask.ConfigureAwait(false);

      // Zero-output transforms return a non-generic Task (void). No output to extract or save.
      if (flowStep.Outputs.Count == 0)
      {
        return;
      }

      var output = GetTaskResult(resultTask);

      // Save outputs
      if (output != null && flowStep.Outputs.Count > 0)
      {
        if (flowStep.Outputs.Count == 1)
        {
          await flowStep.Outputs[0].ConsumeUntyped(output).Run(cancellationToken);
        }
        else
        {
          var tupleFields = output.GetType().GetFields();
          for (int i = 0; i < flowStep.Outputs.Count; i++)
          {
            var outputData = tupleFields[i].GetValue(output);
            await flowStep.Outputs[i].ConsumeUntyped(outputData!).Run(cancellationToken);
          }
        }
      }
    }
    catch (Exception ex)
    {
      Logger?.LogError(ex, "Step {StepName} failed: {ErrorMessage}", flowStep.Label, ex.Message);
      throw;
    }
  }

  /// <summary>
  /// Helper method to extract the result from a Task&lt;T&gt;.
  /// </summary>
  private static object GetTaskResult(Task task)
  {
    var taskType = task.GetType();
    if (!taskType.IsGenericType)
    {
      throw new InvalidOperationException("Task must be Task<T>, not Task");
    }
    var resultProperty = taskType.GetProperty("Result")!;
    return resultProperty.GetValue(task)!;
  }
}
