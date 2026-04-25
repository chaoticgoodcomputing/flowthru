using Flowthru.Core.Abstractions;

namespace FlowthruCoverage.Data._04_Reporting.Schemas;

/// <summary>
/// Coverage aggregate annotated with the heatmap section it belongs to.
/// Produced by <see cref="Flows.Reporting.Steps.ClassifyCoverageStep"/> from
/// the tidy <see cref="Data._03_Primary.Schemas.PackageCoverageRow"/> data.
/// </summary>
[FlowthruSchema]
public partial record PivotCoverageRow
{
  /// <summary>"Library Tests", "Integration Tests", or "Examples".</summary>
  public required string Section { get; init; }

  /// <summary>
  /// Domain subgroup within the section: "Core", "Extensions", or "Misc".
  /// Derived from the base package name (or src package name for Y-axis grouping).
  /// </summary>
  public required string Subgroup { get; init; }

  /// <summary>Domain subgroup of the source package (Y axis): "Core", "Extensions", or "Misc". Authoritative from the project manifest.</summary>
  public required string SrcSubgroup { get; init; }

  /// <summary>The test or example project that produced this coverage reading.</summary>
  public required string TestProject { get; init; }

  /// <summary>The assembly/package being measured.</summary>
  public required string SrcPackage { get; init; }

  /// <summary>Percentage of instrumented lines hit (0–100), or -1 for ghost entries.</summary>
  public required double CoveragePercent { get; init; }

  /// <summary>
  /// When <see langword="true"/> this row is a ghost anchor: either the source package has no
  /// Cobertura data, the test project has no Cobertura data, or one side of the Library ↔
  /// LibraryTest pairing is absent from the manifest. CoveragePercent is -1.
  /// </summary>
  public required bool IsGhost { get; init; }
}
