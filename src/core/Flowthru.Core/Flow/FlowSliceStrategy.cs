using Flowthru.Data.Catalog;

namespace Flowthru.Flow;

/// <summary>
/// Closed-sum algebra describing a slice of a <see cref="BuiltFlow"/>.
/// Strategies compose via <see cref="And"/> / <see cref="Or"/>, and
/// label patterns in <see cref="From"/> / <see cref="To"/> /
/// <see cref="Only"/> support glob wildcards (<c>*</c>, <c>?</c>).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Heterogeneous labels.</strong>
/// <see cref="From"/>, <see cref="To"/>, and <see cref="Only"/> accept
/// labels that may match either a step's
/// <see cref="IStepNode.Label"/> or any item's
/// <see cref="IItem.Label"/>. The resolver checks both indices for
/// every pattern. The resulting behaviour is:
/// </para>
/// <list type="bullet">
///   <item><see cref="From"/>: <em>step label</em> → seed at the step,
///     include it, walk downstream. <em>Item label</em> → seed at
///     every consumer of the item, walk downstream from each.
///     The producing step itself is <strong>not</strong> included
///     (the slice describes "everything downstream of the item",
///     which excludes its source).</item>
///   <item><see cref="To"/>: <em>step label</em> → seed at the step,
///     include it, walk upstream. <em>Item label</em> → seed at the
///     item's producer, include it, walk upstream.</item>
///   <item><see cref="Only"/>: <em>step label</em> → just the step.
///     <em>Item label</em> → just the producer of the item.</item>
/// </list>
/// <para>
/// A label that matches BOTH a step and an item label contributes
/// each match (union). Labels that match neither are silently
/// dropped — they make no contribution. This matches the legacy
/// "silently skip unknown labels" behaviour from main.
/// </para>
/// <para>
/// <strong>Composition.</strong> <see cref="And"/> is set
/// intersection, <see cref="Or"/> is set union. The resolver
/// preserves the topological ordering of the input step list —
/// slicing filters; it never reorders.
/// </para>
/// </remarks>
public abstract record FlowSliceStrategy
{
  private FlowSliceStrategy() { }

  // ── Primitive strategies ─────────────────────────────────────────────

  /// <summary>
  /// Slice downstream. Patterns matching step labels seed at those
  /// steps (included); patterns matching item labels seed at the
  /// item's consumers (producer is excluded).
  /// </summary>
  public sealed record From(IReadOnlySet<string> LabelPatterns) : FlowSliceStrategy;

  /// <summary>
  /// Slice upstream. Patterns matching step labels seed at those
  /// steps (included); patterns matching item labels seed at the
  /// item's producer (included).
  /// </summary>
  public sealed record To(IReadOnlySet<string> LabelPatterns) : FlowSliceStrategy;

  /// <summary>
  /// Exact set. Patterns matching step labels include those steps;
  /// patterns matching item labels include the producer of each
  /// matching item. No upstream / downstream expansion.
  /// </summary>
  public sealed record Only(IReadOnlySet<string> LabelPatterns) : FlowSliceStrategy;

  /// <summary>
  /// Slice to every step attributed to one of the named flows (via
  /// <see cref="IStepNode.FlowLabel"/>). Flow labels are matched
  /// case-sensitively.
  /// </summary>
  public sealed record Flows(IReadOnlySet<string> FlowLabels) : FlowSliceStrategy;

  /// <summary>The identity slice — every step is included.</summary>
  public sealed record All : FlowSliceStrategy;

  /// <summary>The empty slice — no steps are included.</summary>
  public sealed record None : FlowSliceStrategy;

  // ── Composition ──────────────────────────────────────────────────────

  /// <summary>Set intersection — a step is included iff both children include it.</summary>
  public sealed record And(FlowSliceStrategy A, FlowSliceStrategy B) : FlowSliceStrategy;

  /// <summary>Set union — a step is included iff either child includes it.</summary>
  public sealed record Or(FlowSliceStrategy A, FlowSliceStrategy B) : FlowSliceStrategy;

  // ── Convenience constructors (params-array shortcuts) ────────────────

