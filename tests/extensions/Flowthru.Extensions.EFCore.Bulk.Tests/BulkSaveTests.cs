using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Bulk.Tests;

[TestFixture]
public class BulkSaveTests
{
  private TestDbContextFactory _factory = null!;

  [SetUp]
  public void SetUp()
  {
    var options = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite("DataSource=:memory:")
      .Options;

    _factory = new TestDbContextFactory(options);

    // Create schema — SQLite in-memory DB lives only while the connection is open,
    // so we keep a single connection and reuse it via the factory.
    using var db = _factory.CreateDbContext();
    db.Database.OpenConnection();
    db.Database.EnsureCreated();
  }

  [Test]
  public void Insert_Returns_Valid_SaveFunc_Delegate()
  {
    var saveFunc = BulkSave.Insert<TestEntity, TestDbContext>();
    Assert.That(saveFunc, Is.Not.Null);
  }

  [Test]
  public void TruncateAndInsert_Returns_Valid_SaveFunc_Delegate()
  {
    var saveFunc = BulkSave.TruncateAndInsert<TestEntity, TestDbContext>();
    Assert.That(saveFunc, Is.Not.Null);
  }

  [Test]
  public void InsertOrUpdate_Returns_Valid_SaveFunc_Delegate()
  {
    var saveFunc = BulkSave.InsertOrUpdate<TestEntity, TestDbContext>();
    Assert.That(saveFunc, Is.Not.Null);
  }

  [Test]
  public void InsertOrUpdateOrDelete_Returns_Valid_SaveFunc_Delegate()
  {
    var saveFunc = BulkSave.InsertOrUpdateOrDelete<TestEntity, TestDbContext>();
    Assert.That(saveFunc, Is.Not.Null);
  }

  [Test]
  public void Insert_With_Options_Passes_Options_Through()
  {
    var options = new BulkSaveOptions
    {
      BatchSize = 5000,
      TimeoutSeconds = 120,
      PreserveInsertOrder = false,
      SetOutputIdentity = true,
      UseUnlogged = true,
    };

    var saveFunc = BulkSave.Insert<TestEntity, TestDbContext>(options);
    Assert.That(saveFunc, Is.Not.Null);
  }

  [Test]
  public void TruncateAndInsert_With_Null_Options_Uses_Defaults()
  {
    var saveFunc = BulkSave.TruncateAndInsert<TestEntity, TestDbContext>(null);
    Assert.That(saveFunc, Is.Not.Null);
  }
}
