using Flowthru.Core.SourceGenerators.Schema;
using Flowthru.Data.Schema;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Positive + negative tests for the Phase-2 schema analyzer's two
/// active diagnostics: FT1001 (schema must be partial) and FT1002
/// (manually-applied schema marker conflicts with the generator).
/// </summary>
[TestFixture]
public class Ft1001Ft1002SchemaAnalyzerTests
{
  // ── FT1001: must be partial ────────────────────────────────────────────

  [Test]
  public async Task NonPartialFlowthruSchemaRecord_FiresFt1001()
  {
    var source = """
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public record NotPartial(int X);
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruSchemaAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT1001").ToList(), Is.Not.Empty,
      "FT1001 should fire when a [FlowthruSchema] record isn't declared partial.");
  }

  [Test]
  public async Task PartialFlowthruSchemaRecord_NoFt1001()
  {
    var source = """
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record IsPartial(int X);
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruSchemaAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT1001").ToList(), Is.Empty,
      "FT1001 should be silent on a properly-declared partial record.");
  }

  // ── FT1002: conflicting manual schema marker ───────────────────────────

  [Test]
  public async Task SchemaWithManualMarkerInterface_FiresFt1002()
  {
    // Manually applying IFlatSchema collides with the generator's emit.
    var source = """
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Manual : IFlatSchema
      {
        public required int X { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruSchemaAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT1002").ToList(), Is.Not.Empty,
      "FT1002 should fire when a [FlowthruSchema] type manually applies a marker interface.");
  }

  [Test]
  public async Task SchemaWithoutManualMarkerInterface_NoFt1002()
  {
    var source = """
      using Flowthru.Data.Schema;

      namespace Sample;

      [FlowthruSchema]
      public partial record Clean
      {
        public required int X { get; init; }
      }
      """;

    var diags = await AnalyzerTestHarness.RunAsync(
      new FlowthruSchemaAnalyzer(),
      source,
      typeof(FlowthruSchemaAttribute).Assembly
    );
    Assert.That(diags.Where("FT1002").ToList(), Is.Empty,
      "FT1002 should be silent when no manual marker interface is applied.");
  }
}
