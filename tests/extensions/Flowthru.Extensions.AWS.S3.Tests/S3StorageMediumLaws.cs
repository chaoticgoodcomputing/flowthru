using Flowthru.Data.Storage;
using Flowthru.Data.Storage.S3;
using Flowthru.Data.Storage.S3.Local;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// Exercises <see cref="IStorageMediumLaws"/> against <see cref="S3StorageMedium"/>
/// over the shipped <see cref="LocalFileS3Gateway"/> — existence, write-read
/// round-trip, and write-reachability all run offline in a fresh temp root.
/// </summary>
[TestFixture]
public class S3StorageMediumLaws : IStorageMediumLaws
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = Path.Combine(Path.GetTempPath(), $"flowthru-s3-mediumlaws-{Guid.NewGuid():N}");
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

  protected override IStorageMedium CreateMedium() =>
    new S3StorageMedium(new LocalFileS3Gateway(_root), "laws-bucket", $"medium-{Guid.NewGuid():N}.bin");
}
