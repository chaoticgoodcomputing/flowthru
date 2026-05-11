using Flowthru.Data.Storage;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Prelude;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Coverage-fill for the surfaces of <see cref="EFCoreSingleStorageAdapter{T}"/>
/// not exercised by <see cref="EFCoreSingleStorageAdapterTests"/>:
/// <c>Exists</c>, <c>InspectDeep</c>, <c>InspectTarget</c>.
/// </summary>
[TestFixture]
[Category("EFCore")]
public class EFCoreSingleStorageAdapterAdditionalTests
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
  public async Task Exists_EmptyTable_IsFalse()
  {
    var adapter = new EFCoreSingleStorageAdapter<TestSingletonEntity>(
      () => _factory.CreateDbContext()
    );
    var result = await adapter.Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }

  [Test]
  public async Task Exists_PopulatedTable_IsTrue()
  {
    var adapter = new EFCoreSingleStorageAdapter<TestSingletonEntity>(
      () => _factory.CreateDbContext()
    );
    await adapter.Save(new TestSingletonEntity { Id = 1, Description = "x" }).Run();
    var result = await adapter.Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.True);
  }

  [Test]
  public async Task InspectDeep_PopulatedTable_Succeeds()
  {
    var adapter = new EFCoreSingleStorageAdapter<TestSingletonEntity>(
      () => _factory.CreateDbContext()
    );
    await adapter.Save(new TestSingletonEntity { Id = 1, Description = "x" }).Run();
    var inspect = await adapter.InspectDeep().Run();
    Assert.That(((EffResult<ValidationResult>.Success)inspect).Value.IsValid, Is.True);
  }

  [Test]
  public async Task InspectTarget_ExistingTable_Succeeds()
  {
    var adapter = new EFCoreSingleStorageAdapter<TestSingletonEntity>(
      () => _factory.CreateDbContext()
    );
    var inspect = await adapter.InspectTarget().Run();
    Assert.That(((EffResult<ValidationResult>.Success)inspect).Value.IsValid, Is.True);
  }
}
