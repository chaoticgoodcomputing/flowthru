using Flowthru.Core.Data.Storage.Strategies;
using Flowthru.Core.Tests.Schemas;

namespace Flowthru.Core.Tests.Services.StorageStrategies;

/// <summary>
/// Tests for storage entry factory implementations.
/// Verifies environment-specific catalog entry creation.
/// </summary>
public class StorageEntryFactoryTests
{
  [Test]
  public void CsvFactory_CreatesEnumerableEntry_WithCorrectLabel()
  {
    // Arrange
    var factory = new CsvStorageEntryFactory(basePath: "/tmp/test");

    // Act
    var entry = factory.CreateEnumerable<RequiredMembersSchema>("TestData");

    // Assert
    Assert.That(entry, Is.Not.Null);
    Assert.That(entry.Label, Is.EqualTo("TestData"));
  }

  [Test]
  public void CsvFactory_CreatesSingleEntry_WithCorrectLabel()
  {
    // Arrange
    var factory = new CsvStorageEntryFactory(basePath: "/tmp/test");

    // Act
    var entry = factory.CreateSingle<RequiredMembersSchema>("TestConfig");

    // Assert
    Assert.That(entry, Is.Not.Null);
    Assert.That(entry.Label, Is.EqualTo("TestConfig"));
  }

  [Test]
  public void MemoryFactory_CreatesEnumerableEntry_WithCorrectLabel()
  {
    // Arrange
    var factory = new MemoryStorageEntryFactory();

    // Act
    var entry = factory.CreateEnumerable<RequiredMembersSchema>("TestData");

    // Assert
    Assert.That(entry, Is.Not.Null);
    Assert.That(entry.Label, Is.EqualTo("TestData"));
  }

  [Test]
  public void MemoryFactory_CreatesSingleEntry_WithCorrectLabel()
  {
    // Arrange
    var factory = new MemoryStorageEntryFactory();

    // Act
    var entry = factory.CreateSingle<RequiredMembersSchema>("TestConfig");

    // Assert
    Assert.That(entry, Is.Not.Null);
    Assert.That(entry.Label, Is.EqualTo("TestConfig"));
  }

  [Test]
  public void DatabaseFactory_ThrowsNotImplementedException_ForEnumerable()
  {
    // Arrange
    var factory = new DatabaseStorageEntryFactory(connectionString: "Server=test");

    // Act & Assert
    Assert.Throws<NotImplementedException>(
      () => factory.CreateEnumerable<RequiredMembersSchema>("TestData")
    );
  }

  [Test]
  public void DatabaseFactory_ThrowsNotImplementedException_ForSingle()
  {
    // Arrange
    var factory = new DatabaseStorageEntryFactory(connectionString: "Server=test");

    // Act & Assert
    Assert.Throws<NotImplementedException>(
      () => factory.CreateSingle<RequiredMembersSchema>("TestConfig")
    );
  }

  [Test]
  public void CsvFactory_UsesCustomPath_WhenProvided()
  {
    // Arrange
    var factory = new CsvStorageEntryFactory(basePath: "/tmp/test");
    var options = new StorageOptions { Path = "/custom/path/data.csv" };

    // Act
    var entry = factory.CreateEnumerable<RequiredMembersSchema>("TestData", options);

    // Assert - we can't directly verify the path, but we can verify the entry was created
    Assert.That(entry, Is.Not.Null);
    Assert.That(entry.Label, Is.EqualTo("TestData"));
  }

  [Test]
  public void CsvFactory_Constructor_ThrowsOnNullBasePath()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => new CsvStorageEntryFactory(basePath: null!));
  }

  [Test]
  public void DatabaseFactory_Constructor_ThrowsOnNullConnectionString()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(
      () => new DatabaseStorageEntryFactory(connectionString: null!)
    );
  }
}
