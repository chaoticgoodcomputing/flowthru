using System.Runtime.CompilerServices;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Catalog;

/// <summary>
/// Tests for the <c>.AsStream()</c> catalog view (#116): the deferred
/// streaming <c>Load()</c>, the capability gate, schema-mismatch translation,
/// fan-out re-acquisition, and read-only semantics.
/// </summary>
[TestFixture]
public class AsStreamTests
{
  [Test]
  public async Task AsStream_LoadThenCompile_YieldsRows()
  {
    var item = StreamingSource(new StubMedium(), new[] { 1, 2, 3 });

    var source = await LoadSource(item.AsStream());
    var rows = await source.Compile().ToList().Run();

    Assert.That(((EffResult<IReadOnlyList<int>>.Success)rows).Value, Is.EqualTo(new[] { 1, 2, 3 }));
  }

  [Test]
  public async Task AsStream_Load_DoesNotOpenMedium_UntilCompiledAndRun()
  {
    var medium = new StubMedium();
    var item = StreamingSource(medium, new[] { 1, 2 }).AsStream();

    var source = await LoadSource(item);
    Assert.That(medium.ReadStreamCalls, Is.EqualTo(0), "Load returns a description; nothing opens yet.");

    await source.Compile().Drain().Run();
    Assert.That(medium.ReadStreamCalls, Is.EqualTo(1));
  }

  [Test]
  public async Task AsStream_FanOut_ReAcquiresPerCompile()
  {
    var medium = new StubMedium();
    var source = await LoadSource(StreamingSource(medium, new[] { 1, 2 }).AsStream());

    await source.Compile().Drain().Run();
    await source.Compile().Drain().Run();

    Assert.That(medium.ReadStreamCalls, Is.EqualTo(2), "Each consumer re-acquires the source.");
  }

  [Test]
  public async Task AsStream_MediumStream_IsDisposedAfterRun()
  {
    var medium = new StubMedium();
    var source = await LoadSource(StreamingSource(medium, new[] { 1, 2 }).AsStream());

    await source.Compile().Drain().Run();

    Assert.That(medium.Opened.Single().Disposed, Is.True);
  }

  [Test]
  public async Task AsStream_SchemaMismatch_SurfacesAsTypedRuntimeError()
  {
    var source = await LoadSource(
      StreamingSource(new StubMedium(), Array.Empty<int>(), throwSchemaMismatch: true).AsStream()
    );

    var result = await source.Compile().ToList().Run();

    Assert.That(result, Is.InstanceOf<EffResult<IReadOnlyList<int>>.Failure>());
    Assert.That(
      ((EffResult<IReadOnlyList<int>>.Failure)result).Error,
      Is.InstanceOf<RuntimeError.SchemaMismatch>(),
      "The storage layer must translate SchemaMismatchException to the typed variant, not External."
    );
  }

  [Test]
  public void AsStream_OnNonStreamingFormat_ThrowsAtWireUp()
  {
    var adapter = new ComposedStorageAdapter<IEnumerable<int>, int>(
      new StubMedium(),
      new StubNonStreamingReader<int>(),
      writer: null,
      new EnumerableContainerAdapter<int>()
    );
    var item = new Item<IEnumerable<int>>("non-streaming", adapter);

    Assert.Throws<ArgumentException>(() => item.AsStream());
  }

  [Test]
  public async Task AsStream_Item_IsReadOnly_SaveFails()
  {
    var streaming = StreamingSource(new StubMedium(), new[] { 1 }).AsStream();

    var result = await streaming.Save(FlowSource.Empty<int>()).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
  }

  [Test]
  public void AsStream_AfterConstrain_UnwrapsToTheComposedFormat()
  {
    var item = StreamingSource(new StubMedium(), new[] { 1 })
      .Constrain(t => t with { CanWrite = false });

    // Does not throw — the .Constrain() wrapper is unwrapped to the composed format.
    Assert.DoesNotThrow(() => item.AsStream());
  }

  // ── StreamingItem node-delegation ──────────────────────────────────────

