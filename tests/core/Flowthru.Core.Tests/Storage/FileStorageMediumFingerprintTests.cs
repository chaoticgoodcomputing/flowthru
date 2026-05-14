using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Unit tests for <see cref="FileStorageMedium"/>'s
/// <see cref="ISupportsFingerprint"/> implementation. Verifies the
/// three core fingerprint properties — stability across repeat
/// calls, sensitivity to file changes, and FlowIO-failure behaviour
/// when the file is missing — plus the documented mtime+size
/// limitation.
/// </summary>
[TestFixture]
public class FileStorageMediumFingerprintTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-fp-file-{Guid.NewGuid():N}");
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

  [Test]
  public void Implements_ISupportsFingerprint()
  {
    var medium = new FileStorageMedium(Path.Combine(_tempDir, "x.txt"));
    Assert.That(medium, Is.InstanceOf<ISupportsFingerprint>(),
      "FileStorageMedium opts into the cache-plan capability per the smart-caching RFC.");
  }

  [Test]
  public async Task Fingerprint_StableAcrossRepeatCallsWithoutChange()
  {
    var path = Path.Combine(_tempDir, "stable.txt");
    File.WriteAllText(path, "hello");
    var medium = new FileStorageMedium(path);

    var first = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;
    var second = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;

    Assert.That(second, Is.EqualTo(first),
      "Repeat calls without intervening file change must return the same fingerprint.");
  }

  [Test]
  public async Task Fingerprint_ChangesWhenContentSizeChanges()
  {
    var path = Path.Combine(_tempDir, "size-change.txt");
    File.WriteAllText(path, "hello");
    var medium = new FileStorageMedium(path);
    var before = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;

    File.WriteAllText(path, "hello, world");
    var after = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;

    Assert.That(after, Is.Not.EqualTo(before),
      "A length-changing edit must produce a different fingerprint.");
  }

  [Test]
  public async Task Fingerprint_ChangesWhenMtimeChanges()
  {
    var path = Path.Combine(_tempDir, "mtime-change.txt");
    File.WriteAllText(path, "samesize");
    var medium = new FileStorageMedium(path);
    var before = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;

    // Advance the mtime explicitly so the test doesn't depend on
    // clock resolution differences across platforms.
    File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(60));
    var after = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;

    Assert.That(after, Is.Not.EqualTo(before),
      "A mtime-only change must produce a different fingerprint.");
  }

  [Test]
  public async Task Fingerprint_DocumentedLimitation_SameMtimeAndSize_ReturnsSameValue()
  {
    // mtime+size collisions are a documented limitation: in-place
    // byte edits preserving file metadata produce a false hit. This
    // test pins the behaviour so any future strengthening (content
    // hashing) trips a deliberate update rather than silent drift.
    var path = Path.Combine(_tempDir, "false-hit.txt");
    File.WriteAllText(path, "abcdef");
    var medium = new FileStorageMedium(path);
    var pinnedMtime = File.GetLastWriteTimeUtc(path);
    var before = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;

    // Edit content but preserve length + mtime — exactly the
    // collision case the RFC accepts.
    File.WriteAllText(path, "uvwxyz");
    File.SetLastWriteTimeUtc(path, pinnedMtime);
    var after = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;

    Assert.That(after, Is.EqualTo(before),
      "Documented limitation: mtime+size-preserving in-place edits produce a false hit. "
      + "If this test starts failing because the fingerprint now detects the change, "
      + "the limitation has been resolved — update the RFC's risks section.");
  }

  [Test]
  public async Task Fingerprint_MissingFile_ReturnsFlowIOFailure()
  {
    var medium = new FileStorageMedium(Path.Combine(_tempDir, "does-not-exist.txt"));
    var result = await medium.Fingerprint().Run();

    Assert.That(result, Is.InstanceOf<EffResult<string>.Failure>(),
      "Fingerprinting a missing file surfaces a FlowIO failure so the cache plan "
      + "records 'fingerprint unknown' and treats the dependent step as a miss.");
  }

  [Test]
  public async Task Fingerprint_ReturnsHexEncodedSha256()
  {
    var path = Path.Combine(_tempDir, "hexcheck.bin");
    File.WriteAllText(path, "abc");
    var medium = new FileStorageMedium(path);
    var value = ((EffResult<string>.Success)await medium.Fingerprint().Run()).Value;

    Assert.That(value, Has.Length.EqualTo(64),
      "SHA-256 hex digest is exactly 64 lowercase hex characters.");
    Assert.That(value, Does.Match("^[0-9a-f]{64}$"));
  }
}
