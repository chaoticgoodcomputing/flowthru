using Flowthru.Misc.DataFrames.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Analyzers.Tests;

/// <summary>
/// Verifies that FDFRAMES1002 fires when an object initializer inside a <c>Select</c> lambda
/// uses a collection binding (<c>Items = { x }</c>) or nested-object binding
/// (<c>Nested = { Prop = val }</c>) rather than a plain property assignment.
/// </summary>
/// <remarks>
/// The Select stub uses <c>Func&lt;&gt;</c> instead of <c>Expression&lt;Func&lt;&gt;&gt;</c>
/// to avoid the C# expression-tree restriction that prohibits collection and nested-object
/// initializer bindings in lambda-to-expression-tree conversions. The analyzer fires based on
/// the method's containing type name (<c>TypedFrameExtensions</c>), not the parameter type.
/// </remarks>
[TestFixture]
public class FDFRAMES1002Tests
{
  // Stub Select takes Func<> so that the lambda body compiles without C# expression-tree
  // restrictions while still triggering the TypedFrameExtensions name-match in the analyzer.
  private const string Stubs = """
    using System;
    using System.Collections.Generic;
    using Flowthru.Misc.DataFrames;

    namespace Flowthru.Misc.DataFrames
    {
        public class TypedFrame<T> { }

        public static class TypedFrameExtensions
        {
            public static TypedFrame<TResult> Select<TSource, TResult>(
                this TypedFrame<TSource> source,
                Func<TSource, TResult> selector) => null!;
        }
    }

    public class InputSchema { public string Name { get; set; } = ""; }

    public class OutputSchema
    {
        public string Label { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
        public NestedSchema Nested { get; set; } = new NestedSchema();
    }

    public class NestedSchema { public string Prop { get; set; } = ""; }
    """;

  private static DiagnosticResult FDFRAMES1002(int marker) =>
    new DiagnosticResult(DataFrameDiagnostics.NonAssignmentBinding).WithLocation(marker);

  // ─── Negative cases: valid assignment bindings → no diagnostic ──────────────

  [Test]
  public async Task AllAssignmentBindings_DoesNotReport()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => new OutputSchema { Label = x.Name });
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
                frame.Select(x => new { x.Name });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  // ─── Positive cases: non-assignment bindings → FDFRAMES1002 fires ─────────────

  [Test]
  public async Task CollectionBinding_Reports_FDFRAMES1002()
  {
    // Tags = { x.Name } is a MemberListBinding — RHS is a CollectionInitializerExpression.
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => new OutputSchema { {|#0:Tags = { x.Name }|} });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics = { FDFRAMES1002(0).WithArguments("Tags") },
    }.RunAsync();
  }

  [Test]
  public async Task NestedObjectBinding_Reports_FDFRAMES1002()
  {
    // Nested = { Prop = x.Name } is a MemberMemberBinding — RHS is an ObjectInitializerExpression.
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => new OutputSchema { {|#0:Nested = { Prop = x.Name }|} });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics = { FDFRAMES1002(0).WithArguments("Nested") },
    }.RunAsync();
  }

  [Test]
  public async Task MultipleNonAssignmentBindings_ReportsEach()
  {
    var source =
      Stubs
      + """

        class Tests
        {
            void M(Flowthru.Misc.DataFrames.TypedFrame<InputSchema> frame)
            {
                frame.Select(x => new OutputSchema
                {
                    Label = x.Name,
                    {|#0:Tags = { x.Name }|},
                    {|#1:Nested = { Prop = x.Name }|},
                });
            }
        }
        """;

    await new CSharpAnalyzerTest<TypedFrameExpressionAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
      ExpectedDiagnostics =
      {
        FDFRAMES1002(0).WithArguments("Tags"),
        FDFRAMES1002(1).WithArguments("Nested"),
      },
    }.RunAsync();
  }
}
