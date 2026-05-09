using Flowthru.Data.Storage.Gql;
using StrawberryShake;

namespace Flowthru.Data.Catalog;

/// <summary>
/// GraphQL item-builder extensions on <see cref="ItemAnchor{T}"/>.
/// Three shapes are supported, each with distinct method names
/// because GQL's <c>where T : class</c> constraint can't disambiguate
/// singleton vs collection vs deferred-handle via receiver-type
/// pattern alone:
/// <list type="bullet">
///   <item><c>.GqlSingle(...)</c> — single-item query, eager.</item>
///   <item><c>.Gql(...)</c> — collection query, eager.</item>
///   <item><c>.GqlDeferred(...)</c> — deferred handle (the catalog item carries a <see cref="GqlQuery{TResult, T}"/>).</item>
/// </list>
/// </summary>
public static class GqlExtensions
{
  // ── Singleton (eager) ───────────────────────────────────────────────

  /// <summary>Build a single-item GraphQL catalog item (eager).</summary>
  public static GqlSingleBuilder<TResult, T> GqlSingle<TResult, T>(
    this ItemAnchor<T> anchor,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, T> selectData
  )
    where TResult : class
    where T : class =>
    new(anchor.Label, queryFunc, selectData);

  // ── Enumerable (eager) ──────────────────────────────────────────────

  /// <summary>
  /// Build a collection GraphQL catalog item (eager, non-paginated).
  /// </summary>
  public static GqlBuilder<TResult, TRow> Gql<TResult, TRow>(
    this ItemAnchor<IEnumerable<TRow>> anchor,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, IEnumerable<TRow>?> selectData
  )
    where TResult : class
    where TRow : class =>
    new(anchor.Label, queryFunc, selectData);

  /// <summary>
  /// Build a collection GraphQL catalog item with Relay cursor pagination.
  /// </summary>
  public static GqlPagedRelayBuilder<TResult, TRow> Gql<TResult, TRow>(
    this ItemAnchor<IEnumerable<TRow>> anchor,
    Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    RelayPaginationStrategy<TResult, TRow> pagination
  )
    where TResult : class
    where TRow : class =>
    new(anchor.Label, pagedQueryFunc, pagination);

  /// <summary>
  /// Build a collection GraphQL catalog item with offset pagination.
  /// </summary>
  public static GqlPagedOffsetBuilder<TResult, TRow> Gql<TResult, TRow>(
    this ItemAnchor<IEnumerable<TRow>> anchor,
    Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    OffsetPaginationStrategy<TResult, TRow> pagination
  )
    where TResult : class
    where TRow : class =>
    new(anchor.Label, pagedQueryFunc, pagination);

  // ── Deferred (handle returned from Load) ────────────────────────────

  /// <summary>
  /// Build a deferred GraphQL catalog item — the step receives a
  /// <see cref="GqlQuery{TResult, TRow}"/> handle and decides when
  /// to materialise.
  /// </summary>
  public static GqlDeferredBuilder<TResult, TRow> GqlDeferred<TResult, TRow>(
    this ItemAnchor<GqlQuery<TResult, TRow>> anchor,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, IEnumerable<TRow>?> selectData
  )
    where TResult : class
    where TRow : class =>
    new(anchor.Label, queryFunc, selectData);
}

// ── Builders ──────────────────────────────────────────────────────────

