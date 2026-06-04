using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Http.Tests;

/// <summary>
/// Fake <see cref="HttpMessageHandler"/> that returns a pre-configured response
/// and records every received request so tests can assert on method + headers.
/// </summary>
public sealed class FakeHandler : HttpMessageHandler
{
  private readonly HttpStatusCode _defaultStatus;
  private readonly string _defaultBody;
  private readonly string? _etag;

  /// <summary>Override the next response returned by <see cref="SendAsync"/>.</summary>
  public HttpResponseMessage? NextResponse { get; set; }

  /// <summary>All requests received, in order.</summary>
  public List<HttpRequestMessage> Requests { get; } = new();

  public FakeHandler(HttpStatusCode status, string body, string? etag = null)
  {
    _defaultStatus = status;
    _defaultBody = body;
    _etag = etag;
  }

  protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken
  )
  {
    Requests.Add(request);

    if (NextResponse is not null)
    {
      var next = NextResponse;
      NextResponse = null;
      return Task.FromResult(next);
    }

    var response = new HttpResponseMessage(_defaultStatus)
    {
      Content = new StringContent(_defaultBody, Encoding.UTF8),
    };
    if (_etag is not null) response.Headers.ETag = new EntityTagHeaderValue(_etag);
    return Task.FromResult(response);
  }
}

/// <summary>Fake handler that always throws to simulate network failures.</summary>
public sealed class ThrowingHandler : HttpMessageHandler
{
  protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken
  ) => throw new HttpRequestException("Simulated network failure");
}

/// <summary>
/// <see cref="HttpContent"/> whose read stream is <strong>forward-only</strong>
/// (<c>CanSeek == false</c>), the way a real streamed HTTP response body behaves.
/// The default <see cref="StringContent"/> buffers into a seekable
/// <see cref="MemoryStream"/>, which masks seek-required-format bugs over HTTP
/// (issue #105); this content models the production stream faithfully.
/// </summary>
public sealed class ForwardOnlyContent : HttpContent
{
  private readonly byte[] _bytes;

  public ForwardOnlyContent(string body) => _bytes = Encoding.UTF8.GetBytes(body);

  protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
    stream.WriteAsync(_bytes, 0, _bytes.Length);

  // Unknown length, like a chunked/streamed response — keeps the pipeline from
  // assuming a buffered, length-prefixed body.
  protected override bool TryComputeLength(out long length)
  {
    length = 0;
    return false;
  }

  // Bypass the base class's buffer-into-a-MemoryStream behavior so the consumer
  // genuinely receives a non-seekable stream.
  protected override Task<Stream> CreateContentReadStreamAsync() =>
    Task.FromResult<Stream>(new NonSeekableStream(new MemoryStream(_bytes, writable: false)));
}
