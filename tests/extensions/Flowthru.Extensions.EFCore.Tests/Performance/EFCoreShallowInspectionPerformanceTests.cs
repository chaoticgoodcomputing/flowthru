using System.Diagnostics;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Performance;

/// <summary>
/// Guardrail tests asserting that EFCore shallow inspection does NOT load the full table.
/// </summary>
/// <remarks>
/// Both <see cref="Flowthru.Core.Data.Storage.EFCoreStorageAdapter{T}"/> variants are
/// exercised against a 50 000-row SQLite table.  A correct shallow read issues a
/// <c>SELECT TOP N</c> / <c>LIMIT N</c> query and returns in well under a second;
/// a full table scan would take many seconds.
///
/// Wall-clock budget is intentionally generous (5 000 ms) to absorb CI machine variance.
/// </remarks>
[TestFixture]
[Category("Performance")]
public class EFCoreShallowInspectionPerformanceTests
{
    private const int RowCount = 50_000;
    private const int SampleSize = 100;
    private const int BudgetMs = 5_000;

    private SqliteConnection _connection = null!;
    private DbContextOptions<TestDbContext> _options = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var context = new TestDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        // Seed 50 000 rows in batches to keep setup time reasonable
        const int batchSize = 1_000;
        for (int batch = 0; batch < RowCount / batchSize; batch++)
        {
            var entities = Enumerable.Range(batch * batchSize + 1, batchSize)
                .Select(i => new TestEntity { Id = i, Name = $"Entity-{i}" });
            await context.TestEntities.AddRangeAsync(entities);
        }
        await context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _connection.DisposeAsync();
    }

    // ── EFCoreStorageAdapter ─────────────────────────────────────────────────

    [Test]
    public async Task EFCoreStorageAdapter_ShallowInspect_CompletesWithinBudget_On50kRows()
    {
        var entry = EFCoreItemFactory.Enumerable.EFCore<TestEntity>(
            "test-entities",
            () => new TestDbContext(_options)
        );

        var sw = Stopwatch.StartNew();
        var result = await entry.InspectShallow(SampleSize).Run(CancellationToken.None);
        sw.Stop();

        Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.That(
            sw.ElapsedMilliseconds,
            Is.LessThan(BudgetMs),
            $"EFCoreStorageAdapter shallow inspection took {sw.ElapsedMilliseconds}ms — expected < {BudgetMs}ms"
        );

        TestContext.Out.WriteLine($"EFCoreStorageAdapter shallow inspection (50k rows, sample={SampleSize}): {sw.ElapsedMilliseconds}ms");
    }

    // ── DbQueryStorageAdapter ────────────────────────────────────────────────

    [Test]
    public async Task DbQueryStorageAdapter_ShallowInspect_CompletesWithinBudget_On50kRows()
    {
        var entry = EFCoreItemFactory.Query.EFCore<TestEntity>(
            "test-entities-query",
            contextFactory: () => new TestDbContext(_options)
        );

        var sw = Stopwatch.StartNew();
        var result = await entry.InspectShallow(SampleSize).Run(CancellationToken.None);
        sw.Stop();

        Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Select(e => e.Message)));
        Assert.That(
            sw.ElapsedMilliseconds,
            Is.LessThan(BudgetMs),
            $"DbQueryStorageAdapter shallow inspection took {sw.ElapsedMilliseconds}ms — expected < {BudgetMs}ms"
        );

        TestContext.Out.WriteLine($"DbQueryStorageAdapter shallow inspection (50k rows, sample={SampleSize}): {sw.ElapsedMilliseconds}ms");
    }
}
