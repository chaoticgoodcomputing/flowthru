using Flowthru.Core.SourceGenerators.Schema.Column;
using Flowthru.Data.Schema;

namespace Flowthru.Core.SourceGenerators.Tests;

/// <summary>
/// Behavioural tests for <see cref="ColumnNewTypeGenerator"/>. The
/// generator emits one <c>readonly record struct</c> per unique
/// <c>(namespace, NewType name)</c> declared via <c>[FlowthruColumn]</c>,
/// reports <c>FT1003</c> when the backing type isn't a recognized scalar,
/// and reports <c>FT1004</c> when two declarations of the same NewType
/// disagree on the backing type.
/// </summary>
[TestFixture]
public class ColumnNewTypeGeneratorTests
{
  // ── Valid emission ────────────────────────────────────────────────────

  [Test]
  public void IntBackingType_EmitsRecordStructWithIScalar()
  {
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record Order
      {
        [FlowthruColumn(typeof(int))]
        public ShuttleId ShuttleId { get; init; } = default!;
      }
      """;

    var emitted = EmitFirstSource(source);

    Assert.That(emitted,
      Does.Contain("public readonly record struct ShuttleId(int Value)"),
      "Emission shape should be a positional record struct over the backing type.");
    Assert.That(emitted, Does.Contain("global::Flowthru.Data.Schema.IScalar"),
      "Generated NewType should implement IScalar.");
    Assert.That(emitted, Does.Contain("namespace Sample;"),
      "Generated NewType should live in the schema's containing namespace.");
  }

  [Test]
  public void GuidBackingType_IsAcceptedAsScalar()
  {
    // Tier 4: System.Guid is a known BCL scalar struct, so it must
    // pass IsFlatPropertyType and emit a NewType without FT1003.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record Order
      {
        [FlowthruColumn(typeof(System.Guid))]
        public OrderId OrderId { get; init; } = default!;
      }
      """;

    var result = RunGenerator(source);
    Assert.That(result.Diagnostics.Where("FT1003").ToList(), Is.Empty,
      "Guid is a recognized BCL scalar — FT1003 must not fire.");
    Assert.That(result.GeneratedSources, Is.Not.Empty,
      "A valid backing type should produce a NewType.");
  }

  // ── FT1003: invalid backing type ──────────────────────────────────────

  [Test]
  public void NonScalarBackingType_FiresFt1003()
  {
    // System.Collections.Generic.List<int> is not a scalar — the
    // generator must report FT1003 and skip emission for that entry.
    var source = """
      using System.Collections.Generic;
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record Bag
      {
        [FlowthruColumn(typeof(List<int>))]
        public BadColumn BadColumn { get; init; } = default!;
      }
      """;

    var result = RunGenerator(source);
    Assert.That(result.Diagnostics.Where("FT1003").ToList(), Is.Not.Empty,
      "FT1003 should fire for non-scalar backing types.");
  }

  // ── FT1004: conflicting backing types ─────────────────────────────────

  [Test]
  public void SameNewTypeWithConflictingBackings_FiresFt1004AndSkipsEmission()
  {
    // Two [FlowthruColumn] properties name the same NewType in the
    // same namespace, but one says int and the other says string —
    // ambiguous, so FT1004 fires for every conflicting site and no
    // NewType is emitted.
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record A
      {
        [FlowthruColumn(typeof(int))]
        public ShuttleId Id { get; init; } = default!;
      }

      [FlowthruSchema]
      public partial record B
      {
        [FlowthruColumn(typeof(string))]
        public ShuttleId Id { get; init; } = default!;
      }
      """;

    var result = RunGenerator(source);

    Assert.That(result.Diagnostics.Where("FT1004").ToList(), Is.Not.Empty,
      "FT1004 should fire when conflicting backing types are declared for the same NewType.");
    Assert.That(
      result.GeneratedSources.Any(g => g.HintName.Contains("ShuttleId")),
      Is.False,
      "Conflicting NewType declarations must skip emission of the type altogether. Generated: "
        + string.Join(", ", result.GeneratedSources.Select(g => g.HintName))
    );
  }

  // ── Deduplication: agreeing declarations emit once ────────────────────

  [Test]
  public void SameNewTypeWithSameBacking_EmitsOnce()
  {
    var source = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record A
      {
        [FlowthruColumn(typeof(int))]
        public ShuttleId Id { get; init; } = default!;
      }

      [FlowthruSchema]
      public partial record B
      {
        [FlowthruColumn(typeof(int))]
        public ShuttleId Id { get; init; } = default!;
      }
      """;

    var result = RunGenerator(source);
    var shuttleIdFiles = result.GeneratedSources
      .Where(g => g.HintName.Contains("ShuttleId"))
      .ToList();
    Assert.That(shuttleIdFiles, Has.Count.EqualTo(1),
      "Two agreeing [FlowthruColumn] declarations of the same NewType should yield exactly one emission.");
  }

  // ── Hint-name shape ───────────────────────────────────────────────────

  [Test]
  public void EmittedSource_HasAutoGeneratedHeaderAndNamespaceAwareHintName()
  {
    var source = """
      using Flowthru.Data.Schema;
      namespace My.Schemas;

      [FlowthruSchema]
      public partial record Order
      {
        [FlowthruColumn(typeof(int))]
        public ShuttleId ShuttleId { get; init; } = default!;
      }
      """;

    var result = RunGenerator(source);
    var match = result.GeneratedSources
      .FirstOrDefault(g => g.HintName == "My_Schemas.ShuttleId.NewType.g.cs");
    Assert.That(match.Source, Is.Not.Null,
      "Hint name should be `{Namespace-with-underscores}.{NewType}.NewType.g.cs`. Generated: "
      + string.Join(", ", result.GeneratedSources.Select(g => g.HintName)));
    Assert.That(match.Source, Does.Contain("// <auto-generated/>"));
    Assert.That(match.Source, Does.Contain("#nullable enable"));
  }

  // ── helpers ───────────────────────────────────────────────────────────

  private static GeneratorRunResultPayload RunGenerator(string source) =>
    AnalyzerTestHarness.RunGenerator(
      new ColumnNewTypeGenerator(),
      source,
      typeof(FlowthruColumnAttribute).Assembly
    );

  private static string EmitFirstSource(string source)
  {
    var result = RunGenerator(source);
    Assert.That(result.GeneratedSources, Is.Not.Empty,
      "Expected at least one generated file. Diagnostics: "
      + string.Join("; ", result.Diagnostics.Select(d => d.GetMessage())));
    return result.GeneratedSources[0].Source;
  }
}
