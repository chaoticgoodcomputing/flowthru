namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// A <em>permanent</em> failure raised by <see cref="RetryingSheetsGateway"/>
/// when capped exponential backoff has spent every allowed attempt and the
/// underlying gateway is still returning a transient
/// <see cref="SheetsRateLimitException"/>. The last transient failure is chained
/// as the inner exception.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Not transient.</strong> This deliberately does <strong>not</strong>
/// derive from <see cref="SheetsRateLimitException"/>, so an outer retry layer
/// will not loop on it — the retries are already exhausted. It carries the
/// <c>FTGS1607</c> provenance code so the failure is grep-able in run output.
/// </para>
/// </remarks>
public sealed class SheetsRetryExhaustedException : Exception
{
  /// <summary>The provenance code embedded in the message for grep-ability.</summary>
  public const string Code = "FTGS1607";

  /// <summary>How many attempts were made before giving up.</summary>
  public int Attempts { get; }

  /// <summary>
  /// Build an exhaustion failure after <paramref name="attempts"/> attempts,
  /// chaining the last transient <paramref name="lastTransient"/> cause.
  /// </summary>
  public SheetsRetryExhaustedException(int attempts, SheetsRateLimitException lastTransient)
    : base(
      $"[{Code}] The Sheets write quota was still exceeded after {attempts} "
      + "attempt(s) with exponential backoff. The per-user write quota "
      + "(~60 writes/min) may be saturated; reduce write frequency or retry the "
      + "run later.",
      lastTransient)
  {
    Attempts = attempts;
  }
}
