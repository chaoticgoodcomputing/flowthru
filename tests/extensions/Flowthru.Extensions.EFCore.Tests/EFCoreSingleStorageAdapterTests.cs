using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Prelude;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Direct exercises of <see cref="Flowthru.Data.Storage.EFCore.EFCoreSingleStorageAdapter{T}"/>
/// — the single-row table variant. Asserts the "exactly one row"
/// invariant and the inspection failures that surface when it's
/// violated.
/// </summary>
[TestFixture]
[Category("EFCore")]
public class EFCoreSingleStorageAdapterTests
{
  private IDbContextFactory<TestDbContext> _factory = null!;
  private string _dbPath = null!;

  [SetUp]
  public void SetUp()
  {
    (_factory, _dbPath) = TestDbContextFactoryBuilder.Build();
  }

  [TearDown]
  public void TearDown()
  {
    if (File.Exists(_dbPath))
    {
      try { File.Delete(_dbPath); }
      catch { /* best effort */ }
    }
  }

  [Test]
  public async Task SaveLoad_RoundTrips()
  {
    var item = ItemFactory.Singleton.EFCore<TestSingletonEntity, TestDbContext>(
      "single", _factory
    );

    var entity = new TestSingletonEntity { Id = 1, Description = "the one" };

    await item.Save(entity).Run();

    var load = await item.Load().Run();
    var loaded = ((EffResult<TestSingletonEntity>.Success)load).Value;
    Assert.That(loaded, Is.EqualTo(entity));
  }

  [Test]
  public async Task DefaultSave_ReplacesPreviousSingleton()
  {
    var item = ItemFactory.Singleton.EFCore<TestSingletonEntity, TestDbContext>(
      "single", _factory
    );

    await item.Save(new TestSingletonEntity { Id = 1, Description = "first" }).Run();
    await item.Save(new TestSingletonEntity { Id = 2, Description = "second" }).Run();

    var load = await item.Load().Run();
    var loaded = ((EffResult<TestSingletonEntity>.Success)load).Value;
    Assert.That(loaded.Id, Is.EqualTo(2));
    Assert.That(loaded.Description, Is.EqualTo("second"),
      "Default save should leave exactly the latest row in place.");
  }

  [Test]
  public async Task InspectShallow_EmptyTable_FailsByDefault()
  {
    var item = ItemFactory.Singleton.EFCore<TestSingletonEntity, TestDbContext>(
      "single", _factory
    );

    var inspect = await item.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)inspect).Value;
    Assert.That(validation.IsValid, Is.False);
    Assert.That(
      validation.Errors.Any(e => e.ErrorType == ValidationErrorType.EmptyDataset),
      Is.True
    );
  }

  [Test]
  public async Task InspectShallow_MultipleRows_Fails()
  {
    // Seed two rows directly via a context — bypass the adapter so we
    // can trigger the multi-row invariant violation that the adapter
    // wouldn't normally let happen.
    using (var ctx = await _factory.CreateDbContextAsync())
    {
      ctx.Singleton.AddRange(
        new TestSingletonEntity { Id = 1, Description = "a" },
        new TestSingletonEntity { Id = 2, Description = "b" }
      );
      await ctx.SaveChangesAsync();
    }

    var item = ItemFactory.Singleton.EFCore<TestSingletonEntity, TestDbContext>(
      "single", _factory
    );
    var inspect = await item.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)inspect).Value;
    Assert.That(validation.IsValid, Is.False);
    Assert.That(
      validation.Errors.Any(e =>
        e.ErrorType == ValidationErrorType.DeserializationError
        && e.Message.Contains("contains 2 rows")
      ),
      Is.True,
      "A multi-row table should fail single-entity inspection with a clear count."
    );
  }

  [Test]
  public async Task InspectShallow_AllowEmptyData_PassesOnEmpty()
  {
    var item = ItemFactory.Singleton.EFCore<TestSingletonEntity, TestDbContext>(
      "single", _factory, allowEmptyData: true
    );

    var inspect = await item.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)inspect).Value;
    Assert.That(validation.IsValid, Is.True);
  }
}
