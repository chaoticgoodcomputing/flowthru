using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Medium;
using Flowthru.Core.Data.Validation;

namespace Flowthru.Core.Tests.Validation.TargetInspection;

/// <summary>
/// Tests verifying <c>InspectTarget()</c> on file-based storage adapters.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FileStorageMedium"/>, <see cref="TextFileStorageAdapter"/>,
/// <see cref="BinaryFileStorageAdapter"/>, and <see cref="SingletonJsonStorageAdapter"/> all
/// implement the same probe-file pattern: write a zero-byte sentinel file to the destination
/// directory and delete it. The common contracts tested here are:
/// </para>
/// <list type="bullet">
/// <item>Writable parent directory → <c>Success()</c></item>
/// <item>Non-existent parent directory → <c>NotFound</c> failure</item>
/// </list>
/// </remarks>
[TestFixture]
[Category("Validation")]
[Category("TargetInspection")]
public class FileStorageTargetInspectionTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru_target_{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
      Directory.Delete(_tempDir, recursive: true);
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

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task FileStorageMedium_InspectTarget_MissingButCreatableDirectory_ReturnsSuccess()
  {
    // The parent directory does not yet exist, but _tempDir (the ancestor) does and is writable.
    // Save() calls Directory.CreateDirectory() at runtime, so a missing intermediate
    // directory is not a pre-flight blocker.
    var path = Path.Combine(_tempDir, "nonexistent", "sub", "output.bin");
    var medium = new FileStorageMedium(path);

    var result = await medium.InspectTarget().Run();

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task FileStorageMedium_InspectTarget_LeavesNoProbeFile()
  {
    var path = Path.Combine(_tempDir, "output.bin");
    var medium = new FileStorageMedium(path);

    await medium.InspectTarget().Run();

    var probeFiles = Directory.GetFiles(_tempDir, ".flowthru-probe-*");
    Assert.That(probeFiles, Is.Empty, "Probe file must be cleaned up after inspection");
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

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task TextFileStorageAdapter_InspectTarget_MissingButCreatableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "nonexistent", "output.txt");
    var adapter = new TextFileStorageAdapter(path);

    var result = await adapter.InspectTarget().Run();

    Assert.That(result.IsValid, Is.True);
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

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task BinaryFileStorageAdapter_InspectTarget_MissingButCreatableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "nonexistent", "output.bin");
    var adapter = new BinaryFileStorageAdapter(path);

    var result = await adapter.InspectTarget().Run();

    Assert.That(result.IsValid, Is.True);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // SingletonJsonStorageAdapter
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task SingletonJsonStorageAdapter_InspectTarget_WritableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "output.json");
    var adapter = new SingletonJsonStorageAdapter<JsonPayload>(path);

    var result = await adapter.InspectTarget().Run();

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task SingletonJsonStorageAdapter_InspectTarget_MissingButCreatableDirectory_ReturnsSuccess()
  {
    var path = Path.Combine(_tempDir, "nonexistent", "output.json");
    var adapter = new SingletonJsonStorageAdapter<JsonPayload>(path);

    var result = await adapter.InspectTarget().Run();

    Assert.That(result.IsValid, Is.True);
  }

  private record JsonPayload : IStructuredSerializable;
}
