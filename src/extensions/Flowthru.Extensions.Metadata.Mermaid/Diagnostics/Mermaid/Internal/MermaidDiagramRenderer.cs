using System.Text;
using Flowthru.Data.Catalog;
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
/// Service dependencies (declared via <see cref="Flowthru.Data.Catalog.INode.ServiceDependencies"/>
/// on steps and items) render two ways: an inline compartment inside the
/// node — node label, a Unicode rule divider, one service per line — and a
/// distinct-coloured <c>services</c> legend subgraph carrying each
/// service's capacity / cacheability (ADR-0019). No step→service edges; the
/// reader joins the inline name to its legend entry. The compartment
/// inherits whatever fill the node has (heat-map, cache-plan blue, inactive
/// grey).
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
    string CachedStepColor = "#1976D2",
    // The service-legend subgraph (ADR-0019 #100 s7) — a deliberately
    // distinct fill/stroke from the per-flow subgraphs (which have no
    // explicit fill) so the "what services does this DAG use" key reads
    // as a separate region, not another flow.
    string ServiceLegendColor = "#EEF4FF",
    string ServiceLegendStroke = "#3B6FB0"
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

  /// <summary>
  /// Render the per-flow DAG diagram (pre-run). Returns a Markdown
  /// document containing one heading + one fenced Mermaid block per
  /// Flow in the topology. Neighboring Flows are collapsed to
  /// dashed-border subgraphs containing only their boundary Items
  /// (upstream) or boundary Steps (downstream). The heading level is
  /// caller-supplied so the output nests cleanly under whatever parent
  /// heading the document context provides.
  /// </summary>
  public static string RenderDagPerFlow(
    FlowMetadataContext ctx, bool showFullDag,
    MermaidFlowchartDirection direction, Theme theme, int headingLevel
  ) => RenderPerFlow(ctx, result: null, showFullDag, direction, theme, headingLevel);

  /// <summary>
  /// Render the per-flow diagram with run-result coloring (post-run).
  /// Local Steps carry their normal heat-map / cache-plan fill;
  /// collapsed-neighbor Steps stay neutral. The heat-map curve is
  /// normalised globally (across all Flows) so colours are comparable
  /// between per-flow blocks.
  /// </summary>
  public static string RenderRunPerFlow(
    FlowRunMetadataContext ctx, bool showFullDag,
    MermaidFlowchartDirection direction, Theme theme, int headingLevel
  ) => RenderPerFlow(ctx.Static, ctx.Result, showFullDag, direction, theme, headingLevel);

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
    // Label → IItem map so external-item and per-step-output emissions
    // can consult the concrete item's StorageKind and runtime type
    // (e.g. ConfigurationItem<_>) to pick a non-default shape. First
    // occurrence wins on label collision — flow merging keys items by
    // label, so collisions should already be resolved upstream.
    var itemByLabel = new Dictionary<string, IItem>(StringComparer.Ordinal);
    foreach (var step in topology.Steps)
    {
      foreach (var item in step.Inputs)
      {
        allItemLabels.Add(item.Label);
        itemByLabel.TryAdd(item.Label, item);
      }
      foreach (var item in step.Outputs)
      {
        allItemLabels.Add(item.Label);
        itemByLabel.TryAdd(item.Label, item);
      }
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
        itemByLabel.TryGetValue(label, out var item);
        sb.AppendLine($"    {ItemNodeSyntax(label, item)}");
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
        var displayLabel = step.Label;

        if (isFailed)
        {
          displayLabel += " (failed)";
        }

        // Service dependencies render as a compartment inside the node —
        // divider rule + one service per line. Node-generic: AppendServices
        // is shared with item cylinders, and the service legend below
        // carries each service's capacity / cacheability. The whole node
        // shares whatever fill it has (heat-map, cache-plan blue, inactive
        // grey), so the compartment inherits the active styling.
        displayLabel = AppendServices(displayLabel, step.ServiceDependencies);
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
          sb.AppendLine($"        {ItemNodeSyntax(output.Label, output)}");
          if (!activeItemLabels.Contains(output.Label))
          {
            sb.AppendLine(
              $"        style {SanitizeId(output.Label)} fill:{theme.InactiveDataColor},"
              + $"stroke:{theme.InactiveTextColor},color:{theme.InactiveTextColor}"
            );
          }
        }
      }

      sb.AppendLine("    end");
      sb.AppendLine();
    }

    // ── Service legend ─────────────────────────────────────────────────
    // A key for the services annotated on steps and item cylinders: one
    // node per distinct service, carrying capacity + cacheability. No
    // edges — the reader joins by name (a node lists "IPythonExecutor",
    // the legend says what cap/cache that implies). (ADR-0019 #100 s7.)
    RenderServiceLegend(
      sb,
      ServiceUsageAnalyzer.Analyze(topology, ctx.ServiceProfiles ?? new DefaultServiceProfileProvider()),
      theme);

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

    // Services are surfaced two ways: inline in each node's label (the
    // compartment emitted above, naming the services it uses), and once
    // more in the service-legend subgraph (emitted before the edges) that
    // carries each service's capacity / cacheability. There are no
    // step→service edges — the reader joins inline name to legend entry.

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

  /// <summary>
  /// Render one Markdown document holding N per-flow Mermaid blocks,
  /// where N is the number of distinct <see cref="IStepNode.FlowLabel"/>
  /// values in the topology. Each block shows that Flow's local Steps
  /// and Items at full fidelity, with neighbor Flows collapsed.
  /// </summary>
  private static string RenderPerFlow(
    FlowMetadataContext ctx, FlowResult? result, bool showFullDag,
    MermaidFlowchartDirection direction, Theme theme, int headingLevel
  )
  {
    if (headingLevel < 1 || headingLevel > 6)
    {
      throw new ArgumentOutOfRangeException(
        nameof(headingLevel), headingLevel, "Heading level must be between 1 and 6."
      );
    }
    var headingPrefix = new string('#', headingLevel);
    var topology = showFullDag ? ctx.MergedFlow : ctx.EffectiveFlow;

    // Shared maps and run-state lookups — computed once, passed to each
    // per-flow block so colour curves and producer lookups stay
    // consistent across the document.
    var producerByLabel = new Dictionary<string, IStepNode>(StringComparer.Ordinal);
    var itemByLabel = new Dictionary<string, IItem>(StringComparer.Ordinal);
    foreach (var step in topology.Steps)
    {
      foreach (var output in step.Outputs)
      {
        producerByLabel[output.Label] = step;
        itemByLabel.TryAdd(output.Label, output);
      }
      foreach (var input in step.Inputs)
      {
        itemByLabel.TryAdd(input.Label, input);
      }
    }

    var resultsByLabel = result?.StepResults
      .ToDictionary(r => r.StepLabel, r => r, StringComparer.Ordinal);

    // Heat-map normaliser — same rule as Render(): exclude cache hits
    // and only normalise when 2+ non-cached succeeded steps ran.
    var ranSucceededDurations = result?.StepResults
      .OfType<StepResult.Succeeded>()
      .Where(s => !IsCacheHit(s))
      .Select(s => s.Duration)
      .ToList();
    var heatMapMax = ranSucceededDurations is { Count: >= 2 }
      ? ranSucceededDurations.Max()
      : (TimeSpan?)null;

    var cachePlanFreshLabels = ctx.CachePlan?.FreshStepLabels;
    var active = ctx.ActiveStepLabels;
    var profiles = ctx.ServiceProfiles ?? new DefaultServiceProfileProvider();

    // Group steps by FlowLabel — the same grouping the merged renderer
    // uses for subgraphs. The auto-threshold check in the provider also
    // counts distinct FlowLabels off this topology, so the threshold
    // and the rendered block count always agree.
    var stepsByFlow = topology.Steps
      .GroupBy(s => string.IsNullOrEmpty(s.FlowLabel) ? topology.Label : s.FlowLabel,
        StringComparer.Ordinal)
      .OrderBy(g => g.Key, StringComparer.Ordinal)
      .ToList();

    var sb = new StringBuilder();
    foreach (var group in stepsByFlow)
    {
      if (sb.Length > 0) sb.AppendLine();
      sb.Append(RenderSingleFlowBlock(
        flowLabel: group.Key,
        localSteps: group.ToList(),
        topology: topology,
        producerByLabel: producerByLabel,
        itemByLabel: itemByLabel,
        resultsByLabel: resultsByLabel,
        heatMapMax: heatMapMax,
        cachePlanFreshLabels: cachePlanFreshLabels,
        active: active,
        profiles: profiles,
        direction: direction,
        theme: theme,
        headingPrefix: headingPrefix
      ));
    }
    return sb.ToString();
  }

  /// <summary>
  /// Render one Flow's Mermaid block: full local subgraph plus
  /// dashed-border collapsed subgraphs for upstream Flows (containing
  /// the boundary Items they produce) and downstream Flows (containing
  /// the boundary Steps that consume locally-produced Items). External
  /// Items (consumed by the local Flow with no Flowthru producer)
  /// render above the local subgraph as bare nodes.
  /// </summary>
  private static string RenderSingleFlowBlock(
    string flowLabel,
    IReadOnlyList<IStepNode> localSteps,
    Flowthru.Flow.BuiltFlow topology,
    IReadOnlyDictionary<string, IStepNode> producerByLabel,
    IReadOnlyDictionary<string, IItem> itemByLabel,
    IReadOnlyDictionary<string, StepResult>? resultsByLabel,
    TimeSpan? heatMapMax,
    IReadOnlySet<string>? cachePlanFreshLabels,
    IReadOnlySet<string> active,
    IServiceProfileProvider profiles,
    MermaidFlowchartDirection direction,
    Theme theme,
    string headingPrefix
  )
  {
    var localStepLabels = new HashSet<string>(
      localSteps.Select(s => s.Label), StringComparer.Ordinal
    );
    var localItemLabels = new HashSet<string>(
      localSteps.SelectMany(s => s.Outputs).Select(o => o.Label),
      StringComparer.Ordinal
    );

    // Partition consumed Items into upstream-flow groups + external.
    var upstreamByFlow = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
    var externalItems = new SortedSet<string>(StringComparer.Ordinal);
    foreach (var step in localSteps)
    {
      foreach (var input in step.Inputs)
      {
        if (localItemLabels.Contains(input.Label)) continue;
        if (producerByLabel.TryGetValue(input.Label, out var producer))
        {
          var producerFlow = string.IsNullOrEmpty(producer.FlowLabel)
            ? topology.Label : producer.FlowLabel;
          if (!upstreamByFlow.TryGetValue(producerFlow, out var items))
          {
            items = new List<string>();
            upstreamByFlow[producerFlow] = items;
          }
          if (!items.Contains(input.Label, StringComparer.Ordinal))
          {
            items.Add(input.Label);
          }
        }
        else
        {
          externalItems.Add(input.Label);
        }
      }
    }
    foreach (var items in upstreamByFlow.Values)
    {
      items.Sort(StringComparer.Ordinal);
    }

    // Partition consumer Steps that live in other Flows into downstream-flow groups.
    var downstreamByFlow = new SortedDictionary<string, List<IStepNode>>(StringComparer.Ordinal);
    foreach (var step in topology.Steps)
    {
      if (localStepLabels.Contains(step.Label)) continue;
      var consumesLocal = step.Inputs.Any(i => localItemLabels.Contains(i.Label));
      if (!consumesLocal) continue;
      var consumerFlow = string.IsNullOrEmpty(step.FlowLabel)
        ? topology.Label : step.FlowLabel;
      if (!downstreamByFlow.TryGetValue(consumerFlow, out var steps))
      {
        steps = new List<IStepNode>();
        downstreamByFlow[consumerFlow] = steps;
      }
      steps.Add(step);
    }
    foreach (var steps in downstreamByFlow.Values)
    {
      steps.Sort((a, b) => StringComparer.Ordinal.Compare(a.Label, b.Label));
    }

    var sb = new StringBuilder();
    sb.AppendLine($"{headingPrefix} {flowLabel}");
    sb.AppendLine();
    sb.AppendLine("```mermaid");
    sb.AppendLine($"flowchart {DirectionCode(direction)}");
    sb.AppendLine();

    // ── External inputs ───────────────────────────────────────────────
    if (externalItems.Count > 0)
    {
      sb.AppendLine("    %% External Data Inputs");
      foreach (var label in externalItems)
      {
        itemByLabel.TryGetValue(label, out var item);
        sb.AppendLine($"    {ItemNodeSyntax(label, item)}");
      }
      sb.AppendLine();
    }

    // ── Upstream collapsed subgraphs ──────────────────────────────────
    var collapsedSubgraphIds = new List<string>();
    foreach (var (upstreamFlow, items) in upstreamByFlow)
    {
      var subgraphId = SanitizeId(upstreamFlow) + "_us";
      collapsedSubgraphIds.Add(subgraphId);
      sb.AppendLine($"    subgraph {subgraphId}[\"{EscapeLabel(upstreamFlow)}\"]");
      foreach (var label in items)
      {
        itemByLabel.TryGetValue(label, out var item);
        sb.AppendLine($"        {ItemNodeSyntax(label, item)}");
      }
      sb.AppendLine("    end");
      sb.AppendLine();
    }

    // ── Local subgraph (full fidelity) ────────────────────────────────
    sb.AppendLine($"    subgraph {SanitizeId(flowLabel)}[\"{EscapeLabel(flowLabel)}\"]");
    foreach (var step in localSteps)
    {
      var stepId = SanitizeId(step.Label);
      var stepActive = active.Contains(step.Label);

      StepResult? stepResult = null;
      resultsByLabel?.TryGetValue(step.Label, out stepResult);
      var isFailed = stepResult is StepResult.Failed;

      var displayLabel = step.Label;
      if (isFailed)
      {
        displayLabel += " (failed)";
      }
      displayLabel = AppendServices(displayLabel, step.ServiceDependencies);
      sb.AppendLine($"        {stepId}[\"{EscapeLabel(displayLabel)}\"]");

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
        sb.AppendLine(
          $"        style {stepId} fill:{theme.CachedStepColor},color:#FFFFFF"
        );
      }

      foreach (var output in step.Outputs)
      {
        sb.AppendLine($"        {ItemNodeSyntax(output.Label, output)}");
      }
    }
    sb.AppendLine("    end");
    sb.AppendLine();

    // ── Service legend (local services) ────────────────────────────────
    RenderServiceLegend(sb, ServiceUsageAnalyzer.Analyze(localSteps, profiles), theme);

    // ── Downstream collapsed subgraphs ────────────────────────────────
    // Collapsed-neighbor Steps render with no run-state fill, no service
    // compartment, no failed suffix. Reader navigates to that Flow's own
    // per-flow block for full styling.
    foreach (var (downstreamFlow, steps) in downstreamByFlow)
    {
      var subgraphId = SanitizeId(downstreamFlow) + "_ds";
      collapsedSubgraphIds.Add(subgraphId);
      sb.AppendLine($"    subgraph {subgraphId}[\"{EscapeLabel(downstreamFlow)}\"]");
      foreach (var step in steps)
      {
        var stepId = SanitizeId(step.Label);
        sb.AppendLine($"        {stepId}[\"{EscapeLabel(step.Label)}\"]");
      }
      sb.AppendLine("    end");
      sb.AppendLine();
    }

    // ── Edges ─────────────────────────────────────────────────────────
    // Edges from local Steps' inputs/outputs cover: external → local,
    // upstream-boundary → local, local → local, and local → boundary
    // (when the local output is a node that exists in the diagram).
    // Plus: local-boundary → downstream-consumer edges for each
    // collapsed consumer Step.
    sb.AppendLine("    %% Edges");
    foreach (var step in localSteps)
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
    foreach (var steps in downstreamByFlow.Values)
    {
      foreach (var step in steps)
      {
        var stepId = SanitizeId(step.Label);
        foreach (var input in step.Inputs)
        {
          if (!localItemLabels.Contains(input.Label)) continue;
          sb.AppendLine($"    {SanitizeId(input.Label)} --> {stepId}");
        }
      }
    }

    // ── Collapsed-subgraph styling ────────────────────────────────────
    if (collapsedSubgraphIds.Count > 0)
    {
      sb.AppendLine();
      sb.AppendLine("    classDef collapsed stroke-dasharray:5 5,fill:transparent");
      sb.AppendLine($"    class {string.Join(",", collapsedSubgraphIds)} collapsed");
    }

    // ── Failed-step decoration (local Steps only) ─────────────────────
    if (resultsByLabel is not null)
    {
      var failedIds = localSteps
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

  /// <summary>
  /// Build the Mermaid node syntax for a catalog item, picking a shape
  /// based on the item's runtime type and declared
  /// <see cref="IItem.StorageKind"/>. Decision order:
  /// <list type="bullet">
  ///   <item>
  ///     <c>ConfigurationItem&lt;T&gt;</c> (any generic instantiation)
  ///     renders as a hexagon — operator-tunable knob, not data.
  ///   </item>
  ///   <item>
  ///     Known service-backed <c>StorageKind</c> values (<c>gql</c>,
  ///     <c>http</c>, <c>database</c>) render as a stadium — runtime
  ///     service surface, not a static file.
  ///   </item>
  ///   <item>Default (anything else) renders as a cylinder.</item>
  /// </list>
  /// The renderer never enumerates languages or kinds beyond this
  /// table — Core declares the slots, extensions populate them,
  /// unknown values fall back to the cylinder default so new storage
  /// backends drop in without renderer changes.
  /// </summary>
  internal static string ItemNodeSyntax(string label, IItem? item)
  {
    var id = SanitizeId(label);
    // Node-generic service annotation: an item backed by a shared resource
    // (a database, a rate-limited endpoint) shows it in the same
    // divider-rule compartment a step uses for its services. The shape
    // (cylinder / stadium / hexagon) still says "this is data"; the
    // compartment says "on this resource" — look it up in the legend.
    var displayLabel = item is not null
      ? AppendServices(label, item.ServiceDependencies)
      : label;
    var escaped = EscapeLabel(displayLabel);

    if (item is not null && IsConfigurationItem(item))
    {
      return $"{id}{{{{\"{escaped}\"}}}}";
    }

    var kind = item?.StorageKind;
    if (!string.IsNullOrEmpty(kind) && IsServiceBackedKind(kind!))
    {
      return $"{id}([\"{escaped}\"])";
    }

    return $"{id}[(\"{escaped}\")]";
  }

  /// <summary>
  /// Append a node's service dependencies as an inline compartment —
  /// divider rule + one service display-name per line. Shared by steps and
  /// item cylinders so the annotation is node-generic (ADR-0019 #100 s7);
  /// returns <paramref name="baseLabel"/> unchanged when there are none.
  /// </summary>
  private static string AppendServices(string baseLabel, IReadOnlyList<ServiceDependency> deps) =>
    deps.Count > 0
      ? baseLabel + "<br>──<br>" + string.Join("<br>", deps.Select(d => d.DisplayName))
      : baseLabel;

  /// <summary>
  /// Emit the service-legend subgraph: one node per distinct service, each
  /// labelled with its name and a bulleted metadata list (cache, cap). No
  /// edges — steps and items reference services by name in their inline
  /// compartments, and the reader joins to this key. A no-op when the flow
  /// uses no services.
  /// </summary>
  private static void RenderServiceLegend(
    StringBuilder sb, IReadOnlyList<ServiceUsage> services, Theme theme)
  {
    if (services.Count == 0) return;

    sb.AppendLine("    %% Service legend");
    sb.AppendLine("    subgraph service_legend[\"services\"]");
    var ids = new List<string>(services.Count);
    foreach (var svc in services)
    {
      var id = "svc_" + SanitizeId(svc.DagId);
      ids.Add(id);
      sb.AppendLine($"        {id}[\"{EscapeLabel(ServiceLegendNodeLabel(svc))}\"]");
    }
    sb.AppendLine("    end");
    sb.AppendLine(
      $"    style service_legend fill:{theme.ServiceLegendColor},stroke:{theme.ServiceLegendStroke}");
    sb.AppendLine(
      $"    classDef serviceNode fill:{theme.ServiceLegendColor},stroke:{theme.ServiceLegendStroke}");
    sb.AppendLine($"    class {string.Join(",", ids)} serviceNode");
    sb.AppendLine();
  }

  /// <summary>
  /// A legend node's label: the service name, then a bulleted list of its
  /// metadata. Cache is shown only for injected (Use) services — it doesn't
  /// apply to an item's backing resource.
  /// </summary>
  private static string ServiceLegendNodeLabel(ServiceUsage svc)
  {
    var lines = new List<string> { svc.DisplayName };
    if (svc.Cacheable is bool cacheable)
    {
      lines.Add($"• cache: {(cacheable ? "neutral" : "affecting")}");
    }
    lines.Add($"• cap: {CapLabel(svc)}");
    return string.Join("<br>", lines);
  }

  /// <summary>
  /// Format a service's capacity for the legend: a single value when read
  /// and write agree (or only one op applies), or <c>write W · read R</c>
  /// when they differ. <see cref="int.MaxValue"/> renders as ∞.
  /// </summary>
  private static string CapLabel(ServiceUsage svc)
  {
    static string Fmt(int c) => c >= int.MaxValue ? "∞" : c.ToString();
    var hasRead = svc.Ops.Contains(ConflictOp.Read);
    var hasWrite = svc.Ops.Contains(ConflictOp.Use) || svc.Ops.Contains(ConflictOp.Write);
    if (hasRead && hasWrite && svc.WriteCapacity != svc.ReadCapacity)
    {
      return $"write {Fmt(svc.WriteCapacity)} · read {Fmt(svc.ReadCapacity)}";
    }
    return hasRead && !hasWrite ? Fmt(svc.ReadCapacity) : Fmt(svc.WriteCapacity);
  }

  /// <summary>
  /// True when <paramref name="item"/>'s runtime type is a closed or
  /// open instantiation of
  /// <see cref="Flowthru.Data.Catalog.Configuration.ConfigurationItem{T}"/>.
  /// We walk the type chain so a subclass — should one ever exist —
  /// still matches.
  /// </summary>
  private static bool IsConfigurationItem(IItem item)
  {
    for (var t = item.GetType(); t is not null; t = t.BaseType)
    {
      if (t.IsGenericType
          && t.GetGenericTypeDefinition()
            == typeof(Flowthru.Data.Catalog.Configuration.ConfigurationItem<>))
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Known service-backed storage kinds — items whose data lives
  /// behind a runtime service rather than a static file. Membership is
  /// closed here (a small whitelist) so unknown kinds fall back to the
  /// default cylinder shape rather than rendering as a stadium by
  /// accident; new service kinds opt in by adding to the set.
  /// </summary>
  private static bool IsServiceBackedKind(string kind) => kind switch
  {
    "gql" => true,
    "http" => true,
    "database" => true,
    _ => false,
  };

  /// <summary>
  /// Reduce an arbitrary label or resource id to a Mermaid-safe node id.
  /// Replaces every character outside <c>[A-Za-z0-9_]</c> with <c>_</c> —
  /// robust to the punctuation in service DagIds (the legend keys nodes by
  /// DagId, which carries <c>: | / + .</c> etc.), not just the handful of
  /// characters that show up in step/item labels.
  /// </summary>
  internal static string SanitizeId(string id) =>
    System.Text.RegularExpressions.Regex.Replace(id, "[^A-Za-z0-9_]", "_");

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
