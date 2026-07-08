using System.Runtime.CompilerServices;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Catalog;

/// <summary>
/// Tests for the <c>.LocateBytes()</c> catalog demand: the file-backed
/// happy path, the incapable-adapter gate, constrained-adapter
/// composition, and the composed adapter's failure-as-value when the
/// medium is not byte-addressable.
/// </summary>
[TestFixture]
public class LocateBytesTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-locate-bytes-{Guid.NewGuid():N}");
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
  public async Task LocateBytes_FileBackedComposedItem_YieldsTheAbsolutePath()
  {
    var path = Path.Combine(_tempDir, "rows.json");
    await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3 });
    var item = FileBackedItem(path);

    var located = await item.LocateBytes().Run();

    var location = ((EffResult<ByteLocation>.Success)located).Value;
    Assert.That(location, Is.InstanceOf<ByteLocation.LocalFile>());
    Assert.That(((ByteLocation.LocalFile)location).Path, Is.EqualTo(Path.GetFullPath(path)));
  }

  [Test]
  public async Task LocateBytes_FileBacked_DoesNotRequireExistingBytes()
  {
    // A write target is addressable before the first write — locating it
    // must not fail on absence (existence is Exists()'s question).
    var path = Path.Combine(_tempDir, "not-written-yet.json");
    var item = FileBackedItem(path);

    var located = await item.LocateBytes().Run();

    Assert.That(located, Is.InstanceOf<EffResult<ByteLocation>.Success>());
  }

  [Test]
  public void LocateBytes_OnNonAddressableMedium_ThrowsAtWireUp()
  {
    var adapter = new ComposedStorageAdapter<IEnumerable<int>, int>(
      new NonAddressableMedium(),
      new StubReader<int>(),
      writer: null,
      new EnumerableContainerAdapter<int>()
    );
    var item = new Item<IEnumerable<int>>("non-addressable", adapter);

    var ex = Assert.Throws<ArgumentException>(() => item.LocateBytes());
    Assert.That(ex!.Message, Does.Contain("non-addressable"),
      "The wire-up error must name the item.");
  }

  [Test]
  public void LocateBytes_OnDirectAdapter_ThrowsAtWireUp()
  {
    var item = new Item<IEnumerable<int>>("in-memory", new MemoryStorageAdapter<IEnumerable<int>>());

    var ex = Assert.Throws<ArgumentException>(() => item.LocateBytes());
    Assert.That(ex!.Message, Does.Contain("in-memory"),
      "The wire-up error must name the item.");
    Assert.That(ex.Message, Does.Contain(nameof(MemoryStorageAdapter<IEnumerable<int>>)),
      "The wire-up error must name the incapable adapter as the reason.");
  }

  [Test]
  public async Task LocateBytes_AfterConstrain_UnwrapsToTheComposedAdapter()
  {
    var path = Path.Combine(_tempDir, "constrained.json");
    var item = FileBackedItem(path).Constrain(t => t with { CanWrite = false });

    // Does not throw — the .Constrain() wrapper is unwrapped to the composed
    // adapter, exactly as AsStream() composes through it.
    var located = await item.LocateBytes().Run();

    var location = ((EffResult<ByteLocation>.Success)located).Value;
    Assert.That(((ByteLocation.LocalFile)location).Path, Is.EqualTo(Path.GetFullPath(path)));
  }

  // ── ComposedStorageAdapter capability surface ──────────────────────────

  [Test]
  public void ComposedAdapter_IsAddressable_ReflectsTheMedium()
  {
    Assert.Multiple(() =>
    {
      Assert.That(ComposedOver(new FileStorageMedium(Path.Combine(_tempDir, "a.json"))).IsAddressable, Is.True);
      Assert.That(ComposedOver(new NonAddressableMedium()).IsAddressable, Is.False);
    });
  }

  [Test]
  public async Task ComposedAdapter_LocateBytes_OnNonAddressableMedium_FailsAsValue()
  {
    var adapter = ComposedOver(new NonAddressableMedium());

    var located = await adapter.LocateBytes().Run();

    Assert.That(located, Is.InstanceOf<EffResult<ByteLocation>.Failure>());
    var error = ((EffResult<ByteLocation>.Failure)located).Error;
    Assert.That(error, Is.InstanceOf<RuntimeError.External>());
    Assert.That(error.Message, Does.Contain(nameof(NonAddressableMedium)),
      "The failure must name the non-addressable medium as the reason.");
  }

  // ── helpers ────────────────────────────────────────────────────────────

  private static IItem<IEnumerable<int>> FileBackedItem(string path) =>
    new Item<IEnumerable<int>>("file-backed", ComposedOver(new FileStorageMedium(path)));

  private static ComposedStorageAdapter<IEnumerable<int>, int> ComposedOver(IStorageMedium medium) =>
    new(medium, new StubReader<int>(), writer: null, new EnumerableContainerAdapter<int>());

  private sealed class NonAddressableMedium : IStorageMedium
  {
    public StorageTraits Traits => new();

    public FlowIO<Stream> ReadStream() =>
      FlowIO.Lift<Stream>(() => new MemoryStream());

    public FlowIO<FlowUnit> WriteStream(Stream stream) =>
      FlowIO.Fail<FlowUnit>(new RuntimeError.External("NonAddressableMedium", new NotSupportedException()));

    public FlowIO<bool> Exists() => FlowIO.Pure(true);
  }

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
