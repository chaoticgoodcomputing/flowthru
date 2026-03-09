using System.Diagnostics;
using Flowthru.Data;
using Flowthru.Effects;
using Flowthru.Meta.Builders;
using Flowthru.Meta.Models;
using Microsoft.Extensions.Logging;

namespace Flowthru.Pipelines;

/// <summary>
/// Represents a complete data pipeline with nodes, dependencies, and execution order.
/// </summary>
/// <remarks>
/// <para>
/// A pipeline is a directed acyclic graph (DAG) of transformation nodes.
/// Each node reads data from catalog entries, performs transformations,
/// and writes results back to catalog entries.
/// </para>
/// <para>
/// <strong>Execution Model:</strong>
/// </para>
/// <list type="bullet">
/// <item>Nodes are organized into layers via topological sort</item>
/// <item>Nodes in layer 0 have no dependencies (read external data only)</item>
/// <item>Nodes in layer N depend only on nodes in layers 0..N-1</item>
/// <item>Sequential execution: Execute all nodes in layer order</item>
/// <item>Parallel execution (Phase 2): Execute nodes within same layer concurrently</item>
/// </list>
/// <para>
/// <strong>Single Producer Rule:</strong> Each catalog entry can be written by at most
/// one node. This ensures deterministic execution order and prevents race conditions.
/// </para>
/// </remarks>
public class Pipeline
{
  /// <summary>
  /// All nodes in this pipeline, in the order they were added.
  /// </summary>
  /// <remarks>
  /// Exposed as public to enable validation hooks (Phase 4) to inspect nodes.
  /// The collection is read-only - nodes can only be added via PipelineBuilder.
  /// </remarks>
  public IReadOnlyList<PipelineNode> Nodes => _nodes.AsReadOnly();

  /// <summary>
  /// Internal accessor for the mutable node list. Used by pipeline internals.
  /// </summary>
  internal List<PipelineNode> NodesList => _nodes;

  private readonly List<PipelineNode> _nodes = new();

  /// <summary>
  /// Subset of nodes to execute after slicing is applied.
  /// Null if no slicing was applied (execute all nodes).
  /// </summary>
  private List<PipelineNode>? _slicedNodes;

  /// <summary>
  /// Nodes grouped by execution layer.
  /// Populated after Build() is called.
  /// </summary>
  internal IReadOnlyList<List<PipelineNode>>? ExecutionLayers { get; private set; }

  /// <summary>
  /// The slice strategy applied during the most recent Build() call, if any.
  /// </summary>
  /// <remarks>
  /// Cached to enable metadata export to include slice criteria.
  /// Null if pipeline was built without slicing.
  /// </remarks>
  internal PipelineSliceStrategy? AppliedSlice { get; private set; }

  /// <summary>
  /// Optional logger for pipeline execution.
  /// </summary>
  public ILogger? Logger { get; set; }

  /// <summary>
  /// Optional service provider for dependency injection into nodes.
  /// </summary>
  /// <remarks>
  /// Set by the service layer before pipeline execution to enable nodes
  /// to resolve services (e.g., database connections, external APIs).
  /// </remarks>
  public IServiceProvider? ServiceProvider { get; set; }

  /// <summary>
  /// Pipeline name for identification and logging.
  /// </summary>
  /// <remarks>
  /// Set by PipelineRegistry during pipeline registration.
  /// </remarks>
  public string? Name { get; internal set; }

  /// <summary>
  /// Optional description of what this pipeline does.
  /// </summary>
  public string? Description { get; internal set; }

  /// <summary>
  /// Validation options for this pipeline.
  /// </summary>
  /// <remarks>
  /// Configures how external data sources (Layer 0 inputs) are validated
  /// before pipeline execution begins.
  /// </remarks>
  public Validation.ValidationOptions ValidationOptions { get; internal set; } =
    Validation.ValidationOptions.Default();

