using Flowthru.Data.Catalog;

namespace Flowthru.Flow;

/// <summary>
/// Builds a dependency graph over a list of <see cref="IStepNode"/>s
/// using <see cref="IItem.Label"/> as the edge identifier (the
/// "single producer per item" law guarantees the labels are stable
/// keys). Returns a topologically-ordered execution plan or, on
/// detection, the offending cycle / single-producer violation.
/// </summary>
public static class DependencyAnalyzer
{
  /// <summary>
  /// Outcome of analysing a step list — either a valid plan with the
  /// steps in topological order, or a description of the structural
  /// violation that prevents a plan from being built.
  /// </summary>
  public abstract record Result
  {
    private Result() { }

    /// <summary>Topologically-ordered step list, plus the producer map for the engine.</summary>
    public sealed record Ok(
      IReadOnlyList<IStepNode> Order,
      IReadOnlyDictionary<string, IStepNode> ProducerByItemLabel
    ) : Result;

    /// <summary>The dependency graph contains a cycle. <see cref="Cycle"/> lists step labels in walk order.</summary>
    public sealed record CycleDetected(IReadOnlyList<string> Cycle) : Result
    {
      public string Message => $"Cycle detected: {string.Join(" → ", Cycle)} → {Cycle[0]}";
    }

    /// <summary>Two or more steps declare the same item as an output.</summary>
    public sealed record DuplicateProducer(string ItemLabel, IReadOnlyList<string> StepLabels) : Result
    {
      public string Message =>
        $"Item '{ItemLabel}' has {StepLabels.Count} producers ({string.Join(", ", StepLabels)}); " +
        "single-producer law (§2.4) requires at most one.";
    }
  }

  /// <summary>
  /// Build an execution plan. Steps are ordered such that a step's
  /// inputs are written by an earlier step in the list (or are
  /// "source" items with no producer). Returns the plan or the first
  /// structural violation encountered.
  /// </summary>
  public static Result Analyse(IReadOnlyList<IStepNode> steps)
  {
    if (steps is null) throw new ArgumentNullException(nameof(steps));
    if (steps.Count == 0)
    {
      return new Result.Ok(Array.Empty<IStepNode>(), new Dictionary<string, IStepNode>());
    }

    // Build producer map; detect duplicate producers.
    var producerByItemLabel = new Dictionary<string, IStepNode>(StringComparer.Ordinal);
    var duplicates = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    foreach (var step in steps)
    {
      foreach (var output in step.Outputs)
      {
        if (producerByItemLabel.TryGetValue(output.Label, out var existing))
        {
          if (!duplicates.TryGetValue(output.Label, out var list))
          {
            list = new List<string> { existing.Label };
            duplicates[output.Label] = list;
          }
          list.Add(step.Label);
        }
        else
        {
          producerByItemLabel[output.Label] = step;
        }
      }
    }
    if (duplicates.Count > 0)
    {
      var first = duplicates.First();
      return new Result.DuplicateProducer(first.Key, first.Value);
    }

    // Build adjacency — step depends on each step producing one of its inputs.
    var indexByStep = new Dictionary<IStepNode, int>(ReferenceEqualityComparer.Instance);
    for (var i = 0; i < steps.Count; i++)
    {
      indexByStep[steps[i]] = i;
    }
    var dependencies = new List<List<int>>(steps.Count);
    var inDegree = new int[steps.Count];
    for (var i = 0; i < steps.Count; i++)
    {
      dependencies.Add(new List<int>());
    }
    for (var i = 0; i < steps.Count; i++)
    {
      var step = steps[i];
      foreach (var input in step.Inputs)
      {
        if (producerByItemLabel.TryGetValue(input.Label, out var producer)
            && indexByStep.TryGetValue(producer, out var producerIndex)
            && producerIndex != i)
        {
          dependencies[producerIndex].Add(i);
          inDegree[i]++;
        }
      }
    }

    // Kahn's algorithm.
    var ready = new Queue<int>();
    for (var i = 0; i < steps.Count; i++)
    {
      if (inDegree[i] == 0) ready.Enqueue(i);
    }
    var order = new List<IStepNode>(steps.Count);
    while (ready.Count > 0)
    {
      var i = ready.Dequeue();
      order.Add(steps[i]);
      foreach (var j in dependencies[i])
      {
        if (--inDegree[j] == 0) ready.Enqueue(j);
      }
    }
    if (order.Count < steps.Count)
    {
      var cycle = ExtractCycle(steps, dependencies, inDegree);
      return new Result.CycleDetected(cycle);
    }

    return new Result.Ok(order, producerByItemLabel);
  }

  private static IReadOnlyList<string> ExtractCycle(
    IReadOnlyList<IStepNode> steps,
    IReadOnlyList<List<int>> dependencies,
    int[] inDegree
  )
  {
    // Find any node still having inDegree > 0; walk forward following
    // any out-edge until we revisit a node, then trim the prefix.
    var start = -1;
    for (var i = 0; i < steps.Count; i++)
    {
      if (inDegree[i] > 0) { start = i; break; }
    }
    if (start == -1) return Array.Empty<string>();

    var visited = new Dictionary<int, int>();
    var path = new List<int>();
    var current = start;
    while (!visited.ContainsKey(current))
    {
      visited[current] = path.Count;
      path.Add(current);
      var next = -1;
      foreach (var n in dependencies[current])
      {
        if (inDegree[n] > 0) { next = n; break; }
      }
      if (next == -1) break;
      current = next;
    }
    var cycleStart = visited.TryGetValue(current, out var i0) ? i0 : 0;
    return path.GetRange(cycleStart, path.Count - cycleStart)
      .Select(idx => steps[idx].Label)
      .ToList();
  }
}
