using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage.Medium;

/// <summary>
/// HTTP(S) storage medium with local disk caching using conditional-GET semantics.
/// </summary>
/// <remarks>
/// <para>
/// On first access, downloads the resource and writes two files to the cache directory:
/// </para>
/// <list type="bullet">
/// <item><c>{sha256(url)}.dat</c> — response body</item>
/// <item><c>{sha256(url)}.meta.json</c> — ETag, Last-Modified, and original URL</item>
/// </list>
/// <para>
/// On subsequent accesses, issues a conditional <c>GET</c> with
/// <c>If-None-Match</c> / <c>If-Modified-Since</c> headers. A <c>304 Not Modified</c>
/// response streams from the cached <c>.dat</c> file without downloading again.
/// </para>
/// <para>
/// When the server provides no caching headers, the
/// <see cref="Flowthru.Extensions.Http.HttpCacheOptions.MaxAge"/> TTL is used as a
/// fallback: once the cache entry is older than <c>MaxAge</c>, a fresh request is made.
/// </para>
/// <para>
/// <strong>Pre-flight:</strong> <see cref="Exists"/> returns <c>true</c> immediately
/// if a cached <c>.dat</c> file is present, sparing the network entirely.
/// </para>
/// </remarks>
public sealed class CachedHttpStorageMedium : IStorageMedium
{
  private readonly Uri _uri;
  private readonly HttpClient _httpClient;
  private readonly string _cacheDirectory;
  private readonly TimeSpan _maxAge;

  private readonly string _datPath;
  private readonly string _metaPath;

  /// <param name="uri">Remote resource URI.</param>
  /// <param name="httpClient">HTTP client to use for requests.</param>
  /// <param name="cacheDirectory">Directory where cache files are stored.</param>
  /// <param name="maxAge">TTL used when the server provides no cache headers.</param>
  public CachedHttpStorageMedium(
    Uri uri,
    HttpClient httpClient,
    string cacheDirectory,
    TimeSpan maxAge
  )
  {
    _uri = uri;
    _httpClient = httpClient;
    _cacheDirectory = cacheDirectory;
    _maxAge = maxAge;

    var key = ComputeCacheKey(uri);
    _datPath = Path.Combine(cacheDirectory, $"{key}.dat");
    _metaPath = Path.Combine(cacheDirectory, $"{key}.meta.json");
  }

  /// <inheritdoc/>
  public StorageTraits Traits =>
    new StorageTraits
    {
      RequiresNetwork = true,
      CanWrite = false,
      CanStream = true,
    };

  /// <inheritdoc/>
  /// <remarks>
  /// Returns <c>true</c> without a network request when a cached copy is present.
  /// Falls back to an HTTP <c>HEAD</c> request otherwise.
  /// </remarks>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (File.Exists(_datPath))
          return true;

        try
        {
          using var request = new HttpRequestMessage(HttpMethod.Head, _uri);
          var response = await _httpClient.SendAsync(request, ct);
          return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
          return false;
        }
      }
    );

  /// <inheritdoc/>
  /// <remarks>
  /// Issues a conditional GET when cache metadata is available. On a 304, streams
  /// directly from the cached file. On a 200 or cache miss, downloads and updates
  /// the cache before streaming.
  /// </remarks>
  public FlowIO<Stream> ReadStream() =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        EnsureCacheDirectory();

        var meta = await TryReadMetaAsync();
        var isCacheFresh = meta is not null && IsFresh(meta);

        if (isCacheFresh && File.Exists(_datPath))
        {
          // Cache hit — serve local copy without issuing any request.
          return (Stream)new FileStream(_datPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        // Conditional GET when we have metadata but TTL has lapsed.
        using var request = new HttpRequestMessage(HttpMethod.Get, _uri);

        if (meta is not null)
        {
          if (meta.ETag is not null)
            request.Headers.TryAddWithoutValidation("If-None-Match", meta.ETag);
          else if (meta.LastModified is not null)
            request.Headers.TryAddWithoutValidation("If-Modified-Since", meta.LastModified);
        }

        var response = await _httpClient.SendAsync(
          request,
          HttpCompletionOption.ResponseHeadersRead,
          ct
        );

        if (response.StatusCode == System.Net.HttpStatusCode.NotModified && File.Exists(_datPath))
        {
          // Server confirmed nothing changed — update metadata timestamp and serve cache.
          if (meta is not null)
          {
            meta = meta with { CachedAtUtc = DateTime.UtcNow };
            await WriteMetaAsync(meta);
          }
          return (Stream)new FileStream(_datPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        response.EnsureSuccessStatusCode();

        // Fresh content — download to cache file then stream from it.
        var newMeta = new CacheMetadata
        {
          Url = _uri.ToString(),
          ETag = response.Headers.ETag?.ToString(),
          LastModified = response.Content.Headers.LastModified?.ToString("R"),
          CachedAtUtc = DateTime.UtcNow,
        };

        var tempPath = $"{_datPath}.tmp.{Guid.NewGuid():N}";
        try
        {
          using (var responseStream = await response.Content.ReadAsStreamAsync(ct))
          using (
            var fileStream = new FileStream(
              tempPath,
              FileMode.Create,
              FileAccess.Write,
              FileShare.None
            )
          )
          {
            await responseStream.CopyToAsync(fileStream, ct);
          }
          File.Move(tempPath, _datPath, overwrite: true);
          await WriteMetaAsync(newMeta);
        }
        catch
        {
          if (File.Exists(tempPath))
            try
            {
              File.Delete(tempPath);
            }
            catch { }
          throw;
        }

        return (Stream)new FileStream(_datPath, FileMode.Open, FileAccess.Read, FileShare.Read);
      }
    );

  /// <inheritdoc/>
  /// <exception cref="NotSupportedException">Always thrown — HTTP sources are read-only.</exception>
  public FlowIO<FlowUnit> WriteStream(Stream stream) =>
    FlowIO.Fail<FlowUnit>(
      new NotSupportedException($"CachedHttpStorageMedium is read-only. Cannot write to '{_uri}'.")
    );

  // ── Cache helpers ─────────────────────────────────────────────────────────

  private void EnsureCacheDirectory()
  {
    if (!System.IO.Directory.Exists(_cacheDirectory))
      System.IO.Directory.CreateDirectory(_cacheDirectory);
  }

  private bool IsFresh(CacheMetadata meta) => DateTime.UtcNow - meta.CachedAtUtc < _maxAge;

  private async Task<CacheMetadata?> TryReadMetaAsync()
  {
    if (!File.Exists(_metaPath))
      return null;

    try
    {
      await using var stream = new FileStream(
        _metaPath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read
      );
      return await JsonSerializer.DeserializeAsync<CacheMetadata>(stream);
    }
    catch
    {
      return null;
    }
  }

  private async Task WriteMetaAsync(CacheMetadata meta)
  {
    await using var stream = new FileStream(
      _metaPath,
      FileMode.Create,
      FileAccess.Write,
      FileShare.None
    );
    await JsonSerializer.SerializeAsync(stream, meta);
  }

  private static string ComputeCacheKey(Uri uri)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(uri.ToString()));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }

  // ── Cache metadata record ─────────────────────────────────────────────────

  private sealed record CacheMetadata
  {
    public required string Url { get; init; }
    public string? ETag { get; init; }
    public string? LastModified { get; init; }
    public DateTime CachedAtUtc { get; init; }
  }
}
