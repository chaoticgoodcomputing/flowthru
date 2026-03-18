using Flowthru.Data.Storage;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

[TestFixture]
public class EFCoreStorageAdapterTests
{
  private SqliteConnection _connection = null!;
  private DbContextOptions<TestDbContext> _options = null!;

  [SetUp]
  public async Task SetUp()
  {
    _connection = new SqliteConnection("Data Source=:memory:");
    await _connection.OpenAsync();
    _options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;

    await using var context = new TestDbContext(_options);
    await context.Database.EnsureCreatedAsync();
  }

  [TearDown]
  public async Task TearDown()
  {
    await _connection.DisposeAsync();
  }

  [Test]
  public async Task DefaultRoundTrip_SaveAndLoad_ReturnsEntities()
  {
    var testData = new[]
    {
      new TestEntity { Id = 1, Name = "Alice" },
      new TestEntity { Id = 2, Name = "Bob" },
    };

    var entry = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity>(
      "test",
      () => new TestDbContext(_options)
    );

    await entry.Save(testData).Run();
    var loaded = (await entry.Load().Run()).ToList();

    Assert.That(loaded, Has.Count.EqualTo(2));
    Assert.That(loaded.Select(e => e.Name), Is.EquivalentTo(new[] { "Alice", "Bob" }));
  }

  [Test]
  public async Task QueryCustomizer_FiltersResults()
  {
    var testData = new[]
    {
      new TestEntity { Id = 1, Name = "Alice" },
      new TestEntity { Id = 2, Name = "Bob" },
    };

    var saveEntry = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity>(
      "save",
      () => new TestDbContext(_options)
    );
    await saveEntry.Save(testData).Run();

    var filteredEntry = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity>(
      "filtered",
      () => new TestDbContext(_options),
      queryCustomizer: q => q.Where(e => e.Id == 2)
    );
    var loaded = (await filteredEntry.Load().Run()).ToList();

    Assert.That(loaded, Has.Count.EqualTo(1));
    Assert.That(loaded[0].Name, Is.EqualTo("Bob"));
  }

  [Test]
  public async Task CustomSaveDelegate_IsCalled()
  {
    var testData = new[]
    {
      new TestEntity { Id = 1, Name = "Alice" },
    };
    bool saveCalled = false;

    var entry = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity, TestDbContext>(
      "test",
      () => new TestDbContext(_options),
      saveFunc: async (ctx, data, ct) =>
      {
        saveCalled = true;
        await EFCoreStorageAdapter<TestEntity>.DefaultSave(ctx, data, ct);
      }
    );

    await entry.Save(testData).Run();

    Assert.That(saveCalled, Is.True);
  }

  [Test]
  public async Task TypedSaveDelegate_ReceivesTypedContext()
  {
    var testData = new[]
    {
      new TestEntity { Id = 1, Name = "Alice" },
    };
    Type? receivedContextType = null;

    var entry = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity, TestDbContext>(
      "test",
      () => new TestDbContext(_options),
      saveFunc: async (ctx, data, ct) =>
      {
        receivedContextType = ctx.GetType();
        await EFCoreStorageAdapter<TestEntity>.DefaultSave(ctx, data, ct);
      }
    );

    await entry.Save(testData).Run();

    Assert.That(receivedContextType, Is.EqualTo(typeof(TestDbContext)));
  }

  [Test]
  public async Task TypedContextFactory_RoundTrip()
  {
    var testData = new[]
    {
      new TestEntity { Id = 1, Name = "Alice" },
    };

    var entry = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity, TestDbContext>(
      "test",
      () => new TestDbContext(_options)
    );

    await entry.Save(testData).Run();
    var loaded = (await entry.Load().Run()).ToList();

    Assert.That(loaded, Has.Count.EqualTo(1));
    Assert.That(loaded[0].Name, Is.EqualTo("Alice"));
  }

  [Test]
  public async Task IDbContextFactory_RoundTrip()
  {
    var testData = new[]
    {
      new TestEntity { Id = 1, Name = "Alice" },
    };
    var factory = new TestDbContextFactory(_options);

    var entry = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity, TestDbContext>("test", factory);

    await entry.Save(testData).Run();
    var loaded = (await entry.Load().Run()).ToList();

    Assert.That(loaded, Has.Count.EqualTo(1));
    Assert.That(loaded[0].Name, Is.EqualTo("Alice"));
  }
}
