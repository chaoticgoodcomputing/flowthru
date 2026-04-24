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

  /// <summary>The test or example project that produced this coverage reading.</summary>
  public required string TestProject { get; init; }

  /// <summary>The assembly/package being measured.</summary>
  public required string SrcPackage { get; init; }

  /// <summary>Percentage of instrumented lines hit (0–100).</summary>
  public required double CoveragePercent { get; init; }
}
