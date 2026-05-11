using Flowthru.FUnit.CodeFixes;
using Flowthru.FUnit.SourceGenerators;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.FUnit.CodeFixes.Tests;

/// <summary>
/// Tests for FU002: wraps an unguarded <c>FUnitContext</c> subclass with
/// <c>#if FUNIT_ENABLED</c> / <c>#endif</c>.
/// </summary>
[TestFixture]
[Category("CodeFixes")]
public class Fu002Tests
{
  private const string Stubs = """
    namespace Flowthru.Step.Testing
    {
        public abstract class FUnitContext { }
    }
    """;

  [Test]
  public async Task UnguardedTests_WrapsWithPreprocessorDirective()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step.Testing;

            public class {|FU002:Tests|} : FUnitContext { }
        }
        """;

    var fixedSource =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step.Testing;
        #if FUNIT_ENABLED

            public class Tests : FUnitContext { }
        #endif
        }
        """;

    await new CSharpCodeFixTest<
      FUnitDiagnosticAnalyzer,
      Fu002WrapWithFUnitEnabledFix,
      NUnit4Verifier
    >
    {
      TestCode = source,
      FixedCode = fixedSource,
    }.RunAsync();
  }

  [Test]
  public async Task GuardedTests_NoDiagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Step.Testing;

        #if FUNIT_ENABLED
            public class Tests : FUnitContext { }
        #endif
        }
        """;

    await new CSharpAnalyzerTest<FUnitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }
}
