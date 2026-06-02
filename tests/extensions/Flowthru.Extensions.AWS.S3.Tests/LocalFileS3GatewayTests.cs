using System.Text;
using Flowthru.Data.Storage.S3.Local;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// Edge cases for the shipped <see cref="LocalFileS3Gateway"/> stub beyond the
/// backend-agnostic <see cref="Contract.S3GatewayLaws{TBackend}"/> coverage:
/// atomicity, missing-object reads, idempotent delete, and key-confinement.
/// </summary>
[TestFixture]
[Category("AwsS3")]
public class LocalFileS3GatewayTests
{
  private string _root = null!;
  private LocalFileS3Gateway _gateway = null!;

  [SetUp]
  public void SetUp()
  {
    _root = Path.Combine(Path.GetTempPath(), $"flowthru-s3-stub-{Guid.NewGuid():N}");
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

  [Test]
  public void GetObject_Missing_ThrowsFileNotFound()
  {
    Assert.That(
      async () => await _gateway.GetObject("b", "missing.bin", default),
      Throws.InstanceOf<FileNotFoundException>());
  }

  [Test]
  public async Task Put_CreatesIntermediateDirectories()
  {
    await _gateway.PutObject("b", "deep/nested/key.bin", new MemoryStream([1, 2]), default);
    Assert.That(File.Exists(Path.Combine(_root, "b", "deep", "nested", "key.bin")), Is.True);
  }

  [Test]
  public async Task Put_LeavesNoTempArtifacts()
  {
    await _gateway.PutObject("b", "x.bin", new MemoryStream(Encoding.UTF8.GetBytes("data")), default);
    var stray = Directory.EnumerateFiles(_root, "*.tmp.*", SearchOption.AllDirectories).ToList();
    Assert.That(stray, Is.Empty, "The atomic write should leave no .tmp artifacts behind.");
  }

  [Test]
  public async Task Delete_AbsentObject_IsNoOp()
  {
    Assert.That(async () => await _gateway.DeleteObject("b", "never.bin", default), Throws.Nothing);
  }

  [Test]
  public void ResolvePath_KeyEscapingRoot_Rejected()
  {
    Assert.That(
      async () => await _gateway.PutObject("b", "../../escape.bin", new MemoryStream([1]), default),
      Throws.InstanceOf<ArgumentException>(),
      "A key escaping the gateway root via '..' must be rejected.");
  }

  [Test]
  public async Task GetETag_ChangesWithContent_NullWhenAbsent()
  {
    Assert.That(await _gateway.GetETag("b", "e.bin", default), Is.Null);

    await _gateway.PutObject("b", "e.bin", new MemoryStream(Encoding.UTF8.GetBytes("one")), default);
    var first = await _gateway.GetETag("b", "e.bin", default);

    await _gateway.PutObject("b", "e.bin", new MemoryStream(Encoding.UTF8.GetBytes("two")), default);
    var second = await _gateway.GetETag("b", "e.bin", default);

    Assert.That(first, Is.Not.Null);
    Assert.That(second, Is.Not.EqualTo(first));
  }
}
