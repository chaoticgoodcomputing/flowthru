using StrawberryShake;

namespace Flowthru.Extensions.GQL.Data;

/// <summary>
/// Internal pagination execution helpers shared by <see cref="GqlQuery{TResult,T}"/>
/// and <see cref="GqlQuery{TFilter,TResult,T}"/>.
/// </summary>
internal static class GqlQueryExecutor
{
  // ── Unfiltered variants ────────────────────────────────────────────────────

  internal static async Task<List<T>> NonPagedAsync<TResult, T>(
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, IEnumerable<T>?> selectData,
    bool allowEmptyData,
    CancellationToken ct
  )
    where TResult : class
    where T : class
  {
    var result = await queryFunc(ct);
    result.EnsureNoErrors();

    var items = result.Data is not null ? selectData(result.Data) : null;

    if ((items is null || !items.Any()) && !allowEmptyData)
      throw new InvalidOperationException(
        $"GraphQL query for '{label}' returned an empty collection. "
          + "Set allowEmptyData: true if this is a valid state."
      );

    return items?.ToList() ?? [];
  }

  internal static async Task<List<T>> RelayPagedAsync<TResult, T>(
    string label,
    Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    RelayPaginationStrategy<TResult, T> pagination,
    int pageSize,
    bool allowEmptyData,
    CancellationToken ct
  )
    where TResult : class
    where T : class
  {
    var all = new List<T>();
    string? cursor = null;
    int pageNumber = 0;

    while (true)
    {
      pageNumber++;
      IOperationResult<TResult> result;
      try
      {
        result = await queryFunc(cursor, pageSize, ct);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(
          $"GraphQL query for '{label}' failed on page {pageNumber} (cursor: {cursor ?? "null"}).",
          ex
        );
      }

      if (result.Errors.Any())
        throw new InvalidOperationException(
          $"GraphQL query for '{label}' returned errors on page {pageNumber}: "
            + string.Join("; ", result.Errors.Select(e => e.Message))
        );

      if (result.Data is null)
        break;

      var nodes = pagination.GetNodes(result.Data);
      if (nodes is not null)
        all.AddRange(nodes);

      var pageInfo = pagination.GetPageInfo(result.Data);
      if (pageInfo is null || !pageInfo.HasNextPage)
        break;

      cursor = pageInfo.EndCursor;
    }

    if (all.Count == 0 && !allowEmptyData)
      throw new InvalidOperationException(
        $"GraphQL query for '{label}' returned an empty collection across all pages. "
          + "Set allowEmptyData: true if this is a valid state."
      );

    return all;
  }

  internal static async Task<List<T>> OffsetPagedAsync<TResult, T>(
    string label,
    Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    OffsetPaginationStrategy<TResult, T> pagination,
    int pageSize,
    bool allowEmptyData,
    CancellationToken ct
  )
    where TResult : class
    where T : class
  {
    var all = new List<T>();
    int offset = 0;
    int? total = null;
    int pageNumber = 0;

    while (true)
    {
      pageNumber++;
      IOperationResult<TResult> result;
      try
      {
        result = await queryFunc(offset, pageSize, ct);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(
          $"GraphQL query for '{label}' failed on page {pageNumber} (offset: {offset}).",
          ex
        );
      }

      if (result.Errors.Any())
        throw new InvalidOperationException(
          $"GraphQL query for '{label}' returned errors on page {pageNumber}: "
            + string.Join("; ", result.Errors.Select(e => e.Message))
        );

      if (result.Data is null)
        break;

      total ??= pagination.GetTotal(result.Data);
      var items = pagination.GetItems(result.Data);
      if (items is null || !items.Any())
        break;

      all.AddRange(items);
      offset += pageSize;

      if (total.HasValue && all.Count >= total.Value)
        break;
    }

    if (all.Count == 0 && !allowEmptyData)
      throw new InvalidOperationException(
        $"GraphQL query for '{label}' returned an empty collection across all pages. "
          + "Set allowEmptyData: true if this is a valid state."
      );

    return all;
  }

  // ── Filtered variants ──────────────────────────────────────────────────────

  internal static async Task<List<T>> FilteredNonPagedAsync<TFilter, TResult, T>(
    string label,
    Func<TFilter?, CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    TFilter? filter,
    Func<TResult, IEnumerable<T>?> selectData,
    bool allowEmptyData,
    CancellationToken ct
  )
    where TFilter : class
    where TResult : class
    where T : class
  {
    var result = await queryFunc(filter, ct);
    result.EnsureNoErrors();

    var items = result.Data is not null ? selectData(result.Data) : null;

    if ((items is null || !items.Any()) && !allowEmptyData)
      throw new InvalidOperationException(
        $"GraphQL query for '{label}' returned an empty collection. "
          + "Set allowEmptyData: true if this is a valid state."
      );

    return items?.ToList() ?? [];
  }

