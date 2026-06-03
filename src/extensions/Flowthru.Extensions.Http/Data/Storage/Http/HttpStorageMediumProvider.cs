using Microsoft.Extensions.Options;

namespace Flowthru.Data.Storage.Http;

/// <summary>
/// <see cref="IStorageMediumProvider"/> for the <c>http://</c> and
/// <c>https://</c> schemes. Registered by
/// <c>UseHttp()</c> as a singleton; the host-resolved
/// <see cref="StorageMediumResolver"/> picks it up via DI and routes
/// HTTP-scheme URIs through this provider.
/// </summary>
/// <remarks>
/// When <see cref="HttpOptions.Cache"/> is set on the resolved
/// options, the provider returns a
/// <see cref="CachedHttpStorageMedium"/> that persists response
/// bodies to disk and uses conditional-GET semantics to avoid
/// redundant downloads. Otherwise it returns the plain
/// <see cref="HttpStorageMedium"/>.
/// </remarks>
public sealed class HttpStorageMediumProvider : IStorageMediumProvider
{
  private readonly HttpClient _httpClient;
  private readonly HttpCacheOptions? _cache;
  private readonly int _maxConcurrentRequestsPerHost;

  public HttpStorageMediumProvider(IOptions<HttpOptions> options)
  {
    if (options is null) throw new ArgumentNullException(nameof(options));
    var opts = options.Value;
    _httpClient = opts.CreateClient();
    _cache = opts.Cache;
    _maxConcurrentRequestsPerHost = opts.MaxConcurrentRequestsPerHost;
  }

  /// <summary>
  /// Construct a provider with the supplied <see cref="HttpClient"/>,
  /// (optional) cache configuration, and an optional per-host concurrency
  /// cap. Used by tests; production code goes through the
  /// <see cref="IOptions{TOptions}"/> ctor.
  /// </summary>
  public HttpStorageMediumProvider(
    HttpClient httpClient,
    HttpCacheOptions? cache = null,
    int maxConcurrentRequestsPerHost = int.MaxValue)
  {
    _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    _cache = cache;
    _maxConcurrentRequestsPerHost = maxConcurrentRequestsPerHost;
  }

  /// <inheritdoc/>
  public bool CanHandle(Uri uri) =>
    uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

  /// <inheritdoc/>
  public IStorageMedium Create(Uri uri) =>
    _cache is not null
      ? new CachedHttpStorageMedium(uri, _httpClient, _cache.Directory, _cache.MaxAge, _maxConcurrentRequestsPerHost)
      : new HttpStorageMedium(uri, _httpClient, _maxConcurrentRequestsPerHost);
}
