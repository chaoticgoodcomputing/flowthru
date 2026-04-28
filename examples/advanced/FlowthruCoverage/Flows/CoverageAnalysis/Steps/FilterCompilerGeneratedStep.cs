using Flowthru.Core.Steps;
using FlowthruCoverage.Data._02_Intermediate.Schemas;
#if FUNIT_ENABLED
using Flowthru.FUnit;
#endif

namespace FlowthruCoverage.Flows.Coverage.Steps;

/// <summary>
/// Drops compiler-synthesized rows from line coverage so the downstream method-aggregation
/// path reports only authored methods. The package-aggregation path
/// (<see cref="AggregateCoverageStep"/>) continues to consume the unfiltered
/// <c>LineCoverage</c> item, preserving the full instrumented surface in package-level
/// percentages.
/// </summary>
[FlowthruStep]
public static class FilterCompilerGeneratedStep
{
  public static Func<
    IEnumerable<LineCoverageRow>,
    IEnumerable<LineCoverageRow>
  > Create()
  {
    return rows =>
      rows.Where(r => !CompilerGeneratedFilter.IsCompilerGenerated(r.ClassName, r.MethodName));
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="FilterCompilerGeneratedStep"/>.</summary>
  public class Tests : FunitContext
  {
    private static LineCoverageRow Row(string className, string methodName) =>
      new()
      {
        TestProject = "Flowthru.Core.Tests",
        SrcPackage = "Flowthru.Core",
        SourceFile = "src/core/Flowthru.Core/Stub.cs",
        ClassName = className,
        MethodName = methodName,
        MethodSignature = "()",
        LineNumber = 1,
        Hits = 0,
      };

    /// <summary>Empty input produces empty output — no spurious rows materialized.</summary>
    [StepTest(typeof(FilterCompilerGeneratedStep))]
    public void EmptyInput_YieldsEmptyOutput()
    {
      var result = Invoke(FilterCompilerGeneratedStep.Create(), Enumerable.Empty<LineCoverageRow>());

      Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Mixed input: every row whose class or method name carries an angle-bracket marker
    /// is dropped; authored rows survive untouched and in original order.
    /// </summary>
    [StepTest(typeof(FilterCompilerGeneratedStep))]
    public void MixedInput_DropsCompilerGeneratedAndPreservesAuthored()
    {
      var input = new[]
      {
        Row("Flowthru.Core.Effects.FlowUnit", "ToString"),
        Row("Flowthru.Core.Cli.FlowthruCli/<RunAsync>d__5", "MoveNext"),
        Row("Flowthru.Core.Flows.Flow/<>c", "<ExecuteStepAsync>b__50_0"),
        Row("Flowthru.Core.Flows.Flow/<>c__DisplayClass50_0", "<ExecuteStepAsync>b__1"),
        Row("Flowthru.Core.Cli.FlowthruCli", "FormatResult"),
      };

      var result = Invoke(FilterCompilerGeneratedStep.Create(), input).ToList();

      Assert.That(result, Has.Count.EqualTo(2));
      Assert.That(result[0].MethodName, Is.EqualTo("ToString"));
      Assert.That(result[1].MethodName, Is.EqualTo("FormatResult"));
    }

    /// <summary>
    /// All-authored input passes through unchanged — the filter does not mutate or reorder
    /// surviving rows.
    /// </summary>
    [StepTest(typeof(FilterCompilerGeneratedStep))]
    public void AllAuthored_PassesThroughUnchanged()
    {
      var input = new[]
      {
        Row("Flowthru.Core.Effects.FlowUnit", "ToString"),
        Row("Flowthru.Core.Cli.FlowthruCli", "FormatResult"),
      };

      var result = Invoke(FilterCompilerGeneratedStep.Create(), input).ToList();

      Assert.That(result, Has.Count.EqualTo(2));
      Assert.That(result, Is.EqualTo(input));
    }

    /// <summary>
    /// All-compiler-generated input is fully dropped — output is empty even when the input
    /// has many rows.
    /// </summary>
    [StepTest(typeof(FilterCompilerGeneratedStep))]
    public void AllCompilerGenerated_YieldsEmptyOutput()
    {
      var input = new[]
      {
        Row("Flowthru.Core.Cli.FlowthruCli/<RunAsync>d__5", "MoveNext"),
        Row("Flowthru.Core.Flows.Flow/<>c", "<ExecuteStepAsync>b__50_0"),
        Row("Flowthru.FUnit.Samples.SampleBuilder/<>c__2`1", "<FromCsv>b__2_0"),
      };

      var result = Invoke(FilterCompilerGeneratedStep.Create(), input);

      Assert.That(result, Is.Empty);
    }
  }
#endif
}
