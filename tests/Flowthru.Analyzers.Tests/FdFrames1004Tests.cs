using Flowthru.Misc.DataFrames.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Analyzers.Tests;

/// <summary>
/// Verifies that FDFRAMES1004 fires when the <c>Aggregate</c> result selector body is not an
/// object-creation expression, and does not fire for valid object-creation forms.
/// </summary>
[TestFixture]
public class FDFRAMES1004Tests
{
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
        public class GroupedFrame<TKey, TSource> { }

        public sealed class AggregationContext<TKey, TSource>
        {
            private AggregationContext() { }
            public TKey Key => throw new InvalidOperationException();
            public double Avg(Expression<Func<TSource, double>> column) => throw new InvalidOperationException();
            public double Sum(Expression<Func<TSource, double>> column) => throw new InvalidOperationException();
            public long Count() => throw new InvalidOperationException();
        }

        public static class GroupedFrameExtensions
        {
            public static TypedFrame<TResult> Aggregate<TKey, TSource, TResult>(
                this GroupedFrame<TKey, TSource> source,
                Expression<Func<AggregationContext<TKey, TSource>, TResult>> resultSelector) => null!;
        }
    }

    public class ProductSchema { public string Category { get; set; } = ""; public double Price { get; set; } }
    public class AggResult { public string Category { get; set; } = ""; public double AvgPrice { get; set; } }
    public record AggResultRecord(string Category, double AvgPrice);
    """;

    private static DiagnosticResult FDFRAMES1004(int marker) =>
      new DiagnosticResult(DataFrameDiagnostics.InvalidAggregateResultBody).WithLocation(marker);

    // ─── Negative cases: object-creation forms → no diagnostic ──────────────────

    [Test]
    public async Task ObjectInitializer_DoesNotReport()
    {
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new AggResult { Category = ctx.Key, AvgPrice = ctx.Avg(x => x.Price) });
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
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new { Category = ctx.Key, AvgPrice = ctx.Avg(x => x.Price) });
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
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new AggResultRecord(ctx.Key, ctx.Avg(x => x.Price)));
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
        }.RunAsync();
    }

    // ─── Positive cases: non-object-creation bodies → FDFRAMES1004 fires ─────────

    [Test]
    public async Task MemberAccessBody_Reports_FDFRAMES1004()
    {
        // ctx.Key is a member access, not an object-creation expression.
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => {|#0:ctx.Key|});
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
            ExpectedDiagnostics = { FDFRAMES1004(0).WithArguments("ctx.Key") },
        }.RunAsync();
    }

    [Test]
    public async Task InvocationBody_Reports_FDFRAMES1004()
    {
        // ctx.Count() is a method call, not an object-creation expression.
        var source =
          Stubs
          + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => {|#0:ctx.Count()|});
            }
        }
        """;

        await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
        {
            TestCode = source,
            ExpectedDiagnostics = { FDFRAMES1004(0).WithArguments("ctx.Count()") },
        }.RunAsync();
    }
}
