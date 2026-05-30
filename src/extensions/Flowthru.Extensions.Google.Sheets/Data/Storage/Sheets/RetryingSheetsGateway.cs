using Flowthru.Prelude;

namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// An <see cref="ISheetsGateway"/> decorator that retries <em>only transient</em>
/// failures — a <see cref="SheetsRateLimitException"/> (Google's <c>429</c>
/// quota response) — with capped exponential backoff, and passes every other
/// failure straight through. Composes over any gateway: the live
/// <c>SheetsServiceGateway</c> in production, the offline <c>InMemorySheetsGateway</c>
/// in tests.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Transient vs permanent.</strong> The retry loop catches exactly
/// <see cref="SheetsRateLimitException"/>. A
/// <see cref="SheetsSpreadsheetAccessException"/>, a schema error, an over-ceiling
/// payload failure, or any other exception propagates on its first occurrence —
/// re-issuing those fails the same way, so backoff would only delay the error.
/// </para>
/// <para>
/// <strong>Backoff timing is clock-driven.</strong> The wait uses
/// <see cref="Task.Delay(TimeSpan, TimeProvider, CancellationToken)"/> against an
/// injected <see cref="TimeProvider"/>, so a test advances a fake clock instead
/// of sleeping for real. Per-attempt delay is <see cref="SheetsRetryOptions.BaseDelay"/>
/// × 2<sup>attempt</sup> clamped to <see cref="SheetsRetryOptions.MaxDelay"/>, or
/// the exception's <see cref="SheetsRateLimitException.RetryAfter"/> hint when it
/// carries one (also clamped).
/// </para>
/// <para>
/// <strong>Lifecycle is forwarded, not owned.</strong> When the inner gateway is
/// an <see cref="IFlowResourceProvider"/> (factory-mode
/// <c>SheetsServiceGateway</c>), this decorator forwards its
/// <see cref="FlowResource"/> so registering the decorator alone keeps the
/// per-run client lifecycle intact; an inner gateway that owns no resource
/// surfaces a <see langword="null"/> <see cref="FlowResource"/>, a no-op.
/// </para>
/// </remarks>
public sealed class RetryingSheetsGateway : ISheetsGateway, IFlowResourceProvider
{
  private readonly ISheetsGateway _inner;
  private readonly SheetsRetryOptions _options;
  private readonly TimeProvider _timeProvider;

  /// <summary>
  /// Wrap <paramref name="inner"/> with retry-on-transient backoff. The optional
  /// <paramref name="timeProvider"/> drives the backoff delay — pass a fake clock
  /// in tests; it defaults to <see cref="TimeProvider.System"/>.
  /// </summary>
  public RetryingSheetsGateway(
    ISheetsGateway inner,
    SheetsRetryOptions? options = null,
    TimeProvider? timeProvider = null)
  {
    _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    _options = options ?? new SheetsRetryOptions();
    _timeProvider = timeProvider ?? TimeProvider.System;

    if (_options.MaxAttempts < 1)
    {
      throw new ArgumentOutOfRangeException(
        nameof(options),
        _options.MaxAttempts,
        $"{nameof(SheetsRetryOptions.MaxAttempts)} must be at least 1.");
    }
  }

  /// <summary>The gateway this decorator delegates to (transient failures aside).</summary>
  public ISheetsGateway Inner => _inner;

  // ── IFlowResourceProvider (forwarded from the inner gateway) ─────────────

  /// <inheritdoc/>
  public IFlowResource? FlowResource =>
    (_inner as IFlowResourceProvider)?.FlowResource;

  // ── ISheetsGateway ───────────────────────────────────────────────────────

  /// <inheritdoc/>
  public Task<ResolvedTable?> ResolveTable(string spreadsheetId, string tableName, CancellationToken ct) =>
    Retry(() => _inner.ResolveTable(spreadsheetId, tableName, ct), ct);

  /// <inheritdoc/>
  public Task<TableData> ReadRows(string spreadsheetId, ResolvedTable table, CancellationToken ct) =>
    Retry(() => _inner.ReadRows(spreadsheetId, table, ct), ct);

  /// <inheritdoc/>
  public Task ReplaceRows(string spreadsheetId, ResolvedTable table, TableData rows, CancellationToken ct) =>
    Retry(async () =>
    {
      await _inner.ReplaceRows(spreadsheetId, table, rows, ct).ConfigureAwait(false);
      return FlowUnit.Default;
    }, ct);

  /// <inheritdoc/>
  public Task<ResolvedTable> AddTable(
    string spreadsheetId, string tableName, TableSchema schema, CancellationToken ct) =>
    Retry(() => _inner.AddTable(spreadsheetId, tableName, schema, ct), ct);

  // ── Retry core ─────────────────────────────────────────────────────────

  private async Task<T> Retry<T>(Func<Task<T>> operation, CancellationToken ct)
  {
    var attempt = 0;
    while (true)
    {
      try
      {
        return await operation().ConfigureAwait(false);
      }
      catch (SheetsRateLimitException ex)
      {
        attempt++;

        // Exhausted: surface the failure with an FTGS-coded message noting the
        // retries are spent, chaining the last transient cause.
        if (attempt >= _options.MaxAttempts)
        {
          throw new SheetsRetryExhaustedException(_options.MaxAttempts, ex);
        }

        var delay = DelayFor(attempt, ex.RetryAfter);
        await Task.Delay(delay, _timeProvider, ct).ConfigureAwait(false);
      }
    }
  }

  // attempt is 1-based here (incremented after the failed try). Exponential base
  // delay for the n-th backoff is BaseDelay × 2^(attempt-1), clamped to MaxDelay;
  // a RetryAfter hint (also clamped) wins when present.
  private TimeSpan DelayFor(int attempt, TimeSpan? retryAfter)
  {
    if (retryAfter is { } hint)
    {
      return Clamp(hint);
    }

    var exponent = attempt - 1;
    // Compute in ticks against a double to avoid TimeSpan overflow on a high
    // attempt count; the MaxDelay clamp makes the precise large value moot.
    var scaled = _options.BaseDelay.Ticks * Math.Pow(2, exponent);
    if (scaled >= _options.MaxDelay.Ticks)
    {
      return _options.MaxDelay;
    }

    return Clamp(TimeSpan.FromTicks((long)scaled));
  }

  private TimeSpan Clamp(TimeSpan delay)
  {
    if (delay < TimeSpan.Zero) return TimeSpan.Zero;
    return delay > _options.MaxDelay ? _options.MaxDelay : delay;
  }
}
