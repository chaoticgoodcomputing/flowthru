using Flowthru.Flow;

namespace Flowthru.Diagnostics;

/// <summary>
/// Complete information envelope handed to every
/// <see cref="IMetadataProvider"/> at pre-run time. Carries the full
/// merged DAG plus enough slice context that any provider — whether
/// shipped with Flowthru or written by a third party — can render or
/// project the run accurately, even when the user invoked a slice of
/// the merged graph.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why a context, not just a <see cref="BuiltFlow"/>.</strong>
/// When <c>FlowthruService.RunAsync</c> is called with a flow label,
/// it slices the merged DAG to the labelled flow's outputs. Handing
/// only the slice to providers loses two facts a metadata renderer
/// might want: (a) the full topology the user could have invoked,
/// and (b) which subset is actually executing this run. Providers
/// that want to draw the full DAG with the active slice highlighted,
/// or to filter out inactive nodes, need both pieces of information.
/// </para>
/// <para>
/// <strong>Invariants.</strong>
/// <list type="bullet">
/// <item><see cref="MergedFlow"/> always carries the union of every
/// registered flow. When only one flow is registered, it equals that
/// flow.</item>
/// <item><see cref="EffectiveFlow"/> is the slice the host actually
/// executes. When no slice is applied, it is the same instance as
/// <see cref="MergedFlow"/>.</item>
/// <item><see cref="ActiveStepLabels"/> always names every step in
/// <see cref="EffectiveFlow"/> — when no slice is applied, this is
/// every step in <see cref="MergedFlow"/>. Never null.</item>
/// <item><see cref="RequestedFlowLabel"/> is the label the user
/// passed to <c>RunAsync</c>; null means the user invoked the merged
/// DAG directly without naming a flow.</item>
/// </list>
/// </para>
/// </remarks>
public sealed record FlowMetadataContext
{
  /// <summary>
  /// The full merged DAG — the union of every registered flow's steps
  /// after dependency analysis. Always populated; equals the single
  /// registered flow when only one is registered.
  /// </summary>
  public required BuiltFlow MergedFlow { get; init; }

  /// <summary>
  /// The slice the host is actually running. When the user did not
  /// request a slice (<see cref="RequestedFlowLabel"/> is null), this
  /// is the same instance as <see cref="MergedFlow"/>.
  /// </summary>
  public required BuiltFlow EffectiveFlow { get; init; }

  /// <summary>
  /// Every step label the host is actually executing this run. When
  /// no slice is applied, this is the full set of step labels in
  /// <see cref="MergedFlow"/>; otherwise the slice's step labels.
  /// </summary>
  public required IReadOnlySet<string> ActiveStepLabels { get; init; }

  /// <summary>
  /// The flow label the user passed to <c>RunAsync</c>. Null when the
  /// user invoked the merged DAG without naming a flow.
  /// </summary>
  public string? RequestedFlowLabel { get; init; }

  /// <summary>
  /// Build an unsliced context — every step in <paramref name="flow"/>
  /// is active, no slice was requested, and <see cref="MergedFlow"/>
  /// equals <see cref="EffectiveFlow"/>. Convenience for hosts that
  /// run a single registered flow with no slicing, and for tests.
  /// </summary>
  public static FlowMetadataContext Unsliced(BuiltFlow flow)
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));
    return new FlowMetadataContext
    {
      MergedFlow = flow,
      EffectiveFlow = flow,
      ActiveStepLabels = flow.Steps
        .Select(s => s.Label)
        .ToHashSet(StringComparer.Ordinal),
      RequestedFlowLabel = null,
    };
  }
}

/// <summary>
/// Post-run metadata envelope: the same static
/// <see cref="FlowMetadataContext"/> the pre-run providers received,
/// plus the run's <see cref="FlowResult"/>. Post-run providers see
/// every fact a pre-run provider sees, so the two sides stay in
/// lockstep — whatever a pre-run renderer can do, a post-run
/// renderer can do plus per-step outcomes.
/// </summary>
public sealed record FlowRunMetadataContext
{
  /// <summary>The pre-run context — same shape, same instance values.</summary>
  public required FlowMetadataContext Static { get; init; }

  /// <summary>The completed run's outcome.</summary>
  public required FlowResult Result { get; init; }
}
