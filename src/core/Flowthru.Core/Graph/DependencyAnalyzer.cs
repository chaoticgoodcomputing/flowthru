using Flowthru.Core.Data;

namespace Flowthru.Core.Graph;

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
/// - Layer 0: Steps with no dependencies (read only external data)
/// - Layer N: Steps whose dependencies are all in layers 0..N-1
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
    /// <param name="nodes">Steps to assign layers to (full pipeline or sliced subset)</param>
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
                var unassignedSteps = nodes.Where(n => !assigned.Contains(n)).Select(n => n.Label);
                throw new InvalidOperationException(
                  $"Circular dependency detected in pipeline. "
                    + $"Unassigned nodes: {string.Join(", ", unassignedSteps)}"
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
    /// <returns>Steps grouped by layer, ordered by layer number</returns>
    public static IEnumerable<List<FlowStep>> GroupByLayer(List<FlowStep> nodes)
    {
        return nodes.GroupBy(n => n.Layer).OrderBy(g => g.Key).Select(g => g.ToList());
    }

    /// <summary>
    /// Slices a flow to include only steps matching the specified strategy.
    /// </summary>
    /// <param name="allSteps">All steps in the flow</param>
    /// <param name="strategy">The slicing strategy to apply</param>
    /// <returns>Filtered list of steps forming a valid sub-DAG</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if:
    /// - Flows filter matched no steps for the specified flow names
    /// - A label in From/To/Only does not match any step or catalog item in the flow
    /// - A catalog item label in To/Only has no producer step
    /// </exception>
    /// <remarks>
    /// <para>
    /// Labels in <see cref="FlowSliceStrategy.From"/>, <see cref="FlowSliceStrategy.To"/>, and
    /// <see cref="FlowSliceStrategy.Only"/> are resolved uniformly: the step index is checked
    /// first, then the catalog item index (resolving to the relevant consumer or producer steps).
    /// </para>
    /// <para>
    /// Multiple strategies compose via intersection. For example,
    /// <c>From + To</c> produces steps in the intersection of the downstream
    /// tree of From and the upstream tree of To.
    /// </para>
    /// <para>
    /// <strong>Runnability Guarantee:</strong> The returned step set always forms a valid
    /// sub-DAG that can execute without missing dependencies.
    /// </para>
    /// </remarks>
    public static List<FlowStep> SliceSteps(List<FlowStep> allSteps, FlowSliceStrategy strategy)
    {
        if (!strategy.IsSliced)
        {
            return allSteps;
        }

        // Dependencies are already resolved by Flow.Build() before slicing.
        var stepsByLabel = allSteps.ToDictionary(n => n.Label, StringComparer.OrdinalIgnoreCase);
        var selectedSteps = new HashSet<FlowStep>(allSteps);

        // Step 1: Apply flows filter (for merged flows, restricts by flow name prefix)
        if (strategy.Flows is { Count: > 0 })
        {
            var flowsFilter = new HashSet<FlowStep>();

            foreach (var flowName in strategy.Flows)
            {
                // Steps in merged flows are labeled "FlowName.StepName"
                var flowSteps = allSteps.Where(n =>
                {
                    var dotIndex = n.Label.IndexOf('.');
                    if (dotIndex <= 0)
                    {
                        return false;
                    }

                    var nodeFlowName = n.Label.Substring(0, dotIndex);
                    return nodeFlowName.Equals(flowName, StringComparison.OrdinalIgnoreCase);
                });

                flowsFilter.UnionWith(flowSteps);
            }

            if (flowsFilter.Count == 0)
            {
                throw new InvalidOperationException(
                  $"Flows filter matched no steps. Specified: {string.Join(", ", strategy.Flows)}"
                );
            }

            selectedSteps.IntersectWith(flowsFilter);
        }

        // Step 2: Apply Only filter (explicit allowlist + upstream dependencies)
        if (strategy.Only is { Count: > 0 })
        {
            var explicitSteps = new HashSet<FlowStep>();
            foreach (var label in strategy.Only)
            {
                explicitSteps.UnionWith(ResolveToProducers(label, allSteps, stepsByLabel, "Only"));
            }

            var withDependencies = ExpandUpstream(explicitSteps);
            selectedSteps.IntersectWith(withDependencies);
        }

        // Step 3: Apply From filter (resolve to starting steps, expand downstream)
        if (strategy.From is { Count: > 0 })
        {
            var fromSteps = new HashSet<FlowStep>();
            foreach (var label in strategy.From)
            {
                fromSteps.UnionWith(ResolveToConsumers(label, allSteps, stepsByLabel, "From"));
            }

            var withDownstream = ExpandDownstream(fromSteps, allSteps);
            selectedSteps.IntersectWith(withDownstream);
        }

        // Step 4: Apply To filter (resolve to ending steps, expand upstream)
        if (strategy.To is { Count: > 0 })
        {
            var toSteps = new HashSet<FlowStep>();
            foreach (var label in strategy.To)
            {
                toSteps.UnionWith(ResolveToProducers(label, allSteps, stepsByLabel, "To"));
            }

            var withUpstream = ExpandUpstream(toSteps);
            selectedSteps.IntersectWith(withUpstream);
        }

        var slicedList = selectedSteps.ToList();

        // Trim dependencies to only include steps in the sliced set.
        // Edges pointing outside the slice become external inputs in the sliced context.
        var slicedSet = new HashSet<FlowStep>(slicedList);
        foreach (var step in slicedList)
        {
            step.Dependencies.RemoveAll(dep => !slicedSet.Contains(dep));
        }

        return slicedList;
    }

    /// <summary>
    /// Resolves a label to one or more "producer" steps: tries the step index first,
    /// then falls back to finding the step that produces a catalog item with that label.
    /// Used for <c>To</c> and <c>Only</c> targets.
    /// </summary>
    /// <remarks>
    /// When <paramref name="label"/> contains glob metacharacters (<c>*</c> or <c>?</c>),
    /// all step labels and catalog item labels are matched against the pattern and every
    /// matching step (or its producer) is returned. Zero glob matches is still an error.
    /// </remarks>
    private static IEnumerable<FlowStep> ResolveToProducers(
      string label,
      List<FlowStep> allSteps,
      Dictionary<string, FlowStep> stepsByLabel,
      string contextName
    )
    {
        if (GlobMatcher.IsPattern(label))
        {
            var regex = GlobMatcher.ToRegex(label);

            // Try glob against step labels first
            var matchedSteps = stepsByLabel
              .Where(kv => regex.IsMatch(kv.Key))
              .Select(kv => kv.Value)
              .ToList();

            if (matchedSteps.Count > 0)
            {
                return matchedSteps;
            }

            // Fall back: glob against catalog item labels in outputs
            var matchedByOutput = allSteps
              .Where(n => n.Outputs.Any(item => regex.IsMatch(item.Label)))
              .ToList();

            if (matchedByOutput.Count > 0)
            {
                return matchedByOutput;
            }

            throw new InvalidOperationException(
              $"{contextName} pattern '{label}' did not match any step label "
                + "or catalog item produced by any step in the flow."
            );
        }

        // Try as a step label first
        if (stepsByLabel.TryGetValue(label, out var step))
        {
            return [step];
        }

        // Fall back: treat as a catalog item label and find the producing step
        var producer = allSteps.FirstOrDefault(n =>
          n.Outputs.Any(item => item.Label.Equals(label, StringComparison.OrdinalIgnoreCase))
        );

        if (producer != null)
        {
            return [producer];
        }

        throw new InvalidOperationException(
          $"{contextName} references '{label}' which does not match any step label "
            + $"or catalog item produced by any step in the flow."
        );
    }

    /// <summary>
    /// Resolves a label to one or more "consumer" steps: tries the step index first,
    /// then falls back to finding all steps that consume a catalog item with that label.
    /// Used for <c>From</c> targets.
    /// </summary>
    /// <remarks>
    /// When <paramref name="label"/> contains glob metacharacters (<c>*</c> or <c>?</c>),
    /// all step labels and catalog item labels are matched against the pattern and every
    /// matching step (or its consumers) is returned. Zero glob matches is still an error.
    /// </remarks>
    private static IEnumerable<FlowStep> ResolveToConsumers(
      string label,
      List<FlowStep> allSteps,
      Dictionary<string, FlowStep> stepsByLabel,
      string contextName
    )
    {
        if (GlobMatcher.IsPattern(label))
        {
            var regex = GlobMatcher.ToRegex(label);

            // Try glob against step labels first
            var matchedSteps = stepsByLabel
              .Where(kv => regex.IsMatch(kv.Key))
              .Select(kv => kv.Value)
              .ToList();

            if (matchedSteps.Count > 0)
            {
                return matchedSteps;
            }

            // Fall back: glob against catalog item labels in inputs
            var matchedByInput = allSteps
              .Where(n => n.Inputs.Any(item => regex.IsMatch(item.Label)))
              .ToList();

            if (matchedByInput.Count > 0)
            {
                return matchedByInput;
            }

            throw new InvalidOperationException(
              $"{contextName} pattern '{label}' did not match any step label "
                + "or catalog item consumed by any step in the flow."
            );
        }

        // Try as a step label first
        if (stepsByLabel.TryGetValue(label, out var step))
        {
            return [step];
        }

        // Fall back: treat as a catalog item label and find all consuming steps
        var consumers = allSteps
          .Where(n =>
            n.Inputs.Any(item => item.Label.Equals(label, StringComparison.OrdinalIgnoreCase))
          )
          .ToList();

        if (consumers.Count > 0)
        {
            return consumers;
        }

        throw new InvalidOperationException(
          $"{contextName} references '{label}' which does not match any step label "
            + $"or catalog item consumed by any step in the flow."
        );
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
      List<FlowStep> allSteps
    )
    {
        var result = new HashSet<FlowStep>(nodes);
        var dependencyMap = BuildDependencyMap(allSteps);

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
    /// Computes the height of every node in the DAG: the length of the longest path
    /// from that node to any sink (a node with no dependents).
    /// </summary>
    /// <param name="nodes">Steps whose heights should be computed (must already have
    /// dependencies resolved via <see cref="BuildDependencyGraph"/>).</param>
    /// <remarks>
    /// <para>
    /// Height is defined recursively:
    /// <code>
    ///   height(sink) = 0
    ///   height(n)    = 1 + max(height(d) for d in dependents(n))
    /// </code>
    /// This is computed iteratively in reverse-topological order (sinks first) in O(V+E).
    /// </para>
    /// <para>
    /// Used by <see cref="Scheduling.CriticalPathSchedulingStrategy"/> to prioritise ready
    /// steps that gate the most downstream work.
    /// </para>
    /// </remarks>
    public static void ComputeHeights(List<FlowStep> nodes)
    {
        // Build reverse adjacency: step → steps that depend on it.
        var dependents = nodes.ToDictionary(n => n, _ => new List<FlowStep>());
        foreach (var node in nodes)
        {
            foreach (var dep in node.Dependencies)
            {
                if (dependents.TryGetValue(dep, out var list))
                {
                    list.Add(node);
                }
            }
        }

        // Process nodes in reverse topological order (sinks first).
        // Re-derive topological order from Layer assignments: highest Layer = processed first.
        var ordered = nodes.OrderByDescending(n => n.Layer);
        foreach (var node in ordered)
        {
            var deps = dependents[node];
            node.Height = deps.Count == 0 ? 0 : 1 + deps.Max(d => d.Height);
        }
    }

    /// <summary>
    /// Builds a reverse dependency map (node → nodes that depend on it).
    /// </summary>
    private static Dictionary<FlowStep, List<FlowStep>> BuildDependencyMap(List<FlowStep> allSteps)
    {
        var map = new Dictionary<FlowStep, List<FlowStep>>();

        foreach (var node in allSteps)
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
