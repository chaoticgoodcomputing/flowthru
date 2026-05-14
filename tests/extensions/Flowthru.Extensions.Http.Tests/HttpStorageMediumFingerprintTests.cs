using System.Net;
using System.Net.Http.Headers;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Http;
using Flowthru.Prelude;

namespace Flowthru.Extensions.Http.Tests;

/// <summary>
/// Fingerprint tests for <see cref="HttpStorageMedium"/> and
/// <see cref="CachedHttpStorageMedium"/>. The shared shape:
/// fingerprints derive from <c>ETag</c> / <c>Last-Modified</c>
/// headers, are stable when the validators don't change, change
/// when the validators rotate, and surface a FlowIO failure when
/// the server provides neither.
/// </summary>
[TestFixture]
[Category("Http")]
public class HttpStorageMediumFingerprintTests
{
  private string _cacheDir = null!;

  [SetUp]
  public void SetUp()
  {
    _cacheDir = Path.Combine(Path.GetTempPath(), $"flowthru-fp-http-{Guid.NewGuid():N}");
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_cacheDir))
    {
      try { Directory.Delete(_cacheDir, recursive: true); } catch { /* best effort */ }
    }
  }

  // ── HttpStorageMedium (uncached) ─────────────────────────────────────

  [Test]
  public void HttpStorageMedium_Implements_ISupportsFingerprint()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, ""))
    );
    Assert.That(medium, Is.InstanceOf<ISupportsFingerprint>());
  }

  [Test]
  public async Task HttpStorageMedium_Fingerprint_DerivedFromETag()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, "", etag: "\"v1\""))
    );

    var first = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;
    var second = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;
    Assert.That(second, Is.EqualTo(first), "Stable ETag → stable fingerprint.");
    Assert.That(first, Has.Length.EqualTo(64));
  }

  [Test]
  public async Task HttpStorageMedium_Fingerprint_ChangesWhenETagRotates()
  {
    var v1Handler = new FakeHandler(HttpStatusCode.OK, "", etag: "\"v1\"");
    var v1Medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"), new HttpClient(v1Handler)
    );
    var before = ((EffResult<string>.Success)await v1Medium.Fingerprint().Run()).Value;

    var v2Handler = new FakeHandler(HttpStatusCode.OK, "", etag: "\"v2\"");
    var v2Medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"), new HttpClient(v2Handler)
    );
    var after = ((EffResult<string>.Success)await v2Medium.Fingerprint().Run()).Value;

    Assert.That(after, Is.Not.EqualTo(before),
      "Server-side ETag change must yield a different fingerprint.");
  }

  [Test]
  public async Task HttpStorageMedium_Fingerprint_NoValidators_ReturnsFlowIOFailure()
  {
    // FakeHandler with no etag and no Last-Modified header.
    var medium = new HttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, ""))
    );

    var result = await medium.Fingerprint().Run();
    Assert.That(result, Is.InstanceOf<EffResult<string>.Failure>(),
      "Servers that provide neither ETag nor Last-Modified are uncacheable — "
      + "fingerprint surfaces a FlowIO failure; the cache plan records 'unknown'.");
  }

  [Test]
  public async Task HttpStorageMedium_Fingerprint_NetworkFailure_SurfacesAsFlowIOFailure()
  {
    var medium = new HttpStorageMedium(
      new Uri("https://unreachable.example.com/data.csv"),
      new HttpClient(new ThrowingHandler())
    );

    var result = await medium.Fingerprint().Run();
    Assert.That(result, Is.InstanceOf<EffResult<string>.Failure>(),
      "Transient network errors surface as FlowIO failures, not exceptions.");
  }

  // ── CachedHttpStorageMedium ───────────────────────────────────────────

  [Test]
  public void CachedHttpStorageMedium_Implements_ISupportsFingerprint()
  {
    var medium = new CachedHttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, "")),
      _cacheDir,
      TimeSpan.FromHours(1)
    );
    Assert.That(medium, Is.InstanceOf<ISupportsFingerprint>());
  }

  [Test]
  public async Task CachedHttpStorageMedium_Fingerprint_CacheHitUsesPersistedValidator()
  {
    const string body = "Id,Name\n1,Alice\n";
    var handler = new FakeHandler(HttpStatusCode.OK, body, etag: "\"v1\"");
    var medium = new CachedHttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(handler),
      _cacheDir,
      TimeSpan.FromHours(1)
    );

    // Prime cache (populates ETag in .meta.json).
    ((EffResult<Stream>.Success)(await medium.ReadStream().Run())).Value.Dispose();
    var requestsBeforeFingerprint = handler.Requests.Count;

    var fpResult = await medium.Fingerprint().Run();
    Assert.That(fpResult, Is.InstanceOf<EffResult<string>.Success>());
    Assert.That(handler.Requests, Has.Count.EqualTo(requestsBeforeFingerprint),
      "Cache-hit fingerprint must NOT issue a network request — validator is already in .meta.json.");
  }

  [Test]
  public async Task CachedHttpStorageMedium_Fingerprint_ColdCache_QueriesValidatorFromServer()
  {
    var handler = new FakeHandler(HttpStatusCode.OK, "", etag: "\"server-v1\"");
    var medium = new CachedHttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(handler),
      _cacheDir,
      TimeSpan.FromHours(1)
    );

    var fpResult = await medium.Fingerprint().Run();
    Assert.That(fpResult, Is.InstanceOf<EffResult<string>.Success>());
    Assert.That(handler.Requests, Has.Count.AtLeast(1),
      "Cold-cache fingerprint must reach the server to obtain the validator.");
  }

  [Test]
  public async Task CachedHttpStorageMedium_Fingerprint_NoValidators_ReturnsFlowIOFailure()
  {
    var medium = new CachedHttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(new FakeHandler(HttpStatusCode.OK, "no validators here")),
      _cacheDir,
      TimeSpan.FromHours(1)
    );

    var fpResult = await medium.Fingerprint().Run();
    Assert.That(fpResult, Is.InstanceOf<EffResult<string>.Failure>(),
      "Server returning neither ETag nor Last-Modified ⇒ FlowIO failure.");
  }

  [Test]
  public async Task CachedHttpStorageMedium_Fingerprint_LastModified_AcceptedAsValidator()
  {
    var lastModified = "Wed, 06 Nov 2024 12:34:56 GMT";
    var response = new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("body"),
    };
    response.Content.Headers.LastModified = DateTimeOffset.Parse(lastModified);

    var handler = new FakeHandler(HttpStatusCode.OK, "") { NextResponse = response };
    var medium = new CachedHttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(handler),
      _cacheDir,
      TimeSpan.FromHours(1)
    );

    var result = await medium.Fingerprint().Run();
    Assert.That(result, Is.InstanceOf<EffResult<string>.Success>(),
      "Last-Modified alone is a valid fingerprint source.");
  }
}
