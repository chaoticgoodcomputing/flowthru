using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Fingerprint coverage for the composition layer:
/// <see cref="ComposedStorageAdapter{TContainer, TRow}"/> delegates to
/// its underlying medium when fingerprintable; falls back to a FlowIO
/// failure otherwise. <see cref="MemoryStorageAdapter{T}"/>
/// deliberately does not implement the capability —
/// <see cref="IItem{T}.TryGetFingerprint"/> returns <c>null</c> for
/// in-memory items.
/// </summary>
[TestFixture]
public class ComposedAdapterFingerprintTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-fp-composed-{Guid.NewGuid():N}");
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
  public async Task ComposedAdapter_OverFileMedium_DelegatesFingerprint()
  {
    var path = Path.Combine(_tempDir, "rows.json");
    var adapter = new ComposedStorageAdapter<IEnumerable<TestRow>, TestRow>(
      medium: new FileStorageMedium(path),
      format: new JsonFormatSerializer<TestRow>(),
      container: new EnumerableContainerAdapter<TestRow>()
    );
    Assert.That(adapter, Is.InstanceOf<ISupportsFingerprint>(),
      "Composed adapters expose the capability; delegation lives in Fingerprint().");

    await adapter.Save(new[] { new TestRow { Id = 1, Name = "alpha" } }).Run();

    var first = ((EffResult<string>.Success)await ((ISupportsFingerprint)adapter).Fingerprint().Run()).Value;
    var second = ((EffResult<string>.Success)await ((ISupportsFingerprint)adapter).Fingerprint().Run()).Value;
    Assert.That(second, Is.EqualTo(first), "Composed fingerprint must be stable.");
  }

  [Test]
  public async Task ComposedAdapter_OverNonFingerprintMedium_ReturnsFlowIOFailure()
  {
    var adapter = new ComposedStorageAdapter<IEnumerable<TestRow>, TestRow>(
      medium: new NonFingerprintMedium(),
      format: new JsonFormatSerializer<TestRow>(),
      container: new EnumerableContainerAdapter<TestRow>()
    );

    var result = await ((ISupportsFingerprint)adapter).Fingerprint().Run();
    Assert.That(result, Is.InstanceOf<EffResult<string>.Failure>(),
      "When the inner medium lacks ISupportsFingerprint, the composed adapter surfaces "
      + "a FlowIO failure so the cache plan can record 'fingerprint unknown'.");
  }

  [Test]
  public void MemoryStorageAdapter_DoesNotImplement_ISupportsFingerprint()
  {
    var adapter = new MemoryStorageAdapter<int>();
    Assert.That(adapter, Is.Not.InstanceOf<ISupportsFingerprint>(),
      "In-memory adapters have no cross-run identity and explicitly opt out.");
  }

  [Test]
  public void Item_OverMemoryStorage_TryGetFingerprint_ReturnsNull()
  {
    var item = new Item<int>("mem", new MemoryStorageAdapter<int>());
    Assert.That(item.TryGetFingerprint(), Is.Null,
      "Items whose storage adapter doesn't implement ISupportsFingerprint return null — "
      + "the cache plan treats consuming steps as uncacheable.");
  }

  [Test]
  public async Task Item_OverFileStorage_TryGetFingerprint_ReturnsFlowIO()
  {
    var path = Path.Combine(_tempDir, "item.bin");
    File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
    var item = new Item<byte[]>("file", new BinaryFileStorageAdapter(path));

    var io = item.TryGetFingerprint();
    Assert.That(io, Is.Not.Null, "File-backed items participate in the cache plan.");
    var result = await io!.Run();
    Assert.That(result, Is.InstanceOf<EffResult<string>.Success>());
  }

  // Non-fingerprintable storage medium for negative-path tests.
  private sealed class NonFingerprintMedium : IStorageMedium
  {
    public StorageTraits Traits => new();
    public FlowIO<Stream> ReadStream() => FlowIO.Pure<Stream>(new MemoryStream());
    public FlowIO<FlowUnit> WriteStream(Stream stream) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
  }
}
