using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Unit tests for the native rung's byte pump (#139) over in-memory
/// streams: the full payload crosses through the bounded buffer, the
/// import channel is completed only on success, both channels are
/// disposed on every exit path, cancellation aborts, and typed errors
/// from either capability survive to the surface.
/// </summary>
[TestFixture]
public class BulkTransferBytePumpTests
{
  [Test]
  public async Task Transfer_CopiesTheFullPayload_ThroughTheBoundedBuffer()
  {
    // Payload deliberately larger than (and not a multiple of) the pump
    // buffer, so the loop runs several partial iterations.
    var payload = new byte[BulkTransferBytePump.BufferBytes * 3 + 12345];
    new Random(7).NextBytes(payload);
    var export = new StubExport(payload);
    var import = new StubImport();

    var result = await BulkTransferBytePump.Transfer(export, import, "test").Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    Assert.That(import.Channel.Bytes, Is.EqualTo(payload));
    Assert.That(import.Channel.Completed, Is.True);
    Assert.That(import.Channel.Disposed, Is.True);
    Assert.That(export.Stream!.Disposed, Is.True);
  }

  [Test]
  public async Task Transfer_EmptyPayload_StillCompletesTheImport()
  {
    var export = new StubExport(Array.Empty<byte>());
    var import = new StubImport();

    var result = await BulkTransferBytePump.Transfer(export, import, "test").Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    Assert.That(import.Channel.Bytes, Is.Empty);
    Assert.That(import.Channel.Completed, Is.True,
      "An empty table is a legal transfer; the import must still be finalized.");
  }

  [Test]
  public async Task Transfer_ExportReadFailure_DisposesBothWithoutCompleting()
  {
    var export = new StubExport(new byte[200_000], failAfterBytes: 90_000);
    var import = new StubImport();

    var result = await BulkTransferBytePump.Transfer(export, import, "test").Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    Assert.That(import.Channel.Completed, Is.False,
      "A failed pump must never complete (commit) the import.");
    Assert.That(import.Channel.Disposed, Is.True);
    Assert.That(export.Stream!.Disposed, Is.True);
  }

  [Test]
  public async Task Transfer_ImportWriteFailure_DisposesBothWithoutCompleting()
  {
    var export = new StubExport(new byte[200_000]);
    var import = new StubImport(failWrites: true);

    var result = await BulkTransferBytePump.Transfer(export, import, "test").Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    Assert.That(import.Channel.Completed, Is.False);
    Assert.That(import.Channel.Disposed, Is.True);
    Assert.That(export.Stream!.Disposed, Is.True);
  }

  [Test]
  public async Task Transfer_Cancellation_AbortsWithoutCompleting_AndDisposesBoth()
  {
    using var cts = new CancellationTokenSource();
    var export = new StubExport(new byte[BulkTransferBytePump.BufferBytes * 8],
      cancelAfterBytes: BulkTransferBytePump.BufferBytes * 2, cts: cts);
    var import = new StubImport();

    var result = await BulkTransferBytePump.Transfer(export, import, "test").Run(cts.Token);

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    var failure = (EffResult<FlowUnit>.Failure)result;
    Assert.That(failure.Error, Is.InstanceOf<RuntimeError.Cancelled>());
    Assert.That(import.Channel.Completed, Is.False,
      "A cancelled pump must never complete (commit) the import.");
    Assert.That(import.Channel.Disposed, Is.True);
    Assert.That(export.Stream!.Disposed, Is.True);
  }

  [Test]
  public async Task Transfer_ExportOpenFailure_PreservesTheTypedError()
  {
    var export = new StubExport(Array.Empty<byte>(), failOnOpen: true);
    var import = new StubImport();

    var result = await BulkTransferBytePump.Transfer(export, import, "test").Run();

    var failure = (EffResult<FlowUnit>.Failure)result;
    Assert.That(failure.Error.Message, Does.Contain("export refused"),
      "The export capability's typed error must survive unchanged.");
    Assert.That(import.Channel.Disposed, Is.False,
      "The import side must never open when the export side refused.");
  }

  [Test]
  public async Task Transfer_ImportOpenFailure_DisposesTheExportStream_AndPreservesTheTypedError()
  {
    var export = new StubExport(new byte[64]);
    var import = new StubImport(failOnOpen: true);

    var result = await BulkTransferBytePump.Transfer(export, import, "test").Run();

    var failure = (EffResult<FlowUnit>.Failure)result;
    Assert.That(failure.Error.Message, Does.Contain("import refused"));
    Assert.That(export.Stream!.Disposed, Is.True,
      "The already-open export stream must be released when the import fails to open.");
  }

