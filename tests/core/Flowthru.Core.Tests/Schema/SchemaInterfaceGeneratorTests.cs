using Flowthru.Data.Schema;

namespace Flowthru.Core.Tests.Schema;

/// <summary>
/// Verifies that <c>SchemaInterfaceGenerator</c> emits the appropriate
/// marker interfaces for [FlowthruSchema]-attributed types based on
/// their property structure.
/// </summary>
[TestFixture]
public class SchemaInterfaceGeneratorTests
{
  [Test]
  public void FlatSchema_GeneratesFlatMarkers()
  {
    var iface = typeof(IFlatSchema);
    Assert.That(
      iface.IsAssignableFrom(typeof(FlatSchemaFixture)),
      Is.True,
      "Generator should emit IFlatSchema for a schema with all primitive properties."
    );
    Assert.That(
      typeof(ITextSerializable).IsAssignableFrom(typeof(FlatSchemaFixture)),
      Is.True,
      "Flat schema should be ITextSerializable."
    );
    Assert.That(
      typeof(IBinarySerializable).IsAssignableFrom(typeof(FlatSchemaFixture)),
      Is.True,
      "Flat schema should be IBinarySerializable."
    );
    Assert.That(
      typeof(IStructuredSerializable).IsAssignableFrom(typeof(FlatSchemaFixture)),
      Is.True,
      "Flat schema should be IStructuredSerializable."
    );
    Assert.That(
      typeof(INestedSchema).IsAssignableFrom(typeof(FlatSchemaFixture)),
      Is.False,
      "Flat schema should NOT be INestedSchema."
    );
  }

  [Test]
  public void NestedSchema_GeneratesNestedMarkers()
  {
    Assert.That(
      typeof(INestedSchema).IsAssignableFrom(typeof(NestedSchemaFixture)),
      Is.True,
      "Generator should emit INestedSchema for a schema with collection or nested-object properties."
    );
    Assert.That(
      typeof(IStructuredSerializable).IsAssignableFrom(typeof(NestedSchemaFixture)),
      Is.True,
      "Nested schema should be IStructuredSerializable."
    );
    Assert.That(
      typeof(IFlatSchema).IsAssignableFrom(typeof(NestedSchemaFixture)),
      Is.False,
      "Nested schema should NOT be IFlatSchema."
    );
    Assert.That(
      typeof(ITextSerializable).IsAssignableFrom(typeof(NestedSchemaFixture)),
      Is.False,
      "Nested schema should NOT be ITextSerializable."
    );
  }

  [Test]
  public void EnumProperty_ClassifiesAsFlat()
  {
    Assert.That(
      typeof(IFlatSchema).IsAssignableFrom(typeof(SchemaWithEnum)),
      Is.True,
      "Schema with only primitive + enum properties should be flat."
    );
  }

  [Test]
  public void IScalarProperty_ClassifiesAsFlat()
  {
    Assert.That(
      typeof(IFlatSchema).IsAssignableFrom(typeof(SchemaWithIScalar)),
      Is.True,
      "Schema using an IScalar NewType wrapper should be flat."
    );
  }
}

[FlowthruSchema]
public partial record FlatSchemaFixture
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required double Score { get; init; }
}

[FlowthruSchema]
public partial record NestedSchemaFixture
{
  public required string Title { get; init; }
  public required IReadOnlyList<string> Tags { get; init; }
}

public enum FixtureColor
{
  [SerializedEnum("R")]
  Red,

  [SerializedEnum("G")]
  Green,
}

[FlowthruSchema]
public partial record SchemaWithEnum
{
  public required string Label { get; init; }
  public required FixtureColor Color { get; init; }
}

public readonly record struct CustomerId(string Value) : IScalar;

[FlowthruSchema]
public partial record SchemaWithIScalar
{
  public required CustomerId Id { get; init; }
  public required string Name { get; init; }
}
