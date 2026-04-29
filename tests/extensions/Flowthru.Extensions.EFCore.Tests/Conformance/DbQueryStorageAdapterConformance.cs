using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Extensions.EFCore.Data;
using Flowthru.Tests.Kits.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="DbQueryStorageAdapter{T}"/>.
/// </summary>
/// <remarks>
/// <see cref="DbQueryStorageAdapter{T}.Load"/> returns a deferred <see cref="DbQuery{T}"/>
/// handle — no DB I/O occurs until iteration. The kit's round-trip comparer materializes
/// via sequence comparison, which triggers the actual query.
/// </remarks>
[TestFixtureSource(nameof(Fixtures))]
public class DbQueryStorageAdapterConformance
  : StorageAdapterConformance<IEnumerable<TestEntity>>
{
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/dbquery-entities" };

  private SqliteConnection _connection = null!;
  private DbContextOptions<TestDbContext> _options = null!;

  public DbQueryStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

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
    var adapter = new DbQueryStorageAdapter<TestEntity>(() => new TestDbContext(_options));
    adapter.Save(data).Run().GetAwaiter().GetResult();
    return adapter;
  }

  protected override IStorageAdapter<IEnumerable<TestEntity>> CreateMissingSource()
  {
    var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
    using (var ctx = new TestDbContext(options))
    {
      ctx.Database.EnsureCreated();
    }
    return new DbQueryStorageAdapter<TestEntity>(() => new TestDbContext(options));
  }

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
