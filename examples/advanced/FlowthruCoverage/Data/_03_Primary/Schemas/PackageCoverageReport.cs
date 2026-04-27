using Flowthru.Core.Abstractions;

namespace FlowthruCoverage.Data._03_Primary.Schemas;

/// <summary>
/// Top-level coverage report for a single source package (assembly).
/// Contains the full namespace → class → method → test project hit hierarchy.
/// </summary>
[FlowthruSchema]
public partial record PackageCoverageReport
{
  /// <summary>The assembly/package name as reported by Cobertura (e.g. "Flowthru.Core").</summary>
  public required string Package { get; init; }

  /// <summary>Coverage breakdown by namespace within this package.</summary>
  public required List<NamespaceCoverage> Namespaces { get; init; }
}

/// <summary>Coverage data for one namespace within a package.</summary>
[FlowthruSchema]
public partial record NamespaceCoverage
{
  /// <summary>
  /// The namespace (e.g. "Flowthru.Extensions.EFCore.Bulk.Internal").
  /// Empty string for types declared at the root namespace.
  /// </summary>
  public required string Namespace { get; init; }

  /// <summary>Coverage breakdown by class within this namespace.</summary>
  public required List<ClassCoverage> Classes { get; init; }
}

/// <summary>Coverage data for one class within a namespace.</summary>
[FlowthruSchema]
public partial record ClassCoverage
{
  /// <summary>
  /// Short (unqualified) class name (e.g. "BulkConfigMapper").
  /// Null for methods that Cobertura reports outside a named class.
  /// </summary>
  public required string? ClassName { get; init; }

  /// <summary>Coverage breakdown by method within this class.</summary>
  public required List<MethodCoverage> Methods { get; init; }
}

/// <summary>Coverage data for one method, aggregated across all test projects.</summary>
[FlowthruSchema]
public partial record MethodCoverage
{
  /// <summary>
  /// Method name and signature as reported by Cobertura
  /// (e.g. "ToBulkConfig(Flowthru.Extensions.EFCore.Bulk.BulkSaveOptions)").
  /// </summary>
  public required string MethodSignature { get; init; }

  /// <summary>
  /// Source file path (or SourceLink URL) of the file containing this method, taken from the
  /// underlying Cobertura class entry. Lets coverage triage navigate from a row to the code
  /// without grepping; null when the underlying class entry has no filename attribute.
  /// </summary>
  public required string? SourceFile { get; init; }

  /// <summary>
  /// Count of instrumented lines belonging to this method — a rough proxy for method size.
  /// Used during coverage triage to prioritize "big uncovered methods" over trivial getters.
  /// </summary>
  public required int LineCount { get; init; }

  /// <summary>Total hit count across all test projects combined.</summary>
  public required int TotalHits { get; init; }

  /// <summary>Per-test-project breakdown of hits for this method.</summary>
  public required List<TestProjectHits> TestProjects { get; init; }
}

/// <summary>Hit count for a specific method from a specific test project's run.</summary>
[FlowthruSchema]
public partial record TestProjectHits
{
  /// <summary>The test or example project that produced this reading.</summary>
  public required string TestProjectName { get; init; }

  /// <summary>
  /// Sum of all line-level hit counts for this method within this test project's run.
  /// 0 means the method was present in the instrumented binary but never entered.
  /// </summary>
  public required int TotalHits { get; init; }
}
