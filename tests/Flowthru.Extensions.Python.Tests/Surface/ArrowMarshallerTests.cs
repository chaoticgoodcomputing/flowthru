using Apache.Arrow;
using Flowthru.Extensions.Python.Marshalling;
using Flowthru.Extensions.Python.Tests.Schemas;

namespace Flowthru.Extensions.Python.Tests.Surface;

/// <summary>
/// Unit tests for ArrowMarshaller - bidirectional C# ↔ Arrow conversion.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Surface")]
public class ArrowMarshallerTests
{
    [Test]
    public void ToRecordBatch_SimpleRows_CreatesCorrectBatch()
    {
        // Arrange
        var rows = new[]
        {
      new SimpleRowSchema
      {
        Id = 1,
        Name = "Alice",
        Value = 10.5,
      },
      new SimpleRowSchema
      {
        Id = 2,
        Name = "Bob",
        Value = 20.3,
      },
      new SimpleRowSchema
      {
        Id = 3,
        Name = "Charlie",
        Value = 30.7,
      },
    };

        // Act
        var batch = ArrowMarshaller.ToRecordBatch(rows);

        // Assert
        Assert.That(batch.Length, Is.EqualTo(3));
        Assert.That(batch.ColumnCount, Is.EqualTo(3));

        // Check schema
        Assert.That(batch.Schema.GetFieldByName("id"), Is.Not.Null);
        Assert.That(batch.Schema.GetFieldByName("name"), Is.Not.Null);
        Assert.That(batch.Schema.GetFieldByName("value"), Is.Not.Null);
    }

    [Test]
    public void FromRecordBatch_SimpleRows_ReconstructsCorrectly()
    {
        // Arrange
        var originalRows = new[]
        {
      new SimpleRowSchema
      {
        Id = 1,
        Name = "Alice",
        Value = 10.5,
      },
      new SimpleRowSchema
      {
        Id = 2,
        Name = "Bob",
        Value = 20.3,
      },
      new SimpleRowSchema
      {
        Id = 3,
        Name = "Charlie",
        Value = 30.7,
      },
    };

        var batch = ArrowMarshaller.ToRecordBatch(originalRows);

        // Act
        var reconstructedRows = ArrowMarshaller.FromRecordBatch<SimpleRowSchema>(batch).ToList();

        // Assert
        Assert.That(reconstructedRows.Count, Is.EqualTo(3));

        Assert.That(reconstructedRows[0].Id, Is.EqualTo(1));
        Assert.That(reconstructedRows[0].Name, Is.EqualTo("Alice"));
        Assert.That(reconstructedRows[0].Value, Is.EqualTo(10.5).Within(0.0001));

        Assert.That(reconstructedRows[1].Id, Is.EqualTo(2));
        Assert.That(reconstructedRows[1].Name, Is.EqualTo("Bob"));
        Assert.That(reconstructedRows[1].Value, Is.EqualTo(20.3).Within(0.0001));

        Assert.That(reconstructedRows[2].Id, Is.EqualTo(3));
        Assert.That(reconstructedRows[2].Name, Is.EqualTo("Charlie"));
        Assert.That(reconstructedRows[2].Value, Is.EqualTo(30.7).Within(0.0001));
    }

    [Test]
    public void RoundTrip_SimpleRows_PreservesData()
    {
        // Arrange
        var originalRows = new[]
        {
      new SimpleRowSchema
      {
        Id = 1,
        Name = "Alice",
        Value = 10.5,
      },
      new SimpleRowSchema
      {
        Id = 2,
        Name = "Bob",
        Value = 20.3,
      },
    };

        // Act - full round trip
        var batch = ArrowMarshaller.ToRecordBatch(originalRows);
        var reconstructed = ArrowMarshaller.FromRecordBatch<SimpleRowSchema>(batch).ToList();

        // Assert
        for (int i = 0; i < originalRows.Length; i++)
        {
            Assert.That(reconstructed[i].Id, Is.EqualTo(originalRows[i].Id));
            Assert.That(reconstructed[i].Name, Is.EqualTo(originalRows[i].Name));
            Assert.That(reconstructed[i].Value, Is.EqualTo(originalRows[i].Value).Within(0.0001));
        }
    }

