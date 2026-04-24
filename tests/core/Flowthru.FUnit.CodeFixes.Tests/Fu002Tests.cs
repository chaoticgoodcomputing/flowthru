using Flowthru.FUnit.CodeFixes;
using Flowthru.FUnit.SourceGenerators;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.FUnit.CodeFixes.Tests;

/// <summary>
/// Tests for FU002: wraps an unguarded <c>FunitContext</c> subclass with
/// <c>#if FUNIT_ENABLED</c> / <c>#endif</c>.
/// </summary>
[TestFixture]
[Category("CodeFixes")]
public class Fu002Tests
{
  private const string Stubs = """
    namespace Flowthru.FUnit
    {
        public abstract class FunitContext { }
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
            using Flowthru.FUnit;

            public class {|FU002:Tests|} : FunitContext { }
        }
        """;

    var fixedSource =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.FUnit;
        #if FUNIT_ENABLED

            public class Tests : FunitContext { }
        #endif
        }
        """;

    await new CSharpCodeFixTest<
      FunitDiagnosticAnalyzer,
      Fu002WrapWithFunitEnabledFix,
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
            using Flowthru.FUnit;

        #if FUNIT_ENABLED
            public class Tests : FunitContext { }
        #endif
        }
        """;

    await new CSharpAnalyzerTest<FunitDiagnosticAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }
}
