using Flowthru.Misc.DataFrames.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Analyzers.Tests;

/// <summary>
/// Verifies that FDFRAME1005 fires when an <c>Aggregate</c> result selector binding is
/// neither <c>ctx.Key</c> nor a call to an aggregation method on the context, and does not
/// fire for valid bindings.
/// </summary>
[TestFixture]
public class FdFrame1005Tests
{
  private const string Stubs = """
    using System;
    using System.Linq.Expressions;
    using Flowthru.Misc.DataFrames;

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
    public class AggResult
    {
        public string Category { get; set; } = "";
        public double AvgPrice { get; set; }
        public long RowCount { get; set; }
    }
    """;

  private static DiagnosticResult FDFRAME1005(int marker) =>
    new DiagnosticResult(DataFrameDiagnostics.InvalidAggregateBinding).WithLocation(marker);

  // ─── Negative cases: valid ctx.Key / ctx.Method() bindings → no diagnostic ──

  [Test]
  public async Task KeyAndAvg_DoesNotReport()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new AggResult
                {
                    Category = ctx.Key,
                    AvgPrice = ctx.Avg(x => x.Price),
                    RowCount = ctx.Count(),
                });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task AnonymousType_ValidBindings_DoesNotReport()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new { Category = ctx.Key, Avg = ctx.Avg(x => x.Price) });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task AnonymousType_ShorthandKeyAccess_DoesNotReport()
  {
    // new { ctx.Key } is shorthand — d.Expression is ctx.Key, a MemberAccessExpression on ctx.
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new { ctx.Key });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  // ─── Positive cases: non-ctx bindings → FDFRAME1005 fires ───────────────────

  [Test]
  public async Task LiteralBinding_Reports_FDFRAME1005()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new AggResult { Category = ctx.Key, AvgPrice = {|#0:42.0|} });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics = { FDFRAME1005(0).WithArguments("42.0") },
    }.RunAsync();
  }

  [Test]
  public async Task StringConstantBinding_Reports_FDFRAME1005()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new AggResult { Category = {|#0:"hardcoded"|} });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics = { FDFRAME1005(0).WithArguments("\"hardcoded\"") },
    }.RunAsync();
  }

  [Test]
  public async Task BinaryExpressionOnKey_Reports_FDFRAME1005()
  {
    // ctx.Key + "_suffix" is a BinaryExpression, not a direct member access on ctx.
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new AggResult { Category = {|#0:ctx.Key + "_suffix"|}  });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics = { FDFRAME1005(0).WithArguments("ctx.Key + \"_suffix\"") },
    }.RunAsync();
  }

  [Test]
  public async Task ChainedMemberAccess_Reports_FDFRAME1005()
  {
    // ctx.Key.Length accesses a member on the result of ctx.Key, not on ctx directly.
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new { Length = {|#0:ctx.Key.Length|} });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics = { FDFRAME1005(0).WithArguments("ctx.Key.Length") },
    }.RunAsync();
  }

  [Test]
  public async Task MixedBindings_ReportsOnlyInvalid()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.GroupedFrame<string, ProductSchema> grouped)
            {
                grouped.Aggregate(ctx => new AggResult
                {
                    Category = ctx.Key,
                    AvgPrice = ctx.Avg(x => x.Price),
                    RowCount = {|#0:(long)ctx.Count()|},
                });
            }
        }
        """;

    // (long)ctx.Count() is a CastExpression — not a direct ctx.Method() call.
    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics = { FDFRAME1005(0).WithArguments("(long)ctx.Count()") },
    }.RunAsync();
  }
}
