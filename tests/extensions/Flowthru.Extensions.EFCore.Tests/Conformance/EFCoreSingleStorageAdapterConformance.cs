using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Extensions.EFCore.Tests.Backends;
using Flowthru.Tests.Kits.Storage;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="EFCoreSingleStorageAdapter{T}"/> — exactly one row per table —
/// parameterized over the backend.
/// </summary>
[TestFixtureSource(nameof(BackendMatrix))]
public class EFCoreSingleStorageAdapterConformance : StorageAdapterConformance<TestEntity>
{
  public static IEnumerable<TestFixtureData> BackendMatrix()
  {
    const string fixturePath = "Synthetic/efcore-single-entity";
    yield return new TestFixtureData(fixturePath, typeof(SqliteInMemoryBackend));
    var pg = new TestFixtureData(fixturePath, typeof(PostgresContainerBackend));
    pg.Properties.Add("Category", "Integration");
    yield return pg;
  }

  private readonly Type _backendType;
  private IDbBackend _backend = default!;
  private DbContextOptions<TestDbContext> _options = default!;

  public EFCoreSingleStorageAdapterConformance(string fixturePath, Type backendType)
    : base(fixturePath)
  {
    _backendType = backendType;
  }

  [OneTimeSetUp]
  public async Task StartBackend()
  {
    _backend = (IDbBackend)Activator.CreateInstance(_backendType)!;
    _options = await _backend.StartAsync();
    await using var context = new TestDbContext(_options);
    await context.Database.EnsureCreatedAsync();
  }

  [OneTimeTearDown]
  public async Task StopBackend()
  {
    if (_backend is not null)
    {
      await _backend.DisposeAsync();
    }
  }

  [SetUp]
  public async Task ResetTable()
  {
    await using var context = new TestDbContext(_options);
    context.TestEntities.RemoveRange(context.TestEntities);
    await context.SaveChangesAsync();
  }

  protected override TestEntity LoadFixture(string fixturePath) =>
    new TestEntity { Id = 42, Name = "Singleton" };

  protected override IStorageAdapter<TestEntity> CreateWellFormed(TestEntity data)
  {
    var adapter = new EFCoreSingleStorageAdapter<TestEntity>(() => new TestDbContext(_options));
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<TestEntity> CreateMissingSource() =>
    new EFCoreSingleStorageAdapter<TestEntity>(() => new TestDbContext(_options));

  protected override IEqualityComparer<TestEntity>? Comparer => new TestEntityComparer();

  protected override ValidationErrorType MissingSourceErrorType =>
    ValidationErrorType.EmptyDataset;

  private sealed class TestEntityComparer : IEqualityComparer<TestEntity>
  {
    public bool Equals(TestEntity? x, TestEntity? y)
    {
      if (x is null || y is null)
      {
        return ReferenceEquals(x, y);
      }
      return x.Id == y.Id && x.Name == y.Name;
    }

    public int GetHashCode(TestEntity obj) => HashCode.Combine(obj.Id, obj.Name);
  }
}
