using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Tests.Helpers.Adapters;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

/// <summary>
/// Coverage tests for <see cref="TextFileStorageAdapter"/> via the
/// <see cref="StorageAdapterAssertions"/> harness.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("PreFlightInspection")]
public class TextFileStorageAdapterTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-test-{Guid.NewGuid():N}");
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
  public async Task InspectShallow_FileExists_Succeeds()
  {
    var path = await WriteSeed();
    await StorageAdapterAssertions.InspectShallowSucceeds(new TextFileStorageAdapter(path));
  }

  [Test]
  public Task InspectShallow_FileMissing_FailsWithNotFound()
  {
    var path = Path.Combine(_tempDir, "missing.txt");
    return StorageAdapterAssertions.InspectShallowFails(
      new TextFileStorageAdapter(path),
      ValidationErrorType.NotFound
    );
  }

  [Test]
  public async Task InspectDeep_FileExists_Succeeds()
  {
    var path = await WriteSeed();
    await StorageAdapterAssertions.InspectDeepSucceeds(new TextFileStorageAdapter(path));
  }

  [Test]
  public Task InspectTarget_WritableDirectory_Succeeds()
  {
    var path = Path.Combine(_tempDir, "writable.txt");
    return StorageAdapterAssertions.InspectTargetSucceeds(new TextFileStorageAdapter(path));
  }

  [Test]
  public async Task Exists_FilePresent_ReturnsTrue()
  {
    var path = await WriteSeed();
    await StorageAdapterAssertions.ExistsReturns(new TextFileStorageAdapter(path), expected: true);
  }

  [Test]
  public Task Exists_FileMissing_ReturnsFalse()
  {
    var path = Path.Combine(_tempDir, "missing.txt");
    return StorageAdapterAssertions.ExistsReturns(new TextFileStorageAdapter(path), expected: false);
  }

  [Test]
  public Task SaveAndLoad_RoundTripsText()
  {
    var path = Path.Combine(_tempDir, "roundtrip.txt");
    var adapter = new TextFileStorageAdapter(path);
    var data = "Phase 6 task 3 fixture\nTextFileStorageAdapter round-trip\n";

    return StorageAdapterAssertions.SaveAndLoadRoundTrips(adapter, data);
  }

  private async Task<string> WriteSeed()
  {
    var path = Path.Combine(_tempDir, "seed.txt");
    var adapter = new TextFileStorageAdapter(path);
    await adapter.Save("seed content").Run();
    return path;
  }
}
