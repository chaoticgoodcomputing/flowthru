using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Data;

public static partial class GqlItemFactory
{
  /// <summary>
  /// Factory methods for <see cref="Item{T}"/> backed by a deferred
  /// <see cref="GqlQuery{TResult,T}"/> or <see cref="GqlQuery{TFilter,TResult,T}"/> handle.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Entries created by this factory surface a <em>deferred</em> query handle — no network
  /// calls are made when the catalog is constructed or during pre-flight (beyond a lightweight
  /// connectivity probe). The step that consumes the entry decides when to materialize by
  /// calling <c>ToList</c> / <c>ToListAsync</c>, or by using the handle as an
  /// <see cref="System.Collections.Generic.IEnumerable{T}"/> directly.
  /// </para>
  /// <para>
  /// Use the <strong>filtered</strong> overloads (<c>QueryFiltered</c>,
  /// <c>PagedQueryFiltered</c>) when your GQL operation accepts a filter input type
  /// (e.g. a HotChocolate <c>where</c> argument). The step applies the filter via
  /// <see cref="GqlQuery{TFilter,TResult,T}.WithFilter"/> before materializing — the catalog
  /// entry itself is always declared without a filter.
  /// </para>
  /// <para>
  /// Compare with <see cref="Enumerable"/>: those factories eagerly materialize the full
  /// dataset inside the catalog layer. Use <c>Query</c> factory entries for remote sources
  /// where either (a) the dataset is large and step-level filtering avoids pulling unnecessary
  /// data, or (b) the general principle of deferring materialization decisions to the step
  /// is preferred.
  /// </para>
  /// </remarks>
  public static class Query
  {
    // ── Unfiltered — Non-paginated ─────────────────────────────────────────

    /// <summary>
    /// Creates a deferred non-paginated GQL catalog entry.
    /// The server is expected to return all results in a single response.
    /// </summary>
    /// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
    /// <typeparam name="T">The target element type.</typeparam>
    /// <param name="label">Catalog entry label used in the pipeline DAG and validation messages.</param>
    /// <param name="queryFunc">Delegate that executes the StrawberryShake query operation.</param>
    /// <param name="selectData">Projects the result envelope to the collection of <typeparamref name="T"/>.</param>
    /// <param name="allowEmptyData">
    /// If <c>true</c>, an empty collection is valid during pre-flight and at materialization time.
    /// </param>
    public static Item<GqlQuery<TResult, T>> NonPaged<TResult, T>(
      string label,
      Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
      Func<TResult, IEnumerable<T>?> selectData,
      bool allowEmptyData = false
    )
      where TResult : class
      where T : class
    {
      var query = new GqlQuery<TResult, T>(label, queryFunc, selectData, allowEmptyData);
      return new Item<GqlQuery<TResult, T>>(label, new GqlQueryStorageAdapter<TResult, T>(query));
    }

    // ── Unfiltered — Relay-paginated ───────────────────────────────────────

    /// <summary>
    /// Creates a deferred Relay cursor-paginated GQL catalog entry.
    /// </summary>
    /// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
    /// <typeparam name="T">The target element type.</typeparam>
    /// <param name="label">Catalog entry label used in the pipeline DAG and validation messages.</param>
    /// <param name="pagedQueryFunc">
    /// Delegate accepting <c>(cursor, pageSize, cancellationToken)</c> that executes the paginated query.
    /// </param>
    /// <param name="pagination">
    /// Relay pagination strategy created via <see cref="Pagination.Relay{TResult,T}"/>.
    /// </param>
    /// <param name="pageSize">Number of items per page. Defaults to 100.</param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty result set is valid.</param>
    public static Item<GqlQuery<TResult, T>> PagedQuery<TResult, T>(
      string label,
      Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
      RelayPaginationStrategy<TResult, T> pagination,
      int pageSize = 100,
      bool allowEmptyData = false
    )
      where TResult : class
      where T : class
    {
      var query = new GqlQuery<TResult, T>(
        label,
        pagedQueryFunc,
        pagination,
        pageSize,
        allowEmptyData
      );
      return new Item<GqlQuery<TResult, T>>(label, new GqlQueryStorageAdapter<TResult, T>(query));
    }

    // ── Unfiltered — Offset-paginated ──────────────────────────────────────

    /// <summary>
    /// Creates a deferred offset-paginated GQL catalog entry.
    /// </summary>
    /// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
    /// <typeparam name="T">The target element type.</typeparam>
    /// <param name="label">Catalog entry label used in the pipeline DAG and validation messages.</param>
    /// <param name="pagedQueryFunc">
    /// Delegate accepting <c>(offset, limit, cancellationToken)</c> that executes the paginated query.
    /// </param>
    /// <param name="pagination">
    /// Offset pagination strategy created via <see cref="Pagination.Offset{TResult,T}"/>.
    /// </param>
    /// <param name="pageSize">Number of items per page. Defaults to 100.</param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty result set is valid.</param>
    public static Item<GqlQuery<TResult, T>> PagedQuery<TResult, T>(
      string label,
      Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
      OffsetPaginationStrategy<TResult, T> pagination,
      int pageSize = 100,
      bool allowEmptyData = false
    )
      where TResult : class
      where T : class
    {
      var query = new GqlQuery<TResult, T>(
        label,
        pagedQueryFunc,
        pagination,
        pageSize,
        allowEmptyData
      );
      return new Item<GqlQuery<TResult, T>>(label, new GqlQueryStorageAdapter<TResult, T>(query));
    }

