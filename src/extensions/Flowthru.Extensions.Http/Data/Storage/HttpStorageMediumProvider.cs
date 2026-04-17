using Flowthru.Core.Data.Storage.Medium;
using Flowthru.Extensions.Http;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Storage medium provider for <c>http://</c> and <c>https://</c> URIs.
/// </summary>
/// <remarks>
/// Registered by <see cref="Flowthru.Extensions.Http.Services.FlowthruServiceBuilderHttpExtensions"/>
/// as an <see cref="IStorageMediumProvider"/> singleton. The
/// <see cref="StorageMediumResolver"/> picks it up automatically when an HTTP(S) path
/// is used in a catalog entry.
/// <para>
/// When <see cref="HttpOptions.Cache"/> is set, returns a
/// <see cref="CachedHttpStorageMedium"/> that persists response bodies to disk and
/// uses conditional-GET semantics to avoid redundant downloads.
/// </para>
/// </remarks>
public sealed class HttpStorageMediumProvider : IStorageMediumProvider
{
  private readonly HttpClient _httpClient;
  private readonly HttpCacheOptions? _cache;

  /// <summary>
  /// Creates a new HTTP provider using the supplied client and optional cache options.
  /// </summary>
  public HttpStorageMediumProvider(HttpClient httpClient, HttpCacheOptions? cache = null)
  {
    _httpClient = httpClient;
    _cache = cache;
  }

  /// <summary>
  /// Creates a new HTTP provider with a default <see cref="HttpClient"/> and no caching.
  /// Use this for direct construction outside the DI container.
  /// </summary>
  public HttpStorageMediumProvider()
    : this(new HttpClient()) { }

  /// <inheritdoc/>
  public bool CanHandle(Uri uri) =>
    uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

  /// <inheritdoc/>
  public IStorageMedium Create(Uri uri) =>
    _cache is not null
      ? new CachedHttpStorageMedium(uri, _httpClient, _cache.Directory, _cache.MaxAge)
      : new HttpStorageMedium(uri, _httpClient);
}
