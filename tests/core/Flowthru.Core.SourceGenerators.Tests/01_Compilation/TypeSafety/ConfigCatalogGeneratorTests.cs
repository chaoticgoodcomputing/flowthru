using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.Tests.Compilation.TypeSafety;

/// <summary>
/// Tests for the <c>[FlowthruConfig]</c> source generator that emits a
/// <c>CatalogAbstract</c>-derived partial class with an <c>IConfiguration</c>-bound
/// constructor and <c>CreateItem</c> property bodies.
/// </summary>
[TestFixture]
[Category("Compilation")]
[Category("SourceGenerator")]
public class ConfigCatalogGeneratorTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Happy path
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void PartialClass_WithConfigSection_GeneratesCatalogScaffolding()
  {
    var source = """
      using Flowthru.Core.Data;
      using Microsoft.Extensions.Configuration;

      namespace TestProject;

      public class ModelOptions
      {
          public double Threshold { get; set; }
          public string Name { get; set; } = "";
      }

      [FlowthruConfig]
      public partial class FlowConfig
      {
          [ConfigSection("Flowthru:ModelOptions")]
          public partial IItem<ModelOptions> ModelOptions { get; }
      }
      """;

    var result = GeneratorTestHelper.RunConfigCatalogGenerator(source);

    var generated = result.GetGeneratedSource("FlowConfig");
    Assert.That(generated, Is.Not.Null, "Should emit a partial class file for FlowConfig.");
    Assert.That(generated, Does.Contain("CatalogAbstract"));
    Assert.That(generated, Does.Contain("IConfiguration"));
    Assert.That(generated, Does.Contain("Flowthru:ModelOptions"));
  }

  [Test]
  public void PartialClass_WithMultipleConfigSections_GeneratesAllProperties()
  {
    var source = """
      using Flowthru.Core.Data;
      using Microsoft.Extensions.Configuration;

      namespace TestProject;

      public class ModelOptions { public double Threshold { get; set; } }
      public class ReportOptions { public string OutputPath { get; set; } = ""; }

      [FlowthruConfig]
      public partial class FlowConfig
      {
          [ConfigSection("Flowthru:Model")]
          public partial IItem<ModelOptions> Model { get; }

          [ConfigSection("Flowthru:Report")]
          public partial IItem<ReportOptions> Report { get; }
      }
      """;

    var result = GeneratorTestHelper.RunConfigCatalogGenerator(source);

    var generated = result.GetGeneratedSource("FlowConfig");
    Assert.That(generated, Does.Contain("Flowthru:Model"));
    Assert.That(generated, Does.Contain("Flowthru:Report"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Diagnostics
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void NonPartialClass_EmitsFT3001()
  {
    var source = """
      using Flowthru.Core.Data;
      using Microsoft.Extensions.Configuration;

      namespace TestProject;

      public class ModelOptions { public double Threshold { get; set; } }

      [FlowthruConfig]
      public class FlowConfig
      {
          [ConfigSection("Flowthru:Model")]
          public IItem<ModelOptions> Model { get; }
      }
      """;

    var result = GeneratorTestHelper.RunConfigCatalogGenerator(source);

    var ft3001 = result.GeneratorDiagnostics.FirstOrDefault(d => d.Id == "FT3001");
    Assert.That(ft3001, Is.Not.Null, "Expected FT3001 (class must be partial).");
    Assert.That(ft3001!.Severity, Is.EqualTo(DiagnosticSeverity.Error));
  }

  [Test]
  public void IItemPropertyWithoutConfigSection_EmitsFT3002()
  {
    var source = """
      using Flowthru.Core.Data;
      using Microsoft.Extensions.Configuration;

      namespace TestProject;

      public class ModelOptions { public double Threshold { get; set; } }

      [FlowthruConfig]
      public partial class FlowConfig
      {
          // Missing [ConfigSection]
          public IItem<ModelOptions> Model { get; }
      }
      """;

    var result = GeneratorTestHelper.RunConfigCatalogGenerator(source);

    var ft3002 = result.GeneratorDiagnostics.FirstOrDefault(d => d.Id == "FT3002");
    Assert.That(ft3002, Is.Not.Null, "Expected FT3002 (missing [ConfigSection]).");
    Assert.That(ft3002!.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // No-op
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void ClassWithoutFlowthruConfig_GeneratesNothing()
  {
    var source = """
      using Flowthru.Core.Data;

      namespace TestProject;

      public partial class NotAConfigClass { }
      """;

    var result = GeneratorTestHelper.RunConfigCatalogGenerator(source);

    Assert.That(result.GeneratedSources, Is.Empty);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static string FormatDiagnostics(GeneratorTestResult result)
  {
    if (result.Success)
    {
      return "(success)";
    }
    return "Diagnostics:\n"
      + string.Join(
        "\n",
        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString())
      );
  }
}
