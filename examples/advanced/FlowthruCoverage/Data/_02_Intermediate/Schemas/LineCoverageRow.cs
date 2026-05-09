using Flowthru.Data.Schema;

namespace FlowthruCoverage.Data._02_Intermediate.Schemas;

/// <summary>
/// A single instrumented line from a coverage report, tagged with its origin test project.
/// One row per source line per test project that exercised it.
/// </summary>
[FlowthruSchema]
public partial record LineCoverageRow
{
  /// <summary>The test or example project that produced this coverage reading (derived from file name).</summary>
  public required string TestProject { get; init; }

  /// <summary>The assembly/package name as reported by Cobertura (e.g. "Flowthru.Core").</summary>
  public required string SrcPackage { get; init; }

  /// <summary>The source file path as recorded in the Cobertura XML.</summary>
  public required string SourceFile { get; init; }

  /// <summary>The fully-qualified class name.</summary>
  public required string ClassName { get; init; }

  /// <summary>The method name.</summary>
  public required string MethodName { get; init; }

  /// <summary>The method signature.</summary>
  public required string MethodSignature { get; init; }

  /// <summary>The source line number.</summary>
  public required int LineNumber { get; init; }

  /// <summary>
  /// Number of times this line was executed. 0 = uncovered; &gt;0 = covered with that many hits.
  /// </summary>
  public required int Hits { get; init; }
}
