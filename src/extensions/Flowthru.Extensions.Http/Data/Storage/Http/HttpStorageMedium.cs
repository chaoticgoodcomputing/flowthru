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
public sealed class HttpStorageMedium : IStorageMedium
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
}
