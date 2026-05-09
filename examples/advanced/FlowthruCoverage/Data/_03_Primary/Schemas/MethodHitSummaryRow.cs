using Flowthru.Data.Schema;

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

  /// <summary>
  /// Source file path (or SourceLink URL) of the file containing this method. Lets coverage
  /// triage open the code directly from the report; empty string when no filename is available.
  /// For name-summarized rows that collapse overloads from one class, all overloads share the
  /// same source file so the value is unambiguous.
  /// </summary>
  public required string SourceFile { get; init; }

  /// <summary>
  /// Approximate instrumented line count for this method (or sum across collapsed overloads in
  /// name-summarized rows). Useful for prioritizing larger uncovered methods over trivial ones.
  /// </summary>
  public required int LineCount { get; init; }

  /// <summary>Total hit count summed across all test projects.</summary>
  public required int TotalHits { get; init; }

  /// <summary>Number of distinct test projects that hit this method at least once.</summary>
  public required int ProjectHits { get; init; }
}
