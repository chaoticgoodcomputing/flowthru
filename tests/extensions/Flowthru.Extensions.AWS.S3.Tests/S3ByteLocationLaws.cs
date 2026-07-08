using Flowthru.Data.Storage;
using Flowthru.Data.Storage.S3;
using Flowthru.Data.Storage.S3.Local;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// Runs <see cref="ISupportsByteLocationLaws"/> against
/// <see cref="S3StorageMedium"/> over the local stub. The present probe
/// is a seeded object; the absent probe is a key nothing has been PUT
/// to — a write target must locate before the first write.
/// </summary>
[TestFixture]
[Category("AwsS3")]
public class S3ByteLocationLaws : ISupportsByteLocationLaws
{
  private string _root = null!;
  private LocalFileS3Gateway _gateway = null!;
  private const string Bucket = "loc-bucket";

  [SetUp]
  public void SetUp()
  {
    _root = Path.Combine(Path.GetTempPath(), $"flowthru-s3-loc-laws-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_root);
    _gateway = new LocalFileS3Gateway(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_root))
    {
      try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }
  }

  protected override ISupportsByteLocation CreateProbe()
  {
    var key = $"probe-{Guid.NewGuid():N}.bin";
    _gateway.PutObject(Bucket, key, new MemoryStream([1, 2, 3, 4]), default)
      .GetAwaiter().GetResult();
    return new S3StorageMedium(_gateway, Bucket, key);
  }

  protected override ISupportsByteLocation CreateAbsentProbe() =>
    new S3StorageMedium(_gateway, Bucket, $"absent-{Guid.NewGuid():N}.bin");
}
