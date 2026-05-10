using Flowthru.Data.Storage;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Coverage-fill for the surfaces of <see cref="EFCoreQueryStorageAdapter{T}"/>
/// not exercised by <see cref="EFCoreQueryStorageAdapterTests"/>:
/// <c>InspectShallow</c>, <c>InspectDeep</c>, <c>InspectTarget</c>, and
/// <c>IHasEfficientCount.GetCountAsync</c>.
/// </summary>
[TestFixture]
[Category("EFCore")]
public class EFCoreQueryStorageAdapterAdditionalTests
{
  private IDbContextFactory<TestDbContext> _factory = null!;
  private string _dbPath = null!;

  [SetUp]
  public void SetUp()
  {
    (_factory, _dbPath) = TestDbContextFactoryBuilder.Build();
    // Pre-populate with one row so default-disallow-empty inspections succeed.
    using var ctx = _factory.CreateDbContext();
    ctx.Set<TestEntity>().Add(new TestEntity { Id = 1, Name = "seed", Value = 1.0 });
    ctx.SaveChanges();
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
  public async Task InspectShallow_PopulatedTable_Succeeds()
  {
    var adapter = new EFCoreQueryStorageAdapter<TestEntity>(
      () => _factory.CreateDbContext()
    );
    var inspect = await adapter.InspectShallow(5).Run();
    Assert.That(((EffResult<ValidationResult>.Success)inspect).Value.IsValid, Is.True);
  }

  [Test]
  public async Task InspectDeep_PopulatedTable_Succeeds()
  {
    var adapter = new EFCoreQueryStorageAdapter<TestEntity>(
      () => _factory.CreateDbContext()
    );
    var inspect = await adapter.InspectDeep().Run();
    Assert.That(((EffResult<ValidationResult>.Success)inspect).Value.IsValid, Is.True);
  }

  [Test]
  public async Task InspectTarget_ExistingTable_Succeeds()
  {
    var adapter = new EFCoreQueryStorageAdapter<TestEntity>(
      () => _factory.CreateDbContext()
    );
    var inspect = await adapter.InspectTarget().Run();
    Assert.That(((EffResult<ValidationResult>.Success)inspect).Value.IsValid, Is.True);
  }

  [Test]
  public async Task GetCountAsync_ReportsRowCount()
  {
    var adapter = new EFCoreQueryStorageAdapter<TestEntity>(
      () => _factory.CreateDbContext()
    );
    var hasCount = (IHasEfficientCount)adapter;
    var result = await hasCount.GetCountAsync().Run();
    Assert.That(((EffResult<int>.Success)result).Value, Is.EqualTo(1));
  }
}
