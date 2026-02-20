using System.Diagnostics;
using Flowthru.Data;
using Flowthru.Meta.Builders;
using Flowthru.Meta.Models;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

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
  internal List<PipelineNode> Nodes { get; } = new();

  /// <summary>
  /// Nodes grouped by execution layer.
  /// Populated after Build() is called.
  /// </summary>
  internal IReadOnlyList<List<PipelineNode>>? ExecutionLayers { get; private set; }

  /// <summary>
  /// Optional logger for pipeline execution.
  /// </summary>
  public ILogger? Logger { get; set; }

  /// <summary>
  /// Optional service provider for dependency injection into nodes.
  /// </summary>
  /// <remarks>
  /// Set by FlowthruApplication before pipeline execution to enable nodes
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
  /// Tags for categorizing and filtering pipelines.
  /// </summary>
  public IReadOnlyList<string> Tags { get; internal set; } = System.Array.Empty<string>();

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
  /// Indicates whether the pipeline has been built (dependencies analyzed and layers assigned).
  /// </summary>
  public bool IsBuilt => ExecutionLayers != null;

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

    Nodes.Add(node);
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
  /// <exception cref="InvalidOperationException">
  /// Thrown if:
  /// - Multiple nodes write to the same catalog entry
  /// - A circular dependency is detected
  /// </exception>
  public void Build()
  {
    if (IsBuilt)
    {
      Logger?.LogWarning("Pipeline.Build() called on already-built pipeline. Rebuilding...");
    }

    Logger?.LogInformation("Building pipeline with {NodeCount} nodes", Nodes.Count);

    // Analyze dependencies and assign layers
    DependencyAnalyzer.AnalyzeAndAssignLayers(Nodes);

    // Group nodes by layer
    ExecutionLayers = DependencyAnalyzer.GroupByLayer(Nodes).ToList();

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
  /// Validates all external inputs (Layer 0) before pipeline execution.
  /// </summary>
  /// <returns>ValidationResult containing any errors found</returns>
  /// <exception cref="InvalidOperationException">Thrown if pipeline has not been built</exception>
  /// <remarks>
  /// <para>
  /// This method inspects catalog entries that serve as inputs to Layer 0 nodes.
  /// These are pre-existing external data sources (files, databases, APIs) that
  /// must exist and be valid before the pipeline can execute.
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
  /// <item>If entry implements IShallowInspectable → Shallow</item>
  /// <item>Otherwise → None (skip)</item>
  /// </list>
  /// <para>
  /// <strong>Important:</strong> Only Layer 0 inputs are inspected. Intermediate pipeline
  /// outputs (Layer 1+) are never inspected, as they don't exist yet.
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
  public async Task<Data.Validation.ValidationResult> ValidateExternalInputsAsync()
  {
    if (!IsBuilt)
    {
      throw new InvalidOperationException(
        "Pipeline must be built before validation. Call Build() first."
      );
    }

    var result = Data.Validation.ValidationResult.Success();

    // No Layer 0? No external inputs to validate
    if (ExecutionLayers!.Count == 0)
    {
      Logger?.LogInformation("No nodes in pipeline, nothing to validate");
      return result;
    }

    var layer0Nodes = ExecutionLayers[0];
    Logger?.LogInformation(
      "Validating external inputs from {Layer0NodeCount} Layer 0 nodes",
      layer0Nodes.Count
    );

    // Extract all unique input catalog entries from Layer 0 nodes
    // With the tuple-based approach, inputs are always direct ICatalogEntry references
    var externalInputs = layer0Nodes
      .SelectMany(node => node.Inputs)
      .DistinctBy(entry => entry.Label)
      .ToList();

    Logger?.LogInformation(
      "Found {ExternalInputCount} unique external input(s) to validate",
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

        if (inspectionLevel == Data.Validation.InspectionLevel.Shallow)
        {
          // Try shallow inspection
          var shallowInterface = catalogEntry
            .GetType()
            .GetInterfaces()
            .FirstOrDefault(i =>
              i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IShallowInspectable<>)
            );

          if (shallowInterface != null)
          {
            inspectionResult = await InvokeShallowInspectionAsync(catalogEntry, shallowInterface);
          }
          else
          {
            // Entry doesn't support shallow inspection but was configured for it
            inspectionResult = Data.Validation.ValidationResult.Failure(
              catalogEntry.Label,
              Data.Validation.ValidationErrorType.InspectionFailure,
              "Entry does not implement IShallowInspectable<T>"
            );
          }
        }
        else // Deep
        {
          // Try deep inspection
          var deepInterface = catalogEntry
            .GetType()
            .GetInterfaces()
            .FirstOrDefault(i =>
              i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDeepInspectable<>)
            );

          if (deepInterface != null)
          {
            inspectionResult = await InvokeDeepInspectionAsync(catalogEntry, deepInterface);
          }
          else
          {
            // Entry doesn't support deep inspection but was configured for it
            inspectionResult = Data.Validation.ValidationResult.Failure(
              catalogEntry.Label,
              Data.Validation.ValidationErrorType.InspectionFailure,
              "Entry does not implement IDeepInspectable<T>"
            );
          }
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
  /// Invokes shallow inspection on a catalog entry using reflection to handle generic types.
  /// </summary>
  private async Task<Data.Validation.ValidationResult> InvokeShallowInspectionAsync(
    ICatalogEntry catalogEntry,
    Type shallowInterface
  )
  {
    var method = shallowInterface.GetMethod(nameof(IShallowInspectable<object>.InspectShallow));
    var result = method!.Invoke(catalogEntry, new object[] { 10 })!;

    // Handle both Aff<ValidationResult> and Task<ValidationResult> return types
    if (result is LanguageExt.Aff<Data.Validation.ValidationResult> io)
    {
      return await io.Run();
    }
    else if (result is Task<Data.Validation.ValidationResult> task)
    {
      return await task;
    }
    else
    {
      throw new InvalidOperationException(
        $"InspectShallow returned unexpected type: {result.GetType().FullName}"
      );
    }
  }

  /// <summary>
  /// Invokes deep inspection on a catalog entry using reflection to handle generic types.
  /// </summary>
  private async Task<Data.Validation.ValidationResult> InvokeDeepInspectionAsync(
    ICatalogEntry catalogEntry,
    Type deepInterface
  )
  {
    var method = deepInterface.GetMethod(nameof(IDeepInspectable<object>.InspectDeep));
    var result = method!.Invoke(catalogEntry, System.Array.Empty<object>())!;

    // Handle both Aff<ValidationResult> and Task<ValidationResult> return types
    if (result is LanguageExt.Aff<Data.Validation.ValidationResult> io)
    {
      return await io.Run();
    }
    else if (result is Task<Data.Validation.ValidationResult> task)
    {
      return await task;
    }
    else
    {
      throw new InvalidOperationException(
        $"InspectDeep returned unexpected type: {result.GetType().FullName}"
      );
    }
  }

  /// <summary>
  /// /// Builds and executes the pipeline, returning comprehensive execution results.
  /// </summary>
  /// <returns>PipelineResult containing execution status, timing, and node results</returns>
  /// <remarks>
  /// <para>
  /// This is the primary high-level API for executing pipelines. It automatically
  /// calls Build() if the pipeline hasn't been built yet, then executes and tracks results.
  /// </para>
  /// </remarks>
  public async Task<PipelineResult> RunAsync()
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
          var nodeResult = await ExecuteNodeWithTrackingAsync(pipelineNode);
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
  public async Task ExecuteAsync()
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
          await ExecuteNodeAsync(pipelineNode);
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
  /// Executes a single node with execution tracking and returns detailed results.
  /// </summary>
  /// <param name="pipelineNode">The node to execute</param>
  /// <returns>NodeResult with execution details</returns>
  private async Task<NodeResult> ExecuteNodeWithTrackingAsync(PipelineNode pipelineNode)
  {
    var stopwatch = Stopwatch.StartNew();

    try
    {
      // Get input counts for diagnostics (before loading data)
      var inputCountAffs = pipelineNode.Inputs.Select(entry => entry.GetCountAsync());
      var inputCountTasks = inputCountAffs.Select(aff => aff.Run().AsTask());
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
      var inputLoadTasks = inputAffs.Select(aff => aff.Run().AsTask());
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
      var transformFunc = pipelineNode.TransformFunction;
      var resultTask = (Task?)transformFunc.DynamicInvoke(inputParameter);

      if (resultTask == null)
      {
        throw new InvalidOperationException(
          $"Transform function for node {pipelineNode.Label} returned null"
        );
      }

      await resultTask.ConfigureAwait(false);

      // Extract result from Task<TOutput>
      var output = GetTaskResult(resultTask);

      // Save outputs to catalog entries
      // SaveUntyped() accepts T directly (singleton or collection), no unwrapping needed
      if (output != null && pipelineNode.Outputs.Count > 0)
      {
        if (pipelineNode.Outputs.Count == 1)
        {
          // Single output: save directly
          var catalogEntry = pipelineNode.Outputs[0];
          await catalogEntry.SaveUntyped(output).Run();
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

            await catalogEntry.SaveUntyped(outputData!).Run();
          }
        }
      }

      stopwatch.Stop();

      // Get output counts for diagnostics (after saving data)
      var outputCountAffs = pipelineNode.Outputs.Select(entry => entry.GetCountAsync());
      var outputCountTasks = outputCountAffs.Select(aff => aff.Run().AsTask());
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
  private async Task ExecuteNodeAsync(PipelineNode pipelineNode)
  {
    Logger?.LogInformation("Executing node: {NodeName}", pipelineNode.Label);

    try
    {
      // Load inputs from catalog entries
      var inputAffs = pipelineNode.Inputs.Select(entry => entry.LoadUntyped());
      var inputLoadTasks = inputAffs.Select(aff => aff.Run().AsTask());
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
      var transformFunc = pipelineNode.TransformFunction;
      var resultTask = (Task?)transformFunc.DynamicInvoke(inputParameter);

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
          await pipelineNode.Outputs[0].SaveUntyped(output).Run();
        }
        else
        {
          var tupleFields = output.GetType().GetFields();
          for (int i = 0; i < pipelineNode.Outputs.Count; i++)
          {
            var outputData = tupleFields[i].GetValue(output);
            await pipelineNode.Outputs[i].SaveUntyped(outputData!).Run();
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
