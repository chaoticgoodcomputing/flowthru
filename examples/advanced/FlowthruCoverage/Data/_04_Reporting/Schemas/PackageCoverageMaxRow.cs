using Flowthru.Core.Abstractions;

namespace FlowthruCoverage.Data._04_Reporting.Schemas;

/// <summary>
/// Per-source-package coverage rolled up to the maximum across test projects.
/// </summary>
/// <remarks>
/// The detail-level <see cref="PivotCoverageRow"/> emits one row per
/// (TestProject, SrcPackage) pair, which means a package tested by multiple
/// projects (e.g. <c>Flowthru.Core.SourceGenerators</c>, exercised by both
/// <c>Flowthru.Core.Tests</c> at 0% and <c>Flowthru.Core.SourceGenerators.Tests</c>
/// at 74.41%) appears multiple times. Naive averaging or first-row reporting drags
/// the package's apparent coverage to 0%; the max across the row group is the
/// authoritative per-package figure.
/// </remarks>
[FlowthruSchema]
public partial record PackageCoverageMaxRow
{
  /// <summary>The assembly/package being measured.</summary>
  public required string SrcPackage { get; init; }

  /// <summary>Domain subgroup of the source package: "Core", "Extensions", or "Misc".</summary>
  public required string SrcSubgroup { get; init; }

  /// <summary>Highest coverage percentage observed for this package across any test project.</summary>
  public required double MaxCoveragePercent { get; init; }

  /// <summary>
  /// The test project that produced the maximum reading — the one consumers should
  /// inspect for actual test surface against this package.
  /// </summary>
  public required string BestTestProject { get; init; }
}
