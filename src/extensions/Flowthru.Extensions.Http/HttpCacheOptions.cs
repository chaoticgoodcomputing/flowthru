namespace Flowthru.Extensions.Http;

/// <summary>
/// Configuration for local disk caching of HTTP responses.
/// </summary>
/// <remarks>
/// <para>
/// When set on <see cref="HttpOptions.Cache"/>, <see cref="Flowthru.Core.Data.Storage.HttpStorageMediumProvider"/>
/// returns a caching medium that persists response bodies to disk and uses HTTP
/// conditional-GET semantics (<c>ETag</c> / <c>If-None-Match</c>,
/// <c>Last-Modified</c> / <c>If-Modified-Since</c>) to avoid re-downloading
/// unchanged resources.
/// </para>
/// <para>
/// Cache files are stored under <see cref="Directory"/> as two files per URL:
/// </para>
/// <list type="bullet">
/// <item><c>{sha256(url)}.dat</c> — the response body</item>
/// <item><c>{sha256(url)}.meta.json</c> — URL, ETag, and Last-Modified metadata</item>
/// </list>
/// </remarks>
public sealed class HttpCacheOptions
{
  /// <summary>
  /// Directory where cached response bodies and metadata are stored.
  /// The directory is created if it does not exist.
  /// </summary>
  public required string Directory { get; init; }

  /// <summary>
  /// Maximum age of a cached response when the server provides no caching headers.
  /// Once this TTL expires, a conditional GET is issued on the next access.
  /// Defaults to 24 hours.
  /// </summary>
  public TimeSpan MaxAge { get; init; } = TimeSpan.FromHours(24);
}