  /// <summary>
  /// Validation hooks that run during pre-flight checks.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Extensions can register hooks to validate their own node types during pre-flight.
  /// Hooks are invoked after DAG analysis but before external input inspection.
  /// </para>
  /// <para>
  /// <strong>Hook execution order:</strong>
  /// </para>
  /// <list type="number">
  /// <item>Pipeline.Build() - DAG construction and layer assignment</item>
  /// <item>ValidationHooks.ValidateAsync() - Extension-specific validation</item>
  /// <item>Pipeline.ValidateExternalInputsAsync() - External input inspection</item>
  /// </list>
  /// <para>
  /// <strong>Example (Python extension):</strong>
  /// </para>
  /// <code>
  /// pipeline.ValidationHooks.Add(new PythonNodeValidator(executor, runtime));
  /// </code>
  /// </remarks>
  public List<Validation.IPipelineValidationHook> ValidationHooks { get; } = new();

  /// <summary>
  /// Indicates whether the pipeline has been built (dependencies analyzed and layers assigned).
  /// </summary>
  public bool IsBuilt => ExecutionLayers != null;

  /// <summary>
  /// Gets the sliced subset of nodes (if slicing was applied), otherwise null.
  /// </summary>
  /// <remarks>
  /// Used by metadata export to ensure only nodes that will execute are included in the DAG.
  /// </remarks>
  internal List<PipelineNode>? GetSlicedNodes() => _slicedNodes;

  /// <summary>
  /// Adds a node to the pipeline.
  /// </summary>
  /// <param name="node">The pipeline node to add</param>
  /// <exception cref="InvalidOperationException">Thrown if pipeline has already been built</exception>
  internal void AddNode(PipelineNode node)
  {
    if (IsBuilt)
    {
      throw new InvalidOperationException(
        "Cannot add nodes to a pipeline that has already been built. "
          + "Create a new pipeline or use PipelineBuilder."
      );
    }

    _nodes.Add(node);
  }

  /// <summary>
  /// Merges multiple pipelines into a single pipeline by combining all their nodes.
  /// </summary>
  /// <param name="pipelines">Dictionary of pipeline names to pipeline instances</param>
  /// <returns>A new pipeline containing all nodes from all input pipelines</returns>
  /// <remarks>
  /// <para>
  /// This method creates a new pipeline by combining all nodes from the input pipelines.
  /// Node names are prefixed with their source pipeline name (e.g., "data_processing.PreprocessCompanies")
  /// to ensure uniqueness and maintain traceability in logs.
  /// </para>
  /// <para>
  /// The existing DependencyAnalyzer will automatically resolve cross-pipeline dependencies
  /// based on catalog entries. The single producer rule is enforced - if multiple pipelines
  /// attempt to write to the same catalog entry, Build() will throw an InvalidOperationException.
  /// </para>
  /// </remarks>
  public static Pipeline Merge(Dictionary<string, Pipeline> pipelines)
  {
    var mergedPipeline = new Pipeline
    {
      Name = "Pipelines",
      Description = $"Combined execution of: {string.Join(", ", pipelines.Keys)}",
    };

    // Combine all nodes from all pipelines, prefixing node names with pipeline name
    foreach (var (pipelineName, pipeline) in pipelines)
    {
      foreach (var node in pipeline.Nodes)
      {
        // Create a new node with prefixed name
        var prefixedNode = new PipelineNode(
          label: $"{pipelineName}.{node.Label}",
          description: node.Description,
          node: node.TransformFunction,
          inputs: node.Inputs,
          outputs: node.Outputs
        );

        mergedPipeline.AddNode(prefixedNode);
      }
    }

    return mergedPipeline;
  }

