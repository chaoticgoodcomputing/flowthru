using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Flowthru.Data.Storage.Gql.Internal;
using StrawberryShake;

namespace Flowthru.Data.Storage.Gql;

/// <summary>
/// A deferred GQL query handle. Catalog construction captures all
/// query configuration (client delegate, pagination strategy, page
/// size); no network I/O happens until the step explicitly materialises
/// via <see cref="ToListAsync"/> / <see cref="ToList"/>, or
/// implicitly via <see cref="IEnumerable{T}"/> enumeration.
/// </summary>
/// <remarks>
/// <para>
/// Catalog declares <em>what</em> to query and <em>how</em> to
/// paginate; steps decide <em>when</em> to materialise. Use this when
/// the dataset is large enough that step-level filtering matters, or
/// where deferring the network boundary to step code is preferred.
/// </para>
/// <para>
/// For filtered queries (HotChocolate <c>where</c> arguments, etc.)
/// see the three-parameter variant
/// <see cref="GqlQuery{TFilter,TResult,T}"/>.
/// </para>
/// </remarks>
public sealed class GqlQuery<TResult, T> : IEnumerable<T>
  where TResult : class
  where T : class
{
  // Non-paginated
  internal readonly Func<CancellationToken, Task<IOperationResult<TResult>>>? QueryFunc;
  internal readonly Func<TResult, IEnumerable<T>?>? SelectData;

  // Relay paginated
  internal readonly Func<
    string?,
    int,
    CancellationToken,
    Task<IOperationResult<TResult>>
  >? RelayQueryFunc;
  internal readonly RelayPaginationStrategy<TResult, T>? RelayPagination;

  // Offset paginated
  internal readonly Func<
    int,
    int,
    CancellationToken,
    Task<IOperationResult<TResult>>
  >? OffsetQueryFunc;
  internal readonly OffsetPaginationStrategy<TResult, T>? OffsetPagination;

  internal readonly string Label;
  internal readonly int PageSize;
  internal readonly bool AllowEmptyData;

  internal GqlQuery(
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, IEnumerable<T>?> selectData,
    bool allowEmptyData
  )
  {
    Label = label;
    QueryFunc = queryFunc;
    SelectData = selectData;
    AllowEmptyData = allowEmptyData;
    PageSize = 0;
  }

  internal GqlQuery(
    string label,
    Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> relayQueryFunc,
    RelayPaginationStrategy<TResult, T> relayPagination,
    int pageSize,
    bool allowEmptyData
  )
  {
    Label = label;
    RelayQueryFunc = relayQueryFunc;
    RelayPagination = relayPagination;
    PageSize = pageSize;
    AllowEmptyData = allowEmptyData;
  }

  internal GqlQuery(
    string label,
    Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> offsetQueryFunc,
    OffsetPaginationStrategy<TResult, T> offsetPagination,
    int pageSize,
    bool allowEmptyData
  )
  {
    Label = label;
    OffsetQueryFunc = offsetQueryFunc;
    OffsetPagination = offsetPagination;
    PageSize = pageSize;
    AllowEmptyData = allowEmptyData;
  }

  /// <summary>
  /// Executes the GQL query (including all pagination pages) and
  /// returns the results as a list. This is the primary materialisation
  /// point — calling it triggers network I/O.
  /// </summary>
  public List<T> ToList() => ToListAsync(CancellationToken.None).GetAwaiter().GetResult();

  /// <summary>
  /// Executes the GQL query (including all pagination pages) and
  /// returns the results as a list. This is the primary materialisation
  /// point — calling it triggers network I/O.
  /// </summary>
  public Task<List<T>> ToListAsync(CancellationToken ct = default)
  {
    if (QueryFunc is not null && SelectData is not null)
      return GqlQueryExecutor.NonPagedAsync(Label, QueryFunc, SelectData, AllowEmptyData, ct);

    if (RelayQueryFunc is not null && RelayPagination is not null)
      return GqlQueryExecutor.RelayPagedAsync(
        Label, RelayQueryFunc, RelayPagination, PageSize, AllowEmptyData, ct
      );

    if (OffsetQueryFunc is not null && OffsetPagination is not null)
      return GqlQueryExecutor.OffsetPagedAsync(
        Label, OffsetQueryFunc, OffsetPagination, PageSize, AllowEmptyData, ct
      );

    throw new InvalidOperationException(
      $"GQL query '{Label}' is in an invalid state: no query delegate configured."
    );
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Triggers materialisation. Prefer <see cref="ToList"/> /
  /// <see cref="ToListAsync"/> for explicit control over when network
  /// I/O occurs.
  /// </remarks>
  public IEnumerator<T> GetEnumerator() => ToList().GetEnumerator();

  /// <inheritdoc/>
  [ExcludeFromCodeCoverage]
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// A deferred GQL query handle that supports a typed filter input.
/// Adds a <see cref="WithFilter"/> method returning a new handle with
/// the filter applied; the catalog declares the query without a
/// filter, and the step applies one before materialising.
/// </summary>
public sealed class GqlQuery<TFilter, TResult, T> : IEnumerable<T>
  where TFilter : class
  where TResult : class
  where T : class
{
  private readonly Func<TFilter?, CancellationToken, Task<IOperationResult<TResult>>>? _queryFunc;
  private readonly Func<TResult, IEnumerable<T>?>? _selectData;

  private readonly Func<
    TFilter?,
    string?,
    int,
    CancellationToken,
    Task<IOperationResult<TResult>>
  >? _relayQueryFunc;
  private readonly RelayPaginationStrategy<TResult, T>? _relayPagination;

  private readonly Func<
    TFilter?,
    int,
    int,
    CancellationToken,
    Task<IOperationResult<TResult>>
  >? _offsetQueryFunc;
  private readonly OffsetPaginationStrategy<TResult, T>? _offsetPagination;

  private readonly string _label;
  private readonly int _pageSize;
  private readonly bool _allowEmptyData;

  /// <summary>The catalog item label, used in error messages.</summary>
  internal string Label => _label;

  /// <summary>
  /// The current filter applied to the query. <see langword="null"/>
  /// when no filter has been set.
  /// </summary>
  public TFilter? Filter { get; }

  internal GqlQuery(
    string label,
    Func<TFilter?, CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, IEnumerable<T>?> selectData,
    bool allowEmptyData,
    TFilter? filter = null
  )
  {
    _label = label;
    _queryFunc = queryFunc;
    _selectData = selectData;
    _allowEmptyData = allowEmptyData;
    Filter = filter;
    _pageSize = 0;
  }

  internal GqlQuery(
    string label,
    Func<TFilter?, string?, int, CancellationToken, Task<IOperationResult<TResult>>> relayQueryFunc,
    RelayPaginationStrategy<TResult, T> relayPagination,
    int pageSize,
    bool allowEmptyData,
    TFilter? filter = null
  )
  {
    _label = label;
    _relayQueryFunc = relayQueryFunc;
    _relayPagination = relayPagination;
    _pageSize = pageSize;
    _allowEmptyData = allowEmptyData;
    Filter = filter;
  }

  internal GqlQuery(
    string label,
    Func<TFilter?, int, int, CancellationToken, Task<IOperationResult<TResult>>> offsetQueryFunc,
    OffsetPaginationStrategy<TResult, T> offsetPagination,
    int pageSize,
    bool allowEmptyData,
    TFilter? filter = null
  )
  {
    _label = label;
    _offsetQueryFunc = offsetQueryFunc;
    _offsetPagination = offsetPagination;
    _pageSize = pageSize;
    _allowEmptyData = allowEmptyData;
    Filter = filter;
  }

  /// <summary>
  /// Returns a new query handle with the specified filter applied.
  /// Does not trigger materialisation — the query is still deferred.
  /// </summary>
  public GqlQuery<TFilter, TResult, T> WithFilter(TFilter filter)
  {
    if (_queryFunc is not null && _selectData is not null)
      return new(_label, _queryFunc, _selectData, _allowEmptyData, filter);

    if (_relayQueryFunc is not null && _relayPagination is not null)
      return new(_label, _relayQueryFunc, _relayPagination, _pageSize, _allowEmptyData, filter);

    if (_offsetQueryFunc is not null && _offsetPagination is not null)
      return new(_label, _offsetQueryFunc, _offsetPagination, _pageSize, _allowEmptyData, filter);

    throw new InvalidOperationException(
      $"GQL query '{_label}' is in an invalid state: no query delegate configured."
    );
  }

  /// <summary>
  /// Executes the GQL query (with the current filter, if any) and
  /// returns results as a list. Triggers network I/O.
  /// </summary>
  public List<T> ToList() => ToListAsync(CancellationToken.None).GetAwaiter().GetResult();

  /// <summary>
  /// Executes the GQL query (with the current filter, if any) and
  /// returns results as a list. Triggers network I/O.
  /// </summary>
  public Task<List<T>> ToListAsync(CancellationToken ct = default)
  {
    if (_queryFunc is not null && _selectData is not null)
      return GqlQueryExecutor.FilteredNonPagedAsync(
        _label, _queryFunc, Filter, _selectData, _allowEmptyData, ct
      );

    if (_relayQueryFunc is not null && _relayPagination is not null)
      return GqlQueryExecutor.FilteredRelayPagedAsync(
        _label, _relayQueryFunc, Filter, _relayPagination, _pageSize, _allowEmptyData, ct
      );

    if (_offsetQueryFunc is not null && _offsetPagination is not null)
      return GqlQueryExecutor.FilteredOffsetPagedAsync(
        _label, _offsetQueryFunc, Filter, _offsetPagination, _pageSize, _allowEmptyData, ct
      );

    throw new InvalidOperationException(
      $"GQL query '{_label}' is in an invalid state: no query delegate configured."
    );
  }

  /// <inheritdoc/>
  public IEnumerator<T> GetEnumerator() => ToList().GetEnumerator();

  /// <inheritdoc/>
  [ExcludeFromCodeCoverage]
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
