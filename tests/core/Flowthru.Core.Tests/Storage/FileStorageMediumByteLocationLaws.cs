using System.Runtime.CompilerServices;
using Flowthru.Data.Storage;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Runs <see cref="ISupportsByteLocationLaws"/> against
/// <see cref="FileStorageMedium"/>. The present probe is a seeded temp
/// file; the absent probe is a path nothing has been written to.
/// </summary>
[TestFixture]
public class FileStorageMediumByteLocationLaws : ISupportsByteLocationLaws
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-loc-laws-{Guid.NewGuid():N}");
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

  protected override ISupportsByteLocation CreateProbe()
  {
    var path = Path.Combine(_tempDir, $"probe-{Guid.NewGuid():N}.bin");
    File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
    return new FileStorageMedium(path);
  }

  protected override ISupportsByteLocation CreateAbsentProbe() =>
    new FileStorageMedium(Path.Combine(_tempDir, $"absent-{Guid.NewGuid():N}.bin"));
}

/// <summary>
/// Runs <see cref="ISupportsByteLocationLaws"/> against the composed
/// adapter over <see cref="FileStorageMedium"/>, proving the capability
/// surfaces through the medium × format × container composition
/// unchanged.
/// </summary>
[TestFixture]
public class ComposedStorageAdapterByteLocationLaws : ISupportsByteLocationLaws
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-loc-laws-composed-{Guid.NewGuid():N}");
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

  protected override ISupportsByteLocation CreateProbe()
  {
    var path = Path.Combine(_tempDir, $"probe-{Guid.NewGuid():N}.bin");
    File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
    return ComposedOver(path);
  }

  protected override ISupportsByteLocation CreateAbsentProbe() =>
    ComposedOver(Path.Combine(_tempDir, $"absent-{Guid.NewGuid():N}.bin"));

  private static ComposedStorageAdapter<IEnumerable<int>, int> ComposedOver(string path) =>
    new(
      new FileStorageMedium(path),
      new StubReader<int>(),
      writer: null,
      new EnumerableContainerAdapter<int>()
    );

  private sealed class StubReader<TRow> : IFormatRowReader<TRow>
    where TRow : notnull
  {
    public StorageTraits Traits => new();

    public async IAsyncEnumerable<TRow> DeserializeRows(
      Stream stream,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      await Task.CompletedTask.ConfigureAwait(false);
      yield break;
    }
  }
}
