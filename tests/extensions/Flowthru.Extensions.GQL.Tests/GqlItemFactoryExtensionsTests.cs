using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.Gql;
using Flowthru.Extensions.GQL.Tests.Fixtures;
using Flowthru.Prelude;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

/// <summary>
/// Smoke tests for the smart-constructor extension methods on
/// <see cref="ItemFactory.Singleton"/> and
/// <see cref="ItemFactory.Enumerable"/>. The adapter shapes are
/// exercised by their dedicated test classes; here we only verify the
/// factory wiring lands the right adapter type with the right traits.
/// </summary>
[TestFixture]
public class GqlItemFactoryExtensionsTests
{
  // ── Singleton.GqlQuery — read-only ────────────────────────────────────

  [Test]
  public void Singleton_GqlQuery_ReadOnly_ProducesItemWithCanWriteFalse()
  {
    var item = ItemFactory.Singleton.GqlQuery<TestSingleResult, TestUser>(
      label: "user",
      queryFunc: _ => Task.FromResult<IOperationResult<TestSingleResult>>(
        StubOperationResult<TestSingleResult>.Success(new TestSingleResult { User = new TestUser { Id = 1, Name = "x" } })
      ),
      selectData: r => r.User!
    );

    Assert.That(item.Label, Is.EqualTo("user"));
    var item_ = (Item<TestUser>)item;
    Assert.That(item_.Storage.Traits.CanWrite, Is.False);
  }

  // ── Singleton.GqlQuery — read/write ───────────────────────────────────

  [Test]
  public void Singleton_GqlQuery_WithMutation_ProducesItemWithCanWriteTrue()
  {
    var item = ItemFactory.Singleton.GqlQuery<TestSingleResult, TestUser>(
      label: "user",
      queryFunc: _ => Task.FromResult<IOperationResult<TestSingleResult>>(
        StubOperationResult<TestSingleResult>.Success(new TestSingleResult { User = new TestUser { Id = 1, Name = "x" } })
      ),
      selectData: r => r.User!,
      mutationFunc: (_, _) =>
        Task.FromResult<IOperationResult>(StubOperationResult<TestSingleResult>.Success(new()))
    );

    var item_ = (Item<TestUser>)item;
    Assert.That(item_.Storage.Traits.CanWrite, Is.True);
  }

  // ── Enumerable.GqlQuery (non-paginated) ──────────────────────────────

  [Test]
  public async Task Enumerable_GqlQuery_LoadReturnsCollection()
  {
    var users = new[] { new TestUser { Id = 1, Name = "a" }, new TestUser { Id = 2, Name = "b" } };
    var item = ItemFactory.Enumerable.GqlQuery<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = users })
      ),
      selectData: r => r.Nodes
    );

    var result = await item.Load().Run();
    var loaded = ((EffResult<IEnumerable<TestUser>>.Success)result).Value.ToList();
    Assert.That(loaded, Has.Count.EqualTo(2));
  }

  // ── Enumerable.GqlPagedQuery (Relay) ──────────────────────────────────

  [Test]
  public async Task Enumerable_GqlPagedQuery_Relay_AcceptsPaginationStrategy()
  {
    var item = ItemFactory.Enumerable.GqlPagedQuery<TestPagedResult, TestUser>(
      label: "users",
      pagedQueryFunc: (_, _, _) => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult
        {
          Nodes = new[] { new TestUser { Id = 1, Name = "x" } },
          PageInfo = new StubPageInfo { HasNextPage = false, EndCursor = null },
        })
      ),
      pagination: Pagination.Relay<TestPagedResult, TestUser>(
        getNodes: r => r.Nodes,
        getPageInfo: r => r.PageInfo is { } pi ? new PageInfo(pi.HasNextPage, pi.EndCursor) : null
      )
    );

    var result = await item.Load().Run();
    var loaded = ((EffResult<IEnumerable<TestUser>>.Success)result).Value.ToList();
    Assert.That(loaded, Has.Count.EqualTo(1));
  }

  // ── Enumerable.GqlDeferredQuery — deferred handle, unfiltered ────────

  [Test]
  public async Task Enumerable_GqlDeferredQuery_LoadReturnsHandle_NoEarlyExecution()
  {
    var queryCalls = 0;
    var item = ItemFactory.Enumerable.GqlDeferredQuery<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ =>
      {
        queryCalls++;
        return Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult
          {
            Nodes = new[] { new TestUser { Id = 1, Name = "x" } },
          })
        );
      },
      selectData: r => r.Nodes
    );

    var result = await item.Load().Run();
    Assert.That(result, Is.InstanceOf<EffResult<GqlQuery<TestPagedResult, TestUser>>.Success>());
    Assert.That(queryCalls, Is.Zero);
  }

  // ── Enumerable.GqlDeferredQuery — filtered ────────────────────────────

  [Test]
  public async Task Enumerable_GqlDeferredQuery_Filtered_StepCanApplyFilter()
  {
    TestFilter? observed = null;
    var item = ItemFactory.Enumerable.GqlDeferredQuery<TestFilter, TestPagedResult, TestUser>(
      label: "users",
      queryFunc: (filter, _) =>
      {
        observed = filter;
        return Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult
          {
            Nodes = new[] { new TestUser { Id = 1, Name = "x" } },
          })
        );
      },
      selectData: r => r.Nodes
    );

    var loadResult = await item.Load().Run();
    var handle = ((EffResult<GqlQuery<TestFilter, TestPagedResult, TestUser>>.Success)loadResult).Value;
    await handle.WithFilter(new TestFilter { NameContains = "z" }).ToListAsync();

    Assert.That(observed, Is.Not.Null);
    Assert.That(observed!.NameContains, Is.EqualTo("z"));
  }
}
