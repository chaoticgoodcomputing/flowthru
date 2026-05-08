using System.Text.Json.Serialization;
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

  public static DagMetadataProjection From(FlowMetadataContext ctx)
  {
    var merged = ctx.MergedFlow;
    var active = ctx.ActiveStepLabels;
    var steps = merged.Steps
      .Select(s => StepProjection.From(s, active.Contains(s.Label)))
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

  public static StepProjection From(IStepNode step, bool active) => new()
  {
    Label = step.Label,
    FlowOfOrigin = step.FlowLabel,
    Active = active,
    Inputs = step.Inputs.Select(i => i.Label).ToList(),
    Outputs = step.Outputs.Select(o => o.Label).ToList(),
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
    Result = RunResultProjection.From(ctx.Result),
  };
}

/// <summary>Result projection: per-step outcome + overall success flag.</summary>
internal sealed record RunResultProjection
{
  [JsonPropertyOrder(0)]
  public required bool Success { get; init; }

  [JsonPropertyOrder(1)]
  public required IReadOnlyList<StepResultProjection> StepResults { get; init; }

  public static RunResultProjection From(FlowResult result) => new()
  {
    Success = result.IsSuccess,
    StepResults = result.StepResults.Select(StepResultProjection.From).ToList(),
  };
}

/// <summary>Per-step outcome: succeeded, failed, or skipped.</summary>
internal sealed record StepResultProjection
{
  public required string StepLabel { get; init; }
  public required string Status { get; init; }
  public string? FailureMessage { get; init; }
  public string? SkipReason { get; init; }

  public static StepResultProjection From(StepResult result) => result switch
  {
    StepResult.Succeeded s => new StepResultProjection
    {
      StepLabel = s.StepLabel,
      Status = "succeeded",
    },
    StepResult.Skipped s => new StepResultProjection
    {
      StepLabel = s.StepLabel,
      Status = "skipped",
      SkipReason = s.Reason,
    },
    StepResult.Failed f => new StepResultProjection
    {
      StepLabel = f.StepLabel,
      Status = "failed",
      FailureMessage = f.Error.Message,
    },
    _ => throw new InvalidOperationException(
      $"Unreachable: StepResult is a closed sum, got {result.GetType().Name}."
    ),
  };
}
