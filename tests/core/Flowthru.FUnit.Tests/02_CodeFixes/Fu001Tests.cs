using Flowthru.FUnit.CodeFixes;
using Flowthru.FUnit.SourceGenerators;
using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.FUnit.Tests.CodeFixes;

/// <summary>
/// Tests for FU001: scaffolds a stub <c>Tests : FunitContext</c> class inside a
/// <c>#if FUNIT_ENABLED</c> guard when a step has no tests.
/// </summary>
[TestFixture]
[Category("CodeFixes")]
public class Fu001Tests
{
  // Minimal stubs so the test compilation resolves the referenced types.
  private const string Stubs = """
    namespace Flowthru.Core.Steps
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public class FlowthruStepAttribute : System.Attribute { }
    }
    namespace Flowthru.FUnit
    {
        public abstract class FunitContext { }
        [System.AttributeUsage(System.AttributeTargets.Method)]
        public class StepTestAttribute : System.Attribute
        {
            public StepTestAttribute(System.Type stepType) { }
        }
    }
    """;

  [Test]
  public async Task StepWithNoTests_ScaffoldsTestsClass()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Steps;

            [FlowthruStep]
            public class {|FU001:MyStep|}
            {
            }
        }
        """;

    // The fix inserts the Tests class before the class's closing brace, inserting
    // before the brace's full span (to preserve indentation of the closing brace).
    var fixedSource =
      "using Flowthru.FUnit;\n"
      + Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Steps;

            [FlowthruStep]
            public class MyStep
            {

        #if FUNIT_ENABLED
              /// <summary>FUnit tests for <see cref="MyStep"/>.</summary>
              public class Tests : FunitContext
              {
                  [StepTest(typeof(MyStep))]
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
    // after the fix the Tests class is inside #if FUNIT_ENABLED and becomes visible,
    // meaning FU001 no longer fires and the verifier can confirm convergence.
    var test = new CSharpCodeFixTest<
      FunitDiagnosticAnalyzer,
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
    // A step that already has a [StepTest] in the file → no FU001.
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Steps;
            using Flowthru.FUnit;

            [FlowthruStep]
            public class MyStep
            {
        #if FUNIT_ENABLED
                public class Tests : FunitContext
                {
                    [StepTest(typeof(MyStep))]
                    public void Test1() { }
                }
        #endif
            }
        }
        """;

    var analyzerTest = new CSharpAnalyzerTest<FunitDiagnosticAnalyzer, NUnit4Verifier>
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
