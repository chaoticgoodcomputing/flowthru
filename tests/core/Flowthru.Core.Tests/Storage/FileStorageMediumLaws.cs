using Flowthru.Data.Storage;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Exercises <see cref="IStorageMediumLaws"/> against
/// <see cref="FileStorageMedium"/>. Each test runs in a freshly-created
/// temp directory and cleans up on tear-down.
/// </summary>
[TestFixture]
public class FileStorageMediumLaws : IStorageMediumLaws
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-mediumlaws-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try
      {
        Directory.Delete(_tempDir, recursive: true);
      }
      catch
      {
        // Best-effort cleanup.
      }
    }
  }

  protected override IStorageMedium CreateMedium()
  {
    var path = Path.Combine(_tempDir, $"medium-{Guid.NewGuid():N}.bin");
    return new FileStorageMedium(path);
  }
}
