namespace Flowthru.Extensions.Http;

/// <summary>
/// Configuration options for the HTTP storage medium extension.
/// </summary>
public sealed class HttpOptions
{
  /// <summary>
  /// Timeout for HTTP requests. Defaults to 5 minutes to accommodate large remote files.
  /// </summary>
  public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

  /// <summary>
  /// Optional <c>User-Agent</c> header value sent with every request.
  /// Defaults to <c>Flowthru-Http/1.0</c>.
  /// </summary>
  public string UserAgent { get; set; } = "Flowthru-Http/1.0";

  /// <summary>
  /// Optional local disk cache configuration. When <c>null</c> (default), every
  /// <see cref="Flowthru.Core.Data.Storage.Medium.HttpStorageMedium.ReadStream"/> call
  /// issues a fresh HTTP request. Set this to enable conditional-GET caching.
  /// </summary>
  public HttpCacheOptions? Cache { get; set; }

  internal HttpClient CreateClient()
  {
    var client = new HttpClient { Timeout = Timeout };
    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    return client;
  }
}
