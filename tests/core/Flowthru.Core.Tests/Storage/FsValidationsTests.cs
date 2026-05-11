using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Pins the filesystem-shaped validation helpers used by every file-based
/// storage adapter — the building blocks for adapter
/// <c>InspectTarget()</c>. On the FP rewrite the helpers consolidate into
/// <see cref="LocalFileWriteProbe.ProbeAsync"/> (write-probe) and
/// <see cref="FileStorageMedium.Exists"/> (existence check); on the legacy
/// branch they lived under <c>FsValidations.IsWritable</c> and
/// <c>FsValidations.Exists</c>. The behaviour pinned is the same:
/// write-probe-and-clean-up against an existing-ancestor directory, plus
/// a true/false existence check.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why this matters.</strong> These helpers are the lowest-level
/// invariant in the <c>InspectTarget()</c> / <c>Exists()</c> chain. If
/// they leak probe files, misreport access, or misclassify a missing
/// intermediate directory as a pre-flight failure, every file-based
/// adapter inherits the bug. Pinning the helpers directly catches
/// regressions before they propagate into the per-adapter inspection
/// suites.
/// </para>
/// </remarks>
[TestFixture]
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

  // ─────────────────────────────────────────────────────────────────────────
  // Write-probe: LocalFileWriteProbe.ProbeAsync (was FsValidations.IsWritable)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task IsWritable_OnWritableDirectory_Passes()
  {
    var path = Path.Combine(_tempDir, "output.bin");

    var result = await LocalFileWriteProbe.ProbeAsync(path, CancellationToken.None);

    Assert.That(result.IsValid, Is.True,
      $"Expected valid against writable temp dir, got: {FormatErrors(result)}");
  }

  [Test]
  public async Task IsWritable_OnMissingIntermediateDirectory_Passes()
  {
    // The intermediate directory does not exist, but its ancestor (_tempDir)
    // does. Save() creates intermediates at runtime, so this must NOT be a
    // pre-flight blocker — the probe walks up to the nearest writable
    // ancestor. This is a deliberate behaviour change from the legacy
    // FsValidations.IsWritable, which required the exact directory to
    // exist.
    var path = Path.Combine(_tempDir, "nonexistent", "sub", "output.bin");

    var result = await LocalFileWriteProbe.ProbeAsync(path, CancellationToken.None);

    Assert.That(result.IsValid, Is.True,
      $"Expected valid for missing-but-creatable path, got: {FormatErrors(result)}");
  }

  [Test]
  public async Task IsWritable_OnEmptyPath_FailsAsValue()
  {
    // Empty path is a recoverable validation finding, not an exception.
    // Every probe-shaped helper in Flowthru holds the fail-as-value
    // contract: pre-flight aggregates findings into FT3xxx diagnostics,
    // so a thrown ArgumentException would bypass the aggregation surface.
    // FT5xxx (no-throw analyzer) protects this invariant at the source
    // level.
    var result = await LocalFileWriteProbe.ProbeAsync(string.Empty, CancellationToken.None);

    Assert.That(result.IsValid, Is.False,
      "Empty path must surface as a ValidationResult.Failure, not throw.");
    Assert.That(result.Errors.Single().ErrorType,
      Is.EqualTo(ValidationErrorType.WriteAccessDenied));
    Assert.That(result.Errors.Single().Message, Does.Contain("empty"));
  }

  [Test]
  public async Task IsWritable_OnWhitespacePath_FailsAsValue()
  {
    // Whitespace is treated identically to empty — both are unambiguous
    // configuration errors at the adapter boundary.
    var result = await LocalFileWriteProbe.ProbeAsync("   ", CancellationToken.None);

    Assert.That(result.IsValid, Is.False);
    Assert.That(result.Errors.Single().ErrorType,
      Is.EqualTo(ValidationErrorType.WriteAccessDenied));
  }

  [Test]
  public async Task IsWritable_DoesNotLeaveProbeFile()
  {
    var path = Path.Combine(_tempDir, "output.bin");

    await LocalFileWriteProbe.ProbeAsync(path, CancellationToken.None);

    var leftovers = Directory.GetFiles(_tempDir, ".flowthru-probe-*");
    Assert.That(leftovers, Is.Empty,
      "Probe sentinel file must be cleaned up after a successful probe.");
  }

  [Test]
  public async Task IsWritable_DoesNotLeaveProbeFile_EvenForMissingIntermediates()
  {
    // When the probe walks up to find an existing ancestor, the sentinel
    // file lands in that ancestor — it must still be cleaned up there.
    var path = Path.Combine(_tempDir, "nonexistent", "sub", "output.bin");

    await LocalFileWriteProbe.ProbeAsync(path, CancellationToken.None);

    var leftovers = Directory.GetFiles(_tempDir, ".flowthru-probe-*");
    Assert.That(leftovers, Is.Empty,
      "Probe sentinel must be cleaned up in the walked-to ancestor.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Existence: FileStorageMedium.Exists (was FsValidations.Exists)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_OnExistingFile_ReturnsTrue()
  {
    var filePath = Path.Combine(_tempDir, "file.txt");
    File.WriteAllText(filePath, string.Empty);
    var medium = new FileStorageMedium(filePath);

    var result = await medium.Exists().Run();

    Assert.That(result, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)result).Value, Is.True);
  }

  [Test]
  public async Task Exists_OnMissingPath_ReturnsFalse()
  {
    var filePath = Path.Combine(_tempDir, "missing.txt");
    var medium = new FileStorageMedium(filePath);

    var result = await medium.Exists().Run();

    Assert.That(result, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static string FormatErrors(ValidationResult result) =>
    string.Join(", ", result.Errors.Select(e => $"{e.ErrorType}: {e.Message}"));
}