  /// <summary>
  /// Builds the pipeline by analyzing dependencies and assigning execution layers.
  /// Must be called before executing the pipeline.
  /// </summary>
  /// <param name="sliceStrategy">Optional slicing strategy to filter nodes before execution</param>
  /// <exception cref="InvalidOperationException">
  /// Thrown if:
  /// - Multiple nodes write to the same catalog entry (single producer rule)
  /// - Circular dependency is detected
  /// - Slice strategy references non-existent nodes or catalog entries
  /// </exception>
  /// <remarks>
  /// <para>
  /// <strong>Slicing:</strong> If a slicing strategy is provided, only nodes matching
  /// the strategy will be included in the execution. The slice always forms a valid
  /// sub-DAG with all required dependencies.
  /// </para>
  /// </remarks>
  public void Build(PipelineSliceStrategy? sliceStrategy = null)
  {
    if (IsBuilt)
    {
      Logger?.LogWarning("Pipeline.Build() called on already-built pipeline. Rebuilding...");
    }

    Logger?.LogInformation("Building pipeline with {NodeCount} nodes", _nodes.Count);

    // Cache the slice strategy for metadata export
    AppliedSlice = sliceStrategy?.IsSliced == true ? sliceStrategy : null;

    // Step 1: Build dependency graph on the FULL node set
    // This must happen before slicing, as slicing logic traverses dependencies
    DependencyAnalyzer.BuildDependencyGraph(_nodes);

    // Step 2: Apply slicing if requested
    if (sliceStrategy?.IsSliced == true)
    {
      Logger?.LogInformation("Applying pipeline slice strategy");
      _slicedNodes = DependencyAnalyzer.SliceNodes(_nodes, sliceStrategy);
      Logger?.LogInformation(
        "Slice reduced pipeline from {OriginalCount} to {SlicedCount} nodes",
        _nodes.Count,
        _slicedNodes.Count
      );
    }
    else
    {
      _slicedNodes = null; // No slicing - execute all nodes
    }

    // Step 3: Assign execution layers to the final node set (sliced or full)
    // This ensures Layer 0 correctly identifies external inputs in the execution context
    var nodesToExecute = _slicedNodes ?? _nodes;
    DependencyAnalyzer.AssignLayers(nodesToExecute);

    // Step 4: Group nodes by layer for execution
    ExecutionLayers = DependencyAnalyzer.GroupByLayer(nodesToExecute).ToList();

    Logger?.LogInformation(
      "Pipeline built successfully. Execution will proceed in {LayerCount} layers",
      ExecutionLayers.Count
    );

    // Log layer details
    for (int i = 0; i < ExecutionLayers.Count; i++)
    {
      var layerNodes = ExecutionLayers[i];
      Logger?.LogDebug(
        "Layer {LayerIndex}: {NodeCount} nodes ({NodeNames})",
        i,
        layerNodes.Count,
        string.Join(", ", layerNodes.Select(n => n.Label))
      );
    }
  }

  /// <summary>
  /// Exports DAG metadata for this pipeline.
  /// </summary>
  /// <returns>Complete DAG metadata including nodes, catalog entries, and edges</returns>
  /// <exception cref="InvalidOperationException">Thrown if pipeline has not been built</exception>
  /// <remarks>
  /// <para>
  /// This method extracts structural metadata from the built pipeline, creating
  /// a complete representation of the DAG (Directed Acyclic Graph) that can be
  /// serialized to JSON for visualization in Flowthru.Viz.
  /// </para>
  /// <para>
  /// <strong>Prerequisites:</strong> Pipeline must be built before calling this method.
  /// Call Build() first if IsBuilt is false.
  /// </para>
  /// <para>
  /// <strong>Usage:</strong>
  /// </para>
  /// <code>
  /// var pipeline = DataProcessingPipeline.Create(catalog);
  /// pipeline.Build();
  ///
  /// var dag = pipeline.ExportDag();
  /// var json = dag.ToJson();
  /// File.WriteAllText("dag.json", json);
  /// </code>
  /// <para>
  /// This method is non-destructive and idempotent - it can be called multiple
  /// times without affecting the pipeline state.
  /// </para>
  /// </remarks>
  public DagMetadata ExportDag()
  {
    if (!IsBuilt)
    {
      throw new InvalidOperationException(
        "Cannot export DAG metadata from an unbuilt pipeline. Call Build() first."
      );
    }

    Logger?.LogDebug(
      "Exporting DAG metadata for pipeline '{PipelineName}'",
      Name ?? "UnnamedPipeline"
    );

    return DagBuilder.Build(this);
  }

