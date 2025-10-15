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
