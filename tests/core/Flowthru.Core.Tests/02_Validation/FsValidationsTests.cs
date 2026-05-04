using Flowthru.Core.Validation;

namespace Flowthru.Core.Tests.Validation;

/// <summary>
/// Tests for the filesystem-shaped <see cref="FsValidations"/> helpers.
/// </summary>
[TestFixture]
[Category("Validation")]
public class FsValidationsTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-fsvalid-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      Directory.Delete(_tempDir, recursive: true);
    }
  }

  [Test]
  public void IsWritable_OnWritableDirectory_Passes()
  {
    var v = FsValidations.IsWritable(_tempDir);

    Assert.That(v.IsValid, Is.True);
  }

  [Test]
  public void IsWritable_OnMissingDirectory_Fails()
  {
    var v = FsValidations.IsWritable(Path.Combine(_tempDir, "does-not-exist"));

    Assert.That(v.IsValid, Is.False);
    Assert.That(v.Failures[0].Message, Does.Contain("does not exist"));
  }

  [Test]
  public void IsWritable_OnEmptyPath_Fails()
  {
    var v = FsValidations.IsWritable(string.Empty);

    Assert.That(v.IsValid, Is.False);
  }

  [Test]
  public void IsWritable_DoesNotLeaveProbeFile()
  {
    FsValidations.IsWritable(_tempDir);

    var leftovers = Directory.GetFiles(_tempDir, ".flowthru-probe-*");
    Assert.That(leftovers, Is.Empty);
  }

  [Test]
  public void Exists_OnExistingDirectory_Passes()
  {
    var v = FsValidations.Exists(_tempDir);

    Assert.That(v.IsValid, Is.True);
  }

  [Test]
  public void Exists_OnExistingFile_Passes()
  {
    var filePath = Path.Combine(_tempDir, "file.txt");
    File.WriteAllText(filePath, "");

    var v = FsValidations.Exists(filePath);

    Assert.That(v.IsValid, Is.True);
  }

  [Test]
  public void Exists_OnMissingPath_Fails()
  {
    var v = FsValidations.Exists(Path.Combine(_tempDir, "missing"));

    Assert.That(v.IsValid, Is.False);
  }
}
