using System.Net;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Medium;
using Flowthru.Tests.Kits.Medium;

namespace Flowthru.Extensions.Http.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="CachedHttpStorageMedium"/>.
/// </summary>
[TestFixtureSource(nameof(Fixtures))]
public class CachedHttpStorageMediumConformance : StorageMediumConformance
{
  public static IEnumerable<string> Fixtures => new[] { "Synthetic/cached-http-bytes" };

  private string _cacheDir = string.Empty;

  public CachedHttpStorageMediumConformance(string scenarioName) : base(scenarioName) { }

  [SetUp]
  public void SetUp()
  {
    _cacheDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-cached-http-conformance-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(_cacheDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_cacheDir))
    {
      Directory.Delete(_cacheDir, recursive: true);
    }
  }

  protected override IStorageMedium CreateReadable(byte[] data)
  {
    var body = System.Text.Encoding.UTF8.GetString(data);
    var handler = new FakeHandler(HttpStatusCode.OK, body);
    var client = new HttpClient(handler);
    return new CachedHttpStorageMedium(
      new Uri($"https://example.com/data?{Guid.NewGuid():N}"),
      client,
      _cacheDir,
      TimeSpan.FromMinutes(5)
    );
  }

  protected override IStorageMedium CreateNonexistent()
  {
    var handler = new FakeHandler(HttpStatusCode.NotFound, body: string.Empty);
    var client = new HttpClient(handler);
    return new CachedHttpStorageMedium(
      new Uri($"https://example.com/missing?{Guid.NewGuid():N}"),
      client,
      _cacheDir,
      TimeSpan.FromMinutes(5)
    );
  }

  protected override IStorageMedium CreateWritable() => CreateReadable(FixtureBytes);
}
