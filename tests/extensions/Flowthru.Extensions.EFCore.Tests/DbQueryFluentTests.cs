using Flowthru.Extensions.EFCore.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Tests for <see cref="DbQuery{T}"/>'s public fluent LINQ surface
/// (<c>OrderBy</c>, <c>OrderByDescending</c>, <c>Skip</c>, <c>Take</c>).
/// </summary>
/// <remarks>
/// The LINQ overloads return new <see cref="DbQuery{T}"/> handles with composed expression
/// trees; tests materialize the resulting handles to confirm the composition produced the
/// intended SQL.
/// </remarks>
[TestFixture]
public class DbQueryFluentTests
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

    context.TestEntities.AddRange(
      new TestEntity { Id = 3, Name = "Charlie" },
      new TestEntity { Id = 1, Name = "Alice" },
      new TestEntity { Id = 2, Name = "Bob" }
    );
    await context.SaveChangesAsync();
  }

  [TearDown]
  public async Task TearDown()
  {
    await _connection.DisposeAsync();
  }

  private DbQuery<TestEntity> NewQuery() =>
    new(
      label: "test",
      scope: DbScope.Inferred(_options),
      contextFactory: () => new TestDbContext(_options),
      ownsContext: true,
      buildQuery: ctx => ctx.Set<TestEntity>()
    );

  [Test]
  public async Task OrderBy_SortsAscending()
  {
    var query = NewQuery().OrderBy(e => e.Id);
    var materialized = await query.ToListAsync();

    Assert.That(materialized.Select(e => e.Id), Is.EqualTo(new[] { 1, 2, 3 }));
  }

  [Test]
  public async Task OrderByDescending_SortsDescending()
  {
    var query = NewQuery().OrderByDescending(e => e.Id);
    var materialized = await query.ToListAsync();

    Assert.That(materialized.Select(e => e.Id), Is.EqualTo(new[] { 3, 2, 1 }));
  }

  [Test]
  public async Task Skip_SkipsFirstNRows()
  {
    var query = NewQuery().OrderBy(e => e.Id).Skip(1);
    var materialized = await query.ToListAsync();

    Assert.That(materialized.Select(e => e.Id), Is.EqualTo(new[] { 2, 3 }));
  }

  [Test]
  public async Task SkipAndTake_Compose()
  {
    // Composition test: OrderBy + Skip + Take chained — confirms each operator
    // returns a new DbQuery that the next operator can build on.
    var query = NewQuery().OrderBy(e => e.Id).Skip(1).Take(1);
    var materialized = await query.ToListAsync();

    Assert.That(materialized.Select(e => e.Id), Is.EqualTo(new[] { 2 }));
  }
}
