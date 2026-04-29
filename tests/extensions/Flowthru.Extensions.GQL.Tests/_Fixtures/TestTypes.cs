using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// Test entity types (stand in for StrawberryShake-generated data types)
// ─────────────────────────────────────────────────────────────────────────────

public class TestUser
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
}

public class TestPagedResult
{
  public IReadOnlyList<TestUser>? Nodes { get; set; }
  public StubPageInfo? PageInfo { get; set; }
  public int? Total { get; set; }
}

public class StubPageInfo
{
  public bool HasNextPage { get; set; }
  public string? EndCursor { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Mock IOperationResult<T> implementation
// ─────────────────────────────────────────────────────────────────────────────

public class StubOperationResult<T> : IOperationResult<T>
  where T : class
{
  public T? Data { get; init; }
  public IReadOnlyList<IClientError> Errors { get; init; } = Array.Empty<IClientError>();
  public IReadOnlyDictionary<string, object?> Extensions { get; init; } =
    new Dictionary<string, object?>();
  public IReadOnlyDictionary<string, object?> ContextData { get; init; } =
    new Dictionary<string, object?>();

  // Explicit IOperationResult members
  object? IOperationResult.Data => Data;
  Type IOperationResult.DataType => typeof(T);
  IOperationResultDataInfo? IOperationResult.DataInfo => null;
  object IOperationResult.DataFactory => NullDataFactory.Instance;
  IOperationResultDataFactory<T> IOperationResult<T>.DataFactory => NullDataFactory<T>.Instance;

  IOperationResult<T> IOperationResult<T>.WithData(T data, IOperationResultDataInfo dataInfo) =>
    new StubOperationResult<T> { Data = data };

  public static StubOperationResult<T> Success(T data) => new() { Data = data };

  public static StubOperationResult<T> WithErrors(params string[] messages) =>
    new() { Errors = messages.Select(m => (IClientError)new StubClientError(m)).ToArray() };
}

public class StubClientError : IClientError
{
  public StubClientError(string message) => Message = message;

  public string Message { get; }
  public string? Code => null;
  public IReadOnlyList<object>? Path => null;
  public IReadOnlyList<Location>? Locations => null;
  public Exception? Exception => null;
  public IReadOnlyDictionary<string, object?>? Extensions => null;
}

// Minimal no-op IOperationResultDataFactory implementations required by the interface
public class NullDataFactory : IOperationResultDataFactory
{
  public static readonly NullDataFactory Instance = new();

  public Type ResultType => typeof(object);

  public object Create(IOperationResultDataInfo dataInfo, IEntityStoreSnapshot? snapshot = null) =>
    null!;
}

public class NullDataFactory<T> : IOperationResultDataFactory<T>
  where T : class
{
  public static readonly NullDataFactory<T> Instance = new();

  public Type ResultType => typeof(T);

  public T Create(IOperationResultDataInfo dataInfo, IEntityStoreSnapshot? snapshot = null) =>
    null!;

  object IOperationResultDataFactory.Create(
    IOperationResultDataInfo dataInfo,
    IEntityStoreSnapshot? snapshot
  ) => null!;
}
