using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Verifies <see cref="DirectoryStorageAdapter{T}"/>'s fingerprint
/// composition. The adapter walks the directory once per call,
/// digests each child's mtime+size, and returns a SHA-256 over the
/// composed payload — sensitive to additions, removals, and
/// in-place modifications.
/// </summary>
[TestFixture]
public class DirectoryStorageAdapterFingerprintTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-fp-dir-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }
  }

  private DirectoryStorageAdapter<byte[]> CreateAdapter() =>
    new(_tempDir, "*.bin", path => new BinaryFileStorageAdapter(path));

  [Test]
  public void Implements_ISupportsFingerprint()
  {
    var adapter = CreateAdapter();
    Assert.That(adapter, Is.InstanceOf<ISupportsFingerprint>());
  }

  [Test]
  public async Task Fingerprint_StableAcrossRepeatCalls()
  {
    File.WriteAllBytes(Path.Combine(_tempDir, "a.bin"), new byte[] { 1, 2, 3 });
    File.WriteAllBytes(Path.Combine(_tempDir, "b.bin"), new byte[] { 4, 5, 6 });
    var adapter = CreateAdapter();

    var first = ((EffResult<string>.Success)await adapter.Fingerprint().Run()).Value;
    var second = ((EffResult<string>.Success)await adapter.Fingerprint().Run()).Value;

    Assert.That(second, Is.EqualTo(first));
  }

  [Test]
  public async Task Fingerprint_ChangesWhenFileAdded()
  {
    File.WriteAllBytes(Path.Combine(_tempDir, "a.bin"), new byte[] { 1, 2, 3 });
    var adapter = CreateAdapter();
    var before = ((EffResult<string>.Success)await adapter.Fingerprint().Run()).Value;

    File.WriteAllBytes(Path.Combine(_tempDir, "new.bin"), new byte[] { 9 });
    var after = ((EffResult<string>.Success)await adapter.Fingerprint().Run()).Value;

    Assert.That(after, Is.Not.EqualTo(before));
  }

  [Test]
  public async Task Fingerprint_ChangesWhenFileRemoved()
  {
    File.WriteAllBytes(Path.Combine(_tempDir, "a.bin"), new byte[] { 1, 2, 3 });
    File.WriteAllBytes(Path.Combine(_tempDir, "b.bin"), new byte[] { 4, 5, 6 });
    var adapter = CreateAdapter();
    var before = ((EffResult<string>.Success)await adapter.Fingerprint().Run()).Value;

    File.Delete(Path.Combine(_tempDir, "a.bin"));
    var after = ((EffResult<string>.Success)await adapter.Fingerprint().Run()).Value;

    Assert.That(after, Is.Not.EqualTo(before));
  }

  [Test]
  public async Task Fingerprint_ChangesWhenChildContentChanges()
  {
    var aPath = Path.Combine(_tempDir, "a.bin");
    File.WriteAllBytes(aPath, new byte[] { 1, 2, 3 });
    var adapter = CreateAdapter();
    var before = ((EffResult<string>.Success)await adapter.Fingerprint().Run()).Value;

    File.WriteAllBytes(aPath, new byte[] { 1, 2, 3, 4 }); // length differs
    var after = ((EffResult<string>.Success)await adapter.Fingerprint().Run()).Value;

    Assert.That(after, Is.Not.EqualTo(before));
  }

  [Test]
  public async Task Fingerprint_EmptyDirectory_ReturnsStableValue()
  {
    var adapter = CreateAdapter();
    var first = ((EffResult<string>.Success)await adapter.Fingerprint().Run()).Value;
    var second = ((EffResult<string>.Success)await adapter.Fingerprint().Run()).Value;

    Assert.That(first, Is.EqualTo(second));
    Assert.That(first, Has.Length.EqualTo(64));
  }

  [Test]
  public async Task Fingerprint_MissingDirectory_ReturnsFlowIOFailure()
  {
    Directory.Delete(_tempDir, recursive: true);
    var adapter = CreateAdapter();

    var result = await adapter.Fingerprint().Run();
    Assert.That(result, Is.InstanceOf<EffResult<string>.Failure>());
  }
}
