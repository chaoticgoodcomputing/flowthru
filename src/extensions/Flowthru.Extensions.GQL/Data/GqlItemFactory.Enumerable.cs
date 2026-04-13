using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Data;

/// <summary>
/// Factory methods for creating collection GQL catalog entries.
/// </summary>
public static partial class GqlItemFactory
{
    /// <summary>
    /// Factory methods for <see cref="Item{T}"/> backed by a collection GraphQL query.
    /// </summary>
    public static class Enumerable
    {
        /// <summary>
        /// Creates a non-paginated collection catalog entry from a StrawberryShake query.
        /// The server is expected to return all results in a single response.
        /// </summary>
        /// <typeparam name="TResult">
        /// The StrawberryShake-generated result data type (e.g. <c>IGetUsersResult</c>).
        /// </typeparam>
        /// <typeparam name="T">
        /// The target element type (e.g. <c>GetUsers_User</c>).
        /// </typeparam>
        /// <param name="label">Catalog entry label used in the pipeline DAG and validation messages.</param>
        /// <param name="queryFunc">Delegate that executes the StrawberryShake query operation.</param>
        /// <param name="selectData">
        /// Projects the result data envelope to the collection of <typeparamref name="T"/>.
        /// Return <c>null</c> to yield empty (subject to <paramref name="allowEmptyData"/>).
        /// </param>
        /// <param name="allowEmptyData">
        /// If <c>true</c>, an empty or null result collection is valid during pre-flight inspection.
        /// Defaults to <c>false</c>.
        /// </param>
        public static Item<IEnumerable<T>> Query<TResult, T>(
            string label,
            Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
            Func<TResult, IEnumerable<T>?> selectData,
            bool allowEmptyData = false
        )
            where TResult : class
            where T : class
        {
            var adapter = new GqlEnumerableStorageAdapter<TResult, T>(
                label,
                queryFunc,
                selectData,
                allowEmptyData
            );
            return new Item<IEnumerable<T>>(label, adapter);
        }

        /// <summary>
        /// Creates a Relay cursor-paginated collection catalog entry.
        /// The adapter iterates pages until <c>HasNextPage</c> is <c>false</c>, yielding
        /// a flat <c>IEnumerable&lt;T&gt;</c> to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">
        /// The StrawberryShake-generated result data type (e.g. <c>IGetSessionsResult</c>).
        /// </typeparam>
        /// <typeparam name="T">
        /// The target element type (e.g. <c>GetSessions_Session</c>).
        /// </typeparam>
        /// <param name="label">Catalog entry label used in the pipeline DAG and validation messages.</param>
        /// <param name="pagedQueryFunc">
        /// Delegate accepting <c>(cursor, pageSize, cancellationToken)</c>. Map <c>cursor</c> to
        /// the GraphQL <c>after</c> argument and <c>pageSize</c> to <c>first</c>.
        /// </param>
        /// <param name="pagination">
        /// Relay pagination strategy created via <see cref="Pagination.Relay{TResult,T}"/>.
        /// </param>
        /// <param name="pageSize">Items to fetch per page. Defaults to 100.</param>
        /// <param name="allowEmptyData">
        /// If <c>true</c>, an empty result set is valid during pre-flight inspection.
        /// Defaults to <c>false</c>.
        /// </param>
        public static Item<IEnumerable<T>> PagedQuery<TResult, T>(
            string label,
            Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
            RelayPaginationStrategy<TResult, T> pagination,
            int pageSize = 100,
            bool allowEmptyData = false
        )
            where TResult : class
            where T : class
        {
            var adapter = new GqlEnumerableStorageAdapter<TResult, T>(
                label,
                pagedQueryFunc,
                pagination,
                pageSize,
                allowEmptyData
            );
            return new Item<IEnumerable<T>>(label, adapter);
        }

        /// <summary>
        /// Creates an offset-paginated collection catalog entry.
        /// The adapter advances the offset until all items (per <c>getTotal</c>) are fetched
        /// or a page returns no items, yielding a flat <c>IEnumerable&lt;T&gt;</c> to the pipeline.
        /// </summary>
        /// <typeparam name="TResult">
        /// The StrawberryShake-generated result data type (e.g. <c>IGetProductsResult</c>).
        /// </typeparam>
        /// <typeparam name="T">
        /// The target element type (e.g. <c>GetProducts_Product</c>).
        /// </typeparam>
        /// <param name="label">Catalog entry label used in the pipeline DAG and validation messages.</param>
        /// <param name="pagedQueryFunc">
        /// Delegate accepting <c>(offset, limit, cancellationToken)</c>. Map directly to the
        /// GraphQL <c>skip</c>/<c>take</c> (or equivalent) arguments.
        /// </param>
        /// <param name="pagination">
        /// Offset pagination strategy created via <see cref="Pagination.Offset{TResult,T}"/>.
        /// </param>
        /// <param name="pageSize">Items to fetch per page. Defaults to 100.</param>
        /// <param name="allowEmptyData">
        /// If <c>true</c>, an empty result set is valid during pre-flight inspection.
        /// Defaults to <c>false</c>.
        /// </param>
        public static Item<IEnumerable<T>> PagedQuery<TResult, T>(
            string label,
            Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
            OffsetPaginationStrategy<TResult, T> pagination,
            int pageSize = 100,
            bool allowEmptyData = false
        )
            where TResult : class
            where T : class
        {
            var adapter = new GqlEnumerableStorageAdapter<TResult, T>(
                label,
                pagedQueryFunc,
                pagination,
                pageSize,
                allowEmptyData
            );
            return new Item<IEnumerable<T>>(label, adapter);
        }
    }
}
