using Flowthru.Misc.DataFrames.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Analyzers.Tests;

/// <summary>
/// Verifies that FDFRAMES1001 fires when a <c>Select</c> lambda body is not an object
/// initializer, positional record constructor, anonymous type, or member access — and
/// does not fire for those valid forms.
/// </summary>
[TestFixture]
public class FDFRAMES1001Tests
{
    // Minimal stubs that satisfy the analyzer's type-name check without requiring the
    // full runtime library (which in turn requires the Spark JVM at runtime).
    private const string Stubs = """
    using System;
    using System.Linq.Expressions;
    using Flowthru.Misc.DataFrames;

    namespace System.Runtime.CompilerServices
    {
        internal sealed class IsExternalInit { }
    }

    namespace Flowthru.Misc.DataFrames
    {
        public class TypedFrame<T> { }

        public static class TypedFrameExtensions
        {
            public static TypedFrame<TResult> Select<TSource, TResult>(
                this TypedFrame<TSource> source,
                Expression<Func<TSource, TResult>> selector) => null!;
        }
    }

    public class InputSchema  { public string Name { get; set; } = ""; public int Age { get; set; } }
    public class OutputSchema
    {
        public OutputSchema() { }
        public OutputSchema(string name, int age) { Name = name; Age = age; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
    public record OutputRecord(string Name, int Age);
    """;

    private static DiagnosticResult FDFRAMES1001(int line, int col) =>
      new DiagnosticResult(DataFrameDiagnostics.InvalidProjectionBody).WithLocation(line, col);

    // ─── Negative cases: valid projection bodies → no diagnostic ────────────────

    [Test]
    public async Task ObjectInitializer_DoesNotReport()
    {
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => new OutputSchema { Name = x.Name, Age = x.Age });
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
        }.RunAsync();
    }

    [Test]
    public async Task RecordPositionalConstructor_DoesNotReport()
    {
        // Uses an actual record so FDFRAMES1003 (positional ctor on non-record) does not fire
        // — this test specifically verifies FDFRAMES1001 is silent for valid positional forms.
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => new OutputRecord(x.Name, x.Age));
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
        }.RunAsync();
    }

    [Test]
    public async Task AnonymousTypeCreation_DoesNotReport()
    {
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => new { x.Name, x.Age });
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
        }.RunAsync();
    }

    [Test]
    public async Task SingleMemberAccess_DoesNotReport()
    {
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => x.Name);
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
        }.RunAsync();
    }

    // ─── Positive cases: invalid projection bodies → FDFRAMES1001 fires ──────────

    [Test]
    public async Task TupleCreate_Reports_FDFRAMES1001()
    {
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => {|#0:Tuple.Create(x.Name, x.Age)|});
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
            ExpectedDiagnostics =
      {
        new DiagnosticResult(DataFrameDiagnostics.InvalidProjectionBody)
          .WithLocation(0)
          .WithArguments("Tuple.Create(x.Name, x.Age)"),
      },
        }.RunAsync();
    }

    [Test]
    public async Task ArbitraryMethodCall_Reports_FDFRAMES1001()
    {
        var source =
          Stubs
          + """

        class Tests
        {
            static OutputSchema Map(InputSchema x) => new OutputSchema();

            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => {|#0:Map(x)|});
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
            ExpectedDiagnostics =
      {
        new DiagnosticResult(DataFrameDiagnostics.InvalidProjectionBody)
          .WithLocation(0)
          .WithArguments("Map(x)"),
      },
        }.RunAsync();
    }
}
