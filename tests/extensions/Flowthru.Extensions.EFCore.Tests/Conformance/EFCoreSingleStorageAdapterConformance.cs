using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Tests.Kits.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="EFCoreSingleStorageAdapter{T}"/> — exactly one row per table.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class EFCoreSingleStorageAdapterConformance : StorageAdapterConformance<TestEntity>
{
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/efcore-single-entity" };

  private SqliteConnection _connection = null!;
  private DbContextOptions<TestDbContext> _options = null!;

  public EFCoreSingleStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

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

  protected override TestEntity LoadFixture(string fixturePath) =>
    new TestEntity { Id = 42, Name = "Singleton" };

  protected override IStorageAdapter<TestEntity> CreateWellFormed(TestEntity data)
  {
    var adapter = new EFCoreSingleStorageAdapter<TestEntity>(() => new TestDbContext(_options));
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<TestEntity> CreateMissingSource()
  {
    var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
    using (var ctx = new TestDbContext(options))
    {
      ctx.Database.EnsureCreated();
    }
    return new EFCoreSingleStorageAdapter<TestEntity>(() => new TestDbContext(options));
  }

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
