using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.Gql;
using Flowthru.Extensions.GQL.Tests.Fixtures;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

/// <summary>
/// Pins the argument-validation surface on the four <c>GqlExtensions</c>
/// builders (<see cref="GqlSingleBuilder{TResult, T}"/>,
/// <see cref="GqlBuilder{TResult, TRow}"/>,
/// <see cref="GqlPagedRelayBuilder{TResult, TRow}"/>,
/// <see cref="GqlPagedOffsetBuilder{TResult, TRow}"/>) plus the deferred
/// variant. Each builder is constructed via the Catalog Developer surface
/// (<c>Item.Of&lt;T&gt;("…").Gql(…)</c>) and its mutator/Build methods are
/// exercised. Concrete adapter behaviour lives in the Storage tests.
/// </summary>
[TestFixture]
[Category("Gql")]
public class GqlBuilderTests
{
  private static Func<CancellationToken, Task<IOperationResult<TestSingleResult>>> SingleQuery =>
    _ => Task.FromResult<IOperationResult<TestSingleResult>>(
      StubOperationResult<TestSingleResult>.Success(
        new TestSingleResult { User = new TestUser { Id = 1, Name = "alice" } }
      )
    );

  private static Func<CancellationToken, Task<IOperationResult<TestPagedResult>>> ListQuery =>
    _ => Task.FromResult<IOperationResult<TestPagedResult>>(
      StubOperationResult<TestPagedResult>.Success(
        new TestPagedResult { Nodes = new[] { new TestUser { Id = 1, Name = "alice" } } }
      )
    );

  // ── GqlSingleBuilder ────────────────────────────────────────────────

  [Test]
  public void GqlSingle_NullQueryFunc_Throws()
  {
    Assert.That(
      () => Item.Of<TestUser>("u").GqlSingle<TestSingleResult, TestUser>(
        queryFunc: null!,
        selectData: r => r.User!
      ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void GqlSingle_NullSelectData_Throws()
  {
    Assert.That(
      () => Item.Of<TestUser>("u").GqlSingle<TestSingleResult, TestUser>(
        queryFunc: SingleQuery,
        selectData: null!
      ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void GqlSingle_WithMutation_NullThrows()
  {
    var builder = Item.Of<TestUser>("u").GqlSingle(SingleQuery, r => r.User!);
    Assert.That(
      () => builder.WithMutation(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void GqlSingle_WithMutation_ReturnsBuilderForChaining()
  {
    var builder = Item.Of<TestUser>("u").GqlSingle(SingleQuery, r => r.User!);
    Assert.That(
      builder.WithMutation((_, _) => Task.FromResult<IOperationResult>(StubOperationResult<TestSingleResult>.Success(new()))),
      Is.SameAs(builder)
    );
  }

  [Test]
  public void GqlSingle_AllowEmpty_ReturnsBuilderForChaining()
  {
    var builder = Item.Of<TestUser>("u").GqlSingle(SingleQuery, r => r.User!);
    Assert.That(builder.AllowEmpty(), Is.SameAs(builder));
  }

  [Test]
  public void GqlSingle_Build_ReturnsItemWithMatchingLabel()
  {
    var item = Item
      .Of<TestUser>("CurrentUser")
      .GqlSingle(SingleQuery, r => r.User!)
      .Build();
    Assert.That(item.Label, Is.EqualTo("CurrentUser"));
  }

  // ── GqlBuilder (eager collection, non-paged) ────────────────────────

  [Test]
  public void Gql_EagerCollection_NullQueryFunc_Throws()
  {
    Assert.That(
      () => Item.Of<IEnumerable<TestUser>>("u").Gql<TestPagedResult, TestUser>(
        queryFunc: null!,
        selectData: r => r.Nodes
      ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Gql_EagerCollection_NullSelectData_Throws()
  {
    Assert.That(
      () => Item.Of<IEnumerable<TestUser>>("u").Gql<TestPagedResult, TestUser>(
        queryFunc: ListQuery,
        selectData: null!
      ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Gql_EagerCollection_AllowEmpty_ReturnsBuilderForChaining()
  {
    var builder = Item.Of<IEnumerable<TestUser>>("u").Gql(ListQuery, r => r.Nodes);
    Assert.That(builder.AllowEmpty(), Is.SameAs(builder));
  }

  [Test]
  public void Gql_EagerCollection_Build_ReturnsItemWithMatchingLabel()
  {
    var item = Item
      .Of<IEnumerable<TestUser>>("Users")
      .Gql(ListQuery, r => r.Nodes)
      .Build();
    Assert.That(item.Label, Is.EqualTo("Users"));
  }

  // ── GqlPagedRelayBuilder ────────────────────────────────────────────

  [Test]
  public void Gql_RelayPaged_WithPageSize_ReturnsBuilderForChaining()
  {
    var pagination = Pagination.Relay<TestPagedResult, TestUser>(
      getNodes: r => r.Nodes,
      getPageInfo: r => r.PageInfo is null
        ? null
        : new PageInfo(r.PageInfo.HasNextPage, r.PageInfo.EndCursor)
    );
    var builder = Item
      .Of<IEnumerable<TestUser>>("u")
      .Gql<TestPagedResult, TestUser>(
        pagedQueryFunc: (_, _, _) => Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new())),
        pagination: pagination
      );
    Assert.That(builder.WithPageSize(50), Is.SameAs(builder));
    Assert.That(builder.AllowEmpty(), Is.SameAs(builder));
  }

  // ── GqlPagedOffsetBuilder ───────────────────────────────────────────

  [Test]
  public void Gql_OffsetPaged_WithPageSize_ReturnsBuilderForChaining()
  {
    var pagination = Pagination.Offset<TestPagedResult, TestUser>(
      getItems: r => r.Nodes,
      getTotal: r => r.Total
    );
    var builder = Item
      .Of<IEnumerable<TestUser>>("u")
      .Gql<TestPagedResult, TestUser>(
        pagedQueryFunc: (_, _, _) => Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new())),
        pagination: pagination
      );
    Assert.That(builder.WithPageSize(25), Is.SameAs(builder));
    Assert.That(builder.AllowEmpty(), Is.SameAs(builder));
  }

  // ── GqlDeferredBuilder ──────────────────────────────────────────────

  [Test]
  public void GqlDeferred_NullQueryFunc_Throws()
  {
    Assert.That(
      () => Item.Of<GqlQuery<TestPagedResult, TestUser>>("u").GqlDeferred<TestPagedResult, TestUser>(
        queryFunc: null!,
        selectData: r => r.Nodes
      ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void GqlDeferred_NullSelectData_Throws()
  {
    Assert.That(
      () => Item.Of<GqlQuery<TestPagedResult, TestUser>>("u").GqlDeferred<TestPagedResult, TestUser>(
        queryFunc: ListQuery,
        selectData: null!
      ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void GqlDeferred_AllowEmpty_ReturnsBuilderForChaining()
  {
    var builder = Item.Of<GqlQuery<TestPagedResult, TestUser>>("u")
      .GqlDeferred(ListQuery, r => r.Nodes);
    Assert.That(builder.AllowEmpty(), Is.SameAs(builder));
  }

  [Test]
  public void GqlDeferred_Build_ReturnsItemWithMatchingLabel()
  {
    var item = Item
      .Of<GqlQuery<TestPagedResult, TestUser>>("DeferredUsers")
      .GqlDeferred(ListQuery, r => r.Nodes)
      .Build();
    Assert.That(item.Label, Is.EqualTo("DeferredUsers"));
  }
}
