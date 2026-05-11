using Flowthru.Data.Storage.Gql;
using StrawberryShake;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Extension methods that contribute GraphQL smart constructors into
/// <see cref="ItemFactory.Singleton"/> and
/// <see cref="ItemFactory.Enumerable"/>. End users see them via a
/// single <c>using Flowthru.Data.Catalog;</c> import.
/// </summary>
/// <remarks>
/// <para>
/// Three families of smart constructor:
/// </para>
/// <list type="bullet">
/// <item>
/// <c>ItemFactory.Singleton.GqlQuery&lt;TResult, T&gt;</c> — single-item
/// query, optionally read-write via a mutation delegate.
/// </item>
/// <item>
/// <c>ItemFactory.Enumerable.GqlQuery&lt;TResult, T&gt;</c> /
/// <c>ItemFactory.Enumerable.GqlPagedQuery&lt;TResult, T&gt;</c> —
/// eagerly-materialised collection, non-paginated or
/// Relay/offset-paginated. Read-only.
/// </item>
/// <item>
/// <c>ItemFactory.Enumerable.GqlDeferredQuery&lt;...&gt;</c> /
/// <c>GqlDeferredPagedQuery&lt;...&gt;</c> — deferred query handle
/// (<see cref="Storage.Gql.GqlQuery{TResult,T}"/> /
/// <see cref="Storage.Gql.GqlQuery{TFilter,TResult,T}"/>); steps decide when to
/// materialise. Filter-typed overloads add a typed-filter input that
/// the step applies via <c>WithFilter</c> before materialisation.
/// </item>
/// </list>
/// </remarks>
public static class GqlItemFactoryExtensions
{
  // ── Singleton.GqlQuery — single-item ─────────────────────────────────

  /// <summary>
  /// Read-only single-item catalog item from a StrawberryShake query.
  /// </summary>
  /// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
  /// <typeparam name="T">The target type surfaced to the catalog item.</typeparam>
  /// <param name="factory">Factory anchor — discriminates the extension target.</param>
  /// <param name="label">Catalog label for DAG resolution.</param>
  /// <param name="queryFunc">Delegate that executes the StrawberryShake query operation.</param>
  /// <param name="selectData">Projects the result envelope to the target type.</param>
  /// <param name="allowEmptyData">If <c>true</c>, null data passes pre-flight inspection.</param>
  public static IItem<T> GqlQuery<TResult, T>(
    this SingletonItemFactory factory,
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, T> selectData,
    bool allowEmptyData = false
  )
    where TResult : class
    where T : class =>
    new Item<T>(
      label,
      new GqlSingleStorageAdapter<TResult, T>(label, queryFunc, selectData, allowEmptyData: allowEmptyData)
    );

  /// <summary>
  /// Read-write single-item catalog item from a StrawberryShake
  /// query and mutation.
  /// </summary>
  public static IItem<T> GqlQuery<TResult, T>(
    this SingletonItemFactory factory,
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, T> selectData,
    Func<T, CancellationToken, Task<IOperationResult>> mutationFunc,
    bool allowEmptyData = false
  )
    where TResult : class
    where T : class =>
    new Item<T>(
      label,
      new GqlSingleStorageAdapter<TResult, T>(label, queryFunc, selectData, mutationFunc, allowEmptyData)
    );

  // ── Enumerable.GqlQuery / GqlPagedQuery — eager collection ───────────

  /// <summary>
  /// Non-paginated collection catalog item — server returns all
  /// results in one response.
  /// </summary>
  public static IItem<IEnumerable<T>> GqlQuery<TResult, T>(
    this EnumerableItemFactory factory,
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, IEnumerable<T>?> selectData,
    bool allowEmptyData = false
  )
    where TResult : class
    where T : class =>
    new Item<IEnumerable<T>>(
      label,
      new GqlEnumerableStorageAdapter<TResult, T>(label, queryFunc, selectData, allowEmptyData)
    );

  /// <summary>
  /// Relay cursor-paginated collection catalog item. The adapter
  /// iterates pages until <c>HasNextPage</c> is false, yielding a
  /// flat <c>IEnumerable&lt;T&gt;</c>.
  /// </summary>
  public static IItem<IEnumerable<T>> GqlPagedQuery<TResult, T>(
    this EnumerableItemFactory factory,
    string label,
    Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    RelayPaginationStrategy<TResult, T> pagination,
    int pageSize = 100,
    bool allowEmptyData = false
  )
    where TResult : class
    where T : class =>
    new Item<IEnumerable<T>>(
      label,
      new GqlEnumerableStorageAdapter<TResult, T>(label, pagedQueryFunc, pagination, pageSize, allowEmptyData)
    );

