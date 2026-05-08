namespace Flowthru.Extensions.GQL.Tests.Fixtures;

/// <summary>
/// Test models that stand in for StrawberryShake-generated result and
/// data types. The class shape is the only thing tests care about — no
/// JSON, no schema, no source generation.
/// </summary>
public sealed class TestUser
{
  public int Id { get; init; }
  public string Name { get; init; } = string.Empty;
}

/// <summary>Result envelope holding a single optional user.</summary>
public sealed class TestSingleResult
{
  public TestUser? User { get; init; }
}

/// <summary>Result envelope holding a paged list of users (Relay-style).</summary>
public sealed class TestPagedResult
{
  public IReadOnlyList<TestUser>? Nodes { get; init; }
  public StubPageInfo? PageInfo { get; init; }
  public int? Total { get; init; }
}

public sealed class StubPageInfo
{
  public bool HasNextPage { get; init; }
  public string? EndCursor { get; init; }
}

/// <summary>Stub filter input — stand-in for a HotChocolate filter.</summary>
public sealed class TestFilter
{
  public string? NameContains { get; init; }
}
