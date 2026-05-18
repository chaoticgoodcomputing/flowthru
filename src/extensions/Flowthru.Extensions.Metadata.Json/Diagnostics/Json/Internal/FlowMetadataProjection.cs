using System.Text.Json.Serialization;
using Flowthru.Caching;
using Flowthru.Flow;
using Flowthru.Step;

namespace Flowthru.Diagnostics.Json.Internal;

/// <summary>
/// Serialisable projection of a <see cref="FlowMetadataContext"/> —
/// the shape the JSON metadata provider writes for the pre-run
/// manifest. Carries the merged DAG (full topology), the slice the
/// host is actually running, the user's requested flow label (if any),
/// and per-step active flags so external tooling can either filter to
/// the active set or render the full graph with the slice highlighted.
/// </summary>
internal sealed record DagMetadataProjection
{
  /// <summary>Label of the slice the host is actually running.</summary>
  [JsonPropertyOrder(0)]
  public required string FlowName { get; init; }

  /// <summary>
  /// The flow label the user passed to <c>RunAsync</c>; null when the
  /// merged DAG was invoked without naming a slice.
  /// </summary>
  [JsonPropertyOrder(1)]
  public string? RequestedFlowLabel { get; init; }

  /// <summary>
  /// Step labels currently in the active slice. When no slice was
  /// applied, this names every step in <see cref="Steps"/>.
  /// </summary>
  [JsonPropertyOrder(2)]
  public required IReadOnlyList<string> ActiveStepLabels { get; init; }

  /// <summary>
  /// Every step in the **merged** DAG (the full union of registered
  /// flows). Each entry carries an <see cref="StepProjection.Active"/>
  /// flag that consumers can use to filter.
  /// </summary>
  [JsonPropertyOrder(3)]
  public required IReadOnlyList<StepProjection> Steps { get; init; }

  [JsonPropertyOrder(4)]
  public required IReadOnlyList<CatalogItemProjection> CatalogItems { get; init; }

  [JsonPropertyOrder(5)]
  public required IReadOnlyList<EdgeProjection> Edges { get; init; }

  /// <summary>
  /// Top-level summary of this run's cache plan. <c>null</c> when caching
  /// is disabled and not bypassed — the more common shape is a populated
  /// projection with empty step sets. Tooling consumers read
  /// <see cref="CachePlanProjection.Mode"/> to distinguish
  /// "planned" (cache plan computed), "bypassed" (<c>--no-cache</c>),
  /// and "disabled" (no <c>UseCacheStorage</c> registration).
  /// </summary>
  [JsonPropertyOrder(6)]
  public CachePlanProjection? CachePlan { get; init; }

  public static DagMetadataProjection From(FlowMetadataContext ctx)
  {
    var merged = ctx.MergedFlow;
    var active = ctx.ActiveStepLabels;
    var plan = ctx.CachePlan;
    var steps = merged.Steps
      .Select(s => StepProjection.From(
        s,
        active.Contains(s.Label),
        CacheStatusProjection.PreRun(s.Label, plan)
      ))
      .ToList();

    var itemsByLabel = new Dictionary<string, CatalogItemProjection>(StringComparer.Ordinal);
    foreach (var step in merged.Steps)
    {
      foreach (var item in step.Inputs)
      {
        if (!itemsByLabel.ContainsKey(item.Label))
        {
          itemsByLabel[item.Label] = new CatalogItemProjection { Label = item.Label };
        }
      }
      foreach (var item in step.Outputs)
      {
        if (!itemsByLabel.ContainsKey(item.Label))
        {
          itemsByLabel[item.Label] = new CatalogItemProjection { Label = item.Label };
        }
      }
    }

    var edges = new List<EdgeProjection>();
    foreach (var step in merged.Steps)
    {
      foreach (var input in step.Inputs)
      {
        edges.Add(new EdgeProjection
        {
          Source = input.Label,
          Sink = step.Label,
          Kind = "input",
        });
      }
      foreach (var output in step.Outputs)
      {
        edges.Add(new EdgeProjection
        {
          Source = step.Label,
          Sink = output.Label,
          Kind = "output",
        });
      }
    }

    return new DagMetadataProjection
    {
      FlowName = ctx.EffectiveFlow.Label,
      RequestedFlowLabel = ctx.RequestedFlowLabel,
      ActiveStepLabels = active.OrderBy(l => l, StringComparer.Ordinal).ToList(),
      Steps = steps,
      CatalogItems = itemsByLabel.Values
        .OrderBy(i => i.Label, StringComparer.Ordinal)
        .ToList(),
      Edges = edges,
      CachePlan = CachePlanProjection.From(plan, ctx.BypassCacheReads),
    };
  }
}