    // ── Filtered — Non-paginated ───────────────────────────────────────────

    /// <summary>
    /// Creates a deferred non-paginated GQL catalog entry that accepts a filter input type.
    /// The entry is declared without a filter; steps apply one via
    /// <see cref="GqlQuery{TFilter,TResult,T}.WithFilter"/> before materializing.
    /// </summary>
    /// <typeparam name="TFilter">The StrawberryShake-generated filter input type.</typeparam>
    /// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
    /// <typeparam name="T">The target element type.</typeparam>
    /// <param name="label">Catalog entry label.</param>
    /// <param name="queryFunc">
    /// Delegate accepting <c>(filter, cancellationToken)</c>. Pass <c>filter</c> directly to the
    /// StrawberryShake <c>ExecuteAsync</c> call's <c>where</c> argument.
    /// </param>
    /// <param name="selectData">Projects the result envelope to the collection of <typeparamref name="T"/>.</param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty result set is valid.</param>
    public static Item<GqlQuery<TFilter, TResult, T>> NonPaged<TFilter, TResult, T>(
      string label,
      Func<TFilter?, CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
      Func<TResult, IEnumerable<T>?> selectData,
      bool allowEmptyData = false
    )
      where TFilter : class
      where TResult : class
      where T : class
    {
      var query = new GqlQuery<TFilter, TResult, T>(label, queryFunc, selectData, allowEmptyData);
      return new Item<GqlQuery<TFilter, TResult, T>>(
        label,
        new GqlQueryStorageAdapter<TFilter, TResult, T>(query)
      );
    }

    // ── Filtered — Relay-paginated ─────────────────────────────────────────

    /// <summary>
    /// Creates a deferred Relay cursor-paginated GQL catalog entry that accepts a filter input type.
    /// </summary>
    /// <typeparam name="TFilter">The StrawberryShake-generated filter input type.</typeparam>
    /// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
    /// <typeparam name="T">The target element type.</typeparam>
    /// <param name="label">Catalog entry label.</param>
    /// <param name="pagedQueryFunc">
    /// Delegate accepting <c>(filter, cursor, pageSize, cancellationToken)</c>.
    /// </param>
    /// <param name="pagination">Relay pagination strategy.</param>
    /// <param name="pageSize">Number of items per page. Defaults to 100.</param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty result set is valid.</param>
    public static Item<GqlQuery<TFilter, TResult, T>> PagedQuery<TFilter, TResult, T>(
      string label,
      Func<
        TFilter?,
        string?,
        int,
        CancellationToken,
        Task<IOperationResult<TResult>>
      > pagedQueryFunc,
      RelayPaginationStrategy<TResult, T> pagination,
      int pageSize = 100,
      bool allowEmptyData = false
    )
      where TFilter : class
      where TResult : class
      where T : class
    {
      var query = new GqlQuery<TFilter, TResult, T>(
        label,
        pagedQueryFunc,
        pagination,
        pageSize,
        allowEmptyData
      );
      return new Item<GqlQuery<TFilter, TResult, T>>(
        label,
        new GqlQueryStorageAdapter<TFilter, TResult, T>(query)
      );
    }

    // ── Filtered — Offset-paginated ────────────────────────────────────────

    /// <summary>
    /// Creates a deferred offset-paginated GQL catalog entry that accepts a filter input type.
    /// </summary>
    /// <typeparam name="TFilter">The StrawberryShake-generated filter input type.</typeparam>
    /// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
    /// <typeparam name="T">The target element type.</typeparam>
    /// <param name="label">Catalog entry label.</param>
    /// <param name="pagedQueryFunc">
    /// Delegate accepting <c>(filter, offset, limit, cancellationToken)</c>.
    /// </param>
    /// <param name="pagination">Offset pagination strategy.</param>
    /// <param name="pageSize">Number of items per page. Defaults to 100.</param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty result set is valid.</param>
    public static Item<GqlQuery<TFilter, TResult, T>> PagedQuery<TFilter, TResult, T>(
      string label,
      Func<TFilter?, int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
      OffsetPaginationStrategy<TResult, T> pagination,
      int pageSize = 100,
      bool allowEmptyData = false
    )
      where TFilter : class
      where TResult : class
      where T : class
    {
      var query = new GqlQuery<TFilter, TResult, T>(
        label,
        pagedQueryFunc,
        pagination,
        pageSize,
        allowEmptyData
      );
      return new Item<GqlQuery<TFilter, TResult, T>>(
        label,
        new GqlQueryStorageAdapter<TFilter, TResult, T>(query)
      );
    }
  }
}
