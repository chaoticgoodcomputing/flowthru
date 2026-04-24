using Flowthru.Core.CodeFixes;
using Flowthru.Core.SourceGenerators.SchemaAnalysis;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Core.CodeFixes.Tests;

/// <summary>
/// Tests for FT1001: adds the <c>partial</c> modifier to a <c>[FlowthruSchema]</c>-annotated
/// type that is missing it.
/// </summary>
[TestFixture]
[Category("CodeFixes")]
public class Ft1001Tests
{
  // Minimal stub so the compiler resolves [FlowthruSchema] without a real assembly reference.
  private const string AttributeStub = """
    namespace Flowthru.Core.Abstractions
    {
        [System.AttributeUsage(System.AttributeTargets.All)]
        public class FlowthruSchemaAttribute : System.Attribute { }
    }
    """;

  [Test]
  public async Task NonPartialRecord_AddsMissingPartialKeyword()
  {
    var source =
      AttributeStub
      + """

        namespace TestProject
        {
            using Flowthru.Core.Abstractions;

            [FlowthruSchema]
            public record {|FT1001:MySchema|} { }
        }
        """;

    var fixedSource =
      AttributeStub
      + """

        namespace TestProject
        {
            using Flowthru.Core.Abstractions;

            [FlowthruSchema]
            public partial record MySchema { }
        }
        """;

    await new CSharpCodeFixTest<FlowthruSchemaAnalyzer, Ft1001AddPartialKeywordFix, NUnit4Verifier>
    {
      TestCode = source,
      FixedCode = fixedSource,
    }.RunAsync();
  }

  [Test]
  public async Task NonPartialClass_AddsMissingPartialKeyword()
  {
    var source =
      AttributeStub
      + """

        namespace TestProject
        {
            using Flowthru.Core.Abstractions;

            [FlowthruSchema]
            public class {|FT1001:MySchema|} { }
        }
        """;

    var fixedSource =
      AttributeStub
      + """

        namespace TestProject
        {
            using Flowthru.Core.Abstractions;

            [FlowthruSchema]
            public partial class MySchema { }
        }
        """;

    await new CSharpCodeFixTest<FlowthruSchemaAnalyzer, Ft1001AddPartialKeywordFix, NUnit4Verifier>
    {
      TestCode = source,
      FixedCode = fixedSource,
    }.RunAsync();
  }

  [Test]
  public async Task PartialRecord_NoDiagnostic()
  {
    var source =
      AttributeStub
      + """

        namespace TestProject
        {
            using Flowthru.Core.Abstractions;

            [FlowthruSchema]
            public partial record MySchema { }
        }
        """;

    await new CSharpAnalyzerTest<FlowthruSchemaAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }
}
