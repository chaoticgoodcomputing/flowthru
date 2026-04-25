using Flowthru.Core.Abstractions;

namespace FlowthruCoverage.Data._03_Primary.Schemas;

/// <summary>
/// Flat summary of hit intensity per method, suitable for sorting and filtering
/// to identify the least- and most-tested code in the repository.
/// </summary>
[FlowthruSchema]
public partial record MethodHitSummaryRow
{
  /// <summary>
  /// Fully-qualified method identifier: <c>{namespace}.{className}.{methodSignature}</c>.
  /// When <c>className</c> is absent (e.g. top-level functions), the segment is omitted.
  /// </summary>
  public required string Id { get; init; }

  /// <summary>Domain subgroup of the source package: <c>Core</c>, <c>Extensions</c>, or <c>Misc</c>.</summary>
  public required string Subgroup { get; init; }

  /// <summary>Total hit count summed across all test projects.</summary>
  public required int TotalHits { get; init; }

  /// <summary>Number of distinct test projects that hit this method at least once.</summary>
  public required int ProjectHits { get; init; }
}