  [Test]
  public async Task StreamingItem_Exists_DelegatesToOrigin()
  {
    var item = StreamingSource(new StubMedium(), new[] { 1 }).AsStream();
    var result = await item.Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.True);
  }

  [Test]
  public async Task StreamingItem_LoadUntyped_BoxesTheSource()
  {
    var item = StreamingSource(new StubMedium(), new[] { 1, 2 }).AsStream();
    var result = await item.LoadUntyped().Run();
    Assert.That(((EffResult<object>.Success)result).Value, Is.InstanceOf<FlowSource<int>>());
  }

  [Test]
  public void StreamingItem_DataType_IsFlowSource()
  {
    var item = StreamingSource(new StubMedium(), new[] { 1 }).AsStream();
    Assert.That(((IItem)item).DataType, Is.EqualTo(typeof(FlowSource<int>)));
  }

  [Test]
  public async Task StreamingItem_SaveUntyped_Fails()
  {
    var item = StreamingSource(new StubMedium(), new[] { 1 }).AsStream();
    var result = await item.SaveUntyped(FlowSource.Empty<int>()).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
  }

  // ── helpers ────────────────────────────────────────────────────────────

  private static IItem<IEnumerable<int>> StreamingSource(
    StubMedium medium,
    IReadOnlyList<int> rows,
    bool throwSchemaMismatch = false
  )
  {
    var adapter = new ComposedStorageAdapter<IEnumerable<int>, int>(
      medium,
      new StubStreamingReader<int>(rows, throwSchemaMismatch),
      writer: null,
      new EnumerableContainerAdapter<int>()
    );
    return new Item<IEnumerable<int>>("stream-test", adapter);
  }

  private static async Task<FlowSource<int>> LoadSource(IReadOnlyItem<FlowSource<int>> item)
  {
    var result = await item.Load().Run();
    return ((EffResult<FlowSource<int>>.Success)result).Value;
  }

  private sealed class TrackingStream : MemoryStream
  {
    public bool Disposed { get; private set; }

    protected override void Dispose(bool disposing)
    {
      if (disposing) Disposed = true;
      base.Dispose(disposing);
    }
  }

  private sealed class StubMedium : IStorageMedium
  {
    public int ReadStreamCalls { get; private set; }
    public List<TrackingStream> Opened { get; } = new();

    public StorageTraits Traits => new() { CanStream = true };

    public FlowIO<Stream> ReadStream() =>
      FlowIO.Lift<Stream>(() =>
      {
        ReadStreamCalls++;
        var stream = new TrackingStream();
        Opened.Add(stream);
        return stream;
      });

    public FlowIO<FlowUnit> WriteStream(Stream stream) =>
      FlowIO.Fail<FlowUnit>(new RuntimeError.External("StubMedium", new NotSupportedException()));

    public FlowIO<bool> Exists() => FlowIO.Pure(true);
  }

  private sealed class StubStreamingReader<TRow>
    : IFormatRowReader<TRow>, IFormatStreamReader<TRow>
    where TRow : notnull
  {
    private readonly IReadOnlyList<TRow> _rows;
    private readonly bool _throwSchemaMismatch;

    public StubStreamingReader(IReadOnlyList<TRow> rows, bool throwSchemaMismatch)
    {
      _rows = rows;
      _throwSchemaMismatch = throwSchemaMismatch;
    }

    public StorageTraits Traits => new() { CanStream = true };

    public async IAsyncEnumerable<TRow> DeserializeRows(
      Stream stream,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      await Task.CompletedTask.ConfigureAwait(false);
      if (_throwSchemaMismatch)
      {
        throw new SchemaMismatchException(
          "missing column 'x'",
          new InvalidOperationException("provider detail")
        );
      }

      foreach (var row in _rows)
      {
        cancellationToken.ThrowIfCancellationRequested();
        yield return row;
      }
    }
  }

  private sealed class StubNonStreamingReader<TRow> : IFormatRowReader<TRow>
    where TRow : notnull
  {
    public StorageTraits Traits => new() { CanStream = false };

    public async IAsyncEnumerable<TRow> DeserializeRows(
      Stream stream,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      await Task.CompletedTask.ConfigureAwait(false);
      yield break;
    }
  }
}