/// <summary>Tier-1 builder for a single-item GQL catalog item.</summary>
public sealed class GqlSingleBuilder<TResult, T>
  where TResult : class
  where T : class
{
  private readonly string _label;
  private readonly Func<CancellationToken, Task<IOperationResult<TResult>>> _queryFunc;
  private readonly Func<TResult, T> _selectData;
  private Func<T, CancellationToken, Task<IOperationResult>>? _mutationFunc;
  private bool _allowEmptyData;

  internal GqlSingleBuilder(
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, T> selectData)
  {
    _label = label;
    _queryFunc = queryFunc ?? throw new ArgumentNullException(nameof(queryFunc));
    _selectData = selectData ?? throw new ArgumentNullException(nameof(selectData));
  }

  /// <summary>Provide a mutation delegate to make this item read/write.</summary>
  public GqlSingleBuilder<TResult, T> WithMutation(
    Func<T, CancellationToken, Task<IOperationResult>> mutationFunc)
  {
    _mutationFunc = mutationFunc ?? throw new ArgumentNullException(nameof(mutationFunc));
    return this;
  }

  /// <summary>Treat null data as a valid result (default: false).</summary>
  public GqlSingleBuilder<TResult, T> AllowEmpty()
  {
    _allowEmptyData = true;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<T> Build() =>
    new Item<T>(
      _label,
      new GqlSingleStorageAdapter<TResult, T>(_label, _queryFunc, _selectData, _mutationFunc, _allowEmptyData)
    );
}

/// <summary>Tier-1 builder for a non-paginated GQL collection item.</summary>
public sealed class GqlBuilder<TResult, TRow>
  where TResult : class
  where TRow : class
{
  private readonly string _label;
  private readonly Func<CancellationToken, Task<IOperationResult<TResult>>> _queryFunc;
  private readonly Func<TResult, IEnumerable<TRow>?> _selectData;
  private bool _allowEmptyData;

  internal GqlBuilder(
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, IEnumerable<TRow>?> selectData)
  {
    _label = label;
    _queryFunc = queryFunc ?? throw new ArgumentNullException(nameof(queryFunc));
    _selectData = selectData ?? throw new ArgumentNullException(nameof(selectData));
  }

  /// <summary>Treat empty collections as valid (default: false).</summary>
  public GqlBuilder<TResult, TRow> AllowEmpty()
  {
    _allowEmptyData = true;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build() =>
    new Item<IEnumerable<TRow>>(
      _label,
      new GqlEnumerableStorageAdapter<TResult, TRow>(_label, _queryFunc, _selectData, _allowEmptyData)
    );
}

/// <summary>Tier-1 builder for a Relay-paginated GQL collection item.</summary>
public sealed class GqlPagedRelayBuilder<TResult, TRow>
  where TResult : class
  where TRow : class
{
  private readonly string _label;
  private readonly Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> _pagedQueryFunc;
  private readonly RelayPaginationStrategy<TResult, TRow> _pagination;
  private int _pageSize = 100;
  private bool _allowEmptyData;

  internal GqlPagedRelayBuilder(
    string label,
    Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    RelayPaginationStrategy<TResult, TRow> pagination)
  {
    _label = label;
    _pagedQueryFunc = pagedQueryFunc ?? throw new ArgumentNullException(nameof(pagedQueryFunc));
    _pagination = pagination ?? throw new ArgumentNullException(nameof(pagination));
  }

  /// <summary>Override the default 100-item page size.</summary>
  public GqlPagedRelayBuilder<TResult, TRow> WithPageSize(int pageSize)
  {
    if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
    _pageSize = pageSize;
    return this;
  }

  /// <summary>Treat empty result sets as valid.</summary>
  public GqlPagedRelayBuilder<TResult, TRow> AllowEmpty()
  {
    _allowEmptyData = true;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build() =>
    new Item<IEnumerable<TRow>>(
      _label,
      new GqlEnumerableStorageAdapter<TResult, TRow>(
        _label, _pagedQueryFunc, _pagination, _pageSize, _allowEmptyData
      )
    );
}

/// <summary>Tier-1 builder for an offset-paginated GQL collection item.</summary>
public sealed class GqlPagedOffsetBuilder<TResult, TRow>
  where TResult : class
  where TRow : class
{
  private readonly string _label;
  private readonly Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> _pagedQueryFunc;
  private readonly OffsetPaginationStrategy<TResult, TRow> _pagination;
  private int _pageSize = 100;
  private bool _allowEmptyData;

  internal GqlPagedOffsetBuilder(
    string label,
    Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    OffsetPaginationStrategy<TResult, TRow> pagination)
  {
    _label = label;
    _pagedQueryFunc = pagedQueryFunc ?? throw new ArgumentNullException(nameof(pagedQueryFunc));
    _pagination = pagination ?? throw new ArgumentNullException(nameof(pagination));
  }

  /// <summary>Override the default 100-item page size.</summary>
  public GqlPagedOffsetBuilder<TResult, TRow> WithPageSize(int pageSize)
  {
    if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
    _pageSize = pageSize;
    return this;
  }

  /// <summary>Treat empty result sets as valid.</summary>
  public GqlPagedOffsetBuilder<TResult, TRow> AllowEmpty()
  {
    _allowEmptyData = true;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build() =>
    new Item<IEnumerable<TRow>>(
      _label,
      new GqlEnumerableStorageAdapter<TResult, TRow>(
        _label, _pagedQueryFunc, _pagination, _pageSize, _allowEmptyData
      )
    );
}

/// <summary>Tier-1 builder for a deferred GQL handle catalog item.</summary>
public sealed class GqlDeferredBuilder<TResult, TRow>
  where TResult : class
  where TRow : class
{
  private readonly string _label;
  private readonly Func<CancellationToken, Task<IOperationResult<TResult>>> _queryFunc;
  private readonly Func<TResult, IEnumerable<TRow>?> _selectData;
  private bool _allowEmptyData;

  internal GqlDeferredBuilder(
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, IEnumerable<TRow>?> selectData)
  {
    _label = label;
    _queryFunc = queryFunc ?? throw new ArgumentNullException(nameof(queryFunc));
    _selectData = selectData ?? throw new ArgumentNullException(nameof(selectData));
  }

  /// <summary>Treat empty collections as valid.</summary>
  public GqlDeferredBuilder<TResult, TRow> AllowEmpty()
  {
    _allowEmptyData = true;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<GqlQuery<TResult, TRow>> Build()
  {
    var query = new GqlQuery<TResult, TRow>(_label, _queryFunc, _selectData, _allowEmptyData);
    return new Item<GqlQuery<TResult, TRow>>(_label, new GqlQueryStorageAdapter<TResult, TRow>(query));
  }
}
