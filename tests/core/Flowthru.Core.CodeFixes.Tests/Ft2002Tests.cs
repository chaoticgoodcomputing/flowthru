using Flowthru.Core.CodeFixes;
using Flowthru.Core.SourceGenerators;
using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Flowthru.Core.CodeFixes.Tests;

/// <summary>
/// Tests for FT2002: removes a <c>RegisterCatalog</c> call that is registered but
/// not referenced by any flow.
/// </summary>
[TestFixture]
[Category("CodeFixes")]
public class Ft2002Tests
{
  /// <summary>
  /// Minimal stubs that satisfy <see cref="FlowthruRegistrationAnalyzer"/>'s type lookups.
  /// The analyzer resolves "Flowthru.Core.Data.DataCatalogBase" by metadata name, so the
  /// namespace must match exactly.
  /// </summary>
  private const string Stubs = """
    namespace Flowthru.Core.Data
    {
        public abstract class DataCatalogBase { }
    }
    namespace TestProject
    {
        public class MyCatalog : global::Flowthru.Core.Data.DataCatalogBase { }
        public class OtherCatalog : global::Flowthru.Core.Data.DataCatalogBase { }

        public interface IFlowthruBuilder
        {
            IFlowthruBuilder RegisterCatalog<TCatalog>()
                where TCatalog : global::Flowthru.Core.Data.DataCatalogBase;
            IFlowthruBuilder RegisterFlow(string label, System.Action<MyCatalog> flow);
        }

        public static class SetupExtensions
        {
            public static void AddFlowthru(
                this object _,
                System.Action<IFlowthruBuilder> configure) { }
        }
    }
    """;

  // ── Analyzer-only tests ───────────────────────────────────────────────────

  [Test]
  public async Task UnregisteredCatalog_EmitsFT2002()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            public class Config
            {
                public void Setup(object services)
                {
                    services.AddFlowthru(flowthru =>
                    {
                        {|FT2002:flowthru.RegisterCatalog<MyCatalog>()|};
                    });
                }
            }
        }
        """;

    await new CSharpAnalyzerTest<FlowthruRegistrationAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  [Test]
  public async Task CatalogUsedByFlow_NoDiagnostic()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            public class Config
            {
                public void Setup(object services)
                {
                    services.AddFlowthru(flowthru =>
                    {
                        flowthru.RegisterCatalog<MyCatalog>();
                        flowthru.RegisterFlow("MyFlow", (MyCatalog catalog) => { });
                    });
                }
            }
        }
        """;

    await new CSharpAnalyzerTest<FlowthruRegistrationAnalyzer, NUnit4Verifier>
    {
      TestCode = source,
    }.RunAsync();
  }

  // ── Code fix test ─────────────────────────────────────────────────────────

  [Test]
  public async Task UnusedCatalog_RemovesRegisterCatalogStatement()
  {
    var source =
      Stubs
      + """

        namespace TestProject
        {
            public class Config
            {
                public void Setup(object services)
                {
                    services.AddFlowthru(flowthru =>
                    {
                        {|FT2002:flowthru.RegisterCatalog<MyCatalog>()|};
                    });
                }
            }
        }
        """;

    // SyntaxRemoveOptions.KeepLeadingTrivia keeps the removed statement's leading
    // whitespace and prepends it to the next token (the closing brace), resulting
    // in 28 spaces of indentation on the closing brace line.
    var fixedSource =
      Stubs
      + """

        namespace TestProject
        {
            public class Config
            {
                public void Setup(object services)
                {
                    services.AddFlowthru(flowthru =>
                    {
                                    });
                }
            }
        }
        """;

    await new CSharpCodeFixTest<
      FlowthruRegistrationAnalyzer,
      Ft2002RemoveUnusedCatalogFix,
      NUnit4Verifier
    >
    {
      TestCode = source,
      FixedCode = fixedSource,
    }.RunAsync();
  }
}
