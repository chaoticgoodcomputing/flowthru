using System.Text;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Step;
using Flowthru.Validation.Runtime;

namespace Flowthru.Diagnostics.Mermaid.Internal;

/// <summary>
/// Renders <see cref="BuiltFlow"/> (and optionally a <see cref="FlowResult"/>)
/// as a Mermaid flowchart wrapped in a Markdown code fence. Diagram shape:
/// </summary>
/// <remarks>
/// <para>
/// <strong>Layout.</strong>
/// External catalog items (consumed but never produced by any step in
/// the flow) appear at the top with a database-shape glyph. The flow
/// itself is wrapped in a single subgraph keyed by <see cref="BuiltFlow.Label"/>;
/// inside, steps are rectangles and produced items are databases.
/// Step→item and item→step edges connect them in topological order.
/// Service dependencies (declared via <see cref="IStepNode.ServiceDependencies"/>)
/// render outside the flow subgraph as a separate, styled cluster
/// with dashed <c>uses</c> edges to the consuming steps.
/// </para>
/// <para>
/// <strong>Run-result coloring.</strong> When a <see cref="FlowResult"/>
/// is supplied, step nodes are coloured by outcome: failed steps in
/// red, skipped steps in grey, succeeded steps in green. The legacy
/// extension drove a green→amber heat-map off per-step execution
/// time; the new <see cref="StepResult"/> closed sum doesn't carry
/// timing data, so the heat-map collapses to a uniform-green
/// "succeeded" colour. Restoring per-step timing is a tracked
/// follow-up (Core-shape change).
/// </para>
/// <para>
/// <strong>Slice highlighting.</strong> Deferred — the legacy renderer
/// supported <c>showFullDag</c> + active-slice colouring driven off
/// <c>DagMetadata.SlicedStepIds</c>. The new <see cref="BuiltFlow"/>
/// doesn't carry slice metadata (the slicer takes targets as call
/// parameters and doesn't store them on the flow), so a slice-aware
/// renderer needs Core-shape changes too. Tracked carryover.
/// </para>
/// </remarks>
internal static class MermaidDiagramRenderer
{
  internal sealed record Theme(
    string ActiveStepColor,
    string ActiveDataColor,
    string FailedStepColor,
    string SkippedStepColor,
    string InactiveStepColor = "#E0E0E0",
    string InactiveDataColor = "#F5F5F5",
    string InactiveTextColor = "#9E9E9E",
    // Heat-map endpoint for the slowest succeeded step. The fastest
    // step uses ActiveStepColor; intermediate steps interpolate linearly
    // toward HeatMapMaxColor. Default deep-red — failed steps are
    // distinguished by a thick FailedStepColor stroke + bold text +
    // "(failed)" label suffix rather than fill colour, so the heat-map
    // can run all the way to red without colliding with the failure
    // signal.
    string HeatMapMaxColor = "#D32F2F",
    // Cache-hit colour: a step the pre-flight cache plan marked Fresh
    // (or whose post-run StepResult.Succeeded.Reason is "cached")
    // renders with this fill. Distinct from the heat-map curve so
    // cache hits are immediately distinguishable from ran-fast steps.
    string CachedStepColor = "#1976D2"
  )
  {
    public static Theme Default => new(
      ActiveStepColor: "#2E7D32",
      ActiveDataColor: "#2E7D32",
      FailedStepColor: "#C62828",
      SkippedStepColor: "#757575"
    );
  }

  /// <summary>Render the DAG-only diagram (pre-run).</summary>
  public static string RenderDag(
    FlowMetadataContext ctx, bool showFullDag,
    MermaidFlowchartDirection direction, Theme theme
  ) => Render(ctx, result: null, showFullDag, direction, theme);

  /// <summary>Render the DAG diagram with run-result coloring (post-run).</summary>
  public static string RenderRun(
    FlowRunMetadataContext ctx, bool showFullDag,
    MermaidFlowchartDirection direction, Theme theme
  ) => Render(ctx.Static, ctx.Result, showFullDag, direction, theme);

