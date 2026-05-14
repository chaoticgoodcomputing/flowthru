using System.Security.Cryptography;
using System.Text;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Data.Storage.Http;

/// <summary>
/// Storage medium for reading bytes over <c>http://</c> or
/// <c>https://</c>. Read-only by construction —
/// <see cref="WriteStream"/> always fails. Streams the response body
/// without buffering via <see cref="HttpCompletionOption.ResponseHeadersRead"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Pre-flight inspection.</strong> <see cref="Exists"/>
/// issues an HTTP <c>HEAD</c> request and reports
/// <see cref="HttpRequestException"/> as <c>false</c> rather than
/// surfacing the exception, so pre-flight failures are reported as
/// validation errors rather than runtime exceptions.
/// </para>
/// <para>
/// <strong>Composition.</strong> Format extensions consume this
/// medium through their resolver-aware smart constructors:
/// <c>ItemFactory.Enumerable.Csv&lt;T&gt;("label", "https://…", resolver)</c>.
/// Direct construction is mostly useful for tests and advanced
/// composition.
/// </para>
/// </remarks>
public sealed class HttpStorageMedium : IStorageMedium, ISupportsFingerprint
{
  private readonly Uri _uri;
  private readonly HttpClient _httpClient;

  public HttpStorageMedium(Uri uri, HttpClient httpClient)
  {
    _uri = uri ?? throw new ArgumentNullException(nameof(uri));
    _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
  }

  /// <inheritdoc/>
  public StorageTraits Traits => new()
  {
    CanWrite = false,
    CanStream = true,
  };

  /// <inheritdoc/>
  public FlowIO<Stream> ReadStream() =>
    FlowIO.LiftAsync(async ct =>
    {
      var response = await _httpClient.GetAsync(
        _uri, HttpCompletionOption.ResponseHeadersRead, ct
      ).ConfigureAwait(false);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
    });

  /// <inheritdoc/>
  /// <remarks>
  /// HTTP is read-only here; <c>Save</c> at the catalog level
  /// short-circuits as <see cref="RuntimeError.External"/> wrapping
  /// an <see cref="InvalidOperationException"/>. Catalog authors
  /// should also <c>Constrain</c> remote items with
  /// <c>traits =&gt; traits with { CanWrite = false }</c> so the
  /// constraint surfaces at wire-up rather than at first save.
  /// </remarks>
  public FlowIO<FlowUnit> WriteStream(Stream stream) =>
    FlowIO.Fail<FlowUnit>(new RuntimeError.External(
      $"HttpStorageMedium.WriteStream[{_uri}]",
      new InvalidOperationException(
        $"HttpStorageMedium is read-only; cannot write to '{_uri}'.")));

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(async ct =>
    {
      try
      {
        using var request = new HttpRequestMessage(HttpMethod.Head, _uri);
        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
      }
      catch (HttpRequestException) { return false; }
    });

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// Fingerprints the remote resource by issuing an HTTP <c>HEAD</c>
  /// request and hashing the response's <c>ETag</c> and
  /// <c>Last-Modified</c> headers. Cheap by design — no body is
  /// transferred.
  /// </para>
  /// <para>
  /// <strong>Failure mode.</strong> When the server returns neither
  /// validator, the fingerprint surfaces a FlowIO failure so the
  /// cache plan records "fingerprint unknown" and downgrades the
  /// dependent step to a cache miss. Network failures (DNS, TLS,
  /// connection refused) also surface as failures rather than
  /// throwing — pre-flight is not aborted.
  /// </para>
  /// </remarks>
  public FlowIO<string> Fingerprint() =>
    FlowIO.LiftAsync(
      async ct =>
      {
        using var request = new HttpRequestMessage(HttpMethod.Head, _uri);
        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var etag = response.Headers.ETag?.ToString();
        var lastModified = response.Content.Headers.LastModified?.ToString("R");

        if (string.IsNullOrEmpty(etag) && string.IsNullOrEmpty(lastModified))
        {
          throw new InvalidOperationException(
            $"HTTP server at '{_uri}' returned neither an ETag nor a Last-Modified header. "
            + "HttpStorageMedium cannot derive a fingerprint without one of these "
            + "validators; the dependent step will be treated as uncacheable."
          );
        }

        var payload = $"{etag ?? string.Empty}|{lastModified ?? string.Empty}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
      },
      source: $"HttpStorageMedium.Fingerprint[{_uri}]"
    );
}
