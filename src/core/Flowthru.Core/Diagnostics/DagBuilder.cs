using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Step;

namespace Flowthru.Diagnostics;

/// <summary>
/// Walks a <see cref="BuiltFlow"/> and produces a value-shaped
/// graph suitable for diagram emission (Mermaid, GraphViz) or
/// metadata export (JSON manifests). The graph captures the
/// bipartite practical structure from §2.4 — items as places,
/// steps as arrows — alongside the flow's topological ordering.
/// </summary>
/// <remarks>
/// <para>
/// This is a pure graph builder — no IO, no rendering. Diagram
/// extensions (<c>Flowthru.Mermaid</c>) consume the
/// <see cref="DagDescription"/> and emit format-specific output.
/// </para>
/// </remarks>
public static class DagBuilder
{
  /// <summary>
  /// Build a <see cref="DagDescription"/> for the supplied flow.
  /// Item nodes are deduplicated by <see cref="INode.Label"/>.
  /// </summary>
  public static DagDescription Build(BuiltFlow flow)
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));

    var items = new Dictionary<string, DagItem>(StringComparer.Ordinal);
    var stepNodes = new List<DagStep>(flow.Steps.Count);
    var edges = new List<DagEdge>();

    foreach (var step in flow.Steps)
    {
      foreach (var input in step.Inputs)
      {
        if (!items.ContainsKey(input.Label))
        {
          items[input.Label] = new DagItem(input.Label, input.DataType.FullName ?? input.DataType.Name);
        }
        edges.Add(new DagEdge(input.Label, step.Label, DagEdgeKind.ItemToStep));
      }
      foreach (var output in step.Outputs)
      {
        if (!items.ContainsKey(output.Label))
        {
          items[output.Label] = new DagItem(output.Label, output.DataType.FullName ?? output.DataType.Name);
        }
        edges.Add(new DagEdge(step.Label, output.Label, DagEdgeKind.StepToItem));
      }
      stepNodes.Add(new DagStep(step.Label));
    }

    return new DagDescription(
      FlowLabel: flow.Label,
      Items: items.Values.ToList(),
      Steps: stepNodes,
      Edges: edges
    );
  }
}

/// <summary>
/// Value-shaped DAG description emitted by <see cref="DagBuilder"/>.
/// </summary>
public sealed record DagDescription(
  string FlowLabel,
  IReadOnlyList<DagItem> Items,
  IReadOnlyList<DagStep> Steps,
  IReadOnlyList<DagEdge> Edges
);

/// <summary>An item node in a rendered DAG.</summary>
public sealed record DagItem(string Label, string DataTypeName);

/// <summary>A step node in a rendered DAG.</summary>
public sealed record DagStep(string Label);

/// <summary>An edge in a rendered DAG. Bipartite: item↔step only.</summary>
public sealed record DagEdge(string From, string To, DagEdgeKind Kind);

/// <summary>Bipartite edge direction: item → step (input) or step → item (output).</summary>
public enum DagEdgeKind
{
  ItemToStep,
  StepToItem,
}
