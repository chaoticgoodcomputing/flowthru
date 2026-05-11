using Flowthru.FUnit.CodeFixes;
using Flowthru.FUnit.SourceGenerators;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.FUnit.CodeFixes.Tests;

/// <summary>
/// Tests for FU001: scaffolds a stub <c>Tests : FUnitContext</c> class inside a
/// <c>#if FUNIT_ENABLED</c> guard when a <c>[FlowthruStep]</c> class has no
/// <c>[FUnitStepTest]</c>-decorated coverage anywhere in the compilation.
/// </summary>
[TestFixture]
[Category("CodeFixes")]
public class Fu001Tests
{
  // Minimal stubs so the test compilation resolves the referenced types without
  // pulling in the real Flowthru.Core / Flowthru.FUnit assemblies.
  private const string Stubs = """
    namespace Flowthru.Step
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public class FlowthruStepAttribute : System.Attribute { }
    }
    namespace Flowthru.Step.Testing
    {
        public abstract class FUnitContext { }
        [System.AttributeUsage(System.AttributeTargets.Method)]
        public class FUnitStepTestAttribute : System.Attribute
        {
            public FUnitStepTestAttribute(System.Type stepType) { }
        }
    }
    """;

  [Test]
  public async Task StepWithNoTests_ScaffoldsTestsClassAndClearsDiagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;

            [FlowthruStep]
            public class {|FU001:MyStep|}
            {
            }
        }
        """;

    // The fix inserts the Tests class before the class's closing brace and
    // adds `using Flowthru.Step.Testing;` at compilation-unit root. The scaffold
    // contains a [FUnitStepTest] placeholder so FU001 stops firing on the second pass.
    var fixedSource =
      "using Flowthru.Step.Testing;\n"
      + Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;

            [FlowthruStep]
            public class MyStep
            {

        #if FUNIT_ENABLED
              /// <summary>FUnit tests for <see cref="MyStep"/>.</summary>
              public class Tests : FUnitContext
              {
                  [FUnitStepTest(typeof(MyStep))]
                  public void TODO_WriteYourTestHere()
                  {
                      throw new System.NotImplementedException();
                  }
              }
        #endif
            }
        }
        """;

    // FUNIT_ENABLED must be defined in the test compilation so the fix converges:
    // after the fix runs the Tests class is inside #if FUNIT_ENABLED and becomes
    // visible, meaning FU001 no longer fires and the verifier confirms convergence.
    var test = new CSharpCodeFixTest<
      FUnitDiagnosticAnalyzer,
      Fu001ScaffoldTestsClassFix,
      NUnit4Verifier
    >
    {
      TestCode = source,
      FixedCode = fixedSource,
    };
    test.SolutionTransforms.Add(
      (solution, projectId) =>
      {
        var project = solution.GetProject(projectId)!;
        var parseOptions = (CSharpParseOptions)project.ParseOptions!;
        parseOptions = parseOptions.WithPreprocessorSymbols(
          parseOptions.PreprocessorSymbolNames.Concat(["FUNIT_ENABLED"])
        );
        return solution.WithProjectParseOptions(projectId, parseOptions);
      }
    );
    await test.RunAsync();
  }

  [Test]
  public async Task StepWithTests_NoDiagnostic()
  {
    // A step that already has a [FUnitStepTest] in the file → no FU001.
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step;
            using Flowthru.Step.Testing;

            [FlowthruStep]
            public class MyStep
            {
        #if FUNIT_ENABLED
                public class Tests : FUnitContext
                {
                    [FUnitStepTest(typeof(MyStep))]
                    public void Test1() { }
                }
        #endif
            }
        }
        """;

    var analyzerTest = new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    };
    analyzerTest.SolutionTransforms.Add(
      (solution, projectId) =>
      {
        var project = solution.GetProject(projectId)!;
        var parseOptions = (CSharpParseOptions)project.ParseOptions!;
        parseOptions = parseOptions.WithPreprocessorSymbols(
          parseOptions.PreprocessorSymbolNames.Concat(["FUNIT_ENABLED"])
        );
        return solution.WithProjectParseOptions(projectId, parseOptions);
      }
    );
    await analyzerTest.RunAsync();
  }
}
