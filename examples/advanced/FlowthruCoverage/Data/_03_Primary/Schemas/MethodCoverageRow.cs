using Flowthru.Core.Abstractions;

namespace FlowthruCoverage.Data._03_Primary.Schemas;

/// <summary>
/// Per-(SrcPackage, Namespace, ClassName, MethodName, MethodSignature, TestProject) coverage
/// aggregate. Tidy format: one row per method × test project pairing.
///
/// This is a model input table. Pivot, filter, or aggregate downstream to answer questions
/// such as "which methods are only covered by one test project?" or "which methods are
/// never touched at all?".
/// </summary>
[FlowthruSchema]
public partial record MethodCoverageRow
{
  /// <summary>The assembly/package name as reported by Cobertura (e.g. "Flowthru.Core").</summary>
  public required string SrcPackage { get; init; }

  /// <summary>
  /// The namespace portion of the fully-qualified class name
  /// (e.g. "Flowthru.Extensions.EFCore.Bulk.Internal").
  /// </summary>
  public required string Namespace { get; init; }

  /// <summary>
  /// The short (unqualified) class name (e.g. "BulkConfigMapper").
  /// </summary>
  public required string ClassName { get; init; }

  /// <summary>The method name as reported by Cobertura (e.g. "ToBulkConfig").</summary>
  public required string MethodName { get; init; }

  /// <summary>
  /// The method signature as reported by Cobertura
  /// (e.g. "(Flowthru.Extensions.EFCore.Bulk.BulkSaveOptions)").
  /// </summary>
  public required string MethodSignature { get; init; }

  /// <summary>The test or example project that produced this coverage reading.</summary>
  public required string TestProject { get; init; }

  /// <summary>
  /// Sum of all line-level hit counts for this method within this test project's run.
  /// 0 = method was never entered; &gt;0 = total executions across all instrumented lines.
  /// </summary>
  public required int TotalHits { get; init; }

  /// <summary>Number of instrumented lines in this method that were hit at least once.</summary>
  public required int CoveredLines { get; init; }

  /// <summary>Total number of instrumented lines in this method.</summary>
  public required int TotalLines { get; init; }
}
