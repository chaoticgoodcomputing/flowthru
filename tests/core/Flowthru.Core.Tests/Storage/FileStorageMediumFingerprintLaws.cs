using Flowthru.Data.Storage;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Runs <see cref="ISupportsFingerprintLaws"/> against
/// <see cref="FileStorageMedium"/>. The mutator appends a byte to
/// the file so size — and therefore fingerprint — changes.
/// </summary>
[TestFixture]
public class FileStorageMediumFingerprintLaws : ISupportsFingerprintLaws
{
  private string _tempDir = null!;
  private string? _currentPath;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-fp-laws-{Guid.NewGuid():N}");
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

  protected override ISupportsFingerprint CreateProbe()
  {
    _currentPath = Path.Combine(_tempDir, $"probe-{Guid.NewGuid():N}.bin");
    File.WriteAllBytes(_currentPath, new byte[] { 1, 2, 3, 4 });
    return new FileStorageMedium(_currentPath);
  }

  protected override Task Mutate(ISupportsFingerprint probe)
  {
    if (_currentPath is null) throw new InvalidOperationException("CreateProbe was not called.");
    var bytes = File.ReadAllBytes(_currentPath);
    File.WriteAllBytes(_currentPath, [.. bytes, 9]); // append → size changes
    return Task.CompletedTask;
  }
}

/// <summary>
/// Runs <see cref="ISupportsFingerprintLaws"/> against the directory
/// composition. The mutator adds a fresh file so the directory
/// fingerprint must change.
/// </summary>
[TestFixture]
public class DirectoryStorageAdapterFingerprintLaws : ISupportsFingerprintLaws
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-fp-laws-dir-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
    File.WriteAllBytes(Path.Combine(_tempDir, "seed.bin"), new byte[] { 1, 2, 3 });
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }
  }

  protected override ISupportsFingerprint CreateProbe() =>
    new DirectoryStorageAdapter<byte[]>(
      _tempDir,
      "*.bin",
      path => new BinaryFileStorageAdapter(path)
    );

  protected override Task Mutate(ISupportsFingerprint probe)
  {
    File.WriteAllBytes(
      Path.Combine(_tempDir, $"added-{Guid.NewGuid():N}.bin"),
      new byte[] { 99 }
    );
    return Task.CompletedTask;
  }
}