  /// <summary>
  /// Offset-paginated collection catalog item. The adapter advances
  /// the offset until all items (per <c>getTotal</c>) are fetched.
  /// </summary>
  public static IItem<IEnumerable<T>> GqlPagedQuery<TResult, T>(
    this EnumerableItemFactory factory,
    string label,
    Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    OffsetPaginationStrategy<TResult, T> pagination,
    int pageSize = 100,
    bool allowEmptyData = false
  )
    where TResult : class
    where T : class =>
    new Item<IEnumerable<T>>(
      label,
      new GqlEnumerableStorageAdapter<TResult, T>(label, pagedQueryFunc, pagination, pageSize, allowEmptyData)
    );

  // ── Enumerable.GqlDeferredQuery / GqlDeferredPagedQuery — deferred ────

  /// <summary>
  /// Deferred non-paginated GQL catalog item. Catalog wires the
  /// query; the step decides when to materialise via
  /// <see cref="GqlQuery{TResult,T}.ToListAsync"/>.
  /// </summary>
  public static IItem<GqlQuery<TResult, T>> GqlDeferredQuery<TResult, T>(
    this EnumerableItemFactory factory,
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

  /// <summary>Deferred Relay cursor-paginated GQL catalog item.</summary>
  public static IItem<GqlQuery<TResult, T>> GqlDeferredPagedQuery<TResult, T>(
    this EnumerableItemFactory factory,
    string label,
    Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    RelayPaginationStrategy<TResult, T> pagination,
    int pageSize = 100,
    bool allowEmptyData = false
  )
    where TResult : class
    where T : class
  {
    var query = new GqlQuery<TResult, T>(label, pagedQueryFunc, pagination, pageSize, allowEmptyData);
    return new Item<GqlQuery<TResult, T>>(label, new GqlQueryStorageAdapter<TResult, T>(query));
  }

  /// <summary>Deferred offset-paginated GQL catalog item.</summary>
  public static IItem<GqlQuery<TResult, T>> GqlDeferredPagedQuery<TResult, T>(
    this EnumerableItemFactory factory,
    string label,
    Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    OffsetPaginationStrategy<TResult, T> pagination,
    int pageSize = 100,
    bool allowEmptyData = false
  )
    where TResult : class
    where T : class
  {
    var query = new GqlQuery<TResult, T>(label, pagedQueryFunc, pagination, pageSize, allowEmptyData);
    return new Item<GqlQuery<TResult, T>>(label, new GqlQueryStorageAdapter<TResult, T>(query));
  }

  /// <summary>
  /// Deferred non-paginated GQL catalog item that accepts a typed
  /// filter input. The catalog declares the query without a filter;
  /// steps apply one via
  /// <see cref="GqlQuery{TFilter,TResult,T}.WithFilter"/>.
  /// </summary>
  public static IItem<GqlQuery<TFilter, TResult, T>> GqlDeferredQuery<TFilter, TResult, T>(
    this EnumerableItemFactory factory,
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
      label, new GqlQueryStorageAdapter<TFilter, TResult, T>(query)
    );
  }

  /// <summary>
  /// Deferred Relay cursor-paginated GQL catalog item that accepts a
  /// typed filter input.
  /// </summary>
  public static IItem<GqlQuery<TFilter, TResult, T>> GqlDeferredPagedQuery<TFilter, TResult, T>(
    this EnumerableItemFactory factory,
    string label,
    Func<TFilter?, string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    RelayPaginationStrategy<TResult, T> pagination,
    int pageSize = 100,
    bool allowEmptyData = false
  )
    where TFilter : class
    where TResult : class
    where T : class
  {
    var query = new GqlQuery<TFilter, TResult, T>(
      label, pagedQueryFunc, pagination, pageSize, allowEmptyData
    );
    return new Item<GqlQuery<TFilter, TResult, T>>(
      label, new GqlQueryStorageAdapter<TFilter, TResult, T>(query)
    );
  }

  /// <summary>
  /// Deferred offset-paginated GQL catalog item that accepts a typed
  /// filter input.
  /// </summary>
  public static IItem<GqlQuery<TFilter, TResult, T>> GqlDeferredPagedQuery<TFilter, TResult, T>(
    this EnumerableItemFactory factory,
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
      label, pagedQueryFunc, pagination, pageSize, allowEmptyData
    );
    return new Item<GqlQuery<TFilter, TResult, T>>(
      label, new GqlQueryStorageAdapter<TFilter, TResult, T>(query)
    );
  }
}