  /// <summary>Build a <see cref="From"/> from a params-style label list.</summary>
  public static FlowSliceStrategy FromLabels(params string[] labels) =>
    new From(labels.ToHashSet(StringComparer.Ordinal));

  /// <summary>Build a <see cref="To"/> from a params-style label list.</summary>
  public static FlowSliceStrategy ToLabels(params string[] labels) =>
    new To(labels.ToHashSet(StringComparer.Ordinal));

  /// <summary>Build an <see cref="Only"/> from a params-style label list.</summary>
  public static FlowSliceStrategy OnlyLabels(params string[] labels) =>
    new Only(labels.ToHashSet(StringComparer.Ordinal));

  /// <summary>
  /// Alias for <see cref="OnlyLabels(string[])"/> preserved for
  /// continuity with earlier call-site idioms in tests.
  /// </summary>
  public static FlowSliceStrategy OnlySteps(params string[] stepLabels) =>
    new Only(stepLabels.ToHashSet(StringComparer.Ordinal));

  /// <summary>Build a <see cref="Flows"/> from a params-style flow-label list.</summary>
  public static FlowSliceStrategy InFlows(params string[] flowLabels) =>
    new Flows(flowLabels.ToHashSet(StringComparer.Ordinal));

  // ── Resolution ───────────────────────────────────────────────────────

  /// <summary>
  /// Resolve this strategy against the given step list and producer
  /// map, returning the subset of steps the strategy includes. The
  /// result preserves the topological ordering of
  /// <paramref name="orderedSteps"/>.
  /// </summary>
  public IReadOnlyList<IStepNode> Apply(
    IReadOnlyList<IStepNode> orderedSteps,
    IReadOnlyDictionary<string, IStepNode> producerByItemLabel
  )
  {
    if (orderedSteps is null) throw new ArgumentNullException(nameof(orderedSteps));
    if (producerByItemLabel is null) throw new ArgumentNullException(nameof(producerByItemLabel));

    var ctx = new ResolutionContext(orderedSteps, producerByItemLabel);
    var keep = ApplyToSet(ctx);
    return orderedSteps.Where(keep.Contains).ToList();
  }

  private HashSet<IStepNode> ApplyToSet(ResolutionContext ctx)
  {
    switch (this)
    {
      case All:
        return new HashSet<IStepNode>(ctx.OrderedSteps, ReferenceEqualityComparer.Instance);

      case None:
        return new HashSet<IStepNode>(ReferenceEqualityComparer.Instance);

      case Only o:
        return ResolveOnly(o.LabelPatterns, ctx);

      case To t:
      {
        // Seed = matching steps + producers of matching items.
        // Walk backward, include seeds.
        var seeds = ResolveStepAndItemSeedsForBackward(t.LabelPatterns, ctx);
        return new HashSet<IStepNode>(
          FlowSlicing.SliceBackwardFromSteps(ctx.OrderedSteps, ctx.ProducerByItemLabel, seeds),
          ReferenceEqualityComparer.Instance
        );
      }

      case From f:
      {
        // Seed = matching steps + CONSUMERS of matching items.
        // Walk forward, include seeds. Producers of item-seeds are
        // deliberately excluded (the slice describes "downstream of
        // the item", not "downstream of its producer").
        var seeds = ResolveStepAndItemSeedsForForward(f.LabelPatterns, ctx);
        return new HashSet<IStepNode>(
          FlowSlicing.SliceForwardFromSteps(ctx.OrderedSteps, ctx.ProducerByItemLabel, seeds),
          ReferenceEqualityComparer.Instance
        );
      }

      case Flows fls:
      {
        var keep = new HashSet<IStepNode>(ReferenceEqualityComparer.Instance);
        foreach (var step in ctx.OrderedSteps)
        {
          if (fls.FlowLabels.Contains(step.FlowLabel))
          {
            keep.Add(step);
          }
        }
        return keep;
      }

      case And a:
      {
        var left = a.A.ApplyToSet(ctx);
        var right = a.B.ApplyToSet(ctx);
        left.IntersectWith(right);
        return left;
      }

      case Or o:
      {
        var left = o.A.ApplyToSet(ctx);
        var right = o.B.ApplyToSet(ctx);
        left.UnionWith(right);
        return left;
      }

      default:
        throw new InvalidOperationException("Unreachable: FlowSliceStrategy is a closed sum");
    }
  }

