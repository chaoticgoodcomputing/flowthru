using Flowthru.Data.Catalog;

namespace Flowthru.Flow;

/// <summary>
/// Low-level slicing primitives used by <see cref="FlowSliceStrategy"/>'s
/// resolver. Direct consumers can call these for narrow target-item-based
/// slicing without going through the closed-sum algebra; most callers
/// should use <see cref="FlowSliceStrategy"/> instead because it carries
/// the algebraic-composition affordances (And, Or, etc.) and the
/// glob-pattern label matching.
/// </summary>
public static class FlowSlicing
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

  /// <summary>
  /// Walk forward from a set of seed steps along consumer edges,
  /// returning every reachable step in topological order. Seed steps
  /// are themselves included. Edges are derived from the
  /// <paramref name="producerByItemLabel"/> map.
  /// </summary>
  public static IReadOnlyList<IStepNode> SliceForwardFromSteps(
    IReadOnlyList<IStepNode> orderedSteps,
    IReadOnlyDictionary<string, IStepNode> producerByItemLabel,
    IEnumerable<IStepNode> seedSteps
  )
  {
    if (orderedSteps is null) throw new ArgumentNullException(nameof(orderedSteps));
    if (producerByItemLabel is null) throw new ArgumentNullException(nameof(producerByItemLabel));
    if (seedSteps is null) throw new ArgumentNullException(nameof(seedSteps));

    var consumersByProducer = BuildConsumerIndex(orderedSteps, producerByItemLabel);
    var keep = new HashSet<IStepNode>(ReferenceEqualityComparer.Instance);
    var pending = new Queue<IStepNode>(seedSteps);
    while (pending.Count > 0)
    {
      var step = pending.Dequeue();
      if (!keep.Add(step)) continue;
      if (!consumersByProducer.TryGetValue(step, out var consumers)) continue;
      foreach (var consumer in consumers)
      {
        pending.Enqueue(consumer);
      }
    }
    return orderedSteps.Where(keep.Contains).ToList();
  }

  /// <summary>
  /// Walk backward from a set of seed steps along producer edges,
  /// returning every reachable step in topological order. Seed steps
  /// are themselves included.
  /// </summary>
  public static IReadOnlyList<IStepNode> SliceBackwardFromSteps(
    IReadOnlyList<IStepNode> orderedSteps,
    IReadOnlyDictionary<string, IStepNode> producerByItemLabel,
    IEnumerable<IStepNode> seedSteps
  )
  {
    if (orderedSteps is null) throw new ArgumentNullException(nameof(orderedSteps));
    if (producerByItemLabel is null) throw new ArgumentNullException(nameof(producerByItemLabel));
    if (seedSteps is null) throw new ArgumentNullException(nameof(seedSteps));

    var keep = new HashSet<IStepNode>(ReferenceEqualityComparer.Instance);
    var pending = new Queue<IStepNode>(seedSteps);
    while (pending.Count > 0)
    {
      var step = pending.Dequeue();
      if (!keep.Add(step)) continue;
      foreach (var input in step.Inputs)
      {
        if (producerByItemLabel.TryGetValue(input.Label, out var producer))
        {
          pending.Enqueue(producer);
        }
      }
    }
    return orderedSteps.Where(keep.Contains).ToList();
  }

  /// <summary>
  /// Build a step→consumers index: for every step, list the downstream
  /// steps that consume any of its outputs as their inputs. Used by
  /// forward-traversal primitives and the strategy resolver.
  /// </summary>
  public static IReadOnlyDictionary<IStepNode, IReadOnlyList<IStepNode>> BuildConsumerIndex(
    IReadOnlyList<IStepNode> orderedSteps,
    IReadOnlyDictionary<string, IStepNode> producerByItemLabel
  )
  {
    var consumersByProducer = new Dictionary<IStepNode, List<IStepNode>>(
      ReferenceEqualityComparer.Instance
    );
    foreach (var step in orderedSteps)
    {
      foreach (var input in step.Inputs)
      {
        if (!producerByItemLabel.TryGetValue(input.Label, out var producer)) continue;
        if (!consumersByProducer.TryGetValue(producer, out var consumers))
        {
          consumers = new List<IStepNode>();
          consumersByProducer[producer] = consumers;
        }
        consumers.Add(step);
      }
    }
    var result = new Dictionary<IStepNode, IReadOnlyList<IStepNode>>(
      ReferenceEqualityComparer.Instance
    );
    foreach (var kvp in consumersByProducer)
    {
      result[kvp.Key] = kvp.Value;
    }
    return result;
  }

  /// <summary>
  /// Build an item-label → consumers index from a step list.
  /// </summary>
  public static IReadOnlyDictionary<string, IReadOnlyList<IStepNode>> BuildItemConsumerIndex(
    IReadOnlyList<IStepNode> orderedSteps
  )
  {
    var consumersByItemLabel = new Dictionary<string, List<IStepNode>>(StringComparer.Ordinal);
    foreach (var step in orderedSteps)
    {
      foreach (var input in step.Inputs)
      {
        if (!consumersByItemLabel.TryGetValue(input.Label, out var list))
        {
          list = new List<IStepNode>();
          consumersByItemLabel[input.Label] = list;
        }
        list.Add(step);
      }
    }
    return consumersByItemLabel.ToDictionary(
      kvp => kvp.Key,
      kvp => (IReadOnlyList<IStepNode>)kvp.Value,
      StringComparer.Ordinal
    );
  }

  /// <summary>
  /// Match <paramref name="value"/> against a glob pattern. Supports
  /// <c>*</c> (any sequence) and <c>?</c> (any single character).
  /// Comparison is ordinal; case-sensitive.
  /// </summary>
  /// <remarks>
  /// Patterns without wildcards reduce to ordinal equality. The helper
  /// is intentionally narrow — it does not support character classes,
  /// negation, or path-separator semantics. Slicing labels are flat
  /// strings, not paths.
  /// </remarks>
  public static bool MatchesGlob(string value, string pattern)
  {
    if (value is null) throw new ArgumentNullException(nameof(value));
    if (pattern is null) throw new ArgumentNullException(nameof(pattern));
    if (pattern.IndexOf('*') < 0 && pattern.IndexOf('?') < 0)
    {
      return string.Equals(value, pattern, StringComparison.Ordinal);
    }
    return GlobToRegex(pattern).IsMatch(value);
  }

  private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Text.RegularExpressions.Regex> _regexCache = new();

  private static System.Text.RegularExpressions.Regex GlobToRegex(string pattern) =>
    _regexCache.GetOrAdd(pattern, static p =>
    {
      var sb = new System.Text.StringBuilder("^");
      foreach (var ch in p)
      {
        sb.Append(ch switch
        {
          '*' => ".*",
          '?' => ".",
          _ => System.Text.RegularExpressions.Regex.Escape(ch.ToString()),
        });
      }
      sb.Append('$');
      return new System.Text.RegularExpressions.Regex(
        sb.ToString(),
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant
      );
    });
}
