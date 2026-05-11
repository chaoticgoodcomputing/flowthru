using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Pins the <c>InspectTarget()</c> contract across every file-based storage
/// adapter in core (<see cref="FileStorageMedium"/>,
/// <see cref="TextFileStorageAdapter"/>,
/// <see cref="BinaryFileStorageAdapter"/>,
/// <see cref="SingletonJsonAdapter{T}"/>).
/// </summary>
/// <remarks>
/// <para>
/// All file-based adapters delegate <c>InspectTarget()</c> to
/// <see cref="LocalFileWriteProbe.ProbeAsync"/> — the canonical
/// write-probe pattern. The contract is:
/// <list type="bullet">
///   <item>Writable directory → <see cref="ValidationResult.Success"/>.</item>
///   <item>Missing-but-creatable directory → <see cref="ValidationResult.Success"/>
///     (probe walks up to nearest existing ancestor; <c>Save()</c> creates
///     intermediate directories at write time).</item>
///   <item>Probe file is cleaned up — no <c>.flowthru-probe-*</c> leakage.</item>
/// </list>
/// </para>
/// <para>
/// <strong>Why this matters.</strong> <c>InspectTarget()</c> is the
/// pre-flight contract for <em>output</em> items — it verifies the
/// destination is ready to be written to before running. File-storage is
/// the simplest case; future targets (EFCore tables, S3 buckets, etc.)
/// extend the same contract with provider-specific readiness checks
/// (connection, schema match, permissions). This suite is the foundation
/// the broader contract extends.
/// </para>
/// </remarks>
[TestFixture]
public class FileStorageTargetInspectionTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-target-{Guid.NewGuid():N}");
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
  // FileStorageMedium
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task FileStorageMedium_InspectTarget_WritableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "output.bin");
    var medium = new FileStorageMedium(path);

    var result = await medium.InspectTarget().Run();

    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True,
      $"Expected valid, got: {FormatErrors(validationResult)}");
  }

  [Test]
  public async Task FileStorageMedium_InspectTarget_MissingButCreatableDirectory_ReturnsSuccess()
  {
    // The parent directory does not yet exist, but _tempDir (the ancestor) does
    // and is writable. WriteStream() calls Directory.CreateDirectory() at
    // runtime, so a missing intermediate directory is not a pre-flight blocker.
    var path = Path.Combine(_tempDir, "nonexistent", "sub", "output.bin");
    var medium = new FileStorageMedium(path);

    var result = await medium.InspectTarget().Run();

    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True,
      $"Expected valid, got: {FormatErrors(validationResult)}");
  }

  [Test]
  public async Task FileStorageMedium_InspectTarget_LeavesNoProbeFile()
  {
    var path = Path.Combine(_tempDir, "output.bin");
    var medium = new FileStorageMedium(path);

    await medium.InspectTarget().Run();

    var probeFiles = Directory.GetFiles(_tempDir, ".flowthru-probe-*");
    Assert.That(probeFiles, Is.Empty,
      "Probe file must be cleaned up after inspection.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // TextFileStorageAdapter
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task TextFileStorageAdapter_InspectTarget_WritableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "output.txt");
    var adapter = new TextFileStorageAdapter(path);

    var result = await adapter.InspectTarget().Run();

    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True,
      $"Expected valid, got: {FormatErrors(validationResult)}");
  }

  [Test]
  public async Task TextFileStorageAdapter_InspectTarget_MissingButCreatableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "nonexistent", "output.txt");
    var adapter = new TextFileStorageAdapter(path);

    var result = await adapter.InspectTarget().Run();

    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True,
      $"Expected valid, got: {FormatErrors(validationResult)}");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // BinaryFileStorageAdapter
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task BinaryFileStorageAdapter_InspectTarget_WritableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "output.bin");
    var adapter = new BinaryFileStorageAdapter(path);

    var result = await adapter.InspectTarget().Run();

    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True,
      $"Expected valid, got: {FormatErrors(validationResult)}");
  }

  [Test]
  public async Task BinaryFileStorageAdapter_InspectTarget_MissingButCreatableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "nonexistent", "output.bin");
    var adapter = new BinaryFileStorageAdapter(path);

    var result = await adapter.InspectTarget().Run();

    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True,
      $"Expected valid, got: {FormatErrors(validationResult)}");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // SingletonJsonAdapter
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task SingletonJsonAdapter_InspectTarget_WritableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "output.json");
    var adapter = new SingletonJsonAdapter<JsonPayload>(path);

    var result = await adapter.InspectTarget().Run();

    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True,
      $"Expected valid, got: {FormatErrors(validationResult)}");
  }

  [Test]
  public async Task SingletonJsonAdapter_InspectTarget_MissingButCreatableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "nonexistent", "output.json");
    var adapter = new SingletonJsonAdapter<JsonPayload>(path);

    var result = await adapter.InspectTarget().Run();

    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True,
      $"Expected valid, got: {FormatErrors(validationResult)}");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static string FormatErrors(ValidationResult result) =>
    string.Join(", ", result.Errors.Select(e => $"{e.ErrorType}: {e.Message}"));

  /// <summary>
  /// Minimal payload type implementing the schema marker required by
  /// <see cref="SingletonJsonAdapter{T}"/>.
  /// </summary>
  private sealed record JsonPayload : IStructuredSerializable;
}
