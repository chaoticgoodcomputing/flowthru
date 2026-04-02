using Flowthru.Data;

namespace Flowthru.Flows;

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
/// <para>
/// <strong>Pipeline Slicing:</strong>
/// Dependency resolution (BuildDependencyGraph) must happen on the full node set before slicing,
/// as the slicing logic needs to traverse dependencies. Layer assignment (AssignLayers) should
/// happen after slicing to ensure Layer 0 correctly identifies external inputs in the sliced context.
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
  /// <remarks>
  /// This method combines BuildDependencyGraph and AssignLayers for convenience.
  /// For sliced pipelines, call these methods separately to recalculate layers post-slice.
  /// </remarks>
  public static void AnalyzeAndAssignLayers(List<FlowStep> nodes)
  {
    BuildDependencyGraph(nodes);
    AssignLayers(nodes);
  }

  /// <summary>
  /// Builds the dependency graph by mapping producers and resolving dependencies.
  /// </summary>
  /// <param name="nodes">All nodes in the pipeline</param>
  /// <exception cref="InvalidOperationException">
  /// Thrown if multiple nodes write to the same catalog entry (violates single producer rule)
  /// </exception>
  /// <remarks>
  /// This phase must occur before slicing, as the slicing logic traverses node dependencies
  /// to determine which nodes to include. Layer assignment should happen separately after slicing.
  /// </remarks>
  public static void BuildDependencyGraph(List<FlowStep> nodes)
  {
    // Step 1: Build producer map (catalog entry → node that produces it)
    var producerMap = BuildProducerMap(nodes);

    // Step 2: Resolve dependencies for each node
    ResolveDependencies(nodes, producerMap);
  }

  /// <summary>
  /// Builds a map from catalog entry labels to the nodes that produce them.
  /// </summary>
  /// <param name="nodes">All nodes in the pipeline</param>
  /// <returns>Dictionary mapping catalog entry labels to their producer nodes</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if multiple nodes write to the same catalog entry
  /// </exception>
  private static Dictionary<string, FlowStep> BuildProducerMap(List<FlowStep> nodes)
  {
    var producerMap = new Dictionary<string, FlowStep>(StringComparer.OrdinalIgnoreCase);

    foreach (var node in nodes)
    {
      foreach (var output in node.Outputs)
      {
        if (producerMap.TryGetValue(output.Label, out var existingProducer))
        {
          throw new InvalidOperationException(
            $"Catalog entry '{output.Label}' is produced by multiple nodes: "
              + $"'{existingProducer.Label}' and '{node.Label}'. "
              + $"Each catalog entry must have at most one producer."
          );
        }

        producerMap[output.Label] = node;
      }
    }

    return producerMap;
  }

  /// <summary>
  /// Resolves dependencies for each node by finding producers of its inputs.
  /// </summary>
  /// <param name="nodes">All nodes in the pipeline</param>
  /// <param name="producerMap">Map of catalog entry labels to their producer nodes</param>
  private static void ResolveDependencies(
    List<FlowStep> nodes,
    Dictionary<string, FlowStep> producerMap
  )
  {
    foreach (var node in nodes)
    {
      foreach (var input in node.Inputs)
      {
        // If this input is produced by another node, add it as a dependency (label-based match)
        if (producerMap.TryGetValue(input.Label, out var producer))
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
  /// <param name="nodes">Nodes to assign layers to (full pipeline or sliced subset)</param>
  /// <exception cref="InvalidOperationException">Thrown if a circular dependency is detected</exception>
  /// <remarks>
  /// This method should be called after slicing to ensure Layer 0 correctly identifies
  /// nodes with no dependencies in the execution context.
  /// </remarks>
  public static void AssignLayers(List<FlowStep> nodes)
  {
    // Track which nodes have been assigned layers
    var assigned = new HashSet<FlowStep>();
    var currentLayer = 0;

    // Keep assigning layers until all nodes are processed
    while (assigned.Count < nodes.Count)
    {
      var nodesInCurrentLayer = new List<FlowStep>();

      // Find nodes whose dependencies are all already assigned
      foreach (var node in nodes)
      {
        if (assigned.Contains(node))
        {
          continue; // Already assigned
        }

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
        var unassignedNodes = nodes.Where(n => !assigned.Contains(n)).Select(n => n.Label);
        throw new InvalidOperationException(
          $"Circular dependency detected in pipeline. "
            + $"Unassigned nodes: {string.Join(", ", unassignedNodes)}"
        );
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
  public static IEnumerable<List<FlowStep>> GroupByLayer(List<FlowStep> nodes)
  {
    return nodes.GroupBy(n => n.Layer).OrderBy(g => g.Key).Select(g => g.ToList());
  }

  /// <summary>
  /// Slices a pipeline to include only nodes matching the specified strategy.
  /// </summary>
  /// <param name="allNodes">All nodes in the pipeline</param>
  /// <param name="strategy">The slicing strategy to apply</param>
  /// <returns>Filtered list of nodes forming a valid sub-DAG</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if:
  /// - FromData references catalog entries not consumed by any node
  /// - ToData references catalog entries not produced by any node
  /// - OnlyNodes references non-existent node names
  /// - FromNodes/ToNodes references non-existent node names
  /// </exception>
  /// <remarks>
  /// <para>
  /// Multiple strategies compose via intersection. For example,
  /// <c>FromNodes + ToNodes</c> produces nodes in the intersection of the downstream
  /// tree of FromNodes and the upstream tree of ToNodes.
  /// </para>
  /// <para>
  /// <strong>Runnability Guarantee:</strong> The returned node set always forms a valid
  /// sub-DAG that can execute without missing dependencies.
  /// </para>
  /// </remarks>
  public static List<FlowStep> SliceSteps(List<FlowStep> allNodes, FlowSliceStrategy strategy)
  {
    if (!strategy.IsSliced)
    {
      return allNodes;
    }

    // Dependencies are already resolved by Pipeline.Build() before slicing
    // No need to call BuildProducerMap/ResolveDependencies here

    var nodesByLabel = allNodes.ToDictionary(n => n.Label, StringComparer.OrdinalIgnoreCase);
    var selectedNodes = new HashSet<FlowStep>(allNodes);

    // Step 1: Apply pipeline filter (if specified, for merged pipelines)
    if (strategy.Flows is { Count: > 0 })
    {
      var pipelineFilter = new HashSet<FlowStep>();

      foreach (var pipelineName in strategy.Flows)
      {
        // Find nodes that belong to this pipeline (prefix match: "FlowName.NodeName")
        var pipelineNodes = allNodes.Where(n =>
        {
          var dotIndex = n.Label.IndexOf('.');
          if (dotIndex <= 0)
          {
            return false; // Not a merged pipeline node
          }

          var nodeFlowName = n.Label.Substring(0, dotIndex);
          return nodeFlowName.Equals(pipelineName, StringComparison.OrdinalIgnoreCase);
        });

        pipelineFilter.UnionWith(pipelineNodes);
      }

      if (pipelineFilter.Count == 0)
      {
        throw new InvalidOperationException(
          $"Pipelines filter did not match any nodes. Specified: {string.Join(", ", strategy.Flows)}"
        );
      }

      selectedNodes.IntersectWith(pipelineFilter);
    }

    // Step 2: Apply OnlyNodes filter (explicit allowlist + dependencies)
    if (strategy.OnlyNodes is { Count: > 0 })
    {
      var explicitNodes = new HashSet<FlowStep>();
      foreach (var nodeName in strategy.OnlyNodes)
      {
        if (!nodesByLabel.TryGetValue(nodeName, out var node))
        {
          throw new InvalidOperationException(
            $"OnlyNodes references non-existent node: '{nodeName}'"
          );
        }
        explicitNodes.Add(node);
      }

      // Include all upstream dependencies to maintain runnability
      var withDependencies = ExpandUpstream(explicitNodes);
      selectedNodes.IntersectWith(withDependencies);
    }

    // Step 3: Apply FromData (find consumers, expand downstream)
    var fromNodesExpanded = new HashSet<FlowStep>();
    if (strategy.FromData is { Count: > 0 })
    {
      // Find nodes that consume any of the specified catalog entries
      foreach (var dataLabel in strategy.FromData)
      {
        var consumingNodes = allNodes.Where(n =>
          n.Inputs.Any(entry => entry.Label.Equals(dataLabel, StringComparison.OrdinalIgnoreCase))
        );

        if (!consumingNodes.Any())
        {
          throw new InvalidOperationException(
            $"FromData references catalog entry '{dataLabel}' which is not consumed by any node"
          );
        }

        fromNodesExpanded.UnionWith(consumingNodes);
      }
    }

    // Step 4: Apply FromNodes (include downstream dependents)
    if (strategy.FromNodes is { Count: > 0 })
    {
      foreach (var nodeName in strategy.FromNodes)
      {
        if (!nodesByLabel.TryGetValue(nodeName, out var node))
        {
          throw new InvalidOperationException(
            $"FromNodes references non-existent node: '{nodeName}'"
          );
        }
        fromNodesExpanded.Add(node);
      }
    }

    if (fromNodesExpanded.Count > 0)
    {
      var withDownstream = ExpandDownstream(fromNodesExpanded, allNodes);
      selectedNodes.IntersectWith(withDownstream);
    }

    // Step 5: Apply ToData (find producers, expand upstream)
    var toNodesExpanded = new HashSet<FlowStep>();
    if (strategy.ToData is { Count: > 0 })
    {
      // Find nodes that produce any of the specified catalog entries
      foreach (var dataLabel in strategy.ToData)
      {
        var producingNode = allNodes.FirstOrDefault(n =>
          n.Outputs.Any(entry => entry.Label.Equals(dataLabel, StringComparison.OrdinalIgnoreCase))
        );

        if (producingNode == null)
        {
          throw new InvalidOperationException(
            $"ToData references catalog entry '{dataLabel}' which is not produced by any node"
          );
        }

        toNodesExpanded.Add(producingNode);
      }
    }

    // Step 6: Apply ToNodes (include upstream dependencies to reach these nodes)
    if (strategy.ToNodes is { Count: > 0 })
    {
      foreach (var nodeName in strategy.ToNodes)
      {
        if (!nodesByLabel.TryGetValue(nodeName, out var node))
        {
          throw new InvalidOperationException(
            $"ToNodes references non-existent node: '{nodeName}'"
          );
        }
        toNodesExpanded.Add(node);
      }
    }

    if (toNodesExpanded.Count > 0)
    {
      var withUpstream = ExpandUpstream(toNodesExpanded);
      selectedNodes.IntersectWith(withUpstream);
    }

    var slicedList = selectedNodes.ToList();

    // Filter each node's dependencies to only include nodes in the sliced set
    // Dependencies pointing outside the slice become external inputs in the sliced context
    var slicedSet = new HashSet<FlowStep>(slicedList);
    foreach (var node in slicedList)
    {
      node.Dependencies.RemoveAll(dep => !slicedSet.Contains(dep));
    }

    return slicedList;
  }

  /// <summary>
  /// Expands a set of nodes to include all upstream dependencies (transitive closure).
  /// </summary>
  private static HashSet<FlowStep> ExpandUpstream(HashSet<FlowStep> nodes)
  {
    var result = new HashSet<FlowStep>();
    var toVisit = new Queue<FlowStep>(nodes);

    while (toVisit.Count > 0)
    {
      var current = toVisit.Dequeue();
      if (result.Add(current))
      {
        foreach (var dependency in current.Dependencies)
        {
          toVisit.Enqueue(dependency);
        }
      }
    }

    return result;
  }

  /// <summary>
  /// Expands a set of nodes to include all downstream dependents (transitive closure).
  /// </summary>
  private static HashSet<FlowStep> ExpandDownstream(
    HashSet<FlowStep> nodes,
    List<FlowStep> allNodes
  )
  {
    var result = new HashSet<FlowStep>(nodes);
    var dependencyMap = BuildDependencyMap(allNodes);

    var toVisit = new Queue<FlowStep>(nodes);
    while (toVisit.Count > 0)
    {
      var current = toVisit.Dequeue();
      if (dependencyMap.TryGetValue(current, out var dependents))
      {
        foreach (var dependent in dependents)
        {
          if (result.Add(dependent))
          {
            toVisit.Enqueue(dependent);
          }
        }
      }
    }

    return result;
  }

  /// <summary>
  /// Builds a reverse dependency map (node → nodes that depend on it).
  /// </summary>
  private static Dictionary<FlowStep, List<FlowStep>> BuildDependencyMap(List<FlowStep> allNodes)
  {
    var map = new Dictionary<FlowStep, List<FlowStep>>();

    foreach (var node in allNodes)
    {
      foreach (var dependency in node.Dependencies)
      {
        if (!map.ContainsKey(dependency))
        {
          map[dependency] = new List<FlowStep>();
        }
        map[dependency].Add(node);
      }
    }

    return map;
  }
}
