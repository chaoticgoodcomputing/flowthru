using Flowthru.Core.Data.Storage;
using Flowthru.Tests.Kits.Storage;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="GqlEnumerableStorageAdapter{TResult, T}"/> — the non-paginated
/// collection variant. Read-only by design (<c>Traits.CanWrite = false</c>); the round-trip
/// test takes the kit's read-only skip path.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class GqlEnumerableStorageAdapterConformance
  : StorageAdapterConformance<IEnumerable<TestUser>>
{
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/gql-user-list" };

  public GqlEnumerableStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  protected override IEnumerable<TestUser> LoadFixture(string fixturePath) =>
    new[]
    {
      new TestUser { Id = 1, Name = "Alice" },
      new TestUser { Id = 2, Name = "Bob" },
    };

  protected override IStorageAdapter<IEnumerable<TestUser>> CreateWellFormed(
    IEnumerable<TestUser> data
  )
  {
    var snapshot = data.ToArray();
    return new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "well-formed",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = snapshot })
        ),
      selectData: r => r.Nodes
    );
  }

  protected override IStorageAdapter<IEnumerable<TestUser>> CreateMissingSource()
  {
    return new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
      label: "missing",
      queryFunc: _ => throw new HttpRequestException("Endpoint unreachable"),
      selectData: r => r.Nodes
    );
  }

  protected override IEqualityComparer<IEnumerable<TestUser>>? Comparer =>
    new UserSequenceComparer();

  private sealed class UserSequenceComparer : IEqualityComparer<IEnumerable<TestUser>>
  {
    public bool Equals(IEnumerable<TestUser>? x, IEnumerable<TestUser>? y)
    {
      if (x is null || y is null)
      {
        return ReferenceEquals(x, y);
      }
      var xList = x.OrderBy(u => u.Id).ToList();
      var yList = y.OrderBy(u => u.Id).ToList();
      if (xList.Count != yList.Count)
      {
        return false;
      }
      for (var i = 0; i < xList.Count; i++)
      {
        if (xList[i].Id != yList[i].Id || xList[i].Name != yList[i].Name)
        {
          return false;
        }
      }
      return true;
    }

    public int GetHashCode(IEnumerable<TestUser> obj) => 0;
  }
}
