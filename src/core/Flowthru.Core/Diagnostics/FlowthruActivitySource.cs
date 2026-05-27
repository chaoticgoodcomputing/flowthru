using System.Diagnostics;

namespace Flowthru.Diagnostics;

/// <summary>
/// Single <see cref="ActivitySource"/> Core uses to publish runtime
/// trace spans (flow run started/finished, pre-flight phase
/// entered/exited, per-step start/finish). Consumers — OpenTelemetry
/// exporters, App Insights, custom dashboards — subscribe via
/// <see cref="ActivityListener"/>.
/// </summary>
/// <remarks>
/// <para>
/// Human-readable logging is handled directly by
/// <c>ILogger&lt;TSelf&gt;</c> in engine components
/// (<c>FlowthruService</c>, <c>ParallelFlowScheduler</c>); this
/// <see cref="ActivitySource"/> emits trace spans for distributed-
/// tracing consumers only. The previous CLI-side
/// <c>FlowthruActivityLogger</c> bridge that translated activities
/// into <c>ILogger</c> calls has been retired.
/// </para>
/// <para>
/// Standard activity names emitted by Core:
/// <list type="bullet">
///   <item><c>flowthru.run</c> — top-level flow run; tags include
///     <c>flowthru.flow.label</c>, <c>flowthru.flow.step_count</c>,
///     <c>flowthru.flow.sliced</c>.</item>
///   <item><c>flowthru.preflight</c> — pre-flight phase; tags include
///     <c>flowthru.preflight.error_count</c> on failure.</item>
///   <item><c>flowthru.step</c> — per-step execution; tags include
///     <c>flowthru.step.label</c>, <c>flowthru.step.input_count</c>,
///     <c>flowthru.step.output_count</c>. Status =
///     <c>ActivityStatusCode.Error</c> with description on failure.</item>
/// </list>
/// </para>
/// </remarks>
public static class FlowthruActivitySource
{
  /// <summary>The <see cref="ActivitySource.Name"/> consumers filter against.</summary>
  public const string SourceName = "Flowthru";

  /// <summary>Activity name for the top-level flow run span.</summary>
  public const string RunActivityName = "flowthru.run";

  /// <summary>Activity name for the pre-flight phase span.</summary>
  public const string PreFlightActivityName = "flowthru.preflight";

  /// <summary>Activity name for a single step's execution span.</summary>
  public const string StepActivityName = "flowthru.step";

  /// <summary>
  /// Reserved activity name for the cache-uncacheability decision.
  /// Engine no longer emits this — <c>FlowthruService</c> logs each
  /// uncacheable-step decision directly via <c>ILogger</c>.
  /// The constant remains for any external consumer that wired filters
  /// against this name in the activity-bridge era.
  /// </summary>
  public const string CacheUncacheableActivityName = "flowthru.cache.uncacheable";

  /// <summary>
  /// Tag carrying the human-readable reason a step is uncacheable —
  /// the output of <c>StepUncacheableReason.Describe()</c>.
  /// </summary>
  public const string TagCacheUncacheableReason = "flowthru.cache.uncacheable_reason";

  /// <summary>Tag name carrying the flow's label (or "(merged)" for a no-slice run).</summary>
  public const string TagFlowLabel = "flowthru.flow.label";

  /// <summary>Tag name carrying the flow's step count after any slicing.</summary>
  public const string TagFlowStepCount = "flowthru.flow.step_count";

  /// <summary>Tag name flagging whether a flow run is a slice (true) or full merged DAG (false).</summary>
  public const string TagFlowSliced = "flowthru.flow.sliced";

  /// <summary>Tag name carrying the count of pre-flight errors when validation fails.</summary>
  public const string TagPreFlightErrorCount = "flowthru.preflight.error_count";

  /// <summary>Tag name carrying a step's label.</summary>
  public const string TagStepLabel = "flowthru.step.label";

  /// <summary>Tag name carrying a step's declared input count.</summary>
  public const string TagStepInputCount = "flowthru.step.input_count";

  /// <summary>Tag name carrying a step's declared output count.</summary>
  public const string TagStepOutputCount = "flowthru.step.output_count";

  /// <summary>The single Core <see cref="ActivitySource"/> instance.</summary>
  public static readonly ActivitySource Source = new(SourceName);
}
