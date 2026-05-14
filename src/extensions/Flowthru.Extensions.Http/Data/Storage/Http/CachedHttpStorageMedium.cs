using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using SysIO = System.IO;

namespace Flowthru.Data.Storage.Http;

/// <summary>
/// HTTP(S) storage medium with local-disk caching using
/// conditional-GET semantics. Wraps the same dispatch surface as
/// <see cref="HttpStorageMedium"/> but persists response bodies to a
/// configured cache directory and serves cache hits without
/// network IO when fresh.
/// </summary>
/// <remarks>
/// <para>
/// On first access, downloads the resource and writes two files to
/// the cache directory:
/// </para>
/// <list type="bullet">
/// <item><c>{sha256(url)}.dat</c> — response body</item>
/// <item><c>{sha256(url)}.meta.json</c> — ETag, Last-Modified, original URL, and cached-at timestamp</item>
/// </list>
/// <para>
/// On subsequent accesses, issues a conditional <c>GET</c> with
/// <c>If-None-Match</c> / <c>If-Modified-Since</c> headers. A
/// <c>304 Not Modified</c> response streams from the cached
/// <c>.dat</c> file without re-downloading.
/// </para>
/// <para>
/// When the server provides no caching headers, the
/// <see cref="HttpCacheOptions.MaxAge"/> TTL is the fallback: cache
/// entries older than <c>MaxAge</c> trigger a fresh request.
/// </para>
/// <para>
/// <strong>Pre-flight.</strong> <see cref="Exists"/> returns
/// <c>true</c> immediately when a cached <c>.dat</c> file is present,
/// sparing the network entirely.
/// </para>
/// </remarks>
public sealed class CachedHttpStorageMedium : IStorageMedium, ISupportsFingerprint
{
  private readonly Uri _uri;
  private readonly HttpClient _httpClient;
  private readonly string _cacheDirectory;
  private readonly TimeSpan _maxAge;

  private readonly string _datPath;
  private readonly string _metaPath;

  public CachedHttpStorageMedium(
    Uri uri,
    HttpClient httpClient,
    string cacheDirectory,
    TimeSpan maxAge
  )
  {
    _uri = uri ?? throw new ArgumentNullException(nameof(uri));
    _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    _cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
    _maxAge = maxAge;

    var key = ComputeCacheKey(uri);
    _datPath = SysIO.Path.Combine(cacheDirectory, $"{key}.dat");
    _metaPath = SysIO.Path.Combine(cacheDirectory, $"{key}.meta.json");
  }

