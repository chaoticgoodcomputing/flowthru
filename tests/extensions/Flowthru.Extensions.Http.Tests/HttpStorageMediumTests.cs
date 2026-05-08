using System.Net;
using Flowthru.Data.Storage.Http;
using Flowthru.Prelude;

namespace Flowthru.Extensions.Http.Tests;

/// <summary>
/// Tests for <see cref="HttpStorageMedium"/>. HTTP sources are
/// read-only — write attempts surface as typed
/// <c>RuntimeError.External</c> failures rather than throws.
/// Network failures during <c>Exists()</c> resolve to <c>false</c>
/// so pre-flight reports them as validation failures, not runtime
/// exceptions.
/// </summary>
[TestFixture]
[Category("Http")]
public class HttpStorageMediumTests
{
  // ── Traits ────────────────────────────────────────────────────────

  [Test]
  public void Traits_AreReadOnlyAndStreaming()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, ""))
    );

    Assert.That(medium.Traits.CanRead, Is.True);
    Assert.That(medium.Traits.CanWrite, Is.False);
    Assert.That(medium.Traits.CanStream, Is.True);
  }

  // ── WriteStream — always fails as RuntimeError.External ───────────

  [Test]
  public async Task WriteStream_ReturnsRuntimeErrorExternal()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, ""))
    );

    var result = await medium.WriteStream(new MemoryStream()).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>(),
      "Write attempts on read-only HTTP medium should be Failure, not Success.");
  }

  // ── ReadStream ────────────────────────────────────────────────────

  [Test]
  public async Task ReadStream_SuccessfulResponse_ReturnsContentStream()
  {
    const string body = "Id,Name\n1,Alice\n";
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, body))
    );

    var result = await medium.ReadStream().Run();
    using var stream = ((EffResult<Stream>.Success)result).Value;
    using var reader = new StreamReader(stream);
    var content = await reader.ReadToEndAsync();
    Assert.That(content, Is.EqualTo(body));
  }

  [Test]
  public async Task ReadStream_ServerError_ReturnsFailure()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.InternalServerError, ""))
    );

    var result = await medium.ReadStream().Run();
    Assert.That(result, Is.InstanceOf<EffResult<Stream>.Failure>(),
      "5xx response should surface as a typed failure rather than propagating.");
  }

  // ── Exists ────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_HeadReturns200_ReturnsTrue()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, ""))
    );

    var result = await medium.Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.True);
  }

  [Test]
  public async Task Exists_HeadReturns404_ReturnsFalse()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/missing.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.NotFound, ""))
    );

    var result = await medium.Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }

  [Test]
  public async Task Exists_NetworkError_ReturnsFalseInsteadOfPropagating()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://unreachable.example.com/data.csv"),
      new HttpClient(new ThrowingHandler())
    );

    var result = await medium.Exists().Run();
    Assert.That(result, Is.InstanceOf<EffResult<bool>.Success>(),
      "Network failures during Exists should resolve to Success(false), not Failure.");
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }
}
