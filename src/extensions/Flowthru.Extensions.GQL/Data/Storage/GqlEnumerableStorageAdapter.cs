using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Extensions.GQL.Data;
using StrawberryShake;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Storage adapter for a collection GraphQL query using a StrawberryShake client.
/// Supports both non-paginated queries (server returns all results in one response) and
/// paginated queries via <see cref="RelayPaginationStrategy{TResult,T}"/> or
/// <see cref="OffsetPaginationStrategy{TResult,T}"/>.
/// </summary>
/// <typeparam name="TResult">
/// The StrawberryShake-generated result data type (e.g. <c>IGetSessionsResult</c>).
/// </typeparam>
/// <typeparam name="T">
/// The target element type surfaced to the Flowthru catalog entry (e.g. <c>GetSessions_Session</c>).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Non-paginated mode:</strong> Provide a <c>queryFunc</c> that accepts only a
/// <see cref="CancellationToken"/>. Results are loaded in a single request.
/// </para>
/// <para>
/// <strong>Relay paginated mode:</strong> Provide a <c>queryFunc</c> accepting <c>(cursor, pageSize, ct)</c>
/// and a <see cref="RelayPaginationStrategy{TResult,T}"/>. The adapter iterates pages until
/// <c>HasNextPage</c> is false, concatenating nodes into a flat <c>IEnumerable&lt;T&gt;</c>.
/// </para>
/// <para>
/// <strong>Offset paginated mode:</strong> Provide a <c>queryFunc</c> accepting <c>(offset, limit, ct)</c>
/// and an <see cref="OffsetPaginationStrategy{TResult,T}"/>. The adapter advances the offset until
/// all items reported by <c>getTotal</c> have been fetched (or a page returns no items).
/// </para>
/// <para>
/// <strong>Pre-flight Validation:</strong>
/// </para>
/// <para>
/// <see cref="InspectShallow"/> executes a minimal one-item probe (<c>pageSize=1</c> / <c>limit=1</c>
/// for paginated modes) to validate endpoint reachability, authentication, and schema compatibility
/// before any pipeline step runs. <see cref="InspectDeep"/> executes the full pagination loop.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Non-paginated
/// var adapter = new GqlEnumerableStorageAdapter&lt;IGetUsersResult, GetUsers_User&gt;(
///     label: "users",
///     queryFunc: ct => _client.GetUsers.ExecuteAsync(ct),
///     selectData: r => r.Users ?? Enumerable.Empty&lt;GetUsers_User&gt;()
/// );
///
/// // Relay paginated
/// var adapter = new GqlEnumerableStorageAdapter&lt;IGetSessionsResult, GetSessions_Session&gt;(
///     label: "sessions",
///     pagedQueryFunc: (cursor, pageSize, ct) =>
///         _client.GetSessions.ExecuteAsync(first: pageSize, after: cursor, cancellationToken: ct),
///     pagination: Pagination.Relay&lt;IGetSessionsResult, GetSessions_Session&gt;(
///         getNodes: r => r.Sessions?.Nodes,
///         getPageInfo: r => r.Sessions?.PageInfo is { } pi
///             ? new PageInfo(pi.HasNextPage, pi.EndCursor)
///             : null
///     ),
///     pageSize: 100
/// );
/// </code>
/// </example>
public sealed class GqlEnumerableStorageAdapter<TResult, T> : IStorageAdapter<IEnumerable<T>>
    where TResult : class
    where T : class
{
    // Non-paginated query delegate
    private readonly Func<CancellationToken, Task<IOperationResult<TResult>>>? _queryFunc;
    private readonly Func<TResult, IEnumerable<T>?>? _selectData;

    // Relay paginated query delegate
    private readonly Func<
        string?,
        int,
        CancellationToken,
        Task<IOperationResult<TResult>>
    >? _relayQueryFunc;
    private readonly RelayPaginationStrategy<TResult, T>? _relayPagination;

    // Offset paginated query delegate
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

    /// <summary>
    /// Creates a non-paginated collection adapter.
    /// </summary>
    /// <param name="label">The catalog entry label, used in validation error messages.</param>
    /// <param name="queryFunc">Delegate that executes the StrawberryShake query operation.</param>
    /// <param name="selectData">
    /// Projects the result data type to the collection of <typeparamref name="T"/>.
    /// Return <c>null</c> to yield an empty collection (subject to <paramref name="allowEmptyData"/>).
    /// </param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty result set is valid.</param>
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

        Traits = new StorageTraits { RequiresNetwork = true, CanWrite = false };
    }

    /// <summary>
    /// Creates a Relay cursor-paginated collection adapter.
    /// </summary>
    /// <param name="label">The catalog entry label, used in validation error messages.</param>
    /// <param name="pagedQueryFunc">
    /// Delegate accepting <c>(cursor, pageSize, cancellationToken)</c> that executes the
    /// StrawberryShake paginated query operation. Pass the cursor as the GraphQL <c>after</c>
    /// argument and the pageSize as <c>first</c>.
    /// </param>
    /// <param name="pagination">
    /// Relay pagination strategy created via <see cref="Pagination.Relay{TResult,T}"/>.
    /// </param>
    /// <param name="pageSize">Number of items to request per page. Defaults to 100.</param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty result set is valid.</param>
    public GqlEnumerableStorageAdapter(
        string label,
        Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
        RelayPaginationStrategy<TResult, T> pagination,
        int pageSize = 100,
        bool allowEmptyData = false
    )
    {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _relayQueryFunc =
            pagedQueryFunc ?? throw new ArgumentNullException(nameof(pagedQueryFunc));
        _relayPagination = pagination ?? throw new ArgumentNullException(nameof(pagination));
        _allowEmptyData = allowEmptyData;
        _pageSize = pageSize;

        Traits = new StorageTraits { RequiresNetwork = true, CanWrite = false };
    }

    /// <summary>
    /// Creates an offset-paginated collection adapter.
    /// </summary>
    /// <param name="label">The catalog entry label, used in validation error messages.</param>
    /// <param name="pagedQueryFunc">
    /// Delegate accepting <c>(offset, limit, cancellationToken)</c> that executes the
    /// StrawberryShake paginated query operation.
    /// </param>
    /// <param name="pagination">
    /// Offset pagination strategy created via <see cref="Pagination.Offset{TResult,T}"/>.
    /// </param>
    /// <param name="pageSize">Number of items to request per page. Defaults to 100.</param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty result set is valid.</param>
    public GqlEnumerableStorageAdapter(
        string label,
        Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc,
        OffsetPaginationStrategy<TResult, T> pagination,
        int pageSize = 100,
        bool allowEmptyData = false
    )
    {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _offsetQueryFunc =
            pagedQueryFunc ?? throw new ArgumentNullException(nameof(pagedQueryFunc));
        _offsetPagination = pagination ?? throw new ArgumentNullException(nameof(pagination));
        _allowEmptyData = allowEmptyData;
        _pageSize = pageSize;

        Traits = new StorageTraits { RequiresNetwork = true, CanWrite = false };
    }

    /// <inheritdoc/>
    public StorageTraits Traits { get; }

    /// <inheritdoc/>
    public FlowIO<IEnumerable<T>> Load() =>
        FlowIO.LiftAsync(async (ct) =>
        {
            if (_queryFunc is not null && _selectData is not null)
                return await LoadNonPaged(ct);

            if (_relayQueryFunc is not null && _relayPagination is not null)
                return await LoadRelayPaged(ct);

            if (_offsetQueryFunc is not null && _offsetPagination is not null)
                return await LoadOffsetPaged(ct);

            throw new InvalidOperationException(
                $"GQL adapter '{_label}' is in an invalid state: no query delegate configured."
            );
        });

    private async Task<IEnumerable<T>> LoadNonPaged(CancellationToken ct)
    {
        var result = await _queryFunc!(ct);
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
                result = await _relayQueryFunc!(cursor, _pageSize, ct);
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

            if (result.Data is null)
                break;

            var pageInfo = _relayPagination!.GetPageInfo(result.Data);
            var nodes = _relayPagination.GetNodes(result.Data);

            if (nodes is not null)
                all.AddRange(nodes);

            if (pageInfo is null || !pageInfo.HasNextPage)
                break;

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
                result = await _offsetQueryFunc!(offset, _pageSize, ct);
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

            if (result.Data is null)
                break;

            // Capture total on first page
            total ??= _offsetPagination!.GetTotal(result.Data);

            var items = _offsetPagination!.GetItems(result.Data);
            if (items is null || !items.Any())
                break;

            all.AddRange(items);
            offset += _pageSize;

            // Stop when we have fetched up to total, or the page was smaller than pageSize
            if (total.HasValue && all.Count >= total.Value)
                break;
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
        FlowIO.Fail<FlowUnit>(
            new NotSupportedException(
                $"GQL catalog entries are read-only. '{_label}' does not support Save(). "
                    + "Use a mutation-enabled GqlStorageAdapter for write operations."
            )
        );

    /// <inheritdoc/>
    public FlowIO<bool> Exists() =>
        FlowIO.LiftAsync(async (ct) =>
        {
            try
            {
                if (_queryFunc is not null)
                {
                    var result = await _queryFunc(ct);
                    return !result.Errors.Any() && result.Data is not null;
                }

                // For paginated modes, probe with a single-item fetch
                if (_relayQueryFunc is not null)
                {
                    var result = await _relayQueryFunc(null, 1, ct);
                    return !result.Errors.Any() && result.Data is not null;
                }

                if (_offsetQueryFunc is not null)
                {
                    var result = await _offsetQueryFunc(0, 1, ct);
                    return !result.Errors.Any() && result.Data is not null;
                }

                return false;
            }
            catch
            {
                return false;
            }
        });

    /// <inheritdoc/>
    /// <remarks>
    /// Executes a minimal one-item probe against the live endpoint to validate reachability,
    /// authentication, and schema compatibility. For paginated modes this uses
    /// <c>pageSize=1</c> to minimise data transfer during pre-flight.
    /// </remarks>
    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
        FlowIO.LiftAsync(async (ct) =>
        {
            try
            {
                return await ProbeEndpoint(probeSize: Math.Max(sampleSize, 1), ct);
            }
            catch (Exception ex)
            {
                return ValidationResult.FromException(_label, ex);
            }
        });

    /// <inheritdoc/>
    /// <remarks>
    /// Executes the full pagination loop to validate that every page deserializes without errors.
    /// For large datasets this may be expensive; prefer <see cref="InspectShallow"/> for routine
    /// pre-flight validation.
    /// </remarks>
    public FlowIO<ValidationResult> InspectDeep() =>
        FlowIO.LiftAsync(async (ct) =>
        {
            try
            {
                await Load().Run(ct);
                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                return ValidationResult.FromException(_label, ex);
            }
        });

    private async Task<ValidationResult> ProbeEndpoint(int probeSize, CancellationToken ct)
    {
        IOperationResult<TResult> result;
        try
        {
            result = _queryFunc is not null
                ? await _queryFunc(ct)
                : _relayQueryFunc is not null
                    ? await _relayQueryFunc(null, probeSize, ct)
                    : await _offsetQueryFunc!(0, probeSize, ct);
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
                details: "Set allowEmptyData: true when creating the catalog entry if null data is valid for this query."
            );
        }

        // For non-paginated mode, also validate the collection itself is non-empty
        if (_queryFunc is not null && _selectData is not null && result.Data is not null)
        {
            var items = _selectData(result.Data);
            if ((items is null || !items.Any()) && !_allowEmptyData)
            {
                return ValidationResult.Failure(
                    catalogKey: _label,
                    errorType: ValidationErrorType.EmptyDataset,
                    message: $"GraphQL query for '{_label}' returned an empty collection.",
                    details: "Set allowEmptyData: true when creating the catalog entry if an empty collection is valid for this query."
                );
            }
        }

        return ValidationResult.Success();
    }
}