  private static string Render(
    FlowMetadataContext ctx, FlowResult? result, bool showFullDag,
    MermaidFlowchartDirection direction, Theme theme
  )
  {
    var sb = new StringBuilder();
    var direnc = DirectionCode(direction);

    sb.AppendLine("```mermaid");
    sb.AppendLine($"flowchart {direnc}");
    sb.AppendLine();

    // Pick the topology to render.
    //   showFullDag=true  → render the merged DAG; mark inactive nodes
    //                       with a muted theme so the surrounding
    //                       structure stays visible to the reader.
    //   showFullDag=false → filter to the active slice only.
    var topology = showFullDag ? ctx.MergedFlow : ctx.EffectiveFlow;
    var active = ctx.ActiveStepLabels;

    // Partition catalog items into "external" (consumed but never produced
    // by any step in the topology) and "produced" (output by some step).
    var producerByLabel = new Dictionary<string, IStepNode>(StringComparer.Ordinal);
    foreach (var step in topology.Steps)
    {
      foreach (var output in step.Outputs)
      {
        producerByLabel[output.Label] = step;
      }
    }

    var allItemLabels = new HashSet<string>(StringComparer.Ordinal);
    foreach (var step in topology.Steps)
    {
      foreach (var item in step.Inputs) allItemLabels.Add(item.Label);
      foreach (var item in step.Outputs) allItemLabels.Add(item.Label);
    }

    var externalItems = allItemLabels
      .Where(l => !producerByLabel.ContainsKey(l))
      .OrderBy(l => l, StringComparer.Ordinal)
      .ToList();

    // An item is "active" if at least one active step references it
    // (as input or output). External items consumed only by inactive
    // steps render as inactive too.
    var activeItemLabels = new HashSet<string>(StringComparer.Ordinal);
    foreach (var step in topology.Steps)
    {
      if (!active.Contains(step.Label)) continue;
      foreach (var item in step.Inputs) activeItemLabels.Add(item.Label);
      foreach (var item in step.Outputs) activeItemLabels.Add(item.Label);
    }

    // Lookup table for run-result coloring.
    var resultsByLabel = result?.StepResults
      .ToDictionary(r => r.StepLabel, r => r, StringComparer.Ordinal);

    // Heat-map normalisation — the slowest *non-cached* succeeded step
    // pins the red endpoint; faster non-cached steps interpolate
    // toward the green baseline. Cached steps (Reason="cached", zero
    // duration) are excluded from the curve so a single instant cache
    // hit doesn't collapse everyone else to red, and so cached steps
    // don't get accidentally drawn as "fast and green". Skipped /
    // Failed steps are also excluded. Null when no run result is
    // present (pre-run diagram).
    var ranSucceededDurations = result?.StepResults
      .OfType<StepResult.Succeeded>()
      .Where(s => !IsCacheHit(s))
      .Select(s => s.Duration)
      .ToList();

    // When only a single non-cached step ran, the curve is degenerate —
    // that step would always read as red. Render it at the green
    // baseline instead so users aren't misled.
    var heatMapMax = ranSucceededDurations is { Count: >= 2 }
      ? ranSucceededDurations.Max()
      : (TimeSpan?)null;

    // Pre-flight cache-hit lookup. The plan is null when caching is
    // disabled, bypassed via --no-cache, or no UseCacheStorage
    // registration was made.
    var cachePlanFreshLabels = ctx.CachePlan?.FreshStepLabels;

    // Group steps by their flow of origin. A merged DAG collects steps
    // from multiple registered flows; each step's IStepNode.FlowLabel
    // still names the flow that originally declared it, so we render
    // one subgraph per distinct flow label. Steps with empty FlowLabel
    // (hand-rolled implementations that didn't tag themselves) fall
    // back to the BuiltFlow's label.
    var stepsByFlow = topology.Steps
      .GroupBy(s => string.IsNullOrEmpty(s.FlowLabel) ? topology.Label : s.FlowLabel,
        StringComparer.Ordinal)
      .OrderBy(g => g.Key, StringComparer.Ordinal)
      .ToList();

    // ── External inputs (above the flow subgraphs) ─────────────────────
    if (externalItems.Count > 0)
    {
      sb.AppendLine("    %% External Data Inputs");
      foreach (var label in externalItems)
      {
        sb.AppendLine($"    {SanitizeId(label)}[(\"{EscapeLabel(label)}\")]");
        if (!activeItemLabels.Contains(label))
        {
          sb.AppendLine(
            $"    style {SanitizeId(label)} fill:{theme.InactiveDataColor},"
            + $"stroke:{theme.InactiveTextColor},color:{theme.InactiveTextColor}"
          );
        }
      }
      sb.AppendLine();
    }

    // ── One subgraph per flow of origin ────────────────────────────────
    foreach (var group in stepsByFlow)
    {
      var flowLabel = group.Key;
      sb.AppendLine($"    subgraph {SanitizeId(flowLabel)}[\"{EscapeLabel(flowLabel)}\"]");

      foreach (var step in group)
      {
        var stepId = SanitizeId(step.Label);
        var stepActive = active.Contains(step.Label);

        // Failed steps get a " (failed)" suffix on their label so a
        // grayscale render is still legible. The fill is the regular
        // heat-map colour (failure doesn't dominate timing); the
        // dedicated failed-step decoration (red stroke, bold text)
        // is applied below via a Mermaid classDef.
        StepResult? stepResult = null;
        resultsByLabel?.TryGetValue(step.Label, out stepResult);
        var isFailed = stepResult is StepResult.Failed;
        var displayLabel = isFailed
          ? step.Label + " (failed)"
          : step.Label;
        sb.AppendLine($"        {stepId}[\"{EscapeLabel(displayLabel)}\"]");

        // Run-result coloring takes precedence over slice styling.
        if (stepResult is not null)
        {
          var color = ColorFor(stepResult, theme, heatMapMax);
          sb.AppendLine($"        style {stepId} fill:{color}");
        }
        else if (!stepActive)
        {
          sb.AppendLine(
            $"        style {stepId} fill:{theme.InactiveStepColor},"
            + $"stroke:{theme.InactiveTextColor},color:{theme.InactiveTextColor}"
          );
        }
        else if (cachePlanFreshLabels is not null
          && cachePlanFreshLabels.Contains(step.Label))
        {
          // Pre-flight: cache plan predicts this step will be skipped.
          // Render it in blue so users can see the cache hit before the
          // run starts.
          sb.AppendLine(
            $"        style {stepId} fill:{theme.CachedStepColor},color:#FFFFFF"
          );
        }

        foreach (var output in step.Outputs)
        {
          var outputId = SanitizeId(output.Label);
          sb.AppendLine($"        {outputId}[(\"{EscapeLabel(output.Label)}\")]");
          if (!activeItemLabels.Contains(output.Label))
          {
            sb.AppendLine(
              $"        style {outputId} fill:{theme.InactiveDataColor},"
              + $"stroke:{theme.InactiveTextColor},color:{theme.InactiveTextColor}"
            );
          }
        }
      }

      sb.AppendLine("    end");
      sb.AppendLine();
    }

    // ── Edges ──────────────────────────────────────────────────────────
    // Emit all input → step and step → output edges at the top level.
    // Mermaid resolves cross-subgraph references by id, so cross-flow
    // edges draw naturally between subgraphs. Edges incident to an
    // inactive step render dashed (`-.->`); edges between active nodes
    // stay solid.
    sb.AppendLine("    %% Edges");
    foreach (var step in topology.Steps)
    {
      var stepId = SanitizeId(step.Label);
      var stepActive = active.Contains(step.Label);
      var arrow = stepActive ? "-->" : "-.->";
      foreach (var input in step.Inputs)
      {
        sb.AppendLine($"    {SanitizeId(input.Label)} {arrow} {stepId}");
      }
      foreach (var output in step.Outputs)
      {
        sb.AppendLine($"    {stepId} {arrow} {SanitizeId(output.Label)}");
      }
    }
    sb.AppendLine();

    // ── Service dependencies (out-of-flow cluster) ─────────────────────
    AppendServiceNodes(sb, topology.Steps);

    // ── Failed-step decoration ─────────────────────────────────────────
    // Mermaid doesn't let `style` directives set font-weight or
    // stroke-width without a classDef. Emit one classDef + a single
    // `class` line listing every failed step's id so the failed
    // decoration (red stroke, bold text) is applied uniformly without
    // colliding with the per-step fill colour set above.
    if (resultsByLabel is not null)
    {
      var failedIds = topology.Steps
        .Where(s => resultsByLabel.TryGetValue(s.Label, out var r) && r is StepResult.Failed)
        .Select(s => SanitizeId(s.Label))
        .ToList();
      if (failedIds.Count > 0)
      {
        sb.AppendLine();
        sb.AppendLine($"    classDef failed stroke:{theme.FailedStepColor},"
          + "stroke-width:3px,font-weight:bold");
        sb.AppendLine($"    class {string.Join(",", failedIds)} failed");
      }
    }

    sb.AppendLine("```");
    return sb.ToString();
  }

