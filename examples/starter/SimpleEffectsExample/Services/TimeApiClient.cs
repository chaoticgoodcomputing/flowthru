using System.Text.Json;

namespace SimpleEffectsExample.Services;

/// <summary>
/// <see cref="IRemoteTimeService"/> implementation backed by the public
/// <c>timeapi.io</c> free service. Demonstrates a Flowthru-independent service
/// authored in the user's project — this client knows nothing about Flowthru;
/// preflight inspection is attached separately in <c>Program.cs</c> via
/// <c>services.AddFlowthruInspect&lt;IRemoteTimeService&gt;(...)</c>.
/// </summary>
/// <remarks>
/// <para>
/// The client uses an <see cref="HttpClient"/> resolved from the typed-client
/// pattern (<c>services.AddHttpClient&lt;IRemoteTimeService, TimeApiClient&gt;()</c>).
/// </para>
/// <para>
/// The free <c>timeapi.io</c> service requires no authentication. If it becomes
/// unreachable the registered Flowthru inspector reports a preflight failure and
/// the flow halts before any step executes — this is the canonical fail-fast
/// behavior for service-bearing flows.
/// </para>
/// </remarks>
public sealed class TimeApiClient : IRemoteTimeService, IDisposable
{
  private readonly HttpClient _httpClient;

  public TimeApiClient()
  {
    _httpClient = new HttpClient
    {
      BaseAddress = new Uri("https://timeapi.io"),
      Timeout = TimeSpan.FromSeconds(10),
    };
  }

  public void Dispose() => _httpClient.Dispose();

  /// <inheritdoc/>
  public async Task<DateTimeOffset> GetCurrentUtcAsync(
    CancellationToken cancellationToken = default
  )
  {
    using var response = await _httpClient
      .GetAsync("/api/Time/current/zone?timeZone=Etc/UTC", cancellationToken)
      .ConfigureAwait(false);

    response.EnsureSuccessStatusCode();

    using var stream = await response
      .Content.ReadAsStreamAsync(cancellationToken)
      .ConfigureAwait(false);

    var payload = await JsonSerializer
      .DeserializeAsync<TimeApiResponse>(stream, _jsonOptions, cancellationToken)
      .ConfigureAwait(false);

    if (payload?.DateTime is null)
    {
      throw new InvalidOperationException(
        "timeapi.io returned a payload without a 'dateTime' field. "
          + "The contract may have changed."
      );
    }

    return DateTimeOffset.Parse(payload.DateTime, System.Globalization.CultureInfo.InvariantCulture)
      .ToUniversalTime();
  }

  /// <summary>
  /// Lightweight reachability probe. Used by the Flowthru inspector registered
  /// in <c>Program.cs</c> to validate the service before any step executes.
  /// </summary>
  public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      using var response = await _httpClient
        .GetAsync("/api/Time/current/zone?timeZone=Etc/UTC", cancellationToken)
        .ConfigureAwait(false);
      return response.IsSuccessStatusCode;
    }
    catch
    {
      return false;
    }
  }

  private static readonly JsonSerializerOptions _jsonOptions =
    new() { PropertyNameCaseInsensitive = true };

  private sealed record TimeApiResponse(string? DateTime);
}
