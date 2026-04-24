using Flowthru.Core.Abstractions;

namespace FlowthruCoverage.Data._03_Primary.Schemas;

/// <summary>
/// Per-(TestProject, SrcPackage) coverage aggregate — the pivot source for the heatmap.
/// Load this CSV into Excel or Python and pivot on <see cref="TestProject"/> vs
/// <see cref="SrcPackage"/> with <see cref="CoveragePercent"/> as the value to get the heatmap.
/// </summary>
[FlowthruSchema]
public partial record PackageCoverageRow
{
  /// <summary>The test or example project that produced this coverage reading.</summary>
  public required string TestProject { get; init; }

  /// <summary>The assembly/package being measured.</summary>
  public required string SrcPackage { get; init; }

  /// <summary>Number of lines with at least one hit.</summary>
  public required int CoveredLines { get; init; }

  /// <summary>Total number of instrumented lines.</summary>
  public required int TotalLines { get; init; }

  /// <summary>Percentage of instrumented lines covered (0–100).</summary>
  public required double CoveragePercent { get; init; }
}