    [Test]
    public void ToIpcBuffer_AndFromIpcBuffer_PreservesRecordBatch()
    {
        // Arrange
        var rows = new[]
        {
      new SimpleRowSchema
      {
        Id = 1,
        Name = "Alice",
        Value = 10.5,
      },
      new SimpleRowSchema
      {
        Id = 2,
        Name = "Bob",
        Value = 20.3,
      },
    };

        var originalBatch = ArrowMarshaller.ToRecordBatch(rows);

        // Act
        var ipcBuffer = ArrowMarshaller.ToIpcBuffer(originalBatch);
        var reconstructedBatch = ArrowMarshaller.FromIpcBuffer(ipcBuffer);

        // Assert
        Assert.That(reconstructedBatch.Length, Is.EqualTo(originalBatch.Length));
        Assert.That(reconstructedBatch.ColumnCount, Is.EqualTo(originalBatch.ColumnCount));

        // Verify data integrity
        var reconstructedRows = ArrowMarshaller
          .FromRecordBatch<SimpleRowSchema>(reconstructedBatch)
          .ToList();

        Assert.That(reconstructedRows[0].Id, Is.EqualTo(1));
        Assert.That(reconstructedRows[0].Name, Is.EqualTo("Alice"));
        Assert.That(reconstructedRows[0].Value, Is.EqualTo(10.5).Within(0.0001));
    }

    [Test]
    public void RoundTrip_ExtendedTypes_PreservesAllTypes()
    {
        // Arrange
        var testGuid = Guid.NewGuid();
        var testDateTime = new DateTime(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);
        var testOffset = new DateTimeOffset(2026, 3, 6, 15, 30, 0, TimeSpan.FromHours(3));
        var testDuration = TimeSpan.FromMinutes(45);

        var originalRows = new[]
        {
      new ExtendedTypesSchema
      {
        Id = testGuid,
        CreatedAt = testDateTime,
        ModifiedAt = testOffset,
        Duration = testDuration,
        Name = "Test",
      },
    };

        // Act
        var batch = ArrowMarshaller.ToRecordBatch(originalRows);
        var reconstructed = ArrowMarshaller.FromRecordBatch<ExtendedTypesSchema>(batch).ToList();

        // Assert
        Assert.That(reconstructed[0].Id, Is.EqualTo(testGuid));
        Assert.That(reconstructed[0].CreatedAt, Is.EqualTo(testDateTime));
        // Note: DateTimeOffset converts to UTC, so compare UTC values
        Assert.That(reconstructed[0].ModifiedAt?.UtcDateTime, Is.EqualTo(testOffset.UtcDateTime));
        Assert.That(reconstructed[0].Duration, Is.EqualTo(testDuration));
        Assert.That(reconstructed[0].Name, Is.EqualTo("Test"));
    }

    [Test]
    public void RoundTrip_NullableValues_PreservesNulls()
    {
        // Arrange
        var rows = new[]
        {
      new ExtendedTypesSchema
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = null, // nullable
        Duration = null, // nullable
        Name = null, // nullable
      },
    };

        // Act
        var batch = ArrowMarshaller.ToRecordBatch(rows);
        var reconstructed = ArrowMarshaller.FromRecordBatch<ExtendedTypesSchema>(batch).ToList();

        // Assert
        Assert.That(reconstructed[0].ModifiedAt, Is.Null);
        Assert.That(reconstructed[0].Duration, Is.Null);
        Assert.That(reconstructed[0].Name, Is.Null);
    }

    [Test]
    public void ToRecordBatch_EmptyEnumerable_CreatesEmptyBatch()
    {
        // Arrange
        var rows = System.Array.Empty<SimpleRowSchema>();

        // Act
        var batch = ArrowMarshaller.ToRecordBatch(rows);

        // Assert
        Assert.That(batch.Length, Is.EqualTo(0));
        Assert.That(batch.ColumnCount, Is.EqualTo(3)); // Schema still has 3 fields
    }

    [Test]
    public void FromRecordBatch_EmptyBatch_ReturnsEmptyEnumerable()
    {
        // Arrange
        var rows = System.Array.Empty<SimpleRowSchema>();
        var batch = ArrowMarshaller.ToRecordBatch(rows);

        // Act
        var reconstructed = ArrowMarshaller.FromRecordBatch<SimpleRowSchema>(batch).ToList();

        // Assert
        Assert.That(reconstructed, Is.Empty);
    }

    [Test]
    public void ToRecordBatch_NullRows_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
          () => ArrowMarshaller.ToRecordBatch<SimpleRowSchema>(null!)
        );
    }

    [Test]
    public void FromRecordBatch_NullBatch_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
          () => ArrowMarshaller.FromRecordBatch<SimpleRowSchema>(null!)
        );
    }

    [Test]
    public void ToIpcBuffer_NullBatch_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ArrowMarshaller.ToIpcBuffer(null!));
    }

    [Test]
    public void FromIpcBuffer_NullBuffer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ArrowMarshaller.FromIpcBuffer(null!));
    }
}
