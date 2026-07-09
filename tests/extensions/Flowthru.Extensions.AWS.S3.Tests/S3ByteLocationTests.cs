using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.S3;
using Flowthru.Data.Storage.S3.Local;
using Flowthru.Prelude;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// Tests for the byte-location capability over S3: the local stub hands
/// back the backing file itself, a composed catalog item resolves its
/// location through the gateway seam — the credential owner — and no
/// object body is transferred along the way.
/// </summary>
[TestFixture]
[Category("AwsS3")]
public class S3ByteLocationTests
{
  private string _root = null!;
  private const string Bucket = "loc-bucket";

  [SetUp]
  public void SetUp()
  {
    _root = Path.Combine(Path.GetTempPath(), $"flowthru-s3-loc-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_root))
    {
      try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }
  }

  [Test]
  public async Task LocateBytes_OverLocalStub_PointsAtTheBackingObjectBytes()
  {
    var gateway = new LocalFileS3Gateway(_root);
    var payload = new byte[] { 10, 20, 30 };
    await gateway.PutObject(Bucket, "orders/2026.bin", new MemoryStream(payload), default);
    var medium = new S3StorageMedium(gateway, Bucket, "orders/2026.bin");

    var located = await medium.LocateBytes().Run();

    var location = ((EffResult<ByteLocation>.Success)located).Value;
    Assert.That(location, Is.InstanceOf<ByteLocation.LocalFile>(),
      "The stub's honest answer is the backing file itself.");
    var path = ((ByteLocation.LocalFile)location).Path;
    Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(payload),
      "The located path must hold the object's actual bytes — a native reader opens it directly.");
  }

  [Test]
  public async Task LocateBytes_ComposedItemOverS3_ResolvesThroughTheGatewaySeam()
  {
    var gateway = new RecordingGateway(
      new ByteLocation.RemoteUri(
        new Uri($"s3://{Bucket}/orders/2026.json"),
        new Dictionary<string, string>
        {
          ["region"] = "eu-west-1",
          ["access_key_id"] = "AKIATEST",
          ["secret_access_key"] = "shh",
        }
      )
    );
    var adapter = new ComposedStorageAdapter<IEnumerable<S3Order>, S3Order>(
      new S3StorageMedium(gateway, Bucket, "orders/2026.json"),
      new JsonFormatSerializer<S3Order>(),
      new EnumerableContainerAdapter<S3Order>()
    );
    var item = new Item<IEnumerable<S3Order>>("s3-orders", adapter);

    var located = await item.LocateBytes().Run();

    var location = (ByteLocation.RemoteUri)((EffResult<ByteLocation>.Success)located).Value;
    Assert.Multiple(() =>
    {
      Assert.That(location.Uri, Is.EqualTo(new Uri($"s3://{Bucket}/orders/2026.json")));
      Assert.That(location.Access["region"], Is.EqualTo("eu-west-1"),
        "The access handoff must be exactly what the gateway minted.");
      Assert.That(gateway.LocateCalls, Is.EqualTo(new[] { (Bucket, "orders/2026.json") }),
        "The medium must delegate to the gateway seam — the credential owner — not ambient state.");
      Assert.That(gateway.GetObjectCalls, Is.Zero,
        "Locating bytes must not transfer the object body.");
    });
  }

  [Test]
  public async Task LocateBytes_DoesNotRequireAnObjectAtTheKey()
  {
    // A write target is addressable before the first PUT — locating it
    // must not fail on absence (existence is Exists()'s question).
    var medium = new S3StorageMedium(new LocalFileS3Gateway(_root), Bucket, "not-put-yet.bin");

    var located = await medium.LocateBytes().Run();

    Assert.That(located, Is.InstanceOf<EffResult<ByteLocation>.Success>());
  }

  /// <summary>
  /// Seam double that hands back a canned location and records every call,
  /// so tests can assert the medium routes byte addressing through the
  /// gateway and transfers no object body while doing it.
  /// </summary>
  private sealed class RecordingGateway(ByteLocation location) : IS3Gateway
  {
    public List<(string Bucket, string Key)> LocateCalls { get; } = new();
    public int GetObjectCalls { get; private set; }

    public Task<ByteLocation> LocateObject(string bucket, string key, CancellationToken ct)
    {
      LocateCalls.Add((bucket, key));
      return Task.FromResult(location);
    }

    public Task<Stream> GetObject(string bucket, string key, CancellationToken ct)
    {
      GetObjectCalls++;
      return Task.FromResult<Stream>(new MemoryStream());
    }

    public Task PutObject(string bucket, string key, Stream content, CancellationToken ct) =>
      Task.CompletedTask;

    public Task<bool> ObjectExists(string bucket, string key, CancellationToken ct) =>
      Task.FromResult(true);

    public Task DeleteObject(string bucket, string key, CancellationToken ct) =>
      Task.CompletedTask;

    public Task<string?> GetETag(string bucket, string key, CancellationToken ct) =>
      Task.FromResult<string?>(null);
  }
}
