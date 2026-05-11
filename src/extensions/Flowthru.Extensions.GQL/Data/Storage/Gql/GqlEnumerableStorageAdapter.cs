using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using StrawberryShake;

namespace Flowthru.Data.Storage.Gql;

/// <summary>
/// Storage adapter for a collection GraphQL query using a
/// StrawberryShake client. Supports non-paginated queries (server
/// returns all results in one response), Relay cursor-paginated
/// queries, and offset-paginated queries.
/// </summary>
/// <typeparam name="TResult">
/// The StrawberryShake-generated result data type
/// (e.g. <c>IGetSessionsResult</c>).
/// </typeparam>
/// <typeparam name="T">
/// The target element type surfaced to the catalog item.
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Non-paginated.</strong> Provide a <c>queryFunc</c>
/// accepting only a <see cref="CancellationToken"/>; results load in
/// one request.
/// </para>
/// <para>
/// <strong>Relay paginated.</strong> Provide a <c>queryFunc</c>
/// accepting <c>(cursor, pageSize, ct)</c> and a
/// <see cref="RelayPaginationStrategy{TResult,T}"/>; the adapter
/// iterates pages until <c>HasNextPage</c> is false.
/// </para>
/// <para>
/// <strong>Offset paginated.</strong> Provide a <c>queryFunc</c>
/// accepting <c>(offset, limit, ct)</c> and an
/// <see cref="OffsetPaginationStrategy{TResult,T}"/>; the adapter
/// advances the offset until <c>getTotal</c> is reached or a page
/// returns no items.
/// </para>
/// <para>
/// <strong>Pre-flight inspection.</strong>
/// <see cref="InspectShallow"/> executes a one-item probe
/// (<c>pageSize=1</c> for paginated modes) to validate connectivity,
/// authentication, and schema compatibility.
/// <see cref="InspectDeep"/> executes the full pagination loop.
/// </para>
/// </remarks>
public sealed class GqlEnumerableStorageAdapter<TResult, T> : IStorageAdapter<IEnumerable<T>>
  where TResult : class
  where T : class
{
  // Non-paginated
  private readonly Func<CancellationToken, Task<IOperationResult<TResult>>>? _queryFunc;
  private readonly Func<TResult, IEnumerable<T>?>? _selectData;

  // Relay paginated
  private readonly Func<
    string?,
    int,
    CancellationToken,
    Task<IOperationResult<TResult>>
  >? _relayQueryFunc;
  private readonly RelayPaginationStrategy<TResult, T>? _relayPagination;

  // Offset paginated
  private readonly Func<
    int,
    int,
    CancellationToken,
    Task<IOperationResult<TResult>>
  >? _offsetQueryFunc;
  private readonly OffsetPaginationStrategy<TResult, T>? _offsetPagination;

  private readonly string _label;
  private readonly int _pageSize;
  private readonly bool _allowEmptyData;

  /// <summary>Non-paginated collection adapter.</summary>
  public GqlEnumerableStorageAdapter(
    string label,
    Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc,
    Func<TResult, IEnumerable<T>?> selectData,
    bool allowEmptyData = false
  )
  {
    _label = label ?? throw new ArgumentNullException(nameof(label));
    _queryFunc = queryFunc ?? throw new ArgumentNullException(nameof(queryFunc));
    _selectData = selectData ?? throw new ArgumentNullException(nameof(selectData));
    _allowEmptyData = allowEmptyData;
    _pageSize = 0;

    Traits = new StorageTraits { CanWrite = false, IsPersistent = false };
  }

  /// <summary>Relay cursor-paginated collection adapter.</summary>
  public GqlEnumerableStorageAdapter(
    string label,
    Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    RelayPaginationStrategy<TResult, T> pagination,
    int pageSize = 100,
    bool allowEmptyData = false
  )
  {
    _label = label ?? throw new ArgumentNullException(nameof(label));
    _relayQueryFunc = pagedQueryFunc ?? throw new ArgumentNullException(nameof(pagedQueryFunc));
    _relayPagination = pagination ?? throw new ArgumentNullException(nameof(pagination));
    _allowEmptyData = allowEmptyData;
    _pageSize = pageSize;

    Traits = new StorageTraits { CanWrite = false, IsPersistent = false };
  }

  /// <summary>Offset-paginated collection adapter.</summary>
  public GqlEnumerableStorageAdapter(
    string label,
    Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
    OffsetPaginationStrategy<TResult, T> pagination,
    int pageSize = 100,
    bool allowEmptyData = false
  )
  {
    _label = label ?? throw new ArgumentNullException(nameof(label));
    _offsetQueryFunc = pagedQueryFunc ?? throw new ArgumentNullException(nameof(pagedQueryFunc));
    _offsetPagination = pagination ?? throw new ArgumentNullException(nameof(pagination));
    _allowEmptyData = allowEmptyData;
    _pageSize = pageSize;

    Traits = new StorageTraits { CanWrite = false, IsPersistent = false };
  }

  /// <inheritdoc/>
  public StorageTraits Traits { get; }

  /// <inheritdoc/>
  public FlowIO<IEnumerable<T>> Load() =>
    FlowIO.LiftAsync(async ct =>
    {
      if (_queryFunc is not null && _selectData is not null)
        return await LoadNonPaged(ct).ConfigureAwait(false);

      if (_relayQueryFunc is not null && _relayPagination is not null)
        return await LoadRelayPaged(ct).ConfigureAwait(false);

      if (_offsetQueryFunc is not null && _offsetPagination is not null)
        return await LoadOffsetPaged(ct).ConfigureAwait(false);

      throw new InvalidOperationException(
        $"GQL adapter '{_label}' is in an invalid state: no query delegate configured."
      );
    }, source: $"GqlEnumerableStorageAdapter.Load[{_label}]");

  private async Task<IEnumerable<T>> LoadNonPaged(CancellationToken ct)
  {
    var result = await _queryFunc!(ct).ConfigureAwait(false);
    result.EnsureNoErrors();

    var items = result.Data is not null ? _selectData!(result.Data) : null;

    if ((items is null || !items.Any()) && !_allowEmptyData)
    {
      throw new InvalidOperationException(
        $"GraphQL query for '{_label}' returned an empty collection. "
          + "Set allowEmptyData: true if this is a valid state."
      );
    }

    return items ?? Enumerable.Empty<T>();
  }

  private async Task<IEnumerable<T>> LoadRelayPaged(CancellationToken ct)
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
        result = await _relayQueryFunc!(cursor, _pageSize, ct).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(
          $"GraphQL query for '{_label}' failed on page {pageNumber} (cursor: {cursor ?? "null"}).",
          ex
        );
      }

      if (result.Errors.Any())
      {
        var details = string.Join("; ", result.Errors.Select(e => e.Message));
        throw new InvalidOperationException(
          $"GraphQL query for '{_label}' returned errors on page {pageNumber} "
            + $"(cursor: {cursor ?? "null"}): {details}"
        );
      }

      if (result.Data is null) break;

      var pageInfo = _relayPagination!.GetPageInfo(result.Data);
      var nodes = _relayPagination.GetNodes(result.Data);

      if (nodes is not null) all.AddRange(nodes);

      if (pageInfo is null || !pageInfo.HasNextPage) break;

      cursor = pageInfo.EndCursor;
    }

    if (all.Count == 0 && !_allowEmptyData)
    {
      throw new InvalidOperationException(
        $"GraphQL query for '{_label}' returned an empty collection across all pages. "
          + "Set allowEmptyData: true if this is a valid state."
      );
    }

    return all;
  }

  private async Task<IEnumerable<T>> LoadOffsetPaged(CancellationToken ct)
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
        result = await _offsetQueryFunc!(offset, _pageSize, ct).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(
          $"GraphQL query for '{_label}' failed on page {pageNumber} (offset: {offset}).",
          ex
        );
      }

      if (result.Errors.Any())
      {
        var details = string.Join("; ", result.Errors.Select(e => e.Message));
        throw new InvalidOperationException(
          $"GraphQL query for '{_label}' returned errors on page {pageNumber} "
            + $"(offset: {offset}): {details}"
        );
      }

      if (result.Data is null) break;

      total ??= _offsetPagination!.GetTotal(result.Data);

      var items = _offsetPagination!.GetItems(result.Data);
      if (items is null || !items.Any()) break;

      all.AddRange(items);
      offset += _pageSize;

      if (total.HasValue && all.Count >= total.Value) break;
    }

    if (all.Count == 0 && !_allowEmptyData)
    {
      throw new InvalidOperationException(
        $"GraphQL query for '{_label}' returned an empty collection across all pages. "
          + "Set allowEmptyData: true if this is a valid state."
      );
    }

    return all;
  }

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(IEnumerable<T> data) =>
    FlowIO.Fail<FlowUnit>(new RuntimeError.External(
      $"GqlEnumerableStorageAdapter.Save[{_label}]",
      new NotSupportedException(
        $"GQL collection items are read-only. '{_label}' does not support Save(). "
          + "Use a mutation-enabled GqlSingleStorageAdapter for write operations."
      )));

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async ct =>
    {
      try
      {
        if (_queryFunc is not null)
        {
          var result = await _queryFunc(ct).ConfigureAwait(false);
          return !result.Errors.Any() && result.Data is not null;
        }

        if (_relayQueryFunc is not null)
        {
          var result = await _relayQueryFunc(null, 1, ct).ConfigureAwait(false);
          return !result.Errors.Any() && result.Data is not null;
        }

        if (_offsetQueryFunc is not null)
        {
          var result = await _offsetQueryFunc(0, 1, ct).ConfigureAwait(false);
          return !result.Errors.Any() && result.Data is not null;
        }

        return false;
      }
      catch
      {
        return false;
      }
    }, source: $"GqlEnumerableStorageAdapter.Exists[{_label}]");

  /// <inheritdoc/>
  /// <remarks>
  /// Executes a one-item probe against the live endpoint. For
  /// paginated modes this uses <c>pageSize=1</c> to minimise data
  /// transfer during pre-flight.
  /// </remarks>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync(async ct =>
    {
      try
      {
        return await ProbeEndpoint(probeSize: Math.Max(sampleSize, 1), ct).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        return ValidationResult.FromException(_label, ex);
      }
    }, source: $"GqlEnumerableStorageAdapter.InspectShallow[{_label}]");

  /// <inheritdoc/>
  /// <remarks>
  /// Executes the full pagination loop to validate that every page
  /// deserialises without errors. For large datasets this may be
  /// expensive; prefer <see cref="InspectShallow"/> for routine
  /// pre-flight validation.
  /// </remarks>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync(async ct =>
    {
      var loadResult = await Load().Run(ct).ConfigureAwait(false);
      return loadResult switch
      {
        EffResult<IEnumerable<T>>.Success => ValidationResult.Success(),
        EffResult<IEnumerable<T>>.Failure f => ValidationResult.Failure(
          catalogKey: _label,
          errorType: ValidationErrorType.InspectionFailure,
          message: $"Deep inspection of '{_label}' failed.",
          details: f.Error.ToString()
        ),
        _ => ValidationResult.Success(),
      };
    }, source: $"GqlEnumerableStorageAdapter.InspectDeep[{_label}]");

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());

  private async Task<ValidationResult> ProbeEndpoint(int probeSize, CancellationToken ct)
  {
    IOperationResult<TResult> result;
    try
    {
      result =
        _queryFunc is not null ? await _queryFunc(ct).ConfigureAwait(false)
        : _relayQueryFunc is not null ? await _relayQueryFunc(null, probeSize, ct).ConfigureAwait(false)
        : await _offsetQueryFunc!(0, probeSize, ct).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      return ValidationResult.Failure(
        catalogKey: _label,
        errorType: ValidationErrorType.NotFound,
        message: $"GraphQL endpoint for '{_label}' is unreachable.",
        details: ex.Message
      );
    }

    if (result.Errors.Any())
    {
      var details = string.Join("; ", result.Errors.Select(e => e.Message));
      return ValidationResult.Failure(
        catalogKey: _label,
        errorType: ValidationErrorType.InspectionFailure,
        message: $"GraphQL query for '{_label}' returned errors.",
        details: details
      );
    }

    if (result.Data is null && !_allowEmptyData)
    {
      return ValidationResult.Failure(
        catalogKey: _label,
        errorType: ValidationErrorType.EmptyDataset,
        message: $"GraphQL query for '{_label}' returned null data.",
        details: "Set allowEmptyData: true when creating the catalog item if null data is valid for this query."
      );
    }

    return ValidationResult.Success();
  }
}