  internal static async Task<List<T>> FilteredRelayPagedAsync<TFilter, TResult, T>(
    string label,
    Func<TFilter?, string?, int, CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    TFilter? filter,
    RelayPaginationStrategy<TResult, T> pagination,
    int pageSize,
    bool allowEmptyData,
    CancellationToken ct
  )
    where TFilter : class
    where TResult : class
    where T : class
  {
    var all = new List<T>();
    string? cursor = null;
    int pageNumber = 0;

    while (true)
    {
      pageNumber++;
      IOperationResult<TResult> result;
      try
      {
        result = await queryFunc(filter, cursor, pageSize, ct);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(
          $"GraphQL query for '{label}' failed on page {pageNumber} (cursor: {cursor ?? "null"}).",
          ex
        );
      }

      if (result.Errors.Any())
        throw new InvalidOperationException(
          $"GraphQL query for '{label}' returned errors on page {pageNumber}: "
            + string.Join("; ", result.Errors.Select(e => e.Message))
        );

      if (result.Data is null)
        break;

      var nodes = pagination.GetNodes(result.Data);
      if (nodes is not null)
        all.AddRange(nodes);

      var pageInfo = pagination.GetPageInfo(result.Data);
      if (pageInfo is null || !pageInfo.HasNextPage)
        break;

      cursor = pageInfo.EndCursor;
    }

    if (all.Count == 0 && !allowEmptyData)
      throw new InvalidOperationException(
        $"GraphQL query for '{label}' returned an empty collection across all pages. "
          + "Set allowEmptyData: true if this is a valid state."
      );

    return all;
  }

  internal static async Task<List<T>> FilteredOffsetPagedAsync<TFilter, TResult, T>(
    string label,
    Func<TFilter?, int, int, CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    TFilter? filter,
    OffsetPaginationStrategy<TResult, T> pagination,
    int pageSize,
    bool allowEmptyData,
    CancellationToken ct
  )
    where TFilter : class
    where TResult : class
    where T : class
  {
    var all = new List<T>();
    int offset = 0;
    int? total = null;
    int pageNumber = 0;

    while (true)
    {
      pageNumber++;
      IOperationResult<TResult> result;
      try
      {
        result = await queryFunc(filter, offset, pageSize, ct);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(
          $"GraphQL query for '{label}' failed on page {pageNumber} (offset: {offset}).",
          ex
        );
      }

      if (result.Errors.Any())
        throw new InvalidOperationException(
          $"GraphQL query for '{label}' returned errors on page {pageNumber}: "
            + string.Join("; ", result.Errors.Select(e => e.Message))
        );

      if (result.Data is null)
        break;

      total ??= pagination.GetTotal(result.Data);
      var items = pagination.GetItems(result.Data);
      if (items is null || !items.Any())
        break;

      all.AddRange(items);
      offset += pageSize;

      if (total.HasValue && all.Count >= total.Value)
        break;
    }

    if (all.Count == 0 && !allowEmptyData)
      throw new InvalidOperationException(
        $"GraphQL query for '{label}' returned an empty collection across all pages. "
          + "Set allowEmptyData: true if this is a valid state."
      );

    return all;
  }

  // ── Probe helper (used by GqlQueryStorageAdapter.InspectShallow) ───────────

  internal static async Task<bool> ProbeAsync<TResult, T>(
    GqlQuery<TResult, T> query,
    CancellationToken ct
  )
    where TResult : class
    where T : class
  {
    try
    {
      // Use page size 1 for paginated, or just run non-paged as-is for a probe
      GqlQuery<TResult, T> probeQuery =
        query.RelayQueryFunc is not null
          ? new(query.Label, query.RelayQueryFunc, query.RelayPagination!, 1, allowEmptyData: true)
        : query.OffsetQueryFunc is not null
          ? new(
            query.Label,
            query.OffsetQueryFunc,
            query.OffsetPagination!,
            1,
            allowEmptyData: true
          )
        : query; // non-paged: run as-is

      await probeQuery.ToListAsync(ct);
      return true;
    }
    catch
    {
      return false;
    }
  }

  internal static async Task<bool> FilteredProbeAsync<TFilter, TResult, T>(
    GqlQuery<TFilter, TResult, T> query,
    CancellationToken ct
  )
    where TFilter : class
    where TResult : class
    where T : class
  {
    // Probe with null filter (validates connectivity without requiring a filter value)
    try
    {
      await query.WithFilter(null!).ToListAsync(ct);
      return true;
    }
    catch
    {
      return false;
    }
  }
}
