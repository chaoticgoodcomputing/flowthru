using Flowthru.Data.Schema;

namespace FlowthruCoverage.Data._04_Reporting.Schemas;

/// <summary>
/// One node in the per-library icicle hierarchy, augmented with line-level
/// provenance counts. Carries the four counts the downstream renderer needs
/// to compute its RGB colour encoding from a single row.
/// </summary>
/// <remarks>
/// <para>
/// The renderer maps the three coverage ratios to RGB channels:
/// <list type="bullet">
///   <item><c>R = 1 − (IntegrationCovered / TotalLines)</c></item>
///   <item><c>G = AnyCovered / TotalLines</c></item>
///   <item><c>B = 1 − (UnitCovered / TotalLines)</c></item>
/// </list>
/// Corner colours:
/// <list type="bullet">
///   <item><strong>green</strong> = covered by both unit + integration (robust)</item>
///   <item><strong>yellow</strong> = unit-only (no integration test exercises it)</item>
///   <item><strong>cyan</strong> = integration-only (no unit test exercises it)</item>
///   <item><strong>white</strong> = covered by peer test only</item>
///   <item><strong>magenta</strong> = uncovered</item>
/// </list>
/// Interior colours are continuous along all three axes, so a tile like
/// "72 % unit / 35 % integration / 84 % total" reads as olive — mostly
/// covered, unit-leaning.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record ProvenanceIcicleNode
{
  /// <summary>Unique node identifier across the whole tree.</summary>
  public required string Id { get; init; }

  /// <summary>Parent node identifier; empty string for project (root) nodes.</summary>
  public required string ParentId { get; init; }

  /// <summary>Display label for this node (project name, file path-suffix, or method name).</summary>
  public required string Label { get; init; }

  /// <summary>Hierarchy level: <c>Project</c>, <c>Directory</c>, <c>File</c>, or <c>Method</c>.</summary>
  public required string Level { get; init; }

  /// <summary>Total number of instrumented lines under this node.</summary>
  public required int TotalLines { get; init; }

  /// <summary>Lines hit by <em>any</em> test project (unit + integration + peer).</summary>
  public required int AnyCovered { get; init; }

  /// <summary>Lines hit by the library's unit-test project (<c>{SrcPackage}.Tests</c>).</summary>
  public required int UnitCovered { get; init; }

  /// <summary>
  /// Lines hit by at least one manifest <c>Example</c>-type project — example
  /// pipelines act as integration coverage for the libraries they exercise.
  /// </summary>
  public required int IntegrationCovered { get; init; }
}
