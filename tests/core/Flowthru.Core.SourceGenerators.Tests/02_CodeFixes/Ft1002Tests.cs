using Flowthru.Core.CodeFixes;
using Flowthru.Core.SourceGenerators.SchemaAnalysis;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Core.SourceGenerators.Tests.CodeFixes;

/// <summary>
/// Tests for FT1002: removes manually-applied marker interfaces that the source
/// generator would emit, resolving the conflict.
/// </summary>
[TestFixture]
[Category("CodeFixes")]
public class Ft1002Tests
{
  private const string Stubs = """
    namespace Flowthru.Core.Abstractions
    {
        [System.AttributeUsage(System.AttributeTargets.All)]
        public class FlowthruSchemaAttribute : System.Attribute { }

        public interface IFlatSchema { }
        public interface INestedSchema { }
        public interface ITextSerializable { }
        public interface IBinarySerializable { }
        public interface IStructuredSerializable { }
    }
    """;

  [Test]
  public async Task ManualIFlatSchema_IsRemoved()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Abstractions;

            [FlowthruSchema]
            public partial record {|FT1002:MySchema|} : IFlatSchema { }
        }
        """;

    var fixedSource =
      Stubs
      + """

        namespace TestProject
        {
            using Flowthru.Core.Abstractions;

            [FlowthruSchema]
            public partial record MySchema { }
        }
        """;

    await new CSharpCodeFixTest<
      FlowthruSchemaAnalyzer,
      Ft1002RemoveConflictingInterfaceFix,
      NUnit4Verifier
    >
    {
      TestCode = source,
      FixedCode = fixedSource,
    }.RunAsync();
  }

  [Test]
  public async Task NoManualInterface_NoDiagnostic()
  {
    var source =
      Stubs
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
