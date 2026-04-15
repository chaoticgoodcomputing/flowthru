using Flowthru.Misc.DataFrames.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Analyzers.Tests;

/// <summary>
/// Verifies that FDFRAMES1003 fires when a <c>Select</c> lambda uses a positional constructor
/// call on a plain class (not a record or anonymous type), and does not fire for records or
/// anonymous types.
/// </summary>
[TestFixture]
public class FDFRAMES1003Tests
{
    // Records require System.Runtime.CompilerServices.IsExternalInit, which the analyzer
    // test framework doesn't inject automatically. This shim satisfies the requirement.
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

    public class InputSchema { public string Name { get; set; } = ""; public int Age { get; set; } }

    public class PlainClass
    {
        public PlainClass(string name, int age) { Name = name; Age = age; }
        public string Name { get; }
        public int Age { get; }
    }

    public record PersonRecord(string Name, int Age);
    """;

    private static DiagnosticResult FDFRAMES1003(int marker) =>
      new DiagnosticResult(DataFrameDiagnostics.PositionalConstructorNonRecord).WithLocation(marker);

    // ─── Negative cases: record and anonymous types → no diagnostic ─────────────

    [Test]
    public async Task RecordPositionalConstructor_DoesNotReport()
    {
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => new PersonRecord(x.Name, x.Age));
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
        }.RunAsync();
    }

    [Test]
    public async Task AnonymousType_DoesNotReport()
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
    public async Task ObjectInitializer_PlainClass_DoesNotReport()
    {
        // FDFRAMES1003 only fires for positional constructors (args, no initializer).
        // An object initializer on a plain class is valid.
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => new PlainClass(x.Name, x.Age) { });
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
        }.RunAsync();
    }

    // ─── Positive case: positional constructor on a plain class → FDFRAMES1003 ───

    [Test]
    public async Task PlainClassPositionalConstructor_Reports_FDFRAMES1003()
    {
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => {|#0:new PlainClass(x.Name, x.Age)|});
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
            ExpectedDiagnostics = { FDFRAMES1003(0).WithArguments("PlainClass") },
        }.RunAsync();
    }
}