  /// <inheritdoc/>
  public StorageTraits Traits => new()
  {
    CanWrite = false,
    CanStream = true,
  };

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async ct =>
    {
      if (SysIO.File.Exists(_datPath)) return true;
      try
      {
        using var request = new HttpRequestMessage(HttpMethod.Head, _uri);
        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
      }
      catch (HttpRequestException) { return false; }
    });

  /// <inheritdoc/>
  public FlowIO<Stream> ReadStream() =>
    FlowIO.LiftAsync(async ct =>
    {
      EnsureCacheDirectory();

      var meta = await TryReadMetaAsync().ConfigureAwait(false);
      var isCacheFresh = meta is not null && IsFresh(meta);

      if (isCacheFresh && SysIO.File.Exists(_datPath))
      {
        return (Stream)new SysIO.FileStream(
          _datPath, SysIO.FileMode.Open, SysIO.FileAccess.Read, SysIO.FileShare.Read);
      }

      using var request = new HttpRequestMessage(HttpMethod.Get, _uri);
      if (meta is not null)
      {
        if (meta.ETag is not null)
          request.Headers.TryAddWithoutValidation("If-None-Match", meta.ETag);
        else if (meta.LastModified is not null)
          request.Headers.TryAddWithoutValidation("If-Modified-Since", meta.LastModified);
      }

      var response = await _httpClient.SendAsync(
        request, HttpCompletionOption.ResponseHeadersRead, ct
      ).ConfigureAwait(false);

      if (response.StatusCode == System.Net.HttpStatusCode.NotModified
          && SysIO.File.Exists(_datPath))
      {
        if (meta is not null)
        {
          meta = meta with { CachedAtUtc = DateTime.UtcNow };
          await WriteMetaAsync(meta).ConfigureAwait(false);
        }
        return (Stream)new SysIO.FileStream(
          _datPath, SysIO.FileMode.Open, SysIO.FileAccess.Read, SysIO.FileShare.Read);
      }

      response.EnsureSuccessStatusCode();

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
        await using (var responseStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
        await using (var fileStream = new SysIO.FileStream(
          tempPath, SysIO.FileMode.Create, SysIO.FileAccess.Write, SysIO.FileShare.None))
        {
          await responseStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
        }
        SysIO.File.Move(tempPath, _datPath, overwrite: true);
        await WriteMetaAsync(newMeta).ConfigureAwait(false);
      }
      catch
      {
        if (SysIO.File.Exists(tempPath))
        {
          try { SysIO.File.Delete(tempPath); } catch { /* best-effort */ }
        }
        throw;
      }

      return (Stream)new SysIO.FileStream(
        _datPath, SysIO.FileMode.Open, SysIO.FileAccess.Read, SysIO.FileShare.Read);
    });

  /// <inheritdoc/>
  /// <remarks>Caching HTTP medium is read-only; surfaces as <see cref="RuntimeError.External"/>.</remarks>
  public FlowIO<FlowUnit> WriteStream(Stream stream) =>
    FlowIO.Fail<FlowUnit>(new RuntimeError.External(
      $"CachedHttpStorageMedium.WriteStream[{_uri}]",
      new InvalidOperationException(
        $"CachedHttpStorageMedium is read-only; cannot write to '{_uri}'.")));

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// Fingerprint derivation from cached HTTP validators. On a cache
  /// hit with a non-stale meta entry, the existing ETag or
  /// Last-Modified value is hashed directly. When no cached meta
  /// exists (or it is missing validators), a conditional GET is
  /// issued against the upstream URL to obtain a current validator
  /// from the server.
  /// </para>
  /// <para>
  /// <strong>Failure mode.</strong> If the upstream server returns
  /// neither an <c>ETag</c> nor a <c>Last-Modified</c> header, the
  /// fingerprint surfaces a FlowIO failure — the cache plan records
  /// "fingerprint unknown" and downgrades the dependent step to a
  /// cache miss. Documented in the HTTP extension's adapter docs.
  /// </para>
  /// </remarks>
  public FlowIO<string> Fingerprint() =>
    FlowIO.LiftAsync(
      async ct =>
      {
        EnsureCacheDirectory();
        var meta = await TryReadMetaAsync().ConfigureAwait(false);

        // Cache-hit fast path: usable validator already in .meta.json.
        if (meta is not null
            && (!string.IsNullOrEmpty(meta.ETag) || !string.IsNullOrEmpty(meta.LastModified)))
        {
          return HashValidator(meta.ETag ?? string.Empty, meta.LastModified ?? string.Empty);
        }

        // Cold or invalidator-less meta — issue a conditional GET.
        using var request = new HttpRequestMessage(HttpMethod.Get, _uri);
        if (meta is not null)
        {
          if (meta.ETag is not null)
            request.Headers.TryAddWithoutValidation("If-None-Match", meta.ETag);
          else if (meta.LastModified is not null)
            request.Headers.TryAddWithoutValidation("If-Modified-Since", meta.LastModified);
        }

        var response = await _httpClient
          .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
          .ConfigureAwait(false);

        var etag = response.Headers.ETag?.ToString();
        var lastModified = response.Content.Headers.LastModified?.ToString("R");

        if (string.IsNullOrEmpty(etag) && string.IsNullOrEmpty(lastModified))
        {
          throw new InvalidOperationException(
            $"HTTP server at '{_uri}' returned neither an ETag nor a Last-Modified header. "
            + "CachedHttpStorageMedium cannot derive a fingerprint without one of these "
            + "validators; the dependent step will be treated as uncacheable."
          );
        }

        return HashValidator(etag ?? string.Empty, lastModified ?? string.Empty);
      },
      source: $"CachedHttpStorageMedium.Fingerprint[{_uri}]"
    );

  private static string HashValidator(string etag, string lastModified)
  {
    var payload = $"{etag}|{lastModified}";
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }

  // ── Cache helpers ──────────────────────────────────────────────────

  private void EnsureCacheDirectory()
  {
    if (!SysIO.Directory.Exists(_cacheDirectory))
      SysIO.Directory.CreateDirectory(_cacheDirectory);
  }

  private bool IsFresh(CacheMetadata meta) => DateTime.UtcNow - meta.CachedAtUtc < _maxAge;

  private async Task<CacheMetadata?> TryReadMetaAsync()
  {
    if (!SysIO.File.Exists(_metaPath)) return null;
    try
    {
      await using var stream = new SysIO.FileStream(
        _metaPath, SysIO.FileMode.Open, SysIO.FileAccess.Read, SysIO.FileShare.Read);
      return await JsonSerializer.DeserializeAsync<CacheMetadata>(stream).ConfigureAwait(false);
    }
    catch { return null; }
  }

  private async Task WriteMetaAsync(CacheMetadata meta)
  {
    await using var stream = new SysIO.FileStream(
      _metaPath, SysIO.FileMode.Create, SysIO.FileAccess.Write, SysIO.FileShare.None);
    await JsonSerializer.SerializeAsync(stream, meta).ConfigureAwait(false);
  }

  private static string ComputeCacheKey(Uri uri)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(uri.ToString()));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }

  private sealed record CacheMetadata
  {
    public required string Url { get; init; }
    public string? ETag { get; init; }
    public string? LastModified { get; init; }
    public DateTime CachedAtUtc { get; init; }
  }
}