  private static void AppendServiceNodes(StringBuilder sb, IReadOnlyList<IStepNode> steps)
  {
    var pairs = steps
      .SelectMany(s => s.ServiceDependencies.Select(svc => (StepLabel: s.Label, Service: svc)))
      .ToList();

    if (pairs.Count == 0) return;

    var uniqueServices = pairs
      .Select(p => p.Service)
      .Distinct()
      .OrderBy(s => s.DagId, StringComparer.Ordinal)
      .ToList();

    sb.AppendLine();
    sb.AppendLine("    %% Service Dependencies");
    foreach (var svc in uniqueServices)
    {
      sb.AppendLine($"    {ServiceNodeId(svc)}[\"{EscapeLabel(svc.DisplayName)}\"]");
    }
    sb.AppendLine();

    foreach (var (stepLabel, svc) in pairs.Distinct().OrderBy(p => p.StepLabel).ThenBy(p => p.Service.DagId))
    {
      sb.AppendLine($"    {SanitizeId(stepLabel)} -.uses.-> {ServiceNodeId(svc)}");
    }
    sb.AppendLine();

    sb.AppendLine("    classDef service fill:#FEF7E0,stroke:#A05A00,color:#5E4400");
    var classList = string.Join(",", uniqueServices.Select(ServiceNodeId));
    sb.AppendLine($"    class {classList} service");
  }

