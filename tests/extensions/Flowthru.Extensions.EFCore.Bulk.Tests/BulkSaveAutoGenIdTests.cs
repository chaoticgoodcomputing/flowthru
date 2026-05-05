using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Bulk.Tests;

/// <summary>
/// Verifies that <c>BulkSave.Insert</c> on an entity with an explicit
/// <c>int Id</c> primary key correctly round-trips the supplied identity
/// values. This is the SpaceflightsStagingSchema example's pattern for
/// <c>TrainingData</c>, <c>TestData</c>, and <c>ModelPredictions</c> — they
/// declare an explicit <c>int Id { get; init; }</c> rather than a shadow
/// property, which is what makes them bulk-compatible.
/// </summary>
/// <remarks>
/// <para>
/// This test uses pre-assigned non-zero Ids because SQLite + EFCore
/// BulkExtensions does <em>not</em> automatically assign Ids to entities
/// arriving with <c>Id = 0</c>; PostgreSQL does (via Npgsql's identity
/// generator), and the SpaceflightsStagingSchema example exercises that
/// path against a real PG container. For SQLite-only unit coverage, the
/// meaningful invariant is explicit-Id round-trip integrity.
/// </para>
/// </remarks>
[TestFixture]
[Category("EFCore")]
public class BulkSaveAutoGenIdTests
{
  private SqliteConnection _connection = null!;
  private DbContextOptions<TestDbContext> _options = null!;

  [SetUp]
  public async Task SetUp()
  {
    _connection = new SqliteConnection("Data Source=:memory:");
    await _connection.OpenAsync();
    _options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;

    await using var ctx = new TestDbContext(_options);
    await ctx.Database.EnsureCreatedAsync();
  }

  [TearDown]
  public async Task TearDown()
  {
    await _connection.DisposeAsync();
  }

  [Test]
  public async Task BulkInsert_PreservesExplicitIds()
  {
    var factory = new TestDbContextFactory(_options);
    var saveFunc = BulkSave.Insert<TestEntity, TestDbContext>();
    var inputs = Enumerable
      .Range(1, 100)
      .Select(i => new TestEntity { Id = i, Name = $"row-{i}" })
      .ToList();

    await using (var ctx = factory.CreateDbContext())
    {
      await saveFunc(ctx, inputs, CancellationToken.None);
    }

    await using (var ctx = factory.CreateDbContext())
    {
      var rows = await ctx.TestEntities.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
      Assert.That(rows, Has.Count.EqualTo(100));
      Assert.That(
        rows.Select(r => r.Id),
        Is.EquivalentTo(Enumerable.Range(1, 100)),
        "BulkSave.Insert should preserve explicit Id values verbatim."
      );
    }
  }
}