  [Test]
  public async Task Transfer_AbortDisposeFailure_DoesNotMaskThePumpError()
  {
    var export = new StubExport(new byte[200_000], failAfterBytes: 10_000);
    var import = new StubImport(failDispose: true);

    var result = await BulkTransferBytePump.Transfer(export, import, "test").Run();

    var failure = (EffResult<FlowUnit>.Failure)result;
    Assert.That(failure.Error.Message, Does.Contain("export channel broke"),
      "The pump's own error must win over a secondary disposal failure.");
    Assert.That(export.Stream!.Disposed, Is.True,
      "A throwing import disposal must not prevent the export stream's disposal.");
  }

  // ---------------------------------------------------------------------------
  // Doubles
  // ---------------------------------------------------------------------------

  private sealed class StubExport : ISupportsBulkExport
  {
    private readonly byte[] _payload;
    private readonly long _failAfterBytes;
    private readonly long _cancelAfterBytes;
    private readonly CancellationTokenSource? _cts;
    private readonly bool _failOnOpen;

    public StubExport(
      byte[] payload,
      long failAfterBytes = -1,
      long cancelAfterBytes = -1,
      CancellationTokenSource? cts = null,
      bool failOnOpen = false
    )
    {
      _payload = payload;
      _failAfterBytes = failAfterBytes;
      _cancelAfterBytes = cancelAfterBytes;
      _cts = cts;
      _failOnOpen = failOnOpen;
    }

    public TrackingStream? Stream { get; private set; }

    public string BulkProvider => "stub";
    public string BulkWireFormat => "stub-bytes";

    public FlowIO<Stream> OpenBulkExport() =>
      _failOnOpen
        ? FlowIO.Fail<Stream>(new RuntimeError.External(
            "StubExport", new InvalidOperationException("export refused")))
        : FlowIO.Lift<Stream>(() =>
            Stream = new TrackingStream(_payload, _failAfterBytes, _cancelAfterBytes, _cts));
  }

  private sealed class StubImport : ISupportsBulkImport
  {
    private readonly bool _failOnOpen;

    public StubImport(bool failWrites = false, bool failDispose = false, bool failOnOpen = false)
    {
      Channel = new RecordingChannel(failWrites, failDispose);
      _failOnOpen = failOnOpen;
    }

    public RecordingChannel Channel { get; }

    public string BulkProvider => "stub";
    public string BulkWireFormat => "stub-bytes";

    public FlowIO<BulkImportChannel> OpenBulkImport() =>
      _failOnOpen
        ? FlowIO.Fail<BulkImportChannel>(new RuntimeError.External(
            "StubImport", new InvalidOperationException("import refused")))
        : FlowIO.Pure<BulkImportChannel>(Channel);
  }

  internal sealed class RecordingChannel : BulkImportChannel
  {
    private readonly MemoryStream _buffer = new();
    private readonly bool _failWrites;
    private readonly bool _failDispose;

    public RecordingChannel(bool failWrites = false, bool failDispose = false)
    {
      _failWrites = failWrites;
      _failDispose = failDispose;
    }

    public bool Completed { get; private set; }
    public bool Disposed { get; private set; }
    public byte[] Bytes => _buffer.ToArray();

    public override ValueTask CompleteAsync(CancellationToken cancellationToken)
    {
      Completed = true;
      return ValueTask.CompletedTask;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _buffer.Length;
    public override long Position
    {
      get => _buffer.Position;
      set => throw new NotSupportedException();
    }

    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
      if (_failWrites) throw new IOException("import channel refused the write");
      _buffer.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
      Disposed = true;
      base.Dispose(disposing);
      if (_failDispose) throw new IOException("import channel teardown failed");
    }

    public override ValueTask DisposeAsync()
    {
      Disposed = true;
      if (_failDispose) throw new IOException("import channel teardown failed");
      return base.DisposeAsync();
    }
  }

  internal sealed class TrackingStream : Stream
  {
    private readonly byte[] _payload;
    private readonly long _failAfterBytes;
    private readonly long _cancelAfterBytes;
    private readonly CancellationTokenSource? _cts;
    private long _position;

    public TrackingStream(
      byte[] payload,
      long failAfterBytes,
      long cancelAfterBytes,
      CancellationTokenSource? cts
    )
    {
      _payload = payload;
      _failAfterBytes = failAfterBytes;
      _cancelAfterBytes = cancelAfterBytes;
      _cts = cts;
    }

    public bool Disposed { get; private set; }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _payload.Length;
    public override long Position
    {
      get => _position;
      set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      if (_failAfterBytes >= 0 && _position >= _failAfterBytes)
      {
        throw new IOException("export channel broke mid-transfer");
      }

      if (_cancelAfterBytes >= 0 && _position >= _cancelAfterBytes)
      {
        // Simulate the ambient token firing mid-transfer.
        _cts!.Cancel();
        _cts.Token.ThrowIfCancellationRequested();
      }

      var remaining = (int)Math.Min(count, _payload.Length - _position);
      if (remaining <= 0) return 0;
      Array.Copy(_payload, _position, buffer, offset, remaining);
      _position += remaining;
      return remaining;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
      Disposed = true;
      base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
      Disposed = true;
      return base.DisposeAsync();
    }
  }
}
