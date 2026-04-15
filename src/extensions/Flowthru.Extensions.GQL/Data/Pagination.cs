namespace Flowthru.Extensions.GQL.Data;

/// <summary>
/// Pagination metadata returned by a Relay-style GraphQL connection.
/// </summary>
/// <param name="HasNextPage">Whether a subsequent page exists.</param>
/// <param name="EndCursor">The opaque cursor identifying the end of the current page.</param>
public record PageInfo(bool HasNextPage, string? EndCursor);

/// <summary>
/// Defines the pagination strategy used by a paginated GQL catalog entry.
/// </summary>
/// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
/// <typeparam name="T">The target element type for the Flowthru catalog entry.</typeparam>
public abstract class PaginationStrategy<TResult, T>
  where TResult : class
  where T : class
{ }

/// <summary>
/// Relay cursor-based pagination strategy. Calls the query function with advancing
/// cursors until <c>HasNextPage</c> is false.
/// </summary>
/// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
/// <typeparam name="T">The target element type.</typeparam>
public sealed class RelayPaginationStrategy<TResult, T> : PaginationStrategy<TResult, T>
  where TResult : class
  where T : class
{
    internal Func<TResult, IEnumerable<T>?> GetNodes { get; }
    internal Func<TResult, PageInfo?> GetPageInfo { get; }

    internal RelayPaginationStrategy(
      Func<TResult, IEnumerable<T>?> getNodes,
      Func<TResult, PageInfo?> getPageInfo
    )
    {
        GetNodes = getNodes;
        GetPageInfo = getPageInfo;
    }
}

/// <summary>
/// Offset-based pagination strategy. Calls the query function with advancing offsets
/// until all items indicated by <c>getTotal</c> have been fetched.
/// </summary>
/// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
/// <typeparam name="T">The target element type.</typeparam>
public sealed class OffsetPaginationStrategy<TResult, T> : PaginationStrategy<TResult, T>
  where TResult : class
  where T : class
{
    internal Func<TResult, IEnumerable<T>?> GetItems { get; }
    internal Func<TResult, int?> GetTotal { get; }

    internal OffsetPaginationStrategy(
      Func<TResult, IEnumerable<T>?> getItems,
      Func<TResult, int?> getTotal
    )
    {
        GetItems = getItems;
        GetTotal = getTotal;
    }
}

/// <summary>
/// Factory for creating pagination strategies for paginated GQL catalog entries.
/// </summary>
public static class Pagination
{
    /// <summary>
    /// Creates a Relay cursor-based pagination strategy.
    /// </summary>
    /// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
    /// <typeparam name="T">The target element type.</typeparam>
    /// <param name="getNodes">Selects the item collection from the result page.</param>
    /// <param name="getPageInfo">Selects the <see cref="PageInfo"/> from the result page.</param>
    public static RelayPaginationStrategy<TResult, T> Relay<TResult, T>(
      Func<TResult, IEnumerable<T>?> getNodes,
      Func<TResult, PageInfo?> getPageInfo
    )
      where TResult : class
      where T : class => new(getNodes, getPageInfo);

    /// <summary>
    /// Creates an offset-based pagination strategy.
    /// </summary>
    /// <typeparam name="TResult">The StrawberryShake-generated result data type.</typeparam>
    /// <typeparam name="T">The target element type.</typeparam>
    /// <param name="getItems">Selects the item collection from the result page.</param>
    /// <param name="getTotal">Selects the total item count from the result (used to determine
    /// when to stop fetching pages). Return <c>null</c> to stop after the first empty page.</param>
    public static OffsetPaginationStrategy<TResult, T> Offset<TResult, T>(
      Func<TResult, IEnumerable<T>?> getItems,
      Func<TResult, int?> getTotal
    )
      where TResult : class
      where T : class => new(getItems, getTotal);
}
