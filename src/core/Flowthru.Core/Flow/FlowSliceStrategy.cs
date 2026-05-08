using Flowthru.Data.Catalog;

namespace Flowthru.Flow;

/// <summary>
/// Slicing extracts a subgraph of a <see cref="BuiltFlow"/> defined
/// by a set of "target" item labels — the engine then runs only the
/// steps reachable from those targets, walking dependencies
/// backwards. Per §2.4 (multi-flow merging), the flow label becomes
/// the slicing key.
/// </summary>
public static class FlowSliceStrategy
{
  /// <summary>
  /// Walk dependencies backwards from <paramref name="targetItemLabels"/>,
  /// returning the subset of <paramref name="orderedSteps"/> needed to
  /// produce them — preserving the topological ordering of the input
  /// list.
  /// </summary>
  public static IReadOnlyList<IStepNode> SliceTo(
    IReadOnlyList<IStepNode> orderedSteps,
    IReadOnlyDictionary<string, IStepNode> producerByItemLabel,
    IEnumerable<string> targetItemLabels
  )
  {
    if (orderedSteps is null) throw new ArgumentNullException(nameof(orderedSteps));
    if (producerByItemLabel is null) throw new ArgumentNullException(nameof(producerByItemLabel));
    if (targetItemLabels is null) throw new ArgumentNullException(nameof(targetItemLabels));

    var keep = new HashSet<IStepNode>(ReferenceEqualityComparer.Instance);
    var pending = new Queue<string>(targetItemLabels);
    while (pending.Count > 0)
    {
      var label = pending.Dequeue();
      if (!producerByItemLabel.TryGetValue(label, out var producer)) continue;
      if (!keep.Add(producer)) continue;
      foreach (var input in producer.Inputs)
      {
        pending.Enqueue(input.Label);
      }
    }
    return orderedSteps.Where(keep.Contains).ToList();
  }
}
