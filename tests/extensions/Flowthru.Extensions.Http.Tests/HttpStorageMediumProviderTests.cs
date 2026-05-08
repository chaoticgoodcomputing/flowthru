using System.Net;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Http;

namespace Flowthru.Extensions.Http.Tests;

/// <summary>
/// Tests for <see cref="HttpStorageMediumProvider"/> — verifies the
/// scheme dispatch, plain vs. cached medium selection, and that
/// non-HTTP schemes are rejected so the resolver can fall through
/// to other providers.
/// </summary>
[TestFixture]
[Category("Http")]
public class HttpStorageMediumProviderTests
{
  private string _cacheDir = null!;

  [SetUp]
  public void SetUp()
  {
    _cacheDir = Path.Combine(Path.GetTempPath(), $"flowthru-http-prov-{Guid.NewGuid():N}");
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_cacheDir))
    {
      try { Directory.Delete(_cacheDir, recursive: true); } catch { /* best effort */ }
    }
  }

  // ── CanHandle ─────────────────────────────────────────────────────

  [TestCase("http://example.com/data.csv", ExpectedResult = true)]
  [TestCase("https://example.com/data.csv", ExpectedResult = true)]
  [TestCase("file:///tmp/data.csv", ExpectedResult = false)]
  [TestCase("ftp://example.com/data.csv", ExpectedResult = false)]
  [TestCase("s3://bucket/data.csv", ExpectedResult = false)]
  public bool CanHandle_OnlyTrueForHttpAndHttps(string uri)
  {
    var provider = new HttpStorageMediumProvider(
      new HttpClient(new FakeHandler(HttpStatusCode.OK, ""))
    );
    return provider.CanHandle(new Uri(uri));
  }

  // ── Create ────────────────────────────────────────────────────────

  [Test]
  public void Create_NoCache_ReturnsPlainHttpStorageMedium()
  {
    var provider = new HttpStorageMediumProvider(
      new HttpClient(new FakeHandler(HttpStatusCode.OK, ""))
    );

    var medium = provider.Create(new Uri("https://example.com/data.csv"));
    Assert.That(medium, Is.InstanceOf<HttpStorageMedium>());
    Assert.That(medium, Is.Not.InstanceOf<CachedHttpStorageMedium>());
  }

  [Test]
  public void Create_WithCache_ReturnsCachedHttpStorageMedium()
  {
    var provider = new HttpStorageMediumProvider(
      new HttpClient(new FakeHandler(HttpStatusCode.OK, "")),
      cache: new HttpCacheOptions { Directory = _cacheDir }
    );

    var medium = provider.Create(new Uri("https://example.com/data.csv"));
    Assert.That(medium, Is.InstanceOf<CachedHttpStorageMedium>());
  }
}
