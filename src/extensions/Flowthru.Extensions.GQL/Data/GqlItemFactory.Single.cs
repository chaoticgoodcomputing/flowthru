using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.GQL.Data;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Data;

/// <summary>
/// Factory methods for creating single-item GQL catalog entries.
/// </summary>
public static partial class GqlItemFactory
{
    /// <summary>
    /// Factory methods for <see cref="Item{T}"/> backed by a single-item GraphQL query.
    /// </summary>
    public static class Single
    {
        /// <summary>
        /// Creates a read-only single-item catalog entry from a StrawberryShake query.
        /// </summary>
        /// <typeparam name="TResult">
        /// The StrawberryShake-generated result data type (e.g. <c>IGetCurrentUserResult</c>).
        /// </typeparam>
        /// <typeparam name="T">
        /// The target type surfaced to the catalog entry (e.g. <c>GetCurrentUser_Me</c>).
        /// </typeparam>
        /// <param name="label">Catalog entry label used in the pipeline DAG and validation messages.</param>
        /// <param name="queryFunc">Delegate that executes the StrawberryShake query operation.</param>
        /// <param name="selectData">
        /// Projects the result data envelope to the target type.
        /// Use a null-forgiving operator (<c>r => r.Me!</c>) when the field is non-null by schema contract.
        /// </param>
        /// <param name="allowEmptyData">
        /// If <c>true</c>, a null <see cref="IOperationResult{TResultData}.Data"/> is treated as
        /// valid during pre-flight inspection. Defaults to <c>false</c>.
        /// </param>
        public static Item<T> Query<TResult, T>(
          string label,
          Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
          Func<TResult, T> selectData,
          bool allowEmptyData = false
        )
          where TResult : class
          where T : class
        {
            var adapter = new GqlStorageAdapter<TResult, T>(
              label,
              queryFunc,
              selectData,
              allowEmptyData: allowEmptyData
            );
            return new Item<T>(label, adapter);
        }

        /// <summary>
        /// Creates a read-write single-item catalog entry from a StrawberryShake query and mutation.
        /// </summary>
        /// <typeparam name="TResult">
        /// The StrawberryShake-generated result data type (e.g. <c>IGetCurrentUserResult</c>).
        /// </typeparam>
        /// <typeparam name="T">
        /// The target type surfaced to the catalog entry (e.g. <c>GetCurrentUser_Me</c>).
        /// </typeparam>
        /// <param name="label">Catalog entry label used in the pipeline DAG and validation messages.</param>
        /// <param name="queryFunc">Delegate that executes the StrawberryShake query operation.</param>
        /// <param name="selectData">Projects the result data envelope to the target type.</param>
        /// <param name="mutationFunc">
        /// Delegate that executes the StrawberryShake mutation when the catalog entry is saved.
        /// Enables <c>StorageTraits.CanWrite = true</c> on the resulting entry.
        /// </param>
        /// <param name="allowEmptyData">
        /// If <c>true</c>, a null <see cref="IOperationResult{TResultData}.Data"/> is treated as
        /// valid during pre-flight inspection. Defaults to <c>false</c>.
        /// </param>
        public static Item<T> Query<TResult, T>(
          string label,
          Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
          Func<TResult, T> selectData,
          Func<T, CancellationToken, Task<IOperationResult>> mutationFunc,
          bool allowEmptyData = false
        )
          where TResult : class
          where T : class
        {
            var adapter = new GqlStorageAdapter<TResult, T>(
              label,
              queryFunc,
              selectData,
              mutationFunc,
              allowEmptyData
            );
            return new Item<T>(label, adapter);
        }
    }
}
