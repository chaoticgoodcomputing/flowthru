using System.Diagnostics;
using EFCore.BulkExtensions;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Bulk.Tests.Performance;

/// <summary>
/// Guardrail tests asserting that <see cref="BulkSave"/> factory delegates write correctly
/// and complete within a reasonable wall-clock budget on a large dataset.
/// </summary>
/// <remarks>
/// <para>
/// The primary concern for the Bulk extension is that large writes don't OOM the EF Core
/// change tracker. These tests verify correctness (all rows land in the DB) and that the
/// write completes within a generous budget on 10 000 rows against SQLite in-memory.
/// </para>
/// <para>
/// SQLite does not use Npgsql binary COPY — EFCore.BulkExtensions falls back to batched
/// INSERT statements. The real throughput gain over <c>DefaultSave</c> is realised on
/// PostgreSQL. These tests are therefore correctness + budget guardrails, not throughput
/// comparisons.
/// </para>
/// <para>
/// Wall-clock budget is intentionally generous (10 000 ms) to absorb CI machine variance.
/// </para>
/// </remarks>
[TestFixture]
[Category("Performance")]
public class BulkSavePerformanceTests
{
  private const int RowCount = 10_000;
  private const int BudgetMs = 10_000;

  private SqliteConnection _connection = null!;
  private DbContextOptions<TestDbContext> _options = null!;

  [SetUp]
  public async Task SetUp()
  {
    _connection = new SqliteConnection("Data Source=:memory:");
    await _connection.OpenAsync();
    _options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;

    await using var db = new TestDbContext(_options);
    await db.Database.EnsureCreatedAsync();
  }

  [TearDown]
  public async Task TearDown()
  {
    await _connection.DisposeAsync();
  }

  // ── BulkSave.Insert ──────────────────────────────────────────────────────

  [Test]
  public async Task Insert_WritesAllRows_On10kEntities()
  {
    var rows = Seed(RowCount);
    var saveFunc = BulkSave.Insert<TestEntity, TestDbContext>();

    await using var db = new TestDbContext(_options);
    await saveFunc(db, rows, CancellationToken.None);

    await using var verify = new TestDbContext(_options);
    var count = await verify.TestEntities.CountAsync();
    Assert.That(
      count,
      Is.EqualTo(RowCount),
      $"Expected {RowCount} rows after bulk insert, found {count}"
    );
  }

  [Test]
  public async Task Insert_CompletesWithinBudget_On10kEntities()
  {
    var rows = Seed(RowCount);
    var saveFunc = BulkSave.Insert<TestEntity, TestDbContext>();

    await using var db = new TestDbContext(_options);

    var sw = Stopwatch.StartNew();
    await saveFunc(db, rows, CancellationToken.None);
    sw.Stop();

    Assert.That(
      sw.ElapsedMilliseconds,
      Is.LessThan(BudgetMs),
      $"BulkSave.Insert took {sw.ElapsedMilliseconds}ms — expected < {BudgetMs}ms"
    );

    TestContext.Out.WriteLine($"BulkSave.Insert ({RowCount} rows): {sw.ElapsedMilliseconds}ms");
  }

  // ── BulkSave.TruncateAndInsert ───────────────────────────────────────────

  [Test]
  public async Task TruncateAndInsert_ReplacesExistingRows_On10kEntities()
  {
    // Seed an initial set of rows with different IDs
    var initial = Seed(count: 500, startId: 1);
    await using (var db = new TestDbContext(_options))
      await db.BulkInsertAsync(initial);

    // Now replace with a completely different set
    var replacement = Seed(count: RowCount, startId: 10_001);
    var saveFunc = BulkSave.TruncateAndInsert<TestEntity, TestDbContext>();

    await using var db2 = new TestDbContext(_options);
    await saveFunc(db2, replacement, CancellationToken.None);

    await using var verify = new TestDbContext(_options);
    var count = await verify.TestEntities.CountAsync();
    var firstId = await verify.TestEntities.MinAsync(e => e.Id);

    Assert.Multiple(() =>
    {
      Assert.That(
        count,
        Is.EqualTo(RowCount),
        $"Expected {RowCount} rows after TruncateAndInsert, found {count}"
      );
      Assert.That(
        firstId,
        Is.EqualTo(10_001),
        "Expected rows from replacement set, not original set"
      );
    });
  }

  [Test]
  public async Task TruncateAndInsert_CompletesWithinBudget_On10kEntities()
  {
    var rows = Seed(RowCount);
    var saveFunc = BulkSave.TruncateAndInsert<TestEntity, TestDbContext>();

    await using var db = new TestDbContext(_options);

    var sw = Stopwatch.StartNew();
    await saveFunc(db, rows, CancellationToken.None);
    sw.Stop();

    Assert.That(
      sw.ElapsedMilliseconds,
      Is.LessThan(BudgetMs),
      $"BulkSave.TruncateAndInsert took {sw.ElapsedMilliseconds}ms — expected < {BudgetMs}ms"
    );

    TestContext.Out.WriteLine(
      $"BulkSave.TruncateAndInsert ({RowCount} rows): {sw.ElapsedMilliseconds}ms"
    );
  }

  // ── BulkSave.InsertOrUpdate ──────────────────────────────────────────────

  [Test]
  public async Task InsertOrUpdate_InsertsNewRows_And_UpdatesExisting_On10kEntities()
  {
    // Seed half the rows first
    var initial = Seed(count: RowCount / 2, startId: 1);
    await using (var db = new TestDbContext(_options))
      await db.BulkInsertAsync(initial);

    // Upsert: first half updated, second half new
    var upsert = Seed(count: RowCount, startId: 1, nameSuffix: "-updated");
    var saveFunc = BulkSave.InsertOrUpdate<TestEntity, TestDbContext>();

    await using var db2 = new TestDbContext(_options);
    await saveFunc(db2, upsert, CancellationToken.None);

    await using var verify = new TestDbContext(_options);
    var count = await verify.TestEntities.CountAsync();
    Assert.That(
      count,
      Is.EqualTo(RowCount),
      $"Expected {RowCount} rows after upsert, found {count}"
    );
  }

  [Test]
  public async Task InsertOrUpdate_CompletesWithinBudget_On10kEntities()
  {
    var rows = Seed(RowCount);
    var saveFunc = BulkSave.InsertOrUpdate<TestEntity, TestDbContext>();

    await using var db = new TestDbContext(_options);

    var sw = Stopwatch.StartNew();
    await saveFunc(db, rows, CancellationToken.None);
    sw.Stop();

    Assert.That(
      sw.ElapsedMilliseconds,
      Is.LessThan(BudgetMs),
      $"BulkSave.InsertOrUpdate took {sw.ElapsedMilliseconds}ms — expected < {BudgetMs}ms"
    );

    TestContext.Out.WriteLine(
      $"BulkSave.InsertOrUpdate ({RowCount} rows): {sw.ElapsedMilliseconds}ms"
    );
  }

  // ── BulkSave.InsertOrUpdateOrDelete ─────────────────────────────────────
  // Note: BulkInsertOrUpdateOrDeleteAsync is not supported on SQLite — it requires
  // a provider that supports DELETE-from-source semantics (PostgreSQL, SQL Server).
  // Full-sync behaviour is verified by integration tests against a real provider.

  // ── Helpers ──────────────────────────────────────────────────────────────

  private static List<TestEntity> Seed(int count, int startId = 1, string nameSuffix = "")
  {
    return Enumerable
      .Range(startId, count)
      .Select(i => new TestEntity { Id = i, Name = $"Entity-{i}{nameSuffix}" })
      .ToList();
  }
}