  /// <summary>
  /// <see cref="Only"/> resolution: for each pattern, collect the
  /// union of (step-label matches) + (producers of item-label matches).
  /// </summary>
  private static HashSet<IStepNode> ResolveOnly(
    IReadOnlySet<string> patterns,
    ResolutionContext ctx
  )
  {
    var keep = new HashSet<IStepNode>(ReferenceEqualityComparer.Instance);
    foreach (var pattern in patterns)
    {
      foreach (var step in ctx.OrderedSteps)
      {
        if (FlowSlicing.MatchesGlob(step.Label, pattern))
        {
          keep.Add(step);
        }
      }
      foreach (var (itemLabel, producer) in ctx.ProducerByItemLabel)
      {
        if (FlowSlicing.MatchesGlob(itemLabel, pattern))
        {
          keep.Add(producer);
        }
      }
    }
    return keep;
  }

  /// <summary>
  /// Backward-direction seed resolution for <see cref="To"/>:
  /// step matches → the step itself; item matches → the item's producer.
  /// </summary>
  private static List<IStepNode> ResolveStepAndItemSeedsForBackward(
    IReadOnlySet<string> patterns,
    ResolutionContext ctx
  )
  {
    var seeds = new HashSet<IStepNode>(ReferenceEqualityComparer.Instance);
    foreach (var pattern in patterns)
    {
      foreach (var step in ctx.OrderedSteps)
      {
        if (FlowSlicing.MatchesGlob(step.Label, pattern))
        {
          seeds.Add(step);
        }
      }
      foreach (var (itemLabel, producer) in ctx.ProducerByItemLabel)
      {
        if (FlowSlicing.MatchesGlob(itemLabel, pattern))
        {
          seeds.Add(producer);
        }
      }
    }
    return seeds.ToList();
  }

  /// <summary>
  /// Forward-direction seed resolution for <see cref="From"/>:
  /// step matches → the step itself; item matches → the item's consumers
  /// (the producer is deliberately excluded so the resulting slice
  /// describes "downstream of the item", not "the item's producer").
  /// </summary>
  private static List<IStepNode> ResolveStepAndItemSeedsForForward(
    IReadOnlySet<string> patterns,
    ResolutionContext ctx
  )
  {
    var seeds = new HashSet<IStepNode>(ReferenceEqualityComparer.Instance);
    foreach (var pattern in patterns)
    {
      foreach (var step in ctx.OrderedSteps)
      {
        if (FlowSlicing.MatchesGlob(step.Label, pattern))
        {
          seeds.Add(step);
        }
      }
      foreach (var (itemLabel, consumers) in ctx.ConsumersByItemLabel)
      {
        if (FlowSlicing.MatchesGlob(itemLabel, pattern))
        {
          foreach (var consumer in consumers)
          {
            seeds.Add(consumer);
          }
        }
      }
    }
    return seeds.ToList();
  }

  /// <summary>
  /// Per-Apply context — caches the orderedSteps, producer index, and
  /// the consumer index so each composite strategy resolves only once.
  /// </summary>
  private sealed class ResolutionContext
  {
    public IReadOnlyList<IStepNode> OrderedSteps { get; }
    public IReadOnlyDictionary<string, IStepNode> ProducerByItemLabel { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<IStepNode>> ConsumersByItemLabel { get; }

    public ResolutionContext(
      IReadOnlyList<IStepNode> orderedSteps,
      IReadOnlyDictionary<string, IStepNode> producerByItemLabel
    )
    {
      OrderedSteps = orderedSteps;
      ProducerByItemLabel = producerByItemLabel;
      ConsumersByItemLabel = FlowSlicing.BuildItemConsumerIndex(orderedSteps);
    }
  }
}
