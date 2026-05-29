namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// The backoff policy a <see cref="RetryingSheetsGateway"/> applies to transient
/// (<see cref="SheetsRateLimitException"/>) gateway failures: how many attempts,
/// the exponential base delay, and the per-delay ceiling. Sane defaults are tuned
/// to Google's ~60-writes/min/user quota — a handful of capped backoffs rides out
/// a transient quota spike without turning a permanent failure into a long hang.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="SheetsRateLimitException"/> is retried. Permanent failures
/// (a missing spreadsheet, a schema mismatch, an over-ceiling payload) are
/// rethrown on the first occurrence — backoff would only delay the same error.
/// </para>
/// <para>
/// When an exception carries a <see cref="SheetsRateLimitException.RetryAfter"/>
/// hint it wins over the computed exponential delay (still clamped to
/// <see cref="MaxDelay"/>); otherwise the delay for attempt <c>n</c> (zero-based)
/// is <see cref="BaseDelay"/> × 2<sup>n</sup>, clamped to <see cref="MaxDelay"/>.
/// </para>
/// </remarks>
public sealed class SheetsRetryOptions
{
  /// <summary>
  /// The maximum number of <em>attempts</em> (the first try plus retries) before
  /// the gateway gives up and surfaces the failure. Must be at least 1. Default 5
  /// (one initial attempt plus up to four backed-off retries).
  /// </summary>
  public int MaxAttempts { get; init; } = 5;

  /// <summary>
  /// The base exponential delay — the wait before the first retry, doubled each
  /// subsequent retry up to <see cref="MaxDelay"/>. Default 1 second.
  /// </summary>
  public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(1);

  /// <summary>
  /// The ceiling on any single backoff delay (and on an honored
  /// <see cref="SheetsRateLimitException.RetryAfter"/> hint), so an extreme hint
  /// or a high attempt count cannot stall a run unboundedly. Default 60 seconds.
  /// </summary>
  public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(60);
}
