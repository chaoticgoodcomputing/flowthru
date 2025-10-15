using Flowthru.Data;

namespace Flowthru.Pipelines;

/// <summary>
/// Analyzes pipeline node dependencies and performs topological sort to determine execution order.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Algorithm Overview:</strong>
/// </para>
/// <list type="number">
/// <item>Build producer map: For each catalog entry, track which node writes it</item>
/// <item>Resolve dependencies: For each node, find producers of its input entries</item>
/// <item>Validate single producer rule: Ensure no catalog entry is written by multiple nodes</item>
/// <item>Perform topological sort: Assign layers based on maximum dependency depth</item>
/// <item>Detect cycles: Fail if any node depends on itself (directly or transitively)</item>
/// </list>
/// <para>
/// <strong>Layer Assignment:</strong>
/// - Layer 0: Nodes with no dependencies (read only external data)
/// - Layer N: Nodes whose dependencies are all in layers 0..N-1
/// </para>
/// </remarks>
internal static class DependencyAnalyzer
{
  /// <summary>
  /// Analyzes dependencies and assigns execution layers to all nodes.
  /// </summary>
  /// <param name="nodes">All nodes in the pipeline</param>
  /// <exception cref="InvalidOperationException">
  /// Thrown if:
  /// - Multiple nodes write to the same catalog entry (violates single producer rule)
  /// - A circular dependency is detected
  /// </exception>
  public static void AnalyzeAndAssignLayers(List<PipelineNode> nodes)
  {
    // Step 1: Build producer map (catalog entry → node that produces it)
    var producerMap = BuildProducerMap(nodes);

    // Step 2: Resolve dependencies for each node
    ResolveDependencies(nodes, producerMap);

    // Step 3: Perform topological sort and assign layers
    AssignLayers(nodes);
  }

  /// <summary>
  /// Builds a map from catalog entries to the nodes that produce them.
  /// </summary>
  /// <param name="nodes">All nodes in the pipeline</param>
  /// <returns>Dictionary mapping catalog entries to their producer nodes</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if multiple nodes write to the same catalog entry
  /// </exception>
  private static Dictionary<ICatalogEntry, PipelineNode> BuildProducerMap(List<PipelineNode> nodes)
  {
    var producerMap = new Dictionary<ICatalogEntry, PipelineNode>();

    foreach (var node in nodes)
    {
      foreach (var output in node.Outputs)
      {
        if (producerMap.TryGetValue(output, out var existingProducer))
        {
          throw new InvalidOperationException(
            $"Catalog entry '{output.Key}' is produced by multiple nodes: " +
            $"'{existingProducer.Name}' and '{node.Name}'. " +
            $"Each catalog entry must have at most one producer.");
        }

        producerMap[output] = node;
      }
    }

    return producerMap;
  }

  /// <summary>
  /// Resolves dependencies for each node by finding producers of its inputs.
  /// </summary>
  /// <param name="nodes">All nodes in the pipeline</param>
  /// <param name="producerMap">Map of catalog entries to their producer nodes</param>
  private static void ResolveDependencies(
    List<PipelineNode> nodes,
    Dictionary<ICatalogEntry, PipelineNode> producerMap)
  {
    foreach (var node in nodes)
    {
      foreach (var input in node.Inputs)
      {
        // If this input is produced by another node, add it as a dependency
        if (producerMap.TryGetValue(input, out var producer))
        {
          // Don't add self-dependencies (would be caught in cycle detection anyway)
          if (producer != node)
          {
            node.Dependencies.Add(producer);
          }
        }
        // If input not in producer map, it's an external prerequisite (already in catalog)
      }
    }
  }

  /// <summary>
  /// Assigns execution layers to nodes via topological sort.
  /// </summary>
  /// <param name="nodes">All nodes in the pipeline</param>
  /// <exception cref="InvalidOperationException">Thrown if a circular dependency is detected</exception>
  private static void AssignLayers(List<PipelineNode> nodes)
  {
    // Track which nodes have been assigned layers
    var assigned = new HashSet<PipelineNode>();
    var currentLayer = 0;

    // Keep assigning layers until all nodes are processed
    while (assigned.Count < nodes.Count)
    {
      var nodesInCurrentLayer = new List<PipelineNode>();

      // Find nodes whose dependencies are all already assigned
      foreach (var node in nodes)
      {
        if (assigned.Contains(node))
          continue; // Already assigned

        // Check if all dependencies have been assigned
        var allDependenciesAssigned = node.Dependencies.All(dep => assigned.Contains(dep));

        if (allDependenciesAssigned)
        {
          node.Layer = currentLayer;
          nodesInCurrentLayer.Add(node);
        }
      }

      // If no nodes were assigned this iteration, we have a cycle
      if (nodesInCurrentLayer.Count == 0)
      {
        var unassignedNodes = nodes.Where(n => !assigned.Contains(n)).Select(n => n.Name);
        throw new InvalidOperationException(
          $"Circular dependency detected in pipeline. " +
          $"Unassigned nodes: {string.Join(", ", unassignedNodes)}");
      }

      // Mark these nodes as assigned and move to next layer
      foreach (var node in nodesInCurrentLayer)
      {
        assigned.Add(node);
      }

      currentLayer++;
    }
  }

  /// <summary>
  /// Groups nodes by their assigned execution layer.
  /// </summary>
  /// <param name="nodes">All nodes in the pipeline (must have layers assigned)</param>
  /// <returns>Nodes grouped by layer, ordered by layer number</returns>
  public static IEnumerable<List<PipelineNode>> GroupByLayer(List<PipelineNode> nodes)
  {
    return nodes
      .GroupBy(n => n.Layer)
      .OrderBy(g => g.Key)
      .Select(g => g.ToList());
  }
}
