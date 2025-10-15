using System.Diagnostics;
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
        "Cannot add nodes to a pipeline that has already been built. " +
        "Create a new pipeline or use PipelineBuilder.");
    }

    Nodes.Add(node);
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
      ExecutionLayers.Count);

    // Log layer details
    for (int i = 0; i < ExecutionLayers.Count; i++)
    {
      var layerNodes = ExecutionLayers[i];
      Logger?.LogDebug(
        "Layer {LayerIndex}: {NodeCount} nodes ({NodeNames})",
        i,
        layerNodes.Count,
        string.Join(", ", layerNodes.Select(n => n.Name)));
    }
  }

  /// <summary>
  /// Builds and executes the pipeline, returning comprehensive execution results.
  /// </summary>
  /// <returns>PipelineResult containing execution status, timing, and node results</returns>
  /// <remarks>
  /// <para>
  /// This is the primary high-level API for executing pipelines. It automatically
  /// calls Build() if the pipeline hasn't been built yet, then executes and tracks results.
  /// </para>
  /// <para>
  /// <strong>Usage Pattern:</strong>
  /// </para>
  /// <code>
  /// var result = await pipeline.RunAsync();
  /// 
  /// if (result.Success)
  /// {
  ///     Console.WriteLine($"Pipeline completed in {result.ExecutionTime.TotalSeconds:F2}s");
  /// }
  /// else
  /// {
  ///     Console.WriteLine($"Pipeline failed: {result.Exception?.Message}");
  /// }
  /// </code>
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
          nodeResults[pipelineNode.Name] = nodeResult;

          // If node failed, stop execution
          if (!nodeResult.Success)
          {
            stopwatch.Stop();
            return PipelineResult.CreateFailure(
              stopwatch.Elapsed,
              nodeResult.Exception!,
              nodeResults);
          }
        }
      }

      stopwatch.Stop();
      Logger?.LogInformation(
        "Pipeline execution completed successfully in {ElapsedMs}ms",
        stopwatch.ElapsedMilliseconds);

      return PipelineResult.CreateSuccess(stopwatch.Elapsed, nodeResults);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      Logger?.LogError(ex, "Pipeline execution failed: {ErrorMessage}", ex.Message);
      return PipelineResult.CreateFailure(stopwatch.Elapsed, ex, nodeResults);
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
        "Pipeline must be built before execution. Call Build() first.");
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
      var inputCountTasks = pipelineNode.Inputs.Select(entry => entry.GetCountAsync());
      var inputCounts = await Task.WhenAll(inputCountTasks);
      var totalInputCount = inputCounts.Sum();

      Logger?.LogInformation(
        "Executing node: {NodeName} (inputs: {InputCount} observations from {EntryCount} entries)",
        pipelineNode.Name,
        totalInputCount,
        pipelineNode.Inputs.Count);

      // Load inputs from catalog entries
      var inputTasks = pipelineNode.Inputs.Select(entry => entry.LoadUntyped());
      var inputs = await Task.WhenAll(inputTasks);

      // Invoke node transformation via reflection
      // Find ExecuteAsync method on the node instance
      var nodeType = pipelineNode.NodeInstance.GetType();
      var executeMethod = nodeType.GetMethod("ExecuteAsync",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

      if (executeMethod == null)
      {
        throw new InvalidOperationException(
          $"Node {pipelineNode.Name} does not have an ExecuteAsync method");
      }

      // Prepare input parameter
      // For single-input nodes: pass the data directly as IEnumerable<TInput>
      // For multi-input nodes: wrap in singleton IEnumerable containing a composite input object
      object inputParameter;
      if (pipelineNode.Inputs.Count == 1 && pipelineNode.InputMappings == null)
      {
        // Single input: pass data directly
        inputParameter = inputs[0];
      }
      else
      {
        // Multi-input: create composite input object using InputMappings
        // The ExecuteAsync signature is: Task<IEnumerable<TOutput>> ExecuteAsync(IEnumerable<TInput> input)
        // For multi-input, TInput is a record/class with properties for each catalog entry
        // We need to wrap the composite object in a singleton IEnumerable

        // Get TInput type from ExecuteAsync method signature
        var executeParams = executeMethod.GetParameters();
        if (executeParams.Length != 1)
        {
          throw new InvalidOperationException(
            $"ExecuteAsync for node {pipelineNode.Name} should have exactly one parameter");
        }

        var inputEnumerableType = executeParams[0].ParameterType; // IEnumerable<TInput>
        var inputItemType = inputEnumerableType.GetGenericArguments()[0]; // TInput

        // Create instance of TInput
        var compositeInput = Activator.CreateInstance(inputItemType);
        if (compositeInput == null)
        {
          throw new InvalidOperationException(
            $"Failed to create instance of {inputItemType.Name} for node {pipelineNode.Name}");
        }

        // Map loaded data to properties using InputMappings
        if (pipelineNode.InputMappings != null)
        {
          foreach (var mapping in pipelineNode.InputMappings)
          {
            if (mapping is Mapping.CatalogPropertyMapping propertyMapping)
            {
              // Find the corresponding loaded data
              var catalogEntry = propertyMapping.CatalogEntry;
              var inputIndex = pipelineNode.Inputs.ToList().FindIndex(e => e.Key == catalogEntry.Key);

              if (inputIndex >= 0 && inputIndex < inputs.Length)
              {
                var data = inputs[inputIndex];

                Logger?.LogDebug(
                  "Mapping catalog entry '{Key}' to property '{PropertyName}' on {TypeName}",
                  catalogEntry.Key,
                  propertyMapping.Property.Name,
                  inputItemType.Name);

                // Set the property value
                if (propertyMapping.Property.CanWrite)
                {
                  propertyMapping.Property.SetValue(compositeInput, data);

                  Logger?.LogDebug(
                    "Set property '{PropertyName}' with data of type {DataType}",
                    propertyMapping.Property.Name,
                    data?.GetType().Name ?? "null");
                }
              }
            }
          }
        }

        // Wrap in singleton enumerable
        var listType = typeof(List<>).MakeGenericType(inputItemType);
        var list = (System.Collections.IList?)Activator.CreateInstance(listType);
        list?.Add(compositeInput);
        inputParameter = list!;
      }

      // Invoke ExecuteAsync and await the result
      var executeTask = (Task?)executeMethod.Invoke(pipelineNode.NodeInstance, new[] { inputParameter });
      if (executeTask == null)
      {
        throw new InvalidOperationException(
          $"ExecuteAsync invocation for node {pipelineNode.Name} returned null");
      }

      await executeTask.ConfigureAwait(false);

      // Extract result from Task<TOutput>
      var resultProperty = executeTask.GetType().GetProperty("Result");
      var output = resultProperty?.GetValue(executeTask);

      // Save outputs to catalog entries
      if (output != null && pipelineNode.Outputs.Count > 0)
      {
        // For single output nodes
        if (pipelineNode.Outputs.Count == 1)
        {
          await pipelineNode.Outputs[0].SaveUntyped(output);
        }
        else
        {
          // For multi-output nodes, use OutputMappings to correctly map properties to catalog entries
          if (pipelineNode.OutputMappings == null || pipelineNode.OutputMappings.Count == 0)
          {
            throw new InvalidOperationException(
              $"Node '{pipelineNode.Name}' has multiple outputs but no OutputMappings configured.");
          }

          // Multi-output nodes return IEnumerable<TOutputSchema>, extract the single item
          if (output is not System.Collections.IEnumerable outputEnumerable)
          {
            throw new InvalidOperationException(
              $"Multi-output node '{pipelineNode.Name}' returned non-enumerable output: {output.GetType().Name}");
          }

          var outputItem = outputEnumerable.Cast<object>().FirstOrDefault();
          if (outputItem == null)
          {
            throw new InvalidOperationException(
              $"Multi-output node '{pipelineNode.Name}' returned empty output collection");
          }

          foreach (var mapping in pipelineNode.OutputMappings)
          {
            // OutputMappings should be CatalogPropertyMapping instances
            if (mapping is not Mapping.CatalogPropertyMapping propertyMapping)
            {
              throw new InvalidOperationException(
                $"Node '{pipelineNode.Name}' has an invalid mapping type: {mapping.GetType().Name}");
            }

            // Use the property info from the mapping (which has the correct property name)
            var propertyValue = propertyMapping.Property.GetValue(outputItem);
            if (propertyValue != null)
            {
              await propertyMapping.CatalogEntry.SaveUntyped(propertyValue);
            }
          }
        }
      }

      stopwatch.Stop();

      // Get output counts for diagnostics (after saving data)
      var outputCountTasks = pipelineNode.Outputs.Select(entry => entry.GetCountAsync());
      var outputCounts = await Task.WhenAll(outputCountTasks);
      var totalOutputCount = outputCounts.Sum();

      Logger?.LogInformation(
        "Node {NodeName} completed: {InputCount} observations in → {OutputCount} observations out ({ElapsedMs}ms)",
        pipelineNode.Name,
        totalInputCount,
        totalOutputCount,
        stopwatch.ElapsedMilliseconds);

      return NodeResult.CreateSuccess(
        pipelineNode.Name,
        stopwatch.Elapsed,
        totalInputCount,
        totalOutputCount);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      Logger?.LogError(ex, "Node {NodeName} failed: {ErrorMessage}", pipelineNode.Name, ex.Message);
      return NodeResult.CreateFailure(pipelineNode.Name, stopwatch.Elapsed, ex);
    }
  }

  /// <summary>
  /// Executes a single node by loading its inputs, invoking the transformation,
  /// and saving its outputs.
  /// </summary>
  /// <param name="pipelineNode">The node to execute</param>
  private async Task ExecuteNodeAsync(PipelineNode pipelineNode)
  {
    Logger?.LogInformation("Executing node: {NodeName}", pipelineNode.Name);

    try
    {
      // Load inputs from catalog entries
      var inputTasks = pipelineNode.Inputs.Select(entry => entry.LoadUntyped());
      var inputs = await Task.WhenAll(inputTasks);

      // TODO: Invoke node transformation
      // This requires either:
      // 1. Reflection to call ExecuteAsync on the NodeBase instance
      // 2. A non-generic INode interface with ExecuteUntyped method
      // 3. Compiled expression to invoke the generic method
      // 
      // For now, we'll need to implement this in the next iteration
      // when we have the full execution context ready

      Logger?.LogWarning(
        "Node execution not yet implemented. Node {NodeName} would process {InputCount} inputs",
        pipelineNode.Name,
        inputs.Length);

      // TODO: Save outputs to catalog entries
    }
    catch (Exception ex)
    {
      Logger?.LogError(ex, "Node {NodeName} failed: {ErrorMessage}", pipelineNode.Name, ex.Message);
      throw;
    }
  }
}
