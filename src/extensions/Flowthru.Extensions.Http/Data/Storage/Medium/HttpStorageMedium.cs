using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage.Medium;

/// <summary>
/// Storage medium for reading files over HTTP or HTTPS.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Responsibility:</strong> Read raw byte streams from remote HTTP(S) endpoints.
/// </para>
/// <para>
/// <strong>Characteristics:</strong>
/// </para>
/// <list type="bullet">
/// <item>Read-only — <see cref="WriteStream"/> is not supported</item>
/// <item>RequiresNetwork: true</item>
/// <item>CanStream: true — uses <c>ResponseHeadersRead</c> to avoid buffering the entire response</item>
/// <item>Pre-flight inspection uses an HTTP <c>HEAD</c> request</item>
/// </list>
/// <para>
/// <strong>Usage via resolver (typical):</strong>
/// </para>
/// <code>
/// // Register the extension once in Program.cs
/// services.AddFlowthru(flowthru => flowthru.UseHttp());
///
/// // Then any catalog entry with an http:// or https:// path is resolved automatically
/// public IItem&lt;IEnumerable&lt;RetailSchema&gt;&gt; RetailData =&gt;
///     CreateItem(() =&gt; ItemFactory.Enumerable.Csv&lt;RetailSchema&gt;(
///         "RetailData",
///         "https://example.com/data/retail.csv",
///         _resolver));
/// </code>
/// <para>
/// <strong>Direct construction (tests, advanced):</strong>
/// </para>
/// <code>
/// var medium = new HttpStorageMedium(
///     new Uri("https://example.com/data.csv"),
///     httpClient);
/// </code>
/// </remarks>
public sealed class HttpStorageMedium : IStorageMedium
{
  private readonly Uri _uri;
  private readonly HttpClient _httpClient;

  /// <summary>
  /// Creates a new HTTP storage medium.
  /// </summary>
  /// <param name="uri">The URI of the remote resource.</param>
  /// <param name="httpClient">The <see cref="HttpClient"/> to use for requests.</param>
  public HttpStorageMedium(Uri uri, HttpClient httpClient)
  {
    _uri = uri;
    _httpClient = httpClient;
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
  /// Uses <see cref="HttpCompletionOption.ResponseHeadersRead"/> to begin streaming
  /// without buffering the full response body into memory.
  /// </remarks>
  public FlowIO<Stream> ReadStream() =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        var response = await _httpClient.GetAsync(
          _uri,
          HttpCompletionOption.ResponseHeadersRead,
          ct
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
      }
    );

  /// <inheritdoc/>
  /// <exception cref="NotSupportedException">Always thrown — HTTP sources are read-only.</exception>
  public FlowIO<FlowUnit> WriteStream(Stream stream) =>
    FlowIO.Fail<FlowUnit>(
      new NotSupportedException($"HttpStorageMedium is read-only. Cannot write to '{_uri}'.")
    );

  /// <inheritdoc/>
  /// <remarks>
  /// Sends an HTTP <c>HEAD</c> request to confirm the resource is reachable and
  /// returns a success status code (2xx). Returns <c>false</c> on any
  /// <see cref="HttpRequestException"/> rather than propagating the error, so
  /// pre-flight can report it as a validation failure rather than an unhandled exception.
  /// </remarks>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
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
}
