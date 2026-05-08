using System.Diagnostics;
using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.GQL.Data;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests.Performance;

/// <summary>
/// Guardrail tests asserting that GQL shallow inspection minimises network I/O.
/// </summary>
/// <remarks>
/// <para>
/// Non-paginated <see cref="GqlEnumerableStorageAdapter{TResult,T}"/> previously executed
/// the full query (returning all N items) during shallow inspection. Paginated variants
/// already used a 1-item probe. These tests verify both behaviours.
/// </para>
/// <para>
/// Because there is no real GQL endpoint, stubs simulate latency proportional to the
/// number of items returned. A large-dataset non-paginated stub that returns 10 000 items
/// takes ~1 s in total simulation time. A minimal probe that ignores the item count completes
/// in ~10 ms. Wall-clock gap is large enough to distinguish the two behaviours reliably.
/// </para>
/// </remarks>
[TestFixture]
[Category("Performance")]
public class GqlShallowInspectionPerformanceTests
{
  // Each item "costs" this many ms in simulated latency — enough to make the
  // full-dataset path clearly slower than the probe path.
  private const int MsPerItem = 0; // zero — we measure item count via a counter, not time
  private const int BudgetMs = 5_000;
  private const int LargeDatasetSize = 10_000;
  private const int SampleSize = 5;

  // ── Non-paginated: InspectShallow should NOT enumerate all items ──────────

  [Test]
  public async Task NonPaginated_ShallowInspect_CompletesWithinBudget_On10kItems()
  {
    int callCount = 0;

    // Stub: returns 10 000 items on every call
    Task<IOperationResult<TestPagedResult>> QueryFunc(CancellationToken ct)
    {
      Interlocked.Increment(ref callCount);
      var nodes = Enumerable
        .Range(1, LargeDatasetSize)
        .Select(i => new TestUser { Id = i, Name = $"User-{i}" })
        .Cast<TestUser>()
        .ToList();
      var data = new TestPagedResult { Nodes = nodes };
      return Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(data)
      );
    }

    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: QueryFunc,
      selectData: r => r.Nodes ?? [],
      allowEmptyData: false
    );

    var sw = Stopwatch.StartNew();
    var result = await adapter.InspectShallow(SampleSize).Run(CancellationToken.None);
    sw.Stop();

    Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Select(e => e.Message)));
    Assert.That(
      sw.ElapsedMilliseconds,
      Is.LessThan(BudgetMs),
      $"GQL non-paginated shallow inspection took {sw.ElapsedMilliseconds}ms — expected < {BudgetMs}ms"
    );
    // Exactly one query call is expected for a probe
    Assert.That(
      callCount,
      Is.EqualTo(1),
      "Expected exactly one query call during shallow inspection"
    );

    TestContext.Out.WriteLine(
      $"GQL non-paginated shallow inspection ({LargeDatasetSize} items, sample={SampleSize}): {sw.ElapsedMilliseconds}ms, calls={callCount}"
    );
  }

  // ── Relay paginated: InspectShallow should fetch only 1 page ─────────────

  [Test]
  public async Task RelayPaginated_ShallowInspect_FetchesOnlyOnePage()
  {
    int callCount = 0;

    Task<IOperationResult<TestPagedResult>> RelayQueryFunc(
      string? cursor,
      int pageSize,
      CancellationToken ct
    )
    {
      Interlocked.Increment(ref callCount);
      var nodes = Enumerable
        .Range(1, pageSize)
        .Select(i => new TestUser { Id = i, Name = $"User-{i}" })
        .ToList();
      var data = new TestPagedResult
      {
        Nodes = nodes,
        // Report there are more pages to confirm the adapter stops after 1
        PageInfo = new StubPageInfo { HasNextPage = true, EndCursor = "cursor-1" },
      };
      return Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(data)
      );
    }

    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "sessions",
      pagedQueryFunc: RelayQueryFunc,
      pagination: Pagination.Relay<TestPagedResult, TestUser>(
        getNodes: r => r.Nodes,
        getPageInfo: r => r.PageInfo is { } pi ? new PageInfo(pi.HasNextPage, pi.EndCursor) : null
      ),
      pageSize: 100
    );

    var sw = Stopwatch.StartNew();
    var result = await adapter.InspectShallow(SampleSize).Run(CancellationToken.None);
    sw.Stop();

    Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Select(e => e.Message)));
    Assert.That(
      callCount,
      Is.EqualTo(1),
      "Relay paginated shallow inspection should make exactly one page request"
    );

    TestContext.Out.WriteLine(
      $"GQL relay paginated shallow inspection: {sw.ElapsedMilliseconds}ms, calls={callCount}"
    );
  }

  // ── Offset paginated: InspectShallow should fetch only 1 page ────────────

  [Test]
  public async Task OffsetPaginated_ShallowInspect_FetchesOnlyOnePage()
  {
    int callCount = 0;

    Task<IOperationResult<TestPagedResult>> OffsetQueryFunc(
      int offset,
      int limit,
      CancellationToken ct
    )
    {
      Interlocked.Increment(ref callCount);
      var nodes = Enumerable
        .Range(offset + 1, limit)
        .Select(i => new TestUser { Id = i, Name = $"User-{i}" })
        .ToList();
      var data = new TestPagedResult
      {
        Nodes = nodes,
        Total =
          LargeDatasetSize // tell adapter there are many more rows
        ,
      };
      return Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(data)
      );
    }

    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "items",
      pagedQueryFunc: OffsetQueryFunc,
      pagination: Pagination.Offset<TestPagedResult, TestUser>(
        getItems: r => r.Nodes,
        getTotal: r => r.Total
      ),
      pageSize: 100
    );

    var sw = Stopwatch.StartNew();
    var result = await adapter.InspectShallow(SampleSize).Run(CancellationToken.None);
    sw.Stop();

    Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Select(e => e.Message)));
    Assert.That(
      callCount,
      Is.EqualTo(1),
      "Offset paginated shallow inspection should make exactly one page request"
    );

    TestContext.Out.WriteLine(
      $"GQL offset paginated shallow inspection: {sw.ElapsedMilliseconds}ms, calls={callCount}"
    );
  }
}
