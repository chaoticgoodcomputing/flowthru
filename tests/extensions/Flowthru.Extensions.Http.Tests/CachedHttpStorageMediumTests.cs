using System.Net;
using Flowthru.Data.Storage.Http;
using Flowthru.Prelude;

namespace Flowthru.Extensions.Http.Tests;

/// <summary>
/// Tests for <see cref="CachedHttpStorageMedium"/> — verifies first
/// access populates the cache and subsequent reads serve from disk
/// without re-downloading until the TTL expires.
/// </summary>
[TestFixture]
[Category("Http")]
public class CachedHttpStorageMediumTests
{
  private string _cacheDir = null!;

  [SetUp]
  public void SetUp()
  {
    _cacheDir = Path.Combine(Path.GetTempPath(), $"flowthru-http-cache-{Guid.NewGuid():N}");
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_cacheDir))
    {
      try { Directory.Delete(_cacheDir, recursive: true); } catch { /* best effort */ }
    }
  }

  [Test]
  public async Task ReadStream_FirstAccess_DownloadsAndPopulatesCache()
  {
    const string body = "Id,Name\n1,Alice\n";
    var handler = new FakeHandler(HttpStatusCode.OK, body, etag: "\"v1\"");
    var medium = new CachedHttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(handler),
      _cacheDir,
      TimeSpan.FromHours(1)
    );

    var result = await medium.ReadStream().Run();
    using var stream = ((EffResult<Stream>.Success)result).Value;
    using var reader = new StreamReader(stream);
    var content = await reader.ReadToEndAsync();

    Assert.That(content, Is.EqualTo(body));
    Assert.That(handler.Requests, Has.Count.EqualTo(1),
      "First access should issue exactly one network request.");

    var datFiles = Directory.GetFiles(_cacheDir, "*.dat");
    var metaFiles = Directory.GetFiles(_cacheDir, "*.meta.json");
    Assert.That(datFiles, Has.Length.EqualTo(1), "Body should be persisted to disk.");
    Assert.That(metaFiles, Has.Length.EqualTo(1), "Metadata file should be written.");
  }

  [Test]
  public async Task ReadStream_SubsequentAccess_WithinTTL_ServesFromCacheWithoutNetwork()
  {
    const string body = "Id,Name\n1,Alice\n";
    var handler = new FakeHandler(HttpStatusCode.OK, body, etag: "\"v1\"");
    var medium = new CachedHttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(handler),
      _cacheDir,
      maxAge: TimeSpan.FromHours(1)
    );

    // Prime the cache.
    var first = await medium.ReadStream().Run();
    ((EffResult<Stream>.Success)first).Value.Dispose();
    Assert.That(handler.Requests, Has.Count.EqualTo(1));

    // Within TTL, second read should use cached body and skip the network.
    var second = await medium.ReadStream().Run();
    using var stream = ((EffResult<Stream>.Success)second).Value;
    using var reader = new StreamReader(stream);
    var content = await reader.ReadToEndAsync();

    Assert.That(content, Is.EqualTo(body));
    Assert.That(handler.Requests, Has.Count.EqualTo(1),
      "TTL-fresh cache hits should NOT issue a network request.");
  }

  [Test]
  public async Task Exists_WhenCached_ReturnsTrueWithoutNetworkCall()
  {
    var handler = new FakeHandler(HttpStatusCode.OK, "body", etag: "\"v1\"");
    var medium = new CachedHttpStorageMedium(
      new Uri("https://example.com/data.csv"),
      new HttpClient(handler),
      _cacheDir,
      TimeSpan.FromHours(1)
    );

    // Prime cache.
    ((EffResult<Stream>.Success)(await medium.ReadStream().Run())).Value.Dispose();
    handler.Requests.Clear();

    // Exists should not call HEAD when cache is populated.
    var existsResult = await medium.Exists().Run();
    Assert.That(((EffResult<bool>.Success)existsResult).Value, Is.True);
    Assert.That(handler.Requests, Has.Count.EqualTo(0),
      "Exists should short-circuit to true without a HEAD request when cached.");
  }
}
