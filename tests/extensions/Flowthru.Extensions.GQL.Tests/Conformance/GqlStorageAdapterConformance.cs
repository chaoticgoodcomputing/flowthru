using Flowthru.Core.Data.Storage;
using Flowthru.Tests.Kits.Storage;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="GqlStorageAdapter{TResult, T}"/> — the single-item GQL adapter.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class GqlStorageAdapterConformance : StorageAdapterConformance<TestUser>
{
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/gql-single-user" };

  public GqlStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  protected override TestUser LoadFixture(string fixturePath) =>
    new TestUser { Id = 1, Name = "Alice" };

  protected override IStorageAdapter<TestUser> CreateWellFormed(TestUser data)
  {
    return new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "well-formed",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(
            new TestPagedResult { Nodes = new[] { data } }
          )
        ),
      selectData: r => r.Nodes![0],
      mutationFunc: (_, _) =>
        Task.FromResult<IOperationResult>(StubOperationResult<TestPagedResult>.Success(new TestPagedResult()))
    );
  }

  protected override IStorageAdapter<TestUser> CreateMissingSource()
  {
    // Endpoint unreachable — queryFunc throws. GQL adapter classifies this as NotFound.
    return new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "missing",
      queryFunc: _ => throw new HttpRequestException("Endpoint unreachable"),
      selectData: r => r.Nodes![0]
    );
  }

  protected override IEqualityComparer<TestUser>? Comparer => new TestUserComparer();

  private sealed class TestUserComparer : IEqualityComparer<TestUser>
  {
    public bool Equals(TestUser? x, TestUser? y)
    {
      if (x is null || y is null)
      {
        return ReferenceEquals(x, y);
      }
      return x.Id == y.Id && x.Name == y.Name;
    }

    public int GetHashCode(TestUser obj) => HashCode.Combine(obj.Id, obj.Name);
  }
}