/// <summary>
/// Per-step projection: label + flow of origin + whether the step is
/// active in the current slice + declared input/output catalog labels.
/// <see cref="FlowOfOrigin"/> survives the merged-DAG view; <see cref="Active"/>
/// names whether this step is part of the slice the host is actually
/// running.
/// </summary>
internal sealed record StepProjection
{
  [JsonPropertyOrder(0)]
  public required string Label { get; init; }

  /// <summary>
  /// Label of the flow that declared this step. Empty when the step
  /// was constructed outside a <c>FlowBuilder</c> context.
  /// </summary>
  [JsonPropertyOrder(1)]
  public required string FlowOfOrigin { get; init; }

  /// <summary>
  /// True when this step is part of the active slice. Always true
  /// when no slice was applied.
  /// </summary>
  [JsonPropertyOrder(2)]
  public required bool Active { get; init; }

  [JsonPropertyOrder(3)]
  public required IReadOnlyList<string> Inputs { get; init; }

  [JsonPropertyOrder(4)]
  public required IReadOnlyList<string> Outputs { get; init; }

  /// <summary>
  /// Pre-flight cache-plan classification for this step:
  /// fresh / stale / uncacheable / unplanned. <see cref="CacheStatusProjection.Ran"/>
  /// is always <c>null</c> on the pre-flight projection — that field
  /// is populated only on the post-run <see cref="StepResultProjection"/>.
  /// </summary>
  [JsonPropertyOrder(5)]
  public required CacheStatusProjection Cache { get; init; }

  public static StepProjection From(IStepNode step, bool active, CacheStatusProjection cache) => new()
  {
    Label = step.Label,
    FlowOfOrigin = step.FlowLabel,
    Active = active,
    Inputs = step.Inputs.Select(i => i.Label).ToList(),
    Outputs = step.Outputs.Select(o => o.Label).ToList(),
    Cache = cache,
  };
}

/// <summary>Catalog item by label — the items steps reference.</summary>
internal sealed record CatalogItemProjection
{
  public required string Label { get; init; }
}

/// <summary>One DAG edge — connects a step to one of its inputs or outputs.</summary>
internal sealed record EdgeProjection
{
  public required string Source { get; init; }
  public required string Sink { get; init; }
  public required string Kind { get; init; }
}

/// <summary>
/// Combined projection of <see cref="FlowMetadataContext"/> +
/// <see cref="FlowResult"/> — the shape the post-run JSON file
/// carries. Embeds the DAG projection so the run record is
/// self-contained.
/// </summary>
internal sealed record RunMetadataProjection
{
  [JsonPropertyOrder(0)]
  public required DagMetadataProjection Dag { get; init; }

  [JsonPropertyOrder(1)]
  public required RunResultProjection Result { get; init; }

  public static RunMetadataProjection From(FlowRunMetadataContext ctx) => new()
  {
    Dag = DagMetadataProjection.From(ctx.Static),
    Result = RunResultProjection.From(ctx.Result, ctx.Static.CachePlan),
  };
}

/// <summary>Result projection: per-step outcome + overall success flag + run duration.</summary>
internal sealed record RunResultProjection
{
  [JsonPropertyOrder(0)]
  public required bool Success { get; init; }

  /// <summary>
  /// Total wall-clock duration of the run, in seconds, as measured
  /// by the scheduler. Surface for downstream tooling (RunSummary,
  /// Mermaid heat-map, dashboards) that don't want to re-aggregate
  /// per-step durations.
  /// </summary>
  [JsonPropertyOrder(1)]
  public required double DurationSeconds { get; init; }