  /// <summary>
  /// Resolve the fill colour for a per-step <see cref="StepResult"/>.
  /// Cache hits (Succeeded with Reason="cached") render in
  /// <see cref="Theme.CachedStepColor"/>; other succeeded steps fall
  /// on the heat-map curve from <see cref="Theme.ActiveStepColor"/>
  /// (fastest) to <see cref="Theme.HeatMapMaxColor"/> (slowest).
  /// Failed steps fall on the same curve — the failed-step decoration
  /// (red stroke, bold text, "(failed)" suffix) carries the failure
  /// signal so timing stays legible on the failed cell.
  /// </summary>
  private static string ColorFor(StepResult result, Theme theme, TimeSpan? heatMapMax) => result switch
  {
    StepResult.Failed f => HeatMapColor(f.Duration, heatMapMax, theme),
    StepResult.Skipped => theme.SkippedStepColor,
    StepResult.Succeeded s when IsCacheHit(s) => theme.CachedStepColor,
    StepResult.Succeeded s => HeatMapColor(s.Duration, heatMapMax, theme),
    _ => throw new InvalidOperationException(
      $"Unreachable: StepResult is a closed sum, got {result.GetType().Name}."
    ),
  };

  /// <summary>
  /// True when a succeeded step was short-circuited by the cache plan.
  /// The scheduler stamps <c>Reason="cached"</c> on the synthetic
  /// <see cref="StepResult.Succeeded"/> it emits for fresh steps.
  /// </summary>
  private static bool IsCacheHit(StepResult.Succeeded s) =>
    string.Equals(s.Reason, "cached", StringComparison.Ordinal);

  /// <summary>
  /// Interpolate between <see cref="Theme.ActiveStepColor"/> (fastest)
  /// and <see cref="Theme.HeatMapMaxColor"/> (slowest succeeded step)
  /// based on this step's duration. Returns the active colour
  /// directly when the heat-map normaliser is unset or zero —
  /// happens for pre-run diagrams or when every step is
  /// instantaneous.
  /// </summary>
  private static string HeatMapColor(TimeSpan duration, TimeSpan? max, Theme theme)
  {
    if (max is not { } slowest || slowest <= TimeSpan.Zero)
    {
      return theme.ActiveStepColor;
    }
    var t = Math.Clamp(duration.TotalMilliseconds / slowest.TotalMilliseconds, 0.0, 1.0);
    return InterpolateHex(theme.ActiveStepColor, theme.HeatMapMaxColor, t);
  }

  /// <summary>
  /// Linear RGB interpolation between two <c>#RRGGBB</c> colours.
  /// <paramref name="t"/> is in [0, 1]; <c>0</c> returns
  /// <paramref name="from"/>, <c>1</c> returns <paramref name="to"/>.
  /// Falls back to <paramref name="from"/> if either input fails to
  /// parse — diagnostics shouldn't crash on a malformed theme.
  /// </summary>
  internal static string InterpolateHex(string from, string to, double t)
  {
    if (!TryParseHex(from, out var fr, out var fg, out var fb)
        || !TryParseHex(to, out var tr, out var tg, out var tb))
    {
      return from;
    }
    var r = (int)Math.Round(fr + (tr - fr) * t);
    var g = (int)Math.Round(fg + (tg - fg) * t);
    var b = (int)Math.Round(fb + (tb - fb) * t);
    return $"#{r:X2}{g:X2}{b:X2}";
  }

  private static bool TryParseHex(string hex, out int r, out int g, out int b)
  {
    r = g = b = 0;
    if (string.IsNullOrEmpty(hex) || hex.Length != 7 || hex[0] != '#') return false;
    return int.TryParse(hex.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out r)
        && int.TryParse(hex.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out g)
        && int.TryParse(hex.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out b);
  }

  private static string ServiceNodeId(ServiceRef svc) => "svc_" + SanitizeId(svc.DagId);

  internal static string SanitizeId(string id) =>
    id.Replace(" ", "_")
      .Replace("-", "_")
      .Replace(".", "_")
      .Replace("(", "_")
      .Replace(")", "_")
      .Replace("[", "_")
      .Replace("]", "_")
      .Replace(":", "_");

  internal static string EscapeLabel(string label) => label.Replace("\"", "\\\"");

  private static string DirectionCode(MermaidFlowchartDirection direction) => direction switch
  {
    MermaidFlowchartDirection.TopToBottom => "TB",
    MermaidFlowchartDirection.LeftToRight => "LR",
    MermaidFlowchartDirection.BottomToTop => "BT",
    MermaidFlowchartDirection.RightToLeft => "RL",
    _ => "TB",
  };
}
