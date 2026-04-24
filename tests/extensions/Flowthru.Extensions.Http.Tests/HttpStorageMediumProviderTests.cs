using System.Net;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Medium;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.Http.Tests;

/// <summary>
/// Tests for <see cref="HttpStorageMediumProvider"/>.
/// Verifies URI scheme routing and the correct medium type returned based on
/// cache configuration presence.
/// </summary>
[TestFixture]
[Category("Http")]
public class HttpStorageMediumProviderTests
{
  // ── CanHandle ─────────────────────────────────────────────────────────────

  [TestCase("http://example.com/data.csv", true)]
  [TestCase("https://example.com/data.csv", true)]
  [TestCase("file:///local/data.csv", false)]
  [TestCase("sftp://example.com/data.csv", false)]
  public void CanHandle_CorrectlyIdentifiesHttpSchemes(string uri, bool expected)
  {
    var provider = new HttpStorageMediumProvider();
    Assert.That(provider.CanHandle(new Uri(uri)), Is.EqualTo(expected));
  }

  // ── Create — no cache config ──────────────────────────────────────────────

  [Test]
  public void Create_WithoutCacheConfig_ReturnsHttpStorageMedium()
  {
    var provider = new HttpStorageMediumProvider();
    var medium = provider.Create(new Uri("https://example.com/data.csv"));
    Assert.That(medium, Is.InstanceOf<HttpStorageMedium>());
  }

  // ── Create — with cache config ────────────────────────────────────────────

  [Test]
  public void Create_WithCacheConfig_ReturnsCachedHttpStorageMedium()
  {
    var opts = Options.Create(
      new HttpOptions
      {
        Cache = new HttpCacheOptions
        {
          Directory = Path.GetTempPath(),
          MaxAge = TimeSpan.FromHours(1),
        },
      }
    );
    var provider = new HttpStorageMediumProvider(opts);
    var medium = provider.Create(new Uri("https://example.com/data.csv"));
    Assert.That(medium, Is.InstanceOf<CachedHttpStorageMedium>());
  }

  // ── DI-less construction ──────────────────────────────────────────────────

  [Test]
  public void DefaultConstructor_CanHandleHttps()
  {
    var provider = new HttpStorageMediumProvider();
    Assert.That(provider.CanHandle(new Uri("https://example.com/data.csv")), Is.True);
  }
}
