using Flowthru.Data.Storage;
using Flowthru.Data.Storage.S3;
using Flowthru.Data.Storage.S3.Local;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// Runs <see cref="ISupportsFingerprintLaws"/> against <see cref="S3StorageMedium"/>
/// over the local stub. The mutator overwrites the object with different bytes so
/// the content hash — and therefore the ETag-derived fingerprint — changes.
/// </summary>
[TestFixture]
public class S3FingerprintLaws : ISupportsFingerprintLaws
{
  private string _root = null!;
  private LocalFileS3Gateway _gateway = null!;
  private string _currentKey = null!;
  private const string Bucket = "fp-bucket";

  [SetUp]
  public void SetUp()
  {
    _root = Path.Combine(Path.GetTempPath(), $"flowthru-s3-fp-laws-{Guid.NewGuid():N}");
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

  protected override ISupportsFingerprint CreateProbe()
  {
    _currentKey = $"probe-{Guid.NewGuid():N}.bin";
    // Fingerprint requires an existing object — seed content before returning.
    _gateway.PutObject(Bucket, _currentKey, new MemoryStream([1, 2, 3, 4]), default)
      .GetAwaiter().GetResult();
    return new S3StorageMedium(_gateway, Bucket, _currentKey);
  }

  protected override async Task Mutate(ISupportsFingerprint probe)
  {
    if (_currentKey is null) throw new InvalidOperationException("CreateProbe was not called.");
    await _gateway.PutObject(Bucket, _currentKey, new MemoryStream([1, 2, 3, 4, 9]), default);
  }
}