  [JsonPropertyOrder(2)]
  public required IReadOnlyList<StepResultProjection> StepResults { get; init; }

  public static RunResultProjection From(FlowResult result, CachePlan? plan) => new()
  {
    Success = result.IsSuccess,
    DurationSeconds = result.Duration.TotalSeconds,
    StepResults = result.StepResults
      .Select(r => StepResultProjection.From(r, plan))
      .ToList(),
  };
}

/// <summary>
/// Per-step outcome: succeeded, failed, or skipped. Succeeded and
/// Failed carry per-step <see cref="DurationSeconds"/>; Skipped omits
/// it because the step did not run. <see cref="Cache"/> joins the
/// pre-flight cache-plan classification with the observed run outcome
/// so consumers can distinguish "cached" (skipped, Reason="cached")
/// from "ran (stale)" or "ran (uncacheable)".
/// </summary>
internal sealed record StepResultProjection
{
  public required string StepLabel { get; init; }
  public required string Status { get; init; }
  public double? DurationSeconds { get; init; }
  public string? FailureMessage { get; init; }
  public string? SkipReason { get; init; }
  public required CacheStatusProjection Cache { get; init; }

  public static StepResultProjection From(StepResult result, CachePlan? plan) => result switch
  {
    StepResult.Succeeded s => new StepResultProjection
    {
      StepLabel = s.StepLabel,
      Status = "succeeded",
      DurationSeconds = s.Duration.TotalSeconds,
      Cache = CacheStatusProjection.PostRun(s.StepLabel, plan, ran: !IsCacheHit(s), reason: s.Reason),
    },
    StepResult.Skipped s => new StepResultProjection
    {
      StepLabel = s.StepLabel,
      Status = "skipped",
      SkipReason = s.Reason,
      Cache = CacheStatusProjection.PostRun(s.StepLabel, plan, ran: false, reason: null),
    },
    StepResult.Failed f => new StepResultProjection
    {
      StepLabel = f.StepLabel,
      Status = "failed",
      DurationSeconds = f.Duration.TotalSeconds,
      FailureMessage = f.Error.Message,
      Cache = CacheStatusProjection.PostRun(f.StepLabel, plan, ran: true, reason: null),
    },
    _ => throw new InvalidOperationException(
      $"Unreachable: StepResult is a closed sum, got {result.GetType().Name}."
    ),
  };

  /// <summary>
  /// True when a succeeded step was short-circuited by the cache plan.
  /// </summary>
  private static bool IsCacheHit(StepResult.Succeeded s) =>
    string.Equals(s.Reason, "cached", StringComparison.Ordinal);
}

/// <summary>
/// Cache classification for a single step. <see cref="Status"/>
/// captures the pre-flight bucket (or "unplanned" when no plan was
/// computed); <see cref="Ran"/> distinguishes pre-flight (always
/// <c>null</c>) from post-run (<c>true</c>/<c>false</c> from the
/// observed scheduler outcome).
/// </summary>
internal sealed record CacheStatusProjection
{
  /// <summary>
  /// One of <c>"fresh"</c>, <c>"stale"</c>, <c>"uncacheable"</c>, or
  /// <c>"unplanned"</c>. Unplanned means no cache plan was computed
  /// (caching disabled or bypassed).
  /// </summary>
  [JsonPropertyOrder(0)]
  public required string Status { get; init; }

  /// <summary>
  /// True when the scheduler actually executed the step. <c>null</c>
  /// on the pre-flight projection — the prediction lives in
  /// <see cref="Status"/>.
  /// </summary>
  [JsonPropertyOrder(1)]
  public bool? Ran { get; init; }

  /// <summary>
  /// Free-form note. On post-run cache hits this carries the
  /// scheduler's <c>Reason</c> string (typically <c>"cached"</c>).
  /// </summary>
  [JsonPropertyOrder(2)]
  public string? Reason { get; init; }

