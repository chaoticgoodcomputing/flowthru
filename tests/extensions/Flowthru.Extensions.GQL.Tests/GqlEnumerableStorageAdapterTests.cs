using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Gql;
using Flowthru.Extensions.GQL.Tests.Fixtures;
using Flowthru.Prelude;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

/// <summary>
/// Unit tests for <see cref="GqlEnumerableStorageAdapter{TResult,T}"/> —
/// non-paginated, Relay cursor-paginated, and offset-paginated
/// collection adapters.
/// </summary>
[TestFixture]
public class GqlEnumerableStorageAdapterTests
{
  private static readonly TestUser[] Users =
  {
    new() { Id = 1, Name = "Alice" },
    new() { Id = 2, Name = "Bob" },
    new() { Id = 3, Name = "Carol" },
  };

  // ── Traits ────────────────────────────────────────────────────────────

  [Test]
  public void Traits_AreReadOnly()
  {
    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
      ),
      selectData: r => r.Nodes
    );

    Assert.That(adapter.Traits.CanWrite, Is.False);
    Assert.That(adapter.Traits.IsPersistent, Is.False);
  }

  // ── Save — always fails ───────────────────────────────────────────────

  [Test]
  public async Task Save_AlwaysFails()
  {
    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
      ),
      selectData: r => r.Nodes
    );

    var result = await adapter.Save(Users).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
  }

  // ── Non-paginated ─────────────────────────────────────────────────────

  [Test]
  public async Task Load_NonPaginated_ReturnsAllItems()
  {
    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
      ),
      selectData: r => r.Nodes
    );

    var result = await adapter.Load().Run();
    var data = ((EffResult<IEnumerable<TestUser>>.Success)result).Value.ToList();
    Assert.That(data, Has.Count.EqualTo(3));
    Assert.That(data.Select(u => u.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
  }

  [Test]
  public async Task Load_NonPaginated_EmptyDisallowed_FailsWithExternalError()
  {
    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Array.Empty<TestUser>() })
      ),
      selectData: r => r.Nodes,
      allowEmptyData: false
    );

    var result = await adapter.Load().Run();
    Assert.That(result, Is.InstanceOf<EffResult<IEnumerable<TestUser>>.Failure>());
  }

  [Test]
  public async Task Load_NonPaginated_EmptyAllowed_ReturnsEmpty()
  {
    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Array.Empty<TestUser>() })
      ),
      selectData: r => r.Nodes,
      allowEmptyData: true
    );

    var result = await adapter.Load().Run();
    var data = ((EffResult<IEnumerable<TestUser>>.Success)result).Value.ToList();
    Assert.That(data, Is.Empty);
  }

  // ── Relay paginated ───────────────────────────────────────────────────

  [Test]
  public async Task Load_RelayPaginated_IteratesAcrossPages()
  {
    // Three pages: [Alice, Bob], [Carol], terminate
    var pages = new (TestUser[], string?, bool)[]
    {
      (new[] { Users[0], Users[1] }, "cursor-1", true),
      (new[] { Users[2] }, "cursor-2", false),
    };

    var pageIndex = 0;
    Task<IOperationResult<TestPagedResult>> Query(string? cursor, int pageSize, CancellationToken ct)
    {
      var (nodes, end, hasNext) = pages[pageIndex++];
      return Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult
        {
          Nodes = nodes,
          PageInfo = new StubPageInfo { HasNextPage = hasNext, EndCursor = end },
        })
      );
    }

    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      pagedQueryFunc: Query,
      pagination: Pagination.Relay<TestPagedResult, TestUser>(
        getNodes: r => r.Nodes,
        getPageInfo: r => r.PageInfo is { } pi ? new PageInfo(pi.HasNextPage, pi.EndCursor) : null
      ),
      pageSize: 2
    );

    var result = await adapter.Load().Run();
    var data = ((EffResult<IEnumerable<TestUser>>.Success)result).Value.ToList();
    Assert.That(data, Has.Count.EqualTo(3));
    Assert.That(pageIndex, Is.EqualTo(2), "Adapter should iterate exactly two pages.");
  }

  [Test]
  public async Task Load_RelayPaginated_ErrorMidStream_FailsWithExternalError()
  {
    var pageIndex = 0;
    Task<IOperationResult<TestPagedResult>> Query(string? cursor, int pageSize, CancellationToken ct)
    {
      pageIndex++;
      if (pageIndex == 2)
      {
        return Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.WithErrors("boom on page 2")
        );
      }
      return Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult
        {
          Nodes = new[] { Users[0] },
          PageInfo = new StubPageInfo { HasNextPage = true, EndCursor = "c" },
        })
      );
    }

    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      pagedQueryFunc: Query,
      pagination: Pagination.Relay<TestPagedResult, TestUser>(
        getNodes: r => r.Nodes,
        getPageInfo: r => r.PageInfo is { } pi ? new PageInfo(pi.HasNextPage, pi.EndCursor) : null
      ),
      pageSize: 1
    );

    var result = await adapter.Load().Run();
    Assert.That(result, Is.InstanceOf<EffResult<IEnumerable<TestUser>>.Failure>());
  }

  // ── Offset paginated ──────────────────────────────────────────────────

  [Test]
  public async Task Load_OffsetPaginated_IteratesUntilTotalReached()
  {
    var pageIndex = 0;
    Task<IOperationResult<TestPagedResult>> Query(int offset, int limit, CancellationToken ct)
    {
      var page = pageIndex switch
      {
        0 => new TestPagedResult { Nodes = new[] { Users[0], Users[1] }, Total = 3 },
        _ => new TestPagedResult { Nodes = new[] { Users[2] }, Total = 3 },
      };
      pageIndex++;
      return Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(page)
      );
    }

    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      pagedQueryFunc: Query,
      pagination: Pagination.Offset<TestPagedResult, TestUser>(
        getItems: r => r.Nodes,
        getTotal: r => r.Total
      ),
      pageSize: 2
    );

    var result = await adapter.Load().Run();
    var data = ((EffResult<IEnumerable<TestUser>>.Success)result).Value.ToList();
    Assert.That(data, Has.Count.EqualTo(3));
    Assert.That(pageIndex, Is.EqualTo(2));
  }

  // ── Exists ────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_NetworkFailure_ReturnsFalse()
  {
    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => throw new HttpRequestException("Endpoint unreachable"),
      selectData: r => r.Nodes
    );

    var result = await adapter.Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }

  // ── InspectShallow ────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_NetworkFailure_ReportsNotFound()
  {
    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => throw new HttpRequestException("Endpoint unreachable"),
      selectData: r => r.Nodes
    );

    var result = await adapter.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.HasErrors, Is.True);
    Assert.That(validation.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.NotFound));
  }

  [Test]
  public async Task InspectShallow_RelayPaginated_UsesProbeSizeOne()
  {
    int? observedPageSize = null;
    Task<IOperationResult<TestPagedResult>> Query(string? cursor, int pageSize, CancellationToken ct)
    {
      observedPageSize ??= pageSize;
      return Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult
        {
          Nodes = new[] { Users[0] },
          PageInfo = new StubPageInfo { HasNextPage = false, EndCursor = null },
        })
      );
    }

    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      pagedQueryFunc: Query,
      pagination: Pagination.Relay<TestPagedResult, TestUser>(
        getNodes: r => r.Nodes,
        getPageInfo: r => r.PageInfo is { } pi ? new PageInfo(pi.HasNextPage, pi.EndCursor) : null
      ),
      pageSize: 100
    );

    await adapter.InspectShallow(sampleSize: 0).Run();
    Assert.That(observedPageSize, Is.EqualTo(1),
      "InspectShallow should issue a probe with pageSize=1, ignoring the configured pageSize.");
  }

  // ── InspectDeep ───────────────────────────────────────────────────────

  [Test]
  public async Task InspectDeep_HappyPath_Succeeds()
  {
    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
      ),
      selectData: r => r.Nodes
    );

    var result = await adapter.InspectDeep().Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True);
  }

  [Test]
  public async Task InspectDeep_LoadFails_ReportsInspectionFailure()
  {
    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.WithErrors("server error")
      ),
      selectData: r => r.Nodes
    );

    var result = await adapter.InspectDeep().Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.HasErrors, Is.True);
    Assert.That(validation.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.InspectionFailure));
  }
}
