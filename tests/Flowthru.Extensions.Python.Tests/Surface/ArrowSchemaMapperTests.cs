using Apache.Arrow;
using Apache.Arrow.Types;
using Flowthru.Extensions.Python.Marshalling;
using Flowthru.Extensions.Python.Tests.Schemas;

namespace Flowthru.Extensions.Python.Tests.Surface;

/// <summary>
/// Unit tests for ArrowSchemaMapper - C# schema → Arrow schema generation.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Surface")]
public class ArrowSchemaMapperTests
{
    [Test]
    public void BuildArrowSchema_SimpleRowSchema_GeneratesCorrectSchema()
    {
        // Act
        var schema = ArrowSchemaMapper.BuildArrowSchema<SimpleRowSchema>();

        // Assert
        Assert.That(schema.FieldsList.Count, Is.EqualTo(3));

        // Check field names (respecting SerializedLabel)
        Assert.That(schema.GetFieldByName("id"), Is.Not.Null);
        Assert.That(schema.GetFieldByName("name"), Is.Not.Null);
        Assert.That(schema.GetFieldByName("value"), Is.Not.Null);

        // Check field types
        var idField = schema.GetFieldByName("id");
        Assert.That(idField.DataType, Is.InstanceOf<Int32Type>());
        Assert.That(idField.IsNullable, Is.False); // required int

        var nameField = schema.GetFieldByName("name");
        Assert.That(nameField.DataType, Is.InstanceOf<StringType>());
        Assert.That(nameField.IsNullable, Is.True); // string is reference type

        var valueField = schema.GetFieldByName("value");
        Assert.That(valueField.DataType, Is.InstanceOf<DoubleType>());
        Assert.That(valueField.IsNullable, Is.False); // required double
    }

    [Test]
    public void BuildArrowSchema_ExtendedTypesSchema_MapsAllTypes()
    {
        // Act
        var schema = ArrowSchemaMapper.BuildArrowSchema<ExtendedTypesSchema>();

        // Assert
        Assert.That(schema.FieldsList.Count, Is.EqualTo(5));

        // Guid → String
        var idField = schema.GetFieldByName("id");
        Assert.That(idField.DataType, Is.InstanceOf<StringType>());
        Assert.That(idField.IsNullable, Is.False);

        // DateTime → Timestamp
        var createdField = schema.GetFieldByName("created_at");
        Assert.That(createdField.DataType, Is.InstanceOf<TimestampType>());
        Assert.That(createdField.IsNullable, Is.False);

        // DateTimeOffset? → Timestamp (nullable)
        var modifiedField = schema.GetFieldByName("modified_at");
        Assert.That(modifiedField.DataType, Is.InstanceOf<TimestampType>());
        Assert.That(modifiedField.IsNullable, Is.True);

        // TimeSpan? → Duration (nullable)
        var durationField = schema.GetFieldByName("duration");
        Assert.That(durationField.DataType, Is.InstanceOf<DurationType>());
        Assert.That(durationField.IsNullable, Is.True);

        // string? → String (nullable)
        var nameField = schema.GetFieldByName("name");
        Assert.That(nameField.DataType, Is.InstanceOf<StringType>());
        Assert.That(nameField.IsNullable, Is.True);
    }

    [Test]
    public void BuildArrowSchema_RespectsSerializedLabel()
    {
        // Act
        var schema = ArrowSchemaMapper.BuildArrowSchema<SimpleRowSchema>();

        // Assert - external field names from [SerializedLabel], not property names
        Assert.That(schema.GetFieldByName("id"), Is.Not.Null);
        Assert.That(schema.GetFieldByName("name"), Is.Not.Null);
        Assert.That(schema.GetFieldByName("value"), Is.Not.Null);

        // Property names should NOT be present
        Assert.That(schema.GetFieldByName("Id"), Is.Null);
        Assert.That(schema.GetFieldByName("Name"), Is.Null);
        Assert.That(schema.GetFieldByName("Value"), Is.Null);
    }

    [Test]
    public void BuildArrowSchema_NullableTypes_MarkedAsNullable()
    {
        // Act
        var schema = ArrowSchemaMapper.BuildArrowSchema<ExtendedTypesSchema>();

        // Assert - nullable types
        Assert.That(schema.GetFieldByName("modified_at").IsNullable, Is.True);
        Assert.That(schema.GetFieldByName("duration").IsNullable, Is.True);
        Assert.That(schema.GetFieldByName("name").IsNullable, Is.True);

        // Non-nullable types
        Assert.That(schema.GetFieldByName("id").IsNullable, Is.False);
        Assert.That(schema.GetFieldByName("created_at").IsNullable, Is.False);
    }
}
