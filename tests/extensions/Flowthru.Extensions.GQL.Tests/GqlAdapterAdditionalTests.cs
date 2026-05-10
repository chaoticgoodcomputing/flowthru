using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Gql;
using Flowthru.Extensions.GQL.Tests.Fixtures;
using Flowthru.Prelude;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

/// <summary>
/// Coverage-fill for the GQL adapter / query surfaces not exercised by
/// the existing fixtures: the deferred-query <c>InspectShallow /
/// InspectDeep / InspectTarget</c> branches, <see cref="GqlQuery{TResult, T}.ToList"/>
/// (sync materialisation), and <see cref="GqlQuery{TResult, T}.GetEnumerator"/>
/// (foreach-style consumption). The filtered three-arg query overload
/// also gets <see cref="GqlQuery{TFilter, TResult, T}.WithFilter"/>
/// coverage.
/// </summary>
[TestFixture]
[Category("Gql")]
public class GqlAdapterAdditionalTests
{
  private static readonly TestUser[] Users =
  {
    new() { Id = 1, Name = "Alice" },
    new() { Id = 2, Name = "Bob" },
  };

  private static GqlQuery<TestPagedResult, TestUser> NonPagedQuery() => new(
    label: "users",
    queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
      StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
    ),
    selectData: r => r.Nodes,
    allowEmptyData: false
  );

  // ── GqlQuery (non-filtered) — sync materialisation paths ────────────

  [Test]
  public void GqlQuery_ToList_MaterialisesSynchronously()
  {
    var rows = NonPagedQuery().ToList();
    Assert.That(rows.Select(u => u.Name), Is.EqualTo(new[] { "Alice", "Bob" }));
  }

  [Test]
  public void GqlQuery_GetEnumerator_TriggersMaterialisation()
  {
    var collected = new List<string>();
    foreach (var user in NonPagedQuery())
    {
      collected.Add(user.Name);
    }
    Assert.That(collected, Is.EqualTo(new[] { "Alice", "Bob" }));
  }

  // ── GqlQueryStorageAdapter — Inspect surface ────────────────────────

  [Test]
  public async Task GqlQueryAdapter_InspectShallow_HappyPath_Succeeds()
  {
    var adapter = new GqlQueryStorageAdapter<TestPagedResult, TestUser>(NonPagedQuery());
    var result = await adapter.InspectShallow(0).Run();
    Assert.That(((EffResult<ValidationResult>.Success)result).Value.IsValid, Is.True);
  }

  [Test]
  public async Task GqlQueryAdapter_InspectDeep_HappyPath_Succeeds()
  {
    var adapter = new GqlQueryStorageAdapter<TestPagedResult, TestUser>(NonPagedQuery());
    var result = await adapter.InspectDeep().Run();
    Assert.That(((EffResult<ValidationResult>.Success)result).Value.IsValid, Is.True);
  }

  [Test]
  public async Task GqlQueryAdapter_InspectTarget_RemoteAdapterReportsReadOnly()
  {
    // GQL adapters are read-only by default — InspectTarget should
    // succeed (no write attempt) or report a sensible result.
    var adapter = new GqlQueryStorageAdapter<TestPagedResult, TestUser>(NonPagedQuery());
    var result = await adapter.InspectTarget().Run();
    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
  }

  // ── Filtered GqlQuery (TFilter, TResult, T) — WithFilter pathway ────

  [Test]
  public async Task GqlQueryFiltered_WithFilter_ReturnsNewHandleWithFilter()
  {
    var seenFilters = new List<TestFilter?>();
    var query = new GqlQuery<TestFilter, TestPagedResult, TestUser>(
      label: "filtered",
      queryFunc: (f, _) =>
      {
        seenFilters.Add(f);
        return Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(
            new TestPagedResult { Nodes = Users }
          )
        );
      },
      selectData: r => r.Nodes,
      allowEmptyData: false
    );

    var filtered = query.WithFilter(new TestFilter { NameContains = "ali" });
    Assert.That(filtered.Filter, Is.Not.Null);
    Assert.That(filtered.Filter!.NameContains, Is.EqualTo("ali"));

    // Materialise the filtered handle so the queryFunc captures the filter.
    _ = await filtered.ToListAsync();
    Assert.That(seenFilters, Has.Count.EqualTo(1));
    Assert.That(seenFilters[0]!.NameContains, Is.EqualTo("ali"));
  }

  [Test]
  public void GqlQueryFiltered_GetEnumerator_TriggersMaterialisation()
  {
    var query = new GqlQuery<TestFilter, TestPagedResult, TestUser>(
      label: "filtered",
      queryFunc: (_, _) => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
      ),
      selectData: r => r.Nodes,
      allowEmptyData: false
    );

    var collected = new List<string>();
    foreach (var user in query)
    {
      collected.Add(user.Name);
    }
    Assert.That(collected, Has.Count.EqualTo(2));
  }

  // ── GqlSingleStorageAdapter — InspectDeep ───────────────────────────

  [Test]
  public async Task GqlSingleAdapter_InspectDeep_HappyPath_Succeeds()
  {
    var adapter = new GqlSingleStorageAdapter<TestSingleResult, TestUser>(
      label: "user",
      queryFunc: _ => Task.FromResult<IOperationResult<TestSingleResult>>(
        StubOperationResult<TestSingleResult>.Success(
          new TestSingleResult { User = Users[0] }
        )
      ),
      selectData: r => r.User!,
      mutationFunc: null,
      allowEmptyData: false
    );
    var result = await adapter.InspectDeep().Run();
    Assert.That(((EffResult<ValidationResult>.Success)result).Value.IsValid, Is.True);
  }

  // ── GqlEnumerableStorageAdapter — InspectTarget ─────────────────────

  [Test]
  public async Task GqlEnumerableAdapter_InspectTarget_ReturnsResult()
  {
    var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "users",
      queryFunc: _ => Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Users })
      ),
      selectData: r => r.Nodes,
      allowEmptyData: false
    );
    var result = await adapter.InspectTarget().Run();
    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
  }
}
