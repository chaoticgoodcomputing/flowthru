using Flowthru.Extensions.Spark.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Analyzers.Tests;

/// <summary>
/// Verifies that FSPARK1002 fires when a lambda inside a <c>TypedFrameExtensions</c> call
/// uses a <c>string</c> or <c>Math</c> method not in <c>SparkTranslatableOperations</c>,
/// and does not fire for supported methods.
/// </summary>
[TestFixture]
public class FSpark1002Tests
{
  private const string Stubs = """
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Flowthru.Misc.DataFrames;

    namespace Flowthru.Misc.DataFrames
    {
        public class TypedFrame<T> { }

        public static class TypedFrameExtensions
        {
            public static TypedFrame<TSource> Where<TSource>(
                this TypedFrame<TSource> source,
                Expression<Func<TSource, bool>> predicate) => null!;

            public static TypedFrame<TResult> Select<TSource, TResult>(
                this TypedFrame<TSource> source,
                Expression<Func<TSource, TResult>> selector) => null!;
        }
    }

    public class PersonSchema { public string Name { get; set; } = ""; public double Score { get; set; } }
    """;

  // ─── Negative cases: supported methods → no diagnostic ──────────────────────

  [Test]
  public async Task SupportedStringMethod_ToUpper_DoesNotReport()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<PersonSchema> frame)
            {
                frame.Where(x => x.Name.ToUpper() == "ALICE");
            }
        }
        """;

    await new CSharpAnalyzerTest<SparkExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task SupportedStringMethod_Contains_DoesNotReport()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<PersonSchema> frame)
            {
                frame.Where(x => x.Name.Contains("Alice"));
            }
        }
        """;

    await new CSharpAnalyzerTest<SparkExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task SupportedMathMethod_Round_DoesNotReport()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<PersonSchema> frame)
            {
                frame.Select(x => new { Rounded = Math.Round(x.Score, 2) });
            }
        }
        """;

    await new CSharpAnalyzerTest<SparkExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  // ─── Positive cases: unsupported methods → FSPARK1002 fires ─────────────────

  [Test]
  public async Task UnsupportedStringMethod_PadLeft_Reports_FSPARK1002()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<PersonSchema> frame)
            {
                frame.Where(x => {|#0:x.Name.PadLeft(10)|} == "     Alice");
            }
        }
        """;

    await new CSharpAnalyzerTest<SparkExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics =
      {
        new DiagnosticResult(SparkDiagnostics.UnsupportedMethodCall)
          .WithLocation(0)
          .WithArguments(
            "String",
            "PadLeft",
            Helpers.SupportedStringList,
            Helpers.SupportedMathList
          ),
      },
    }.RunAsync();
  }

  [Test]
  public async Task UnsupportedStringMethod_IndexOf_Reports_FSPARK1002()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<PersonSchema> frame)
            {
                frame.Select(x => new { Idx = {|#0:x.Name.IndexOf("needle")|} });
            }
        }
        """;

    await new CSharpAnalyzerTest<SparkExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics =
      {
        new DiagnosticResult(SparkDiagnostics.UnsupportedMethodCall)
          .WithLocation(0)
          .WithArguments(
            "String",
            "IndexOf",
            Helpers.SupportedStringList,
            Helpers.SupportedMathList
          ),
      },
    }.RunAsync();
  }

  [Test]
  public async Task UnsupportedMathMethod_Pow_Reports_FSPARK1002()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<PersonSchema> frame)
            {
                frame.Select(x => new { Sq = {|#0:Math.Pow(x.Score, 2)|} });
            }
        }
        """;

    await new CSharpAnalyzerTest<SparkExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics =
      {
        new DiagnosticResult(SparkDiagnostics.UnsupportedMethodCall)
          .WithLocation(0)
          .WithArguments("Math", "Pow", Helpers.SupportedStringList, Helpers.SupportedMathList),
      },
    }.RunAsync();
  }

  [Test]
  public async Task NonFrameLambda_UnsupportedMethod_DoesNotReport()
  {
    // FSPARK1002 must not fire outside a TypedFrameExtensions call — same lambda shape
    // but invoked on a plain List<T> should be silent.
    var source =
      Stubs
      + """

        class Tests
        {
            void M(List<PersonSchema> list)
            {
                list.Where(x => x.Name.PadLeft(10) == "     Alice");
            }
        }
        """;

    await new CSharpAnalyzerTest<SparkExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }
}
