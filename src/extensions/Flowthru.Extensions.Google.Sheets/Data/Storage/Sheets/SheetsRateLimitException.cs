namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// A <em>transient</em>, retryable failure from a <see cref="ISheetsGateway"/> —
/// the gateway refused a request because the per-user write quota was exceeded.
/// </summary>
/// <remarks>
/// <para>
/// This is the neutral, gateway-level shape of Google's <c>429</c>
/// quota-exceeded response. It lives on the gateway namespace (not in any one
/// gateway) because <strong>both</strong> gateways surface it: the offline
/// <c>InMemorySheetsGateway</c> throws it when its configured write quota is
/// breached, and the production <c>SheetsServiceGateway</c> maps Google's
/// <c>429</c> onto it.
/// </para>
/// <para>
/// <strong>Transient by contract.</strong> The retry/backoff layer branches on
/// this type to decide a failure is worth retrying — a caller may catch it,
/// wait, and re-issue the same request. Do not throw it for permanent failures
/// (a missing table, a bad schema); those are not retryable.
/// </para>
/// </remarks>
public sealed class SheetsRateLimitException : Exception
{
  /// <summary>
  /// How long the caller should wait before retrying, if the gateway can
  /// suggest one (Google surfaces a <c>Retry-After</c> hint; the in-memory
  /// gateway computes the window). <see langword="null"/> when no hint is
  /// available.
  /// </summary>
  public TimeSpan? RetryAfter { get; }

  /// <summary>Build a rate-limit failure with a default message.</summary>
  public SheetsRateLimitException()
    : base("The Sheets write quota was exceeded; the request is retryable after a short wait.")
  {
  }

  /// <summary>Build a rate-limit failure with an explanatory message.</summary>
  public SheetsRateLimitException(string message)
    : base(message)
  {
  }

  /// <summary>Build a rate-limit failure with a message and a retry-after hint.</summary>
  public SheetsRateLimitException(string message, TimeSpan? retryAfter)
    : base(message)
  {
    RetryAfter = retryAfter;
  }

  /// <summary>Build a rate-limit failure wrapping an underlying cause.</summary>
  public SheetsRateLimitException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
