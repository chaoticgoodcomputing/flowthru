using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Gql;
using Flowthru.Extensions.GQL.Tests.Fixtures;
using Flowthru.Prelude;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

/// <summary>
/// Unit tests for <see cref="GqlQueryStorageAdapter{TResult,T}"/> and
/// the filtered <see cref="GqlQueryStorageAdapter{TFilter,TResult,T}"/>
/// — the deferred query handle adapters.
/// </summary>
[TestFixture]
public class GqlQueryStorageAdapterTests
{
  private static readonly TestUser[] Users =
  {
    new() { Id = 1, Name = "Alice" },
    new() { Id = 2, Name = "Bob" },
  };

  // ── Load returns the deferred handle without I/O ─────────────────────

  [Test]
  public async Task Load_ReturnsHandleWithoutInvokingQueryFunc()
  {
    var queryCalls = 0;
    var query = new GqlQuery<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ =>
      {
        queryCalls++;
        return Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
        );
      },
      selectData: r => r.Nodes,
      allowEmptyData: false
    );

    var adapter = new GqlQueryStorageAdapter<TestPagedResult, TestUser>(query);
    var result = await adapter.Load().Run();

    Assert.That(result, Is.InstanceOf<EffResult<GqlQuery<TestPagedResult, TestUser>>.Success>());
    Assert.That(queryCalls, Is.Zero,
      "Load() must not trigger the underlying query — the handle is deferred.");
  }

  [Test]
  public async Task HandleMaterialization_TriggersTheQuery()
  {
    var queryCalls = 0;
    var query = new GqlQuery<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ =>
      {
        queryCalls++;
        return Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
        );
      },
      selectData: r => r.Nodes,
      allowEmptyData: false
    );

    var adapter = new GqlQueryStorageAdapter<TestPagedResult, TestUser>(query);
    var loadResult = await adapter.Load().Run();
    var handle = ((EffResult<GqlQuery<TestPagedResult, TestUser>>.Success)loadResult).Value;

    var data = await handle.ToListAsync();
    Assert.That(data, Has.Count.EqualTo(2));
    Assert.That(queryCalls, Is.EqualTo(1));
  }

  // ── Save / Exists ────────────────────────────────────────────────────

  [Test]
  public async Task Save_AlwaysFails()
  {
    var query = new GqlQuery<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
      ),
      selectData: r => r.Nodes,
      allowEmptyData: false
    );
    var adapter = new GqlQueryStorageAdapter<TestPagedResult, TestUser>(query);

    var result = await adapter.Save(query).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
  }

  [Test]
  public async Task Exists_AlwaysTrue()
  {
    var query = new GqlQuery<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
      ),
      selectData: r => r.Nodes,
      allowEmptyData: false
    );
    var adapter = new GqlQueryStorageAdapter<TestPagedResult, TestUser>(query);

    var result = await adapter.Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.True);
  }

  // ── InspectShallow ────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_QueryReachable_Succeeds()
  {
    var query = new GqlQuery<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
      ),
      selectData: r => r.Nodes,
      allowEmptyData: false
    );
    var adapter = new GqlQueryStorageAdapter<TestPagedResult, TestUser>(query);

    var result = await adapter.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_QueryThrows_ReportsNotFound()
  {
    var query = new GqlQuery<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => throw new HttpRequestException("unreachable"),
      selectData: r => r.Nodes,
      allowEmptyData: false
    );
    var adapter = new GqlQueryStorageAdapter<TestPagedResult, TestUser>(query);

    var result = await adapter.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.HasErrors, Is.True);
    Assert.That(validation.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.NotFound));
  }

  // ── Filtered variant ──────────────────────────────────────────────────

  [Test]
  public async Task FilteredQuery_WithFilter_FlowsThroughToTheDelegate()
  {
    TestFilter? observed = null;
    var query = new GqlQuery<TestFilter, TestPagedResult, TestUser>(
      label: "users",
      queryFunc: (filter, _) =>
      {
        observed = filter;
        return Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
        );
      },
      selectData: r => r.Nodes,
      allowEmptyData: false
    );

    var adapter = new GqlQueryStorageAdapter<TestFilter, TestPagedResult, TestUser>(query);
    var loadResult = await adapter.Load().Run();
    var handle = ((EffResult<GqlQuery<TestFilter, TestPagedResult, TestUser>>.Success)loadResult).Value;

    var filter = new TestFilter { NameContains = "Al" };
    await handle.WithFilter(filter).ToListAsync();

    Assert.That(observed, Is.Not.Null);
    Assert.That(observed!.NameContains, Is.EqualTo("Al"));
  }

  [Test]
  public async Task FilteredQuery_HandleIsImmutable_OriginalLacksFilter()
  {
    var query = new GqlQuery<TestFilter, TestPagedResult, TestUser>(
      label: "users",
      queryFunc: (_, _) => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
      ),
      selectData: r => r.Nodes,
      allowEmptyData: false
    );

    var withFilter = query.WithFilter(new TestFilter { NameContains = "x" });
    Assert.That(query.Filter, Is.Null,
      "Original handle should be unchanged — WithFilter returns a new handle.");
    Assert.That(withFilter.Filter, Is.Not.Null);
    await Task.CompletedTask;
  }
}
