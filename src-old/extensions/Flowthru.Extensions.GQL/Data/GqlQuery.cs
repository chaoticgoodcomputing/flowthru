using System.Collections;
using System.Diagnostics.CodeAnalysis;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Data;

/// <summary>
/// A deferred GQL query handle — analogous to <c>TypedFrame&lt;T&gt;</c> in the Spark extension.
/// </summary>
/// <remarks>
/// <para>
/// <c>GqlQuery&lt;TResult, T&gt;</c> captures all query configuration (client delegate, pagination
/// strategy, page size) at catalog construction time but does <em>not</em> execute any network
/// calls until explicitly materialized. The catalog declares <em>what</em> to query and
/// <em>how</em> to paginate; steps decide <em>when</em> to materialize via
/// <see cref="ToListAsync"/> or <see cref="ToList"/>.
/// </para>
/// <para>
/// <strong>Materialization boundaries:</strong>
/// </para>
/// <list type="bullet">
/// <item>
///   Explicit: call <see cref="ToListAsync"/> or <see cref="ToList"/> in your step transform.
/// </item>
/// <item>
///   Implicit: <c>GqlQuery&lt;TResult, T&gt;</c> implements <see cref="IEnumerable{T}"/>, so
///   LINQ operators and <c>foreach</c> trigger materialization automatically. Explicit calls
///   are preferred for readability — they make the network boundary visible in step code.
/// </item>
/// </list>
/// <para>
/// <strong>Filtered variant:</strong> When your GQL operation accepts a filter input type
/// (e.g. a HotChocolate <c>where</c> argument), use
/// <see cref="GqlQuery{TFilter, TResult, T}"/> instead. It adds a
/// <see cref="GqlQuery{TFilter, TResult, T}.WithFilter"/> method that returns a new handle
/// with the filter applied, without triggering materialization.
/// </para>
/// </remarks>
/// <typeparam name="TResult">
/// The StrawberryShake-generated result data type (e.g. <c>IGetCompaniesResult</c>).
/// </typeparam>
/// <typeparam name="T">
/// The target element type surfaced to the step (e.g. <c>IGetCompanies_Companies</c>).
/// </typeparam>
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

  // ── Non-paginated constructor ──────────────────────────────────────────────

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

  // ── Relay-paginated constructor ────────────────────────────────────────────

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

  // ── Offset-paginated constructor ───────────────────────────────────────────

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

  // ── Materialization ────────────────────────────────────────────────────────

  /// <summary>
  /// Executes the GQL query (including all pagination pages) and returns the results as a list.
  /// This is the primary materialization point — calling this triggers network I/O.
  /// </summary>
  public List<T> ToList() => ToListAsync(CancellationToken.None).GetAwaiter().GetResult();

  /// <summary>
  /// Executes the GQL query (including all pagination pages) and returns the results as a list.
  /// This is the primary materialization point — calling this triggers network I/O.
  /// </summary>
  public Task<List<T>> ToListAsync(CancellationToken ct = default)
  {
    if (QueryFunc is not null && SelectData is not null)
      return GqlQueryExecutor.NonPagedAsync(Label, QueryFunc, SelectData, AllowEmptyData, ct);

    if (RelayQueryFunc is not null && RelayPagination is not null)
      return GqlQueryExecutor.RelayPagedAsync(
        Label,
        RelayQueryFunc,
        RelayPagination,
        PageSize,
        AllowEmptyData,
        ct
      );

    if (OffsetQueryFunc is not null && OffsetPagination is not null)
      return GqlQueryExecutor.OffsetPagedAsync(
        Label,
        OffsetQueryFunc,
        OffsetPagination,
        PageSize,
        AllowEmptyData,
        ct
      );

    throw new InvalidOperationException(
      $"GQL query '{Label}' is in an invalid state: no query delegate configured."
    );
  }

  // ── IEnumerable<T> — implicit materialization ──────────────────────────────

  /// <inheritdoc/>
  /// <remarks>
  /// Triggers materialization. Prefer <see cref="ToList"/> or <see cref="ToListAsync"/> for
  /// explicit control over when network I/O occurs.
  /// </remarks>
  public IEnumerator<T> GetEnumerator() => ToList().GetEnumerator();

  /// <inheritdoc/>
  // Required-by-interface shim — Coverlet doesn't credit DIM-shaped explicit
  // interface implementations. See Phase 2 of the Core coverage audit.
  [ExcludeFromCodeCoverage]
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// A deferred GQL query handle that supports a typed filter input.
/// </summary>
/// <remarks>
/// <para>
/// Extends <see cref="GqlQuery{TResult, T}"/> with a <see cref="WithFilter"/> method.
/// The filter is initially <see langword="null"/> (unset) — the catalog declares the query
/// without a filter, and the step applies one before materializing.
/// </para>
/// <para>
/// <strong>Usage pattern in a step:</strong>
/// <code>
/// // Step receives the unfiltered handle from the catalog
/// public static IEnumerable&lt;NetSuiteCustomerSchema&gt; Create(
///     (IList&lt;string&gt; activeOrgNames,
///      GqlQuery&lt;TypedCustomerFilterInput, IGetCustomersResult, IGetCustomers_Nodes&gt; customers) input)
/// {
///     var (orgNames, customers) = input;
///     return customers
///         .WithFilter(new TypedCustomerFilterInput {
///             Companyname = new StringOperationFilterInput { In = orgNames }
///         })
///         .ToList()
///         .Select(MapToSchema);
/// }
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TFilter">
/// The StrawberryShake-generated filter input type (e.g. <c>TypedCustomerFilterInput</c>).
/// </typeparam>
/// <typeparam name="TResult">
/// The StrawberryShake-generated result data type.
/// </typeparam>
/// <typeparam name="T">
/// The target element type surfaced to the step.
/// </typeparam>
public sealed class GqlQuery<TFilter, TResult, T> : IEnumerable<T>
  where TFilter : class
  where TResult : class
  where T : class
{
  // Non-paginated
  private readonly Func<TFilter?, CancellationToken, Task<IOperationResult<TResult>>>? _queryFunc;
  private readonly Func<TResult, IEnumerable<T>?>? _selectData;

  // Relay paginated
  private readonly Func<
    TFilter?,
    string?,
    int,
    CancellationToken,
    Task<IOperationResult<TResult>>
  >? _relayQueryFunc;
  private readonly RelayPaginationStrategy<TResult, T>? _relayPagination;

  // Offset paginated
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

  /// <summary>The catalog entry label, used in error messages.</summary>
  internal string Label => _label;

  /// <summary>
  /// The current filter applied to the query. <see langword="null"/> when no filter has been set.
  /// </summary>
  public TFilter? Filter { get; }

  // ── Non-paginated constructor ──────────────────────────────────────────────

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

  // ── Relay-paginated constructor ────────────────────────────────────────────

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

  // ── Offset-paginated constructor ───────────────────────────────────────────

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

  // ── Filter composition ─────────────────────────────────────────────────────

  /// <summary>
  /// Returns a new query handle with the specified filter applied.
  /// Does not trigger materialization — the query is still deferred.
  /// </summary>
  /// <param name="filter">The filter input to apply when the query is materialized.</param>
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

  // ── Materialization ────────────────────────────────────────────────────────

  /// <summary>
  /// Executes the GQL query (with the current filter, if any) and returns results as a list.
  /// This triggers network I/O.
  /// </summary>
  public List<T> ToList() => ToListAsync(CancellationToken.None).GetAwaiter().GetResult();

  /// <summary>
  /// Executes the GQL query (with the current filter, if any) and returns results as a list.
  /// This triggers network I/O.
  /// </summary>
  public Task<List<T>> ToListAsync(CancellationToken ct = default)
  {
    if (_queryFunc is not null && _selectData is not null)
      return GqlQueryExecutor.FilteredNonPagedAsync(
        _label,
        _queryFunc,
        Filter,
        _selectData,
        _allowEmptyData,
        ct
      );

    if (_relayQueryFunc is not null && _relayPagination is not null)
      return GqlQueryExecutor.FilteredRelayPagedAsync(
        _label,
        _relayQueryFunc,
        Filter,
        _relayPagination,
        _pageSize,
        _allowEmptyData,
        ct
      );

    if (_offsetQueryFunc is not null && _offsetPagination is not null)
      return GqlQueryExecutor.FilteredOffsetPagedAsync(
        _label,
        _offsetQueryFunc,
        Filter,
        _offsetPagination,
        _pageSize,
        _allowEmptyData,
        ct
      );

    throw new InvalidOperationException(
      $"GQL query '{_label}' is in an invalid state: no query delegate configured."
    );
  }

  // ── IEnumerable<T> ─────────────────────────────────────────────────────────

  /// <inheritdoc/>
  public IEnumerator<T> GetEnumerator() => ToList().GetEnumerator();

  /// <inheritdoc/>
  // Required-by-interface shim — Coverlet doesn't credit DIM-shaped explicit
  // interface implementations. See Phase 2 of the Core coverage audit.
  [ExcludeFromCodeCoverage]
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
