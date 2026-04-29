using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

/// <summary>
/// Coverage tests for <see cref="BinaryFileStorageAdapter"/> via the
/// <see cref="StorageAdapterAssertions"/> harness.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("PreFlightInspection")]
public class BinaryFileStorageAdapterTests
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
    await StorageAdapterAssertions.InspectShallowSucceeds(new BinaryFileStorageAdapter(path));
  }

  [Test]
  public Task InspectShallow_FileMissing_FailsWithNotFound()
  {
    var path = Path.Combine(_tempDir, "missing.bin");
    return StorageAdapterAssertions.InspectShallowFails(
      new BinaryFileStorageAdapter(path),
      ValidationErrorType.NotFound
    );
  }

  [Test]
  public async Task InspectDeep_FileExists_Succeeds()
  {
    var path = await WriteSeed();
    await StorageAdapterAssertions.InspectDeepSucceeds(new BinaryFileStorageAdapter(path));
  }

  [Test]
  public Task InspectTarget_WritableDirectory_Succeeds()
  {
    var path = Path.Combine(_tempDir, "writable.bin");
    return StorageAdapterAssertions.InspectTargetSucceeds(new BinaryFileStorageAdapter(path));
  }

  [Test]
  public async Task Exists_FilePresent_ReturnsTrue()
  {
    var path = await WriteSeed();
    await StorageAdapterAssertions.ExistsReturns(new BinaryFileStorageAdapter(path), expected: true);
  }

  [Test]
  public Task Exists_FileMissing_ReturnsFalse()
  {
    var path = Path.Combine(_tempDir, "missing.bin");
    return StorageAdapterAssertions.ExistsReturns(new BinaryFileStorageAdapter(path), expected: false);
  }

  [Test]
  public Task SaveAndLoad_RoundTripsBytes()
  {
    var path = Path.Combine(_tempDir, "roundtrip.bin");
    var adapter = new BinaryFileStorageAdapter(path);
    var data = new byte[] { 1, 2, 3, 4, 5, 250, 251, 252, 253, 254, 255 };

    return StorageAdapterAssertions.SaveAndLoadRoundTrips(
      adapter,
      data,
      comparer: ByteArrayComparer.Instance
    );
  }

  private async Task<string> WriteSeed()
  {
    var path = Path.Combine(_tempDir, "seed.bin");
    var adapter = new BinaryFileStorageAdapter(path);
    await adapter.Save(new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }).Run();
    return path;
  }

  private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
  {
    public static readonly ByteArrayComparer Instance = new();

    public bool Equals(byte[]? x, byte[]? y)
    {
      if (ReferenceEquals(x, y)) return true;
      if (x is null || y is null) return false;
      return x.AsSpan().SequenceEqual(y);
    }

    public int GetHashCode(byte[] obj) => obj.Length.GetHashCode();
  }
}
