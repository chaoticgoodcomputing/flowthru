using Flowthru.Core.Abstractions;

namespace FlowthruCoverage.Data._04_Reporting.Schemas;

/// <summary>
/// One node in the project → file → method coverage icicle hierarchy. Flat shape suited to
/// Plotly's <c>ids</c>/<c>parents</c> tree input — every row is a distinct node, with
/// <see cref="ParentId"/> pointing at its containing node (empty string for project roots).
/// Aggregated line counts are pre-summed at each level so the renderer can use
/// <c>branchvalues="total"</c> directly.
/// </summary>
[FlowthruSchema]
public partial record IcicleCoverageNode
{
  /// <summary>Unique node identifier across the whole tree.</summary>
  public required string Id { get; init; }

  /// <summary>Parent node identifier; empty string for project (root) nodes.</summary>
  public required string ParentId { get; init; }

  /// <summary>Display label for this node (project name, file path-suffix, or method name).</summary>
  public required string Label { get; init; }

  /// <summary>Hierarchy level: <c>Project</c>, <c>File</c>, or <c>Method</c>.</summary>
  public required string Level { get; init; }

  /// <summary>Number of instrumented lines covered (hit at least once by any test project).</summary>
  public required int CoveredLines { get; init; }

  /// <summary>Total number of instrumented lines under this node.</summary>
  public required int TotalLines { get; init; }

  /// <summary>Coverage percentage (0–100); zero when <see cref="TotalLines"/> is zero.</summary>
  public required double CoveragePercent { get; init; }
}
