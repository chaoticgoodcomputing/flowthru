using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace Flowthru.Tests.Compilation.TypeSafety;

/// <summary>
/// Tests for the [FlowthruSchema] source generator that auto-derives
/// marker interfaces from schema property structure.
/// </summary>
[TestFixture]
[Category("Compilation")]
[Category("SourceGenerator")]
public class SchemaInterfaceGeneratorTests
{
    // ─────────────────────────────────────────────────────────────
    // Flat schema classification
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void FlatSchema_WithPrimitives_ImplementsAllInterfaces()
    {
        var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record FlatRecord
      {
          public required string Name { get; init; }
          public required int Value { get; init; }
          public required double Score { get; init; }
          public required bool IsActive { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("FlatRecord.SchemaInterfaces.g.cs");
        Assert.That(generated, Is.Not.Null, "Should generate schema interfaces file");
        Assert.That(generated, Does.Contain("IFlatSchema"));
        Assert.That(generated, Does.Contain("ITextSerializable"));
        Assert.That(generated, Does.Contain("IBinarySerializable"));
        Assert.That(generated, Does.Contain("IStructuredSerializable"));
        Assert.That(generated, Does.Not.Contain("INestedSchema"));
    }

    [Test]
    public void FlatSchema_WithNullablePrimitives_IsClassifiedAsFlat()
    {
        var source = """
      using Flowthru.Core.Abstractions;
      using System;

      namespace TestProject;

      [FlowthruSchema]
      public partial record NullableRecord
      {
          public required string Name { get; init; }
          public int? OptionalValue { get; init; }
          public DateTime? OptionalTimestamp { get; init; }
          public double? OptionalScore { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("NullableRecord.SchemaInterfaces.g.cs");
        Assert.That(generated, Does.Contain("IFlatSchema"));
    }

    [Test]
    public void FlatSchema_WithEnums_IsClassifiedAsFlat()
    {
        var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      public enum Status { Active, Inactive }

      [FlowthruSchema]
      public partial record EnumRecord
      {
          public required string Name { get; init; }
          public required Status Status { get; init; }
          public Status? OptionalStatus { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("EnumRecord.SchemaInterfaces.g.cs");
        Assert.That(generated, Does.Contain("IFlatSchema"));
    }

    [Test]
    public void FlatSchema_WithGuidAndDateTypes_IsClassifiedAsFlat()
    {
        var source = """
      using Flowthru.Core.Abstractions;
      using System;

      namespace TestProject;

      [FlowthruSchema]
      public partial record GuidDateRecord
      {
          public required Guid Id { get; init; }
          public required DateTime CreatedAt { get; init; }
          public required DateTimeOffset ModifiedAt { get; init; }
          public required TimeSpan Duration { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("GuidDateRecord.SchemaInterfaces.g.cs");
        Assert.That(generated, Does.Contain("IFlatSchema"));
    }

    [Test]
    public void FlatSchema_WithIScalarNewType_IsClassifiedAsFlat()
    {
        // A user-defined NewType wrapping a string should be treated as a scalar column,
        // not a nested object — the IScalar interface is the opt-in declaration.
        var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      public readonly record struct CustomerId(string Value) : IScalar;

      [FlowthruSchema]
      public partial record OrderSchema
      {
          public required CustomerId Id { get; init; }
          public required string Name { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("OrderSchema.SchemaInterfaces.g.cs");
        Assert.That(generated, Is.Not.Null);
        Assert.That(
          generated,
          Does.Contain("IFlatSchema"),
          "NewType with IScalar should yield flat schema"
        );
        Assert.That(generated, Does.Contain("ITextSerializable"));
        Assert.That(generated, Does.Not.Contain("INestedSchema"));
    }

    [Test]
    public void NestedSchema_WithUserStructLackingIScalar_IsClassifiedAsNested()
    {
        // An identical struct without IScalar has no declaration of scalar intent.
        // The generator must treat it conservatively as a nested object.
        var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      public readonly record struct CustomerId(string Value);

      [FlowthruSchema]
      public partial record OrderSchema
      {
          public required CustomerId Id { get; init; }
          public required string Name { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("OrderSchema.SchemaInterfaces.g.cs");
        Assert.That(generated, Is.Not.Null);
        Assert.That(
          generated,
          Does.Contain("INestedSchema"),
          "Unannotated struct should yield nested schema"
        );
        Assert.That(generated, Does.Not.Contain("IFlatSchema"));
    }

    // ─────────────────────────────────────────────────────────────
    // Nested schema classification
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void NestedSchema_WithList_IsClassifiedAsNested()
    {
        var source = """
      using Flowthru.Core.Abstractions;
      using System.Collections.Generic;

      namespace TestProject;

      [FlowthruSchema]
      public partial record ListRecord
      {
          public required string Name { get; init; }
          public required List<string> Tags { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("ListRecord.SchemaInterfaces.g.cs");
        Assert.That(generated, Is.Not.Null);
        Assert.That(generated, Does.Contain("INestedSchema"));
        Assert.That(generated, Does.Contain("IStructuredSerializable"));
        Assert.That(generated, Does.Not.Contain("IFlatSchema"));
        Assert.That(generated, Does.Not.Contain("ITextSerializable"));
        Assert.That(generated, Does.Not.Contain("IBinarySerializable"));
    }

    [Test]
    public void NestedSchema_WithArray_IsClassifiedAsNested()
    {
        var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record ArrayRecord
      {
          public required string Name { get; init; }
          public required double[] Values { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("ArrayRecord.SchemaInterfaces.g.cs");
        Assert.That(generated, Does.Contain("INestedSchema"));
        Assert.That(generated, Does.Not.Contain("IFlatSchema"));
    }

    [Test]
    public void NestedSchema_WithNestedObject_IsClassifiedAsNested()
    {
        var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      public record Inner
      {
          public required string Value { get; init; }
      }

      [FlowthruSchema]
      public partial record OuterRecord
      {
          public required string Name { get; init; }
          public required Inner Child { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("OuterRecord.SchemaInterfaces.g.cs");
        Assert.That(generated, Does.Contain("INestedSchema"));
    }

    [Test]
    public void NestedSchema_WithDictionary_IsClassifiedAsNested()
    {
        var source = """
      using Flowthru.Core.Abstractions;
      using System.Collections.Generic;

      namespace TestProject;

      [FlowthruSchema]
      public partial record DictRecord
      {
          public required string Name { get; init; }
          public required Dictionary<string, int> Metadata { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("DictRecord.SchemaInterfaces.g.cs");
        Assert.That(generated, Does.Contain("INestedSchema"));
    }

    // ─────────────────────────────────────────────────────────────
    // Diagnostic: non-partial types
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void NonPartialType_EmitsFT1001Error()
    {
        var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public record NonPartialRecord
      {
          public required string Name { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(
          result.GeneratorDiagnostics,
          Has.Some.Matches<Diagnostic>(d => d.Id == "FT1001"),
          "Should emit FT1001 for non-partial type"
        );
    }

    // ─────────────────────────────────────────────────────────────
    // Diagnostic: manual interface conflicts
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void ManualInterfaces_EmitsFT1002Warning_ButStillCompiles()
    {
        var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record ManuallyAnnotated : IFlatSchema, ITextSerializable
      {
          public required string Name { get; init; }
          public required int Value { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        // Should compile (generator avoids duplicating manual interfaces)
        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        // But should warn about conflict
        Assert.That(
          result.GeneratorDiagnostics,
          Has.Some.Matches<Diagnostic>(d => d.Id == "FT1002"),
          "Should emit FT1002 warning for manual interfaces"
        );

        // Generated source should NOT include the manually-applied ones
        var generated = result.GetGeneratedSource("ManuallyAnnotated.SchemaInterfaces.g.cs");
        Assert.That(generated, Is.Not.Null);
        Assert.That(generated, Does.Not.Contain("IFlatSchema"), "Should not duplicate IFlatSchema");
        Assert.That(
          generated,
          Does.Not.Contain("ITextSerializable"),
          "Should not duplicate ITextSerializable"
        );
        // Should include the ones the user didn't manually apply
        Assert.That(generated, Does.Contain("IBinarySerializable"));
        Assert.That(generated, Does.Contain("IStructuredSerializable"));
    }

    // ─────────────────────────────────────────────────────────────
    // Integration: generated schema works with Items
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void EmptySchema_IsClassifiedAsFlat()
    {
        var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial record EmptyRecord { }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("EmptyRecord.SchemaInterfaces.g.cs");
        Assert.That(generated, Does.Contain("IFlatSchema"));
    }

    [Test]
    public void PartialClass_WorksForNonRecordTypes()
    {
        var source = """
      using Flowthru.Core.Abstractions;

      namespace TestProject;

      [FlowthruSchema]
      public partial class FlatClass
      {
          public required string Name { get; init; }
          public required int Value { get; init; }
      }
      """;

        var result = GeneratorTestHelper.RunSchemaGenerator(source);

        Assert.That(result.Success, Is.True, FormatDiagnostics(result));

        var generated = result.GetGeneratedSource("FlatClass.SchemaInterfaces.g.cs");
        Assert.That(generated, Does.Contain("partial class FlatClass"));
        Assert.That(generated, Does.Contain("IFlatSchema"));
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    private static string FormatDiagnostics(GeneratorTestResult result) =>
      string.Join(
        "\n",
        result
          .Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
          .Select(d => $"  {d.Id}: {d.GetMessage()}")
      );
}
