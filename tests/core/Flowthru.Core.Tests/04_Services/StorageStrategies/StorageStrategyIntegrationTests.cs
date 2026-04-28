using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage.Strategies;
using Flowthru.Tests.Helpers.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Services.StorageStrategies;

/// <summary>
/// Integration tests demonstrating catalog usage with storage strategies.
/// Shows how to switch between CSV (dev), Memory (test), and Database (prod) via DI.
/// </summary>
public class StorageStrategyIntegrationTests
{
  // Example catalog using IStorageEntryFactory injection
  private class TestCatalog : CatalogAbstract
  {
    private readonly IStorageEntryFactory _storage;

    public TestCatalog(IStorageEntryFactory storage)
    {
      _storage = storage;
      InitializeCatalogProperties();
    }

    public IItem<IEnumerable<RequiredMembersSchema>> TestData =>
      CreateItem(() => _storage.CreateEnumerable<RequiredMembersSchema>("TestData"));
  }

  [Test]
  public void Catalog_WithMemoryStrategy_CreatesWorkingEntries()
  {
    // Arrange - Configure DI with memory strategy (test environment)
    var services = new ServiceCollection();
    services.AddSingleton<IStorageEntryFactory, MemoryStorageEntryFactory>();
    services.AddSingleton<TestCatalog>();

    var provider = services.BuildServiceProvider();
    var catalog = provider.GetRequiredService<TestCatalog>();

    // Act - Verify entry was created
    var entry = catalog.TestData;

    // Assert
    Assert.That(entry, Is.Not.Null);
    Assert.That(entry.Label, Is.EqualTo("TestData"));
  }

  [Test]
  public void Catalog_WithCsvStrategy_CreatesWorkingEntries()
  {
    // Arrange - Configure DI with CSV strategy (dev environment)
    using var tempDir = new TempDirectory();
    var services = new ServiceCollection();
    services.AddSingleton<IStorageEntryFactory>(_ => new CsvStorageEntryFactory(tempDir.Path));
    services.AddSingleton<TestCatalog>();

    var provider = services.BuildServiceProvider();
    var catalog = provider.GetRequiredService<TestCatalog>();

    // Act
    var entry = catalog.TestData;

    // Assert
    Assert.That(entry, Is.Not.Null);
    Assert.That(entry.Label, Is.EqualTo("TestData"));
  }

  [Test]
  public void Catalog_SwitchingStrategies_DoesNotAffectSchemaTypes()
  {
    // Arrange - Create two catalogs with different strategies
    var memoryServices = new ServiceCollection();
    memoryServices.AddSingleton<IStorageEntryFactory, MemoryStorageEntryFactory>();
    memoryServices.AddSingleton<TestCatalog>();
    var memoryCatalog = memoryServices.BuildServiceProvider().GetRequiredService<TestCatalog>();

    using var tempDir = new TempDirectory();
    var csvServices = new ServiceCollection();
    csvServices.AddSingleton<IStorageEntryFactory>(_ => new CsvStorageEntryFactory(tempDir.Path));
    csvServices.AddSingleton<TestCatalog>();
    var csvCatalog = csvServices.BuildServiceProvider().GetRequiredService<TestCatalog>();

    // Act - Get entries from both catalogs
    var memoryEntry = memoryCatalog.TestData;
    var csvEntry = csvCatalog.TestData;

    // Assert - Both entries have same schema type
    Assert.That(memoryEntry.Label, Is.EqualTo(csvEntry.Label));
    Assert.That(
      memoryEntry.GetType().GetGenericArguments()[0],
      Is.EqualTo(typeof(IEnumerable<RequiredMembersSchema>))
    );
    Assert.That(
      csvEntry.GetType().GetGenericArguments()[0],
      Is.EqualTo(typeof(IEnumerable<RequiredMembersSchema>))
    );
  }

  [Test]
  public void FlowthruServiceBuilder_UseStorageStrategy_RegistersFactory()
  {
    // Arrange - Use fluent builder API
    var services = new ServiceCollection();
    services.AddFlowthru(
      new ConfigurationBuilder().Build(),
      builder =>
      {
        builder.UseStorageStrategy<MemoryStorageEntryFactory>();
        builder.RegisterCatalog<TestCatalog>();
        builder.RegisterFlows(_ => []);
      }
    );

    var provider = services.BuildServiceProvider();

    // Act - Resolve factory
    var factory = provider.GetService<IStorageEntryFactory>();

    // Assert
    Assert.That(factory, Is.Not.Null);
    Assert.That(factory, Is.InstanceOf<MemoryStorageEntryFactory>());
  }

  [Test]
  public void FlowthruServiceBuilder_UseStorageStrategyWithConfiguration_ReadsSettings()
  {
    // Arrange - Simulate appsettings.json with StorageStrategy section
    var configDict = new Dictionary<string, string?>
    {
      ["Flowthru:StorageStrategy:BasePath"] = "/data/csv",
    };

    var configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

    var services = new ServiceCollection();
    services.AddFlowthru(
      configuration,
      builder =>
      {
        builder.UseStorageStrategy<CsvStorageEntryFactory>();
        builder.RegisterCatalog<TestCatalog>();
        builder.RegisterFlows(_ => []);
      }
    );

    var provider = services.BuildServiceProvider();

    // Act
    var factory = provider.GetService<IStorageEntryFactory>();

    // Assert - Verify factory was created (filesystem construction will happen on first use)
    Assert.That(factory, Is.Not.Null);
    Assert.That(factory, Is.InstanceOf<CsvStorageEntryFactory>());
  }

  /// <summary>
  /// Helper for creating and cleaning up temporary directories
  /// </summary>
  private sealed class TempDirectory : IDisposable
  {
    public string Path { get; }

    public TempDirectory()
    {
      Path = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        $"flowthru-test-{Guid.NewGuid()}"
      );
      Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
      if (Directory.Exists(Path))
      {
        Directory.Delete(Path, recursive: true);
      }
    }
  }
}