  /// <summary>Build the pre-flight projection for one step.</summary>
  public static CacheStatusProjection PreRun(string stepLabel, CachePlan? plan) =>
    new()
    {
      Status = ClassifyStatus(stepLabel, plan),
      Reason = UncacheableReason(stepLabel, plan),
    };

  /// <summary>Build the post-run projection for one step.</summary>
  public static CacheStatusProjection PostRun(
    string stepLabel,
    CachePlan? plan,
    bool ran,
    string? reason
  ) => new()
  {
    Status = ClassifyStatus(stepLabel, plan),
    Ran = ran,
    // Prefer the scheduler's post-run reason (e.g., "cached") when set;
    // otherwise fall back to the cache-plan uncacheable reason so the
    // post-run record carries the same per-step diagnostic the pre-run
    // record did.
    Reason = reason ?? UncacheableReason(stepLabel, plan),
  };

  private static string ClassifyStatus(string stepLabel, CachePlan? plan)
  {
    if (plan is null) return "unplanned";
    if (plan.FreshStepLabels.Contains(stepLabel)) return "fresh";
    if (plan.StaleStepLabels.Contains(stepLabel)) return "stale";
    if (plan.UncacheableStepLabels.Contains(stepLabel)) return "uncacheable";
    return "unplanned";
  }

  private static string? UncacheableReason(string stepLabel, CachePlan? plan)
  {
    if (plan is null) return null;
    return plan.UncacheableReasons.TryGetValue(stepLabel, out var reason)
      ? reason.Describe()
      : null;
  }
}

/// <summary>
/// Top-level run-wide cache plan summary.
/// </summary>
internal sealed record CachePlanProjection
{
  /// <summary>
  /// One of:
  /// <list type="bullet">
  ///   <item><c>"planned"</c> — a cache plan was computed.</item>
  ///   <item><c>"bypassed"</c> — the host opted out of cache reads (<c>--no-cache</c>).</item>
  ///   <item><c>"disabled"</c> — no <c>UseCacheStorage</c> registration was made.</item>
  /// </list>
  /// </summary>
  [JsonPropertyOrder(0)]
  public required string Mode { get; init; }

  /// <summary>Step labels the plan predicted as Fresh (will be short-circuited).</summary>
  [JsonPropertyOrder(1)]
  public required IReadOnlyList<string> Fresh { get; init; }

  /// <summary>Step labels the plan predicted as Stale (eligible but must re-run).</summary>
  [JsonPropertyOrder(2)]
  public required IReadOnlyList<string> Stale { get; init; }

  /// <summary>Step labels the plan classified as Uncacheable (will always re-run).</summary>
  [JsonPropertyOrder(3)]
  public required IReadOnlyList<string> Uncacheable { get; init; }

  /// <summary>
  /// Return a projection or <c>null</c> based on the cache plan and
  /// the host's bypass flag. The three modes are:
  /// <list type="bullet">
  /// <item><c>BypassCacheReads = true</c>: mode = "bypassed",
  /// step sets empty.</item>
  /// <item><c>plan != null</c>: mode = "planned", step sets from the
  /// plan.</item>
  /// <item>Otherwise: mode = "disabled", step sets empty.</item>
  /// </list>
  /// </summary>
  public static CachePlanProjection From(CachePlan? plan, bool bypassed)
  {
    if (bypassed)
    {
      return new CachePlanProjection
      {
        Mode = "bypassed",
        Fresh = Array.Empty<string>(),
        Stale = Array.Empty<string>(),
        Uncacheable = Array.Empty<string>(),
      };
    }
    if (plan is null)
    {
      return new CachePlanProjection
      {
        Mode = "disabled",
        Fresh = Array.Empty<string>(),
        Stale = Array.Empty<string>(),
        Uncacheable = Array.Empty<string>(),
      };
    }
    return new CachePlanProjection
    {
      Mode = "planned",
      Fresh = plan.FreshStepLabels.OrderBy(l => l, StringComparer.Ordinal).ToList(),
      Stale = plan.StaleStepLabels.OrderBy(l => l, StringComparer.Ordinal).ToList(),
      Uncacheable = plan.UncacheableStepLabels.OrderBy(l => l, StringComparer.Ordinal).ToList(),
    };
  }
}
