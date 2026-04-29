using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Flowthru.Extensions.Http.Tests;

/// <summary>
/// Fake <see cref="HttpMessageHandler"/> that returns a pre-configured response.
/// Records every request so tests can assert on method, headers, etc.
/// </summary>
public sealed class FakeHandler : HttpMessageHandler
{
  private readonly HttpStatusCode _defaultStatus;
  private readonly string _defaultBody;
  private readonly string? _etag;

  /// <summary>Override the next response returned by <see cref="SendAsync"/>.</summary>
  public HttpResponseMessage? NextResponse { get; set; }

  /// <summary>All requests received, in order.</summary>
  public List<HttpRequestMessage> Requests { get; } = [];

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

    if (_etag is not null)
      response.Headers.ETag = new EntityTagHeaderValue(_etag);

    return Task.FromResult(response);
  }
}

/// <summary>
/// Fake handler that always throws <see cref="HttpRequestException"/> to simulate
/// network failures.
/// </summary>
public sealed class ThrowingHandler : HttpMessageHandler
{
  protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken
  ) => throw new HttpRequestException("Simulated network failure");
}
