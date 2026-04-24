using System.Net;
using Flowthru.Core.Data.Storage.Medium;

namespace Flowthru.Extensions.Http.Tests;

/// <summary>
/// Tests for <see cref="HttpStorageMedium"/>.
///
/// Error-surface focus: HTTP sources are read-only. Any write attempt must fail
/// fast. Network failures during <c>Exists()</c> return <c>false</c> rather than
/// propagating, so pre-flight can report them as validation failures instead of
/// unhandled exceptions.
/// </summary>
[TestFixture]
[Category("Http")]
public class HttpStorageMediumTests
{
  // ── Traits ────────────────────────────────────────────────────────────────

  [Test]
  public void Traits_CanWrite_IsFalse_RequiresNetwork_IsTrue_CanStream_IsTrue()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, ""))
    );

    Assert.That(medium.Traits.CanWrite, Is.False);
    Assert.That(medium.Traits.RequiresNetwork, Is.True);
    Assert.That(medium.Traits.CanStream, Is.True);
  }

  // ── WriteStream — always fails ────────────────────────────────────────────

  [Test]
  public async Task WriteStream_ThrowsNotSupportedException()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, ""))
    );

    await Assert.ThatAsync(
      () => medium.WriteStream(new MemoryStream()).Run().AsTask(),
      Throws.TypeOf<NotSupportedException>()
    );
  }

  // ── ReadStream ────────────────────────────────────────────────────────────

  [Test]
  public async Task ReadStream_SuccessfulResponse_ReturnsContentStream()
  {
    const string body = "Id,Name\n1,Alice\n";
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, body))
    );

    using var stream = await medium.ReadStream().Run();
    using var reader = new StreamReader(stream);
    var content = await reader.ReadToEndAsync();

    Assert.That(content, Is.EqualTo(body));
  }

  [Test]
  public async Task ReadStream_ServerError_ThrowsHttpRequestException()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.InternalServerError, ""))
    );

    await Assert.ThatAsync(
      () => medium.ReadStream().Run().AsTask(),
      Throws.TypeOf<HttpRequestException>()
    );
  }

  // ── Exists ────────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_HeadReturns200_ReturnsTrue()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, ""))
    );

    Assert.That(await medium.Exists().Run(), Is.True);
  }

  [Test]
  public async Task Exists_HeadReturns404_ReturnsFalse()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/missing.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.NotFound, ""))
    );

    Assert.That(await medium.Exists().Run(), Is.False);
  }

  [Test]
  public async Task Exists_NetworkError_ReturnsFalseInsteadOfThrowing()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://unreachable.example.com/data.csv"),
      new HttpClient(new ThrowingHandler())
    );

    // Must not propagate — pre-flight must catch this as a validation failure.
    Assert.That(await medium.Exists().Run(), Is.False);
  }
}