  /// <summary>
  /// Validates all external inputs before pipeline execution.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token for validation I/O operations</param>
  /// <returns>ValidationResult containing any errors found</returns>
  /// <exception cref="InvalidOperationException">Thrown if pipeline has not been built</exception>
  /// <remarks>
  /// <para>
  /// This method inspects catalog entries that are consumed by the pipeline but not
  /// produced by any node in the execution set. These are pre-existing external data
  /// sources (files, databases, APIs) that must exist and be valid before the pipeline
  /// can execute.
  /// </para>
  /// <para>
  /// <strong>Slicing Support:</strong> In sliced pipelines, catalog entries that were
  /// produced by nodes outside the slice are correctly identified as external inputs
  /// and validated. This prevents runtime failures from missing intermediate data.
  /// </para>
  /// <para>
  /// <strong>Inspection Levels:</strong>
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
  /// <strong>Important:</strong> Only external inputs are inspected. Intermediate pipeline
  /// outputs produced within the execution set are never inspected, as they don't exist yet.
  /// </para>
  /// <para>
  /// <strong>Usage:</strong>
  /// </para>
  /// <code>
  /// pipeline.Build();
  /// var validationResult = await pipeline.ValidateExternalInputsAsync();
  /// if (!validationResult.IsValid) {
  ///   // Handle validation errors before execution
  ///   validationResult.ThrowIfInvalid();
  /// }
  /// await pipeline.RunAsync();
  /// </code>
  /// </remarks>
  public async Task<Data.Validation.ValidationResult> ValidateExternalInputsAsync(
    CancellationToken cancellationToken = default
  )
  {
    if (!IsBuilt)
    {
      throw new InvalidOperationException(
        "Pipeline must be built before validation. Call Build() first."
      );
    }

    var result = Data.Validation.ValidationResult.Success();

    // No nodes? No validation needed
    if (ExecutionLayers!.Count == 0)
    {
      Logger?.LogInformation("No nodes in pipeline, nothing to validate");
      return result;
    }

    // Phase 4: Invoke validation hooks (e.g., Python node validation)
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

    // Build a set of catalog entries produced by nodes in the execution set
    var nodesToExecute = ExecutionLayers.SelectMany(layer => layer).ToList();
    var producedEntries = new HashSet<string>(
      nodesToExecute.SelectMany(node => node.Outputs.Select(entry => entry.Label)),
      StringComparer.OrdinalIgnoreCase
    );

    // Find all catalog entries consumed by nodes that are NOT produced by any node
    // These are external inputs in the execution context (including sliced pipelines)
    var externalInputs = nodesToExecute
      .SelectMany(node => node.Inputs)
      .Where(entry => !producedEntries.Contains(entry.Label))
      .DistinctBy(entry => entry.Label)
      .ToList();

    Logger?.LogInformation(
      "Validating {ExternalInputCount} external input(s) (entries consumed but not produced)",
      externalInputs.Count
    );

    // Inspect each external input based on configured or default level
    foreach (var catalogEntry in externalInputs)
    {
      var inspectionLevel = ValidationOptions.GetEffectiveInspectionLevel(catalogEntry);

      if (inspectionLevel == Data.Validation.InspectionLevel.None)
      {
        Logger?.LogDebug(
          "Skipping inspection for '{CatalogKey}' (level: None)",
          catalogEntry.Label
        );
        continue;
      }

      Logger?.LogInformation(
        "Inspecting '{CatalogKey}' with {InspectionLevel} inspection",
        catalogEntry.Label,
        inspectionLevel
      );

      try
      {
        Data.Validation.ValidationResult inspectionResult;

        // All catalog entries support inspection through their storage adapters
        if (inspectionLevel == Data.Validation.InspectionLevel.Shallow)
        {
          inspectionResult = await catalogEntry
            .InspectShallow(sampleSize: 100)
            .Run(cancellationToken);
        }
        else // Deep
        {
          inspectionResult = await catalogEntry.InspectDeep().Run(cancellationToken);
        }

        result.Merge(inspectionResult);

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
            "'{CatalogKey}' passed {InspectionLevel} inspection",
            catalogEntry.Label,
            inspectionLevel
          );
        }
      }
      catch (Exception ex)
      {
        Logger?.LogError(ex, "Exception during inspection of '{CatalogKey}'", catalogEntry.Label);
        result.AddError(
          new Data.Validation.ValidationError(
            catalogEntry.Label,
            Data.Validation.ValidationErrorType.InspectionFailure,
            $"Inspection threw exception: {ex.Message}",
            ex.ToString()
          )
        );
      }
    }

    if (result.IsValid)
    {
      Logger?.LogInformation("All external inputs passed validation");
    }
    else
    {
      Logger?.LogError(
        "Validation failed with {ErrorCount} error(s) across {CatalogCount} catalog entries",
        result.Errors.Count,
        result.Errors.Select(e => e.CatalogKey).Distinct().Count()
      );
    }

    return result;
  }

  /// <summary>
  /// /// Builds and executes the pipeline, returning comprehensive execution results.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token to signal graceful shutdown</param>
  /// <returns>PipelineResult containing execution status, timing, and node results</returns>
  /// <remarks>
  /// <para>
  /// This is the primary high-level API for executing pipelines. It automatically
  /// calls Build() if the pipeline hasn't been built yet, then executes and tracks results.
  /// </para>
  /// </remarks>
  public async Task<PipelineResult> RunAsync(CancellationToken cancellationToken)
  {
    var stopwatch = Stopwatch.StartNew();
    var nodeResults = new Dictionary<string, NodeResult>();

    try
    {
      // Ensure pipeline is built
      if (!IsBuilt)
      {
        Logger?.LogInformation("Building pipeline before execution");
        Build();
      }

      Logger?.LogInformation("Starting pipeline execution via RunAsync()");

      // Execute all layers
      foreach (var layer in ExecutionLayers!)
      {
        Logger?.LogInformation("Executing layer with {NodeCount} nodes", layer.Count);

        foreach (var pipelineNode in layer)
        {
          // Check for cancellation before starting each node
          cancellationToken.ThrowIfCancellationRequested();

          var nodeResult = await ExecuteNodeWithTrackingAsync(pipelineNode, cancellationToken);
          nodeResults[pipelineNode.Label] = nodeResult;

          // If node failed, stop execution
          if (!nodeResult.Success)
          {
            stopwatch.Stop();
            return PipelineResult.CreateFailure(
              stopwatch.Elapsed,
              nodeResult.Exception!,
              nodeResults,
              Name
            );
          }
        }
      }

      stopwatch.Stop();
      Logger?.LogInformation(
        "Pipeline execution completed successfully in {ElapsedMs}ms",
        stopwatch.ElapsedMilliseconds
      );

      return PipelineResult.CreateSuccess(stopwatch.Elapsed, nodeResults, Name);
    }
    catch (OperationCanceledException)
    {
      // Re-throw cancellation exceptions so they propagate to the caller
      // Cancellation is not a failure but a requested abort
      stopwatch.Stop();
      throw;
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      Logger?.LogError(ex, "Pipeline execution failed: {ErrorMessage}", ex.Message);
      return PipelineResult.CreateFailure(stopwatch.Elapsed, ex, nodeResults, Name);
    }
  }

  /// <summary>
  /// Executes the pipeline sequentially, layer by layer.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token to signal graceful shutdown</param>
  /// <returns>Task representing the pipeline execution</returns>
  /// <exception cref="InvalidOperationException">Thrown if pipeline has not been built</exception>
  /// <remarks>
  /// <para>
  /// This method executes nodes in topological order:
  /// 1. Execute all nodes in layer 0 sequentially
  /// 2. Execute all nodes in layer 1 sequentially
  /// 3. Continue until all layers are complete
  /// </para>
  /// <para>
  /// <strong>Note:</strong> This method throws exceptions on failure. For result-based
  /// execution with error handling, use RunAsync() instead.
  /// </para>
  /// <para>
  /// In Phase 2, this will be replaced with a parallel executor that can run
  /// nodes within the same layer concurrently.
  /// </para>
  /// </remarks>
  public async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    if (!IsBuilt)
    {
      throw new InvalidOperationException(
        "Pipeline must be built before execution. Call Build() first."
      );
    }

    Logger?.LogInformation("Starting pipeline execution");

    try
    {
      foreach (var layer in ExecutionLayers!)
      {
        Logger?.LogInformation("Executing layer with {NodeCount} nodes", layer.Count);

        foreach (var pipelineNode in layer)
        {
          // Check for cancellation before starting each node
          cancellationToken.ThrowIfCancellationRequested();

          await ExecuteNodeAsync(pipelineNode, cancellationToken);
        }
      }

      Logger?.LogInformation("Pipeline execution completed successfully");
    }
    catch (Exception ex)
    {
      Logger?.LogError(ex, "Pipeline execution failed: {ErrorMessage}", ex.Message);
      throw;
    }
  }

  /// <summary>
  /// Determines whether a node's transformation function accepts a CancellationToken parameter.
  /// </summary>
  /// <param name="transformFunc">The transformation function delegate</param>
  /// <returns>True if the function accepts a CancellationToken as its last parameter</returns>
  /// <remarks>
  /// Supports optional cancellation awareness in node functions. Nodes can opt-in to cancellation
  /// by accepting a CancellationToken as the last parameter:
  /// <list type="bullet">
  /// <item>Func&lt;TIn, CancellationToken, Task&lt;TOut&gt;&gt; - single input with cancellation</item>
  /// <item>Func&lt;(TIn1, TIn2), CancellationToken, Task&lt;TOut&gt;&gt; - multi-input with cancellation</item>
  /// </list>
  /// </remarks>
  private static bool NodeAcceptsCancellationToken(Delegate transformFunc)
  {
    var invokeMethod = transformFunc.GetType().GetMethod("Invoke");
    var parameters = invokeMethod!.GetParameters();

    // Check if last parameter is CancellationToken
    return parameters.Length >= 2 && parameters[^1].ParameterType == typeof(CancellationToken);
  }

  /// <summary>
  /// Executes a single node with execution tracking and returns detailed results.
  /// </summary>
  /// <param name="pipelineNode">The node to execute</param>
  /// <param name="cancellationToken">Cancellation token for I/O operations</param>
  /// <returns>NodeResult with execution details</returns>
  private async Task<NodeResult> ExecuteNodeWithTrackingAsync(
    PipelineNode pipelineNode,
    CancellationToken cancellationToken
  )
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      // Get input counts for diagnostics (before loading data)
      var inputCountAffs = pipelineNode.Inputs.Select(entry => entry.GetCountAsync());
      var inputCountTasks = inputCountAffs.Select(aff => aff.Run(cancellationToken).AsTask());
      var inputCountResults = await Task.WhenAll(inputCountTasks);
      var inputCounts = inputCountResults;
      var totalInputCount = inputCounts.Sum();

      Logger?.LogInformation(
        "Executing node: {NodeName} (inputs: {InputCount} observations from {EntryCount} entries)",
        pipelineNode.Label,
        totalInputCount,
        pipelineNode.Inputs.Count
      );

      // Load inputs from catalog entries
      // LoadUntyped() returns T directly (singleton or collection), no wrapping needed
      var inputAffs = pipelineNode.Inputs.Select(entry => entry.LoadUntyped());
      var inputLoadTasks = inputAffs.Select(aff => aff.Run(cancellationToken).AsTask());
      var inputResults = await Task.WhenAll(inputLoadTasks);
      var inputs = inputResults;

      // Prepare input parameter for function invocation
      // For single-input nodes: pass data directly (T)
      // For multi-input nodes: construct tuple (T1, T2, ...)
      object inputParameter;
      if (pipelineNode.Inputs.Count == 1)
      {
        // Single input: pass data directly
        inputParameter = inputs[0];
      }
      else
      {
        // Multi-input: construct tuple from loaded values
        // Use the function's actual parameter type to ensure correct tuple signature
        var funcType = pipelineNode.TransformFunction.GetType();
        var invokeMethod = funcType.GetMethod("Invoke");
        var parameters = invokeMethod!.GetParameters();

        if (parameters.Length != 1)
        {
          throw new InvalidOperationException(
            $"Transform function for node {pipelineNode.Label} should have exactly 1 parameter (tuple), but has {parameters.Length}"
          );
        }

        var tupleType = parameters[0].ParameterType;

        // Create tuple instance from input values
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
            $"Failed to create {inputs.Length}-tuple for node {pipelineNode.Label}. "
              + $"Expected tuple type: {tupleType.FullName}, Input types: [{string.Join(", ", inputs.Select(v => v?.GetType().Name ?? "null"))}]",
            ex
          );
        }
      }

      // Invoke transformation function directly via DynamicInvoke
      // Pass cancellation token if the node signature accepts it
      var transformFunc = pipelineNode.TransformFunction;
      object? result;
      if (NodeAcceptsCancellationToken(transformFunc))
      {
        result = transformFunc.DynamicInvoke(inputParameter, cancellationToken);
      }
      else
      {
        result = transformFunc.DynamicInvoke(inputParameter);
      }
      File.AppendAllText(
        "/tmp/flowthru_diag.log",
        $"[{DateTime.Now:HH:mm:ss.fff}] Pipeline: Transform invoked, result type={result?.GetType().Name ?? "null"}\n"
      );

      if (result == null)
      {
        throw new InvalidOperationException(
          $"Transform function for node {pipelineNode.Label} returned null"
        );
      }

      // Check if result is a Task (async function) or direct value (sync function)
      object output;
      if (result is Task resultTask)
      {
        // Async path: await the task and extract result
        await resultTask.ConfigureAwait(false);
        output = GetTaskResult(resultTask);
      }
      else
      {
        // Sync path: use result directly
        output = result;
      }

      // Save outputs to catalog entries
      // SaveUntyped() accepts T directly (singleton or collection), no unwrapping needed
      if (output != null && pipelineNode.Outputs.Count > 0)
      {
        if (pipelineNode.Outputs.Count == 1)
        {
          // Single output: save directly
          var catalogEntry = pipelineNode.Outputs[0];
          await catalogEntry.SaveUntyped(output).Run(cancellationToken);
        }
        else
        {
          // Multi-output: deconstruct tuple
          var tupleType = output.GetType();
          if (!tupleType.IsGenericType || !tupleType.FullName!.StartsWith("System.ValueTuple"))
          {
            throw new InvalidOperationException(
              $"Multi-output node '{pipelineNode.Label}' must return tuple, got: {tupleType.Name}"
            );
          }

          // Get tuple fields (Item1, Item2, ...)
          var tupleFields = tupleType.GetFields();
          if (tupleFields.Length != pipelineNode.Outputs.Count)
          {
            throw new InvalidOperationException(
              $"Multi-output node '{pipelineNode.Label}': Tuple arity ({tupleFields.Length}) doesn't match output count ({pipelineNode.Outputs.Count})"
            );
          }

          // Save each output directly from tuple field
          for (int i = 0; i < pipelineNode.Outputs.Count; i++)
          {
            var catalogEntry = pipelineNode.Outputs[i];
            var field = tupleFields[i];
            var outputData = field.GetValue(output);

            await catalogEntry.SaveUntyped(outputData!).Run(cancellationToken);
          }
        }
      }

      stopwatch.Stop();

      // Get output counts for diagnostics (after saving data)
      var outputCountAffs = pipelineNode.Outputs.Select(entry => entry.GetCountAsync());
      var outputCountTasks = outputCountAffs.Select(aff => aff.Run(cancellationToken).AsTask());
      var outputCountResults = await Task.WhenAll(outputCountTasks);
      var outputCounts = outputCountResults;
      var totalOutputCount = outputCounts.Sum();

      Logger?.LogInformation(
        "Node {NodeName} completed: {InputCount} observations in → {OutputCount} observations out ({ElapsedMs}ms)",
        pipelineNode.Label,
        totalInputCount,
        totalOutputCount,
        stopwatch.ElapsedMilliseconds
      );

      return NodeResult.CreateSuccess(
        pipelineNode.Label,
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
      Logger?.LogError(
        ex,
        "Node {NodeName} failed: {ErrorMessage}",
        pipelineNode.Label,
        ex.Message
      );
      return NodeResult.CreateFailure(pipelineNode.Label, stopwatch.Elapsed, ex);
    }
  }

  /// <summary>
  /// Executes a single node by loading its inputs, invoking the transformation,
  /// and saving its outputs.
  /// </summary>
  /// <param name="pipelineNode">The node to execute</param>
  /// <param name="cancellationToken">Cancellation token for I/O operations</param>
  private async Task ExecuteNodeAsync(
    PipelineNode pipelineNode,
    CancellationToken cancellationToken
  )
  {
    Logger?.LogInformation("Executing node: {NodeName}", pipelineNode.Label);

    try
    {
      // Load inputs from catalog entries
      var inputAffs = pipelineNode.Inputs.Select(entry => entry.LoadUntyped());
      var inputLoadTasks = inputAffs.Select(aff => aff.Run(cancellationToken).AsTask());
      var inputResults = await Task.WhenAll(inputLoadTasks);
      var inputs = inputResults;

      // Prepare input parameter
      object inputParameter;
      if (pipelineNode.Inputs.Count == 1)
      {
        inputParameter = inputs[0];
      }
      else
      {
        // Use the function's actual parameter type to ensure correct tuple signature
        var funcType = pipelineNode.TransformFunction.GetType();
        var invokeMethod = funcType.GetMethod("Invoke");
        var parameters = invokeMethod!.GetParameters();
        var tupleType = parameters[0].ParameterType;

        inputParameter =
          Activator.CreateInstance(tupleType, inputs)
          ?? throw new InvalidOperationException(
            $"Failed to create tuple for node {pipelineNode.Label}"
          );
      }

      // Invoke transformation function
      // Pass cancellation token if the node signature accepts it
      var transformFunc = pipelineNode.TransformFunction;
      Task? resultTask;
      if (NodeAcceptsCancellationToken(transformFunc))
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
          $"Transform function for {pipelineNode.Label} returned null"
        );
      }

      await resultTask.ConfigureAwait(false);
      var output = GetTaskResult(resultTask);

      // Save outputs
      if (output != null && pipelineNode.Outputs.Count > 0)
      {
        if (pipelineNode.Outputs.Count == 1)
        {
          await pipelineNode.Outputs[0].SaveUntyped(output).Run(cancellationToken);
        }
        else
        {
          var tupleFields = output.GetType().GetFields();
          for (int i = 0; i < pipelineNode.Outputs.Count; i++)
          {
            var outputData = tupleFields[i].GetValue(output);
            await pipelineNode.Outputs[i].SaveUntyped(outputData!).Run(cancellationToken);
          }
        }
      }
    }
    catch (Exception ex)
    {
      Logger?.LogError(
        ex,
        "Node {NodeName} failed: {ErrorMessage}",
        pipelineNode.Label,
        ex.Message
      );
      throw;
    }
  }

  /// <summary>
  /// Helper method to dynamically create a tuple type from input values.
  /// </summary>
  private static Type GetTupleType(object[] values)
  {
    return values.Length switch
    {
      2 => typeof(ValueTuple<,>).MakeGenericType(values[0].GetType(), values[1].GetType()),
      3 => typeof(ValueTuple<,,>).MakeGenericType(
        values[0].GetType(),
        values[1].GetType(),
        values[2].GetType()
      ),
      4 => typeof(ValueTuple<,,,>).MakeGenericType(values.Select(v => v.GetType()).ToArray()),
      5 => typeof(ValueTuple<,,,,>).MakeGenericType(values.Select(v => v.GetType()).ToArray()),
      6 => typeof(ValueTuple<,,,,,>).MakeGenericType(values.Select(v => v.GetType()).ToArray()),
      7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(values.Select(v => v.GetType()).ToArray()),
      8 => typeof(ValueTuple<,,,,,,,>).MakeGenericType(values.Select(v => v.GetType()).ToArray()),
      _ => throw new NotSupportedException(
        $"Tuples with {values.Length} elements not supported. Maximum is 8."
      ),
    };
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
