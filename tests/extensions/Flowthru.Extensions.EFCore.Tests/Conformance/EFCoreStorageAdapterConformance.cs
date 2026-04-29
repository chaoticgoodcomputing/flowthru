using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Extensions.EFCore.Tests.Backends;
using Flowthru.Tests.Kits.Storage;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="EFCoreStorageAdapter{T}"/>, parameterized over the backend.
/// NUnit instantiates the fixture once per <see cref="BackendMatrix"/> entry; each runs
/// the full conformance contract independently against its declared backend.
/// </summary>
/// <remarks>
/// The backend type is passed as a constructor argument (rather than a generic type
/// parameter) because NUnit 4's <see cref="TestFixtureData"/> doesn't expose a settable
/// <c>TypeArgs</c> alongside <c>SetCategory</c>, so the cleanest combination of
/// per-backend instantiation + per-backend categorization is via runtime activation.
/// </remarks>
[TestFixtureSource(nameof(BackendMatrix))]
public class EFCoreStorageAdapterConformance : StorageAdapterConformance<IEnumerable<TestEntity>>
{
  public static IEnumerable<TestFixtureData> BackendMatrix()
  {
    const string fixturePath = "Synthetic/efcore-entities";
    yield return new TestFixtureData(fixturePath, typeof(SqliteInMemoryBackend));
    var pg = new TestFixtureData(fixturePath, typeof(PostgresContainerBackend));
    pg.Properties.Add("Category", "Integration");
    yield return pg;
  }

  private readonly Type _backendType;
  private IDbBackend _backend = default!;
  private DbContextOptions<TestDbContext> _options = default!;

  public EFCoreStorageAdapterConformance(string fixturePath, Type backendType)
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

  protected override IEnumerable<TestEntity> LoadFixture(string fixturePath) =>
    new[]
    {
      new TestEntity { Id = 1, Name = "Alice" },
      new TestEntity { Id = 2, Name = "Bob" },
      new TestEntity { Id = 3, Name = "Charlie" },
    };

  protected override IStorageAdapter<IEnumerable<TestEntity>> CreateWellFormed(
    IEnumerable<TestEntity> data
  )
  {
    var adapter = new EFCoreStorageAdapter<TestEntity>(() => new TestDbContext(_options));
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<IEnumerable<TestEntity>> CreateMissingSource() =>
    new EFCoreStorageAdapter<TestEntity>(() => new TestDbContext(_options));

  protected override IEqualityComparer<IEnumerable<TestEntity>>? Comparer =>
    new EntitySequenceComparer();

  protected override ValidationErrorType MissingSourceErrorType =>
    ValidationErrorType.EmptyDataset;

  private sealed class EntitySequenceComparer : IEqualityComparer<IEnumerable<TestEntity>>
  {
    public bool Equals(IEnumerable<TestEntity>? x, IEnumerable<TestEntity>? y)
    {
      if (x is null || y is null)
      {
        return ReferenceEquals(x, y);
      }
      var xList = x.OrderBy(e => e.Id).ToList();
      var yList = y.OrderBy(e => e.Id).ToList();
      if (xList.Count != yList.Count)
      {
        return false;
      }
      for (var i = 0; i < xList.Count; i++)
      {
        if (xList[i].Id != yList[i].Id || xList[i].Name != yList[i].Name)
        {
          return false;
        }
      }
      return true;
    }

    public int GetHashCode(IEnumerable<TestEntity> obj) => 0;
  }
}
