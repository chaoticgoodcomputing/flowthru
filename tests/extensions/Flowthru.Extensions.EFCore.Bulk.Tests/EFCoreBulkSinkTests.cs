using System.Runtime.CompilerServices;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Bulk.Tests;

/// <summary>
/// Tests for <see cref="EFCoreBulkSink{T, TContext}"/> / <see cref="BulkSink"/> —
/// the streaming per-batch, single-transaction bulk-insert sink. Exercises the
/// end-to-end <c>FlowSource → Into(sink)</c> path (issue #122): all rows on
/// success, incremental per-batch writes (O(batch)), and a full rollback when
/// the stream fails mid-way.
/// </summary>
/// <remarks>
/// Uses a shared, open SQLite in-memory connection so that the sink's context
/// (which opens a transaction) and a separate verification context observe the
/// same database — a fresh <c>DataSource=:memory:</c> per connection would give
/// each context its own empty database.
/// </remarks>
[TestFixture]
public class EFCoreBulkSinkTests
{
  private SqliteConnection _connection = null!;
  private TestDbContextFactory _factory = null!;

  [SetUp]
  public void SetUp()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;

    _factory = new TestDbContextFactory(options);

    using var db = _factory.CreateDbContext();
    db.Database.EnsureCreated();
  }

  [TearDown]
  public void TearDown()
  {
    _connection.Dispose();
  }

  [Test]
  public void Insert_Returns_Sink()
  {
    var sink = BulkSink.Insert<TestEntity, TestDbContext>(_factory);
    Assert.That(sink, Is.Not.Null);
    Assert.That(sink, Is.InstanceOf<IFlowSink<TestEntity>>());
  }

  [Test]
  public void BatchSize_Comes_From_Options()
  {
    var sink = BulkSink.Insert<TestEntity, TestDbContext>(
      _factory,
      new BulkSaveOptions { BatchSize = 500 }
    );
    Assert.That(sink.BatchSize, Is.EqualTo(500));
  }

  [Test]
  public void BatchSize_Defaults_To_2000()
  {
    var sink = BulkSink.Insert<TestEntity, TestDbContext>(_factory);
    Assert.That(sink.BatchSize, Is.EqualTo(2000));
  }

  [Test]
  public async Task Into_WritesAllRows_OnSuccess()
  {
    var entities = MakeEntities(1, 5).ToArray();
    var sink = BulkSink.Insert<TestEntity, TestDbContext>(
      _factory,
      new BulkSaveOptions { BatchSize = 2 }
    );

    var result = await FlowSource.FromEnumerable(entities).Compile().Into(sink).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var rows = await ReadAllAsync();
    Assert.That(rows, Has.Count.EqualTo(5));
    Assert.That(rows.Select(e => e.Id), Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
  }

  [Test]
  public async Task Into_WritesIncrementally_PerBatch()
  {
    // 5 rows at BatchSize 2 → three write calls: [2, 2, 1]. Proves the write is
    // O(batch) per-batch, not a single materialised bulk insert.
    var entities = MakeEntities(1, 5).ToArray();
    var sink = (EFCoreBulkSink<TestEntity, TestDbContext>)
      BulkSink.Insert<TestEntity, TestDbContext>(_factory, new BulkSaveOptions { BatchSize = 2 });

    var result = await FlowSource.FromEnumerable(entities).Compile().Into(sink).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    Assert.That(sink.BatchesWritten, Is.EqualTo(3));
  }

  [Test]
  public async Task Into_EmptyStream_CommitsNothing()
  {
    var sink = BulkSink.Insert<TestEntity, TestDbContext>(_factory);

    var result = await FlowSource.FromEnumerable(Array.Empty<TestEntity>()).Compile().Into(sink).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    Assert.That(await CountAsync(), Is.EqualTo(0));
  }

  [Test]
  public async Task Into_MidStreamFailure_RollsBackEntireWrite()
  {
    // Emit two batches' worth then throw: at least one batch is bulk-inserted
    // into the open transaction before the failure. A truthful IsTransactional
    // means the table is empty afterwards — no corrupt-but-present rows.
    var sink = (EFCoreBulkSink<TestEntity, TestDbContext>)
      BulkSink.Insert<TestEntity, TestDbContext>(_factory, new BulkSaveOptions { BatchSize = 2 });

    var result = await FlowSource.Lift<TestEntity>(ct => FailAfter(3, ct)).Compile().Into(sink).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    Assert.That(
      ((EffResult<FlowUnit>.Failure)result).Error,
      Is.InstanceOf<RuntimeError.External>()
    );

    // A batch was actually bulk-inserted into the transaction before the throw,
    // so the empty table below proves a genuine rollback (not a no-op).
    Assert.That(sink.BatchesWritten, Is.EqualTo(1));
    Assert.That(await CountAsync(), Is.EqualTo(0), "Partial-stream failure must roll the whole write back.");
  }

  [Test]
  public async Task Into_MidStreamFailure_AfterPriorSuccessfulRun_LeavesOnlyCommittedRows()
  {
    // A committed run followed by a rolled-back run must leave exactly the
    // committed rows — the rollback touches only its own transaction.
    var committed = MakeEntities(1, 3).ToArray();
    var okSink = BulkSink.Insert<TestEntity, TestDbContext>(_factory, new BulkSaveOptions { BatchSize = 2 });
    await FlowSource.FromEnumerable(committed).Compile().Into(okSink).Run();

    var failSink = BulkSink.Insert<TestEntity, TestDbContext>(_factory, new BulkSaveOptions { BatchSize = 2 });
    var result = await FlowSource.Lift<TestEntity>(ct => FailAfter(4, ct, startId: 100)).Compile().Into(failSink).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());

    var rows = await ReadAllAsync();
    Assert.That(rows.Select(e => e.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
  }

  // ── helpers ──────────────────────────────────────────────────────────────

  private static IEnumerable<TestEntity> MakeEntities(int startId, int count) =>
    Enumerable.Range(startId, count).Select(i => new TestEntity { Id = i, Name = $"row-{i}" });

  private static async IAsyncEnumerable<TestEntity> FailAfter(
    int emit,
    [EnumeratorCancellation] CancellationToken ct,
    int startId = 1
  )
  {
    await Task.CompletedTask.ConfigureAwait(false);
    for (var i = 0; i < emit; i++)
    {
      ct.ThrowIfCancellationRequested();
      yield return new TestEntity { Id = startId + i, Name = $"row-{startId + i}" };
    }

    throw new InvalidOperationException("boom");
  }

  private async Task<int> CountAsync()
  {
    await using var db = _factory.CreateDbContext();
    return await db.TestEntities.CountAsync();
  }

  private async Task<List<TestEntity>> ReadAllAsync()
  {
    await using var db = _factory.CreateDbContext();
    return await db.TestEntities.AsNoTracking().ToListAsync();
  }
}
