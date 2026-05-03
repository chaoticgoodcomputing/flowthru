using Flowthru.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace Flowthru.Core.SourceGenerators.Tests.Compilation.TypeSafety;

/// <summary>
/// Tests for the [FlowthruColumn] source generator that auto-generates
/// NewType record structs implementing IScalar.
/// </summary>
[TestFixture]
[Category("Compilation")]
[Category("SourceGenerator")]
public class ColumnNewTypeGeneratorTests
{
  // ─────────────────────────────────────────────────────────────
  // Basic NewType generation
  // ─────────────────────────────────────────────────────────────

  [Test]
  public void BasicGeneration_IntBacking_GeneratesReadonlyRecordStruct()
  {
    var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record OrderSchema
      {
          [FlowthruColumn(typeof(int))]
          public required OrderId Id { get; init; }

          public required string Name { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    Assert.That(result.Success, Is.True, FormatDiagnostics(result));

    var generated = result.GetGeneratedSource("OrderId.NewType.g.cs");
    Assert.That(generated, Is.Not.Null, "Should generate OrderId.NewType.g.cs");
    Assert.That(generated, Does.Contain("public readonly record struct OrderId(int Value)"));
    Assert.That(generated, Does.Contain("global::Flowthru.Core.Abstractions.IScalar"));
  }

  [Test]
  public void BasicGeneration_StringBacking_GeneratesCorrectType()
  {
    var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record ShuttleSchema
      {
          [FlowthruColumn(typeof(string))]
          public required ShuttleId Id { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    Assert.That(result.Success, Is.True, FormatDiagnostics(result));

    var generated = result.GetGeneratedSource("ShuttleId.NewType.g.cs");
    Assert.That(generated, Does.Contain("public readonly record struct ShuttleId(string Value)"));
  }

  [Test]
  public void BasicGeneration_GuidBacking_GeneratesCorrectType()
  {
    var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record ItemSchema
      {
          [FlowthruColumn(typeof(System.Guid))]
          public required ItemId Id { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    Assert.That(result.Success, Is.True, FormatDiagnostics(result));

    var generated = result.GetGeneratedSource("ItemId.NewType.g.cs");
    Assert.That(generated, Does.Contain("System.Guid Value"));
  }

  // ─────────────────────────────────────────────────────────────
  // Namespace placement
  // ─────────────────────────────────────────────────────────────

  [Test]
  public void NamespacePlacement_SameAsSchema()
  {
    var source = """
      using Flowthru.Core.Abstractions;

      namespace MyProject.Data.Schemas;

      [FlowthruSchema]
      public partial record UserSchema
      {
          [FlowthruColumn(typeof(int))]
          public required UserId Id { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    Assert.That(result.Success, Is.True, FormatDiagnostics(result));

    var generated = result.GetGeneratedSource("UserId.NewType.g.cs");
    Assert.That(generated, Does.Contain("namespace MyProject.Data.Schemas;"));
  }

  // ─────────────────────────────────────────────────────────────
  // Schema classification with [FlowthruColumn] properties
  // ─────────────────────────────────────────────────────────────

  [Test]
  public void SchemaWithFlowthruColumn_StaysFlat()
  {
    var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record ProductSchema
      {
          [FlowthruColumn(typeof(string))]
          public required ProductId Id { get; init; }

          public required string Name { get; init; }

          public required decimal Price { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    Assert.That(result.Success, Is.True, FormatDiagnostics(result));

    var schemaInterfaces = result.GetGeneratedSource("ProductSchema.SchemaInterfaces.g.cs");
    Assert.That(schemaInterfaces, Is.Not.Null);
    Assert.That(schemaInterfaces, Does.Contain("IFlatSchema"));
    Assert.That(schemaInterfaces, Does.Contain("ITextSerializable"));
    Assert.That(schemaInterfaces, Does.Contain("IBinarySerializable"));
  }

  // ─────────────────────────────────────────────────────────────
  // FT1003 — invalid backing types
  // ─────────────────────────────────────────────────────────────

  [Test]
  public void InvalidBackingType_List_ReportsFT1003()
  {
    var source = """
      using Flowthru.Core.Abstractions;
      using System.Collections.Generic;

      namespace TestProject;

      [FlowthruSchema]
      public partial record BadSchema
      {
          [FlowthruColumn(typeof(List<int>))]
          public required BadId Id { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    var ft1003 = result.GeneratorDiagnostics.FirstOrDefault(d => d.Id == "FT1003");
    Assert.That(ft1003, Is.Not.Null, "Should report FT1003 for invalid backing type");
    Assert.That(ft1003!.Severity, Is.EqualTo(DiagnosticSeverity.Error));
    Assert.That(
      result.GeneratedSources.Any(s => s.HintName.Contains("BadId")),
      Is.False,
      "Should not emit a NewType when backing type is invalid"
    );
  }

  [Test]
  public void InvalidBackingType_CustomClass_ReportsFT1003()
  {
    var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      public class CustomType { }

      [FlowthruSchema]
      public partial record BadSchema
      {
          [FlowthruColumn(typeof(CustomType))]
          public required BadId Id { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    var ft1003 = result.GeneratorDiagnostics.FirstOrDefault(d => d.Id == "FT1003");
    Assert.That(ft1003, Is.Not.Null, "Should report FT1003 for non-scalar class");
    Assert.That(ft1003!.GetMessage(), Does.Contain("CustomType"));
  }

  // ─────────────────────────────────────────────────────────────
  // Deduplication — same NewType across multiple schemas
  // ─────────────────────────────────────────────────────────────

  [Test]
  public void Deduplication_SameNewTypeAcrossSchemas_EmitsOnce()
  {
    // ShuttleSchema and ReviewSchema both reference ShuttleId. The generator must
    // recognize this and emit a single NewType definition rather than two conflicting ones.
    var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record ShuttleSchema
      {
          [FlowthruColumn(typeof(string))]
          public required ShuttleId Id { get; init; }
      }

      [FlowthruSchema]
      public partial record ReviewSchema
      {
          [FlowthruColumn(typeof(string))]
          public required ShuttleId ShuttleId { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    Assert.That(result.Success, Is.True, FormatDiagnostics(result));

    var shuttleIdFiles = result.GeneratedSources
      .Where(s => s.HintName.EndsWith("ShuttleId.NewType.g.cs"))
      .ToList();
    Assert.That(
      shuttleIdFiles.Count,
      Is.EqualTo(1),
      "Should emit exactly one ShuttleId NewType across multiple schemas"
    );
  }

  // ─────────────────────────────────────────────────────────────
  // Cross-namespace use: declare once via [FlowthruColumn], reference
  // by name elsewhere via `using` — the consuming schema must still
  // be classified as flat.
  // ─────────────────────────────────────────────────────────────

  [Test]
  public void CrossNamespaceUse_ConsumingSchemaWithoutDecorator_StaysFlat()
  {
    // SchemaA in namespace "Raw" declares ShuttleId via [FlowthruColumn].
    // SchemaB in namespace "Intermediate" references ShuttleId without [FlowthruColumn]
    // (the typical "downstream layer that imports a NewType" pattern). SchemaB must
    // still receive IFlatSchema, ITextSerializable, IBinarySerializable.
    var source = """
      using Flowthru.Core.Abstractions;
      using TestProject.Raw;

      namespace TestProject.Raw
      {
          [FlowthruSchema]
          public partial record RawSchema
          {
              [FlowthruColumn(typeof(string))]
              public required ShuttleId Id { get; init; }
          }
      }

      namespace TestProject.Intermediate
      {
          [FlowthruSchema]
          public partial record PreprocessedSchema
          {
              public required ShuttleId Id { get; init; }
              public required int Engines { get; init; }
          }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    Assert.That(result.Success, Is.True, FormatDiagnostics(result));

    // The downstream schema must still be classified as flat even though it has no
    // [FlowthruColumn] of its own — the registry of cross-namespace declarations covers it.
    var preprocessedInterfaces = result.GetGeneratedSource(
      "PreprocessedSchema.SchemaInterfaces.g.cs"
    );
    Assert.That(
      preprocessedInterfaces,
      Is.Not.Null,
      "Should generate schema interfaces for PreprocessedSchema"
    );
    Assert.That(preprocessedInterfaces, Does.Contain("IFlatSchema"));
    Assert.That(preprocessedInterfaces, Does.Contain("ITextSerializable"));
    Assert.That(preprocessedInterfaces, Does.Not.Contain("INestedSchema"));
  }

  // ─────────────────────────────────────────────────────────────
  // FT1004 — inconsistent backing types for the same NewType name
  // ─────────────────────────────────────────────────────────────

  [Test]
  public void InconsistentBackingType_AcrossSchemas_ReportsFT1004()
  {
    // Two schemas declare the same NewType name with different backing types.
    // The generator cannot decide which backing type wins; it must report FT1004
    // and emit no NewType source.
    var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record SchemaA
      {
          [FlowthruColumn(typeof(string))]
          public required ShuttleId Id { get; init; }
      }

      [FlowthruSchema]
      public partial record SchemaB
      {
          [FlowthruColumn(typeof(int))]
          public required ShuttleId Id { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    var ft1004 = result.GeneratorDiagnostics.FirstOrDefault(d => d.Id == "FT1004");
    Assert.That(ft1004, Is.Not.Null, "Should report FT1004 for inconsistent backing types");
    Assert.That(
      result.GeneratedSources.Any(s => s.HintName.EndsWith("ShuttleId.NewType.g.cs")),
      Is.False,
      "Should not emit a NewType when backing types disagree"
    );
  }

  // ─────────────────────────────────────────────────────────────
  // Multiple distinct NewTypes in one schema
  // ─────────────────────────────────────────────────────────────

  [Test]
  public void MultipleColumns_GeneratesSeparateFiles()
  {
    var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record LinkSchema
      {
          [FlowthruColumn(typeof(string))]
          public required SourceId Source { get; init; }

          [FlowthruColumn(typeof(string))]
          public required TargetId Target { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    Assert.That(result.Success, Is.True, FormatDiagnostics(result));

    var sourceGenerated = result.GetGeneratedSource("SourceId.NewType.g.cs");
    Assert.That(sourceGenerated, Is.Not.Null);
    Assert.That(sourceGenerated, Does.Contain("readonly record struct SourceId(string Value)"));

    var targetGenerated = result.GetGeneratedSource("TargetId.NewType.g.cs");
    Assert.That(targetGenerated, Is.Not.Null);
    Assert.That(targetGenerated, Does.Contain("readonly record struct TargetId(string Value)"));
  }

  // ─────────────────────────────────────────────────────────────
  // No implicit/explicit conversion operators
  // ─────────────────────────────────────────────────────────────

  [Test]
  public void NoConversionOperators_NotGenerated()
  {
    var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record TestSchema
      {
          [FlowthruColumn(typeof(int))]
          public required TestId Id { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    Assert.That(result.Success, Is.True, FormatDiagnostics(result));

    var generated = result.GetGeneratedSource("TestId.NewType.g.cs");
    Assert.That(generated, Does.Not.Contain("implicit operator"));
    Assert.That(generated, Does.Not.Contain("explicit operator"));
  }

  // ─────────────────────────────────────────────────────────────
  // IScalar-backed NewTypes
  // ─────────────────────────────────────────────────────────────

  [Test]
  public void BackingType_IScalarImplementor_IsValid()
  {
    var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      public readonly record struct BaseId(string Value) : IScalar;

      [FlowthruSchema]
      public partial record WrappedSchema
      {
          [FlowthruColumn(typeof(BaseId))]
          public required WrappedId Id { get; init; }
      }
      """;

    var result = GeneratorTestHelper.RunColumnNewTypeGenerator(source);

    Assert.That(result.Success, Is.True, FormatDiagnostics(result));

    var generated = result.GetGeneratedSource("WrappedId.NewType.g.cs");
    // Backing types are rendered with their full display name (e.g., TestProject.BaseId)
    // for unambiguous resolution regardless of where the generated NewType lands.
    Assert.That(generated, Does.Contain("readonly record struct WrappedId(TestProject.BaseId Value)"));
  }

  // ─────────────────────────────────────────────────────────────
  // Helper methods
  // ─────────────────────────────────────────────────────────────

  private static string FormatDiagnostics(GeneratorTestResult result) =>
    string.Join(
      "\n",
      result
        .Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
        .Select(d => $"  {d.Id}: {d.GetMessage()}")
    );
}
