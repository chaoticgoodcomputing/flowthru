using Flowthru.FUnit.SourceGenerators;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.FUnit.SourceGenerators.Tests.Analysis;

/// <summary>
/// Tests for FU002 edge cases in <see cref="FunitDiagnosticAnalyzer"/>: preprocessor
/// guard nesting, wrong guard names, and <c>#else</c> branch placement.
///
/// The basic guarded/unguarded cases are covered by <c>FUnit.CodeFixes.Tests/Fu002Tests</c>
/// (which exercises the full fix round-trip). These tests focus on subtleties of
/// <see cref="Flowthru.FUnit.SourceGenerators.FunitSyntaxHelpers.IsInsidePreprocessorGuard"/>.
/// </summary>
[TestFixture]
[Category("Analyzers")]
public class FU002AnalyzerTests
{
  private const string Stubs = """
    namespace Flowthru.FUnit
    {
        public abstract class FunitContext { }
    }
    """;

  /// <summary>
  /// A class inside <c>#if SOME_OTHER_GUARD</c> is not inside <c>#if FUNIT_ENABLED</c>
  /// and should emit FU002. The guard name must match exactly.
  /// </summary>
  [Test]
  public async Task WrongGuardName_EmitsFU002()
  {
    // SOME_OTHER_GUARD is defined so the class is compiled and visible to the analyzer.
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.FUnit;

        #if SOME_OTHER_GUARD
            public class {|FU002:Tests|} : FunitContext { }
        #endif
        }
        """;

    var test = new CSharpAnalyzerTest<FunitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    };
    test.SolutionTransforms.Add(
      (solution, projectId) =>
      {
        var project = solution.GetProject(projectId)!;
        var parseOptions = (CSharpParseOptions)project.ParseOptions!;
        parseOptions = parseOptions.WithPreprocessorSymbols(
          parseOptions.PreprocessorSymbolNames.Concat(["SOME_OTHER_GUARD"])
        );
        return solution.WithProjectParseOptions(projectId, parseOptions);
      }
    );
    await test.RunAsync();
  }

  /// <summary>
  /// A class nested inside both an outer guard and <c>#if FUNIT_ENABLED</c> is properly
  /// guarded — no FU002. Tests that the stack-based check handles nested directives.
  /// </summary>
  [Test]
  public async Task NestedInsideFunitEnabled_NoDiagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.FUnit;

        #if OUTER
        #if FUNIT_ENABLED
            public class Tests : FunitContext { }
        #endif
        #endif
        }
        """;

    var test = new CSharpAnalyzerTest<FunitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    };
    // Both symbols must be defined so the class is compiled and visible.
    test.SolutionTransforms.Add(
      (solution, projectId) =>
      {
        var project = solution.GetProject(projectId)!;
        var parseOptions = (CSharpParseOptions)project.ParseOptions!;
        parseOptions = parseOptions.WithPreprocessorSymbols(
          parseOptions.PreprocessorSymbolNames.Concat(["OUTER", "FUNIT_ENABLED"])
        );
        return solution.WithProjectParseOptions(projectId, parseOptions);
      }
    );
    await test.RunAsync();
  }

  /// <summary>
  /// A class placed in the <c>#else</c> branch of <c>#if FUNIT_ENABLED</c> is NOT
  /// inside the guard — FU002 fires. The <c>#else</c> branch is active when
  /// <c>FUNIT_ENABLED</c> is not defined (the default in test compilations).
  /// </summary>
  [Test]
  public async Task ElseBranchOfFunitEnabled_EmitsFU002()
  {
    // FUNIT_ENABLED is NOT defined → #else branch IS compiled → class is visible to analyzer.
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.FUnit;

        #if FUNIT_ENABLED
            // intentionally empty — guard is off in default compilation
        #else
            public class {|FU002:Tests|} : FunitContext { }
        #endif
        }
        """;

    await new CSharpAnalyzerTest<FunitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }
}
