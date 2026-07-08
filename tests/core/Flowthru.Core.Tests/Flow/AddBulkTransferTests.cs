using Flowthru.Core.Tests.Diagnostics;
using Flowthru.Core.Tests.Storage;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Tests for the bulk-transfer intent verb (#134, #139): the on-DAG
/// identity step <c>AddBulkTransfer</c> wires between two endpoint items,
/// the pre-flight rung negotiation (native selection for matched
/// capability pairs, visible streaming fallback otherwise), the native
/// rung's byte passthrough, and the <c>RequireNative</c> pre-flight
/// error.
/// </summary>
[TestFixture]
public class AddBulkTransferTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-bulktransfer-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }
  }

  // ===========================================================================
  // On-DAG wiring
  // ===========================================================================

  [Test]
  public void AddBulkTransfer_WiresOnDagIdentityStep_WithEndpointItemLabels()
  {
    var source = new StubStreamingSource<TestRow>("orders");
    var target = new StubSinkTarget<TestRow>("warehouse");

    var flow = FlowBuilder.CreateFlow("TransferWiring", p => p.AddBulkTransfer(source, target));

    Assert.That(flow.Steps, Has.Count.EqualTo(1),
      "AddBulkTransfer should emit exactly one on-DAG step.");
    var step = flow.Steps[0];
    Assert.That(step.Label, Is.EqualTo("BulkTransfer_orders_to_warehouse"));
    Assert.That(step.Inputs.Single().Label, Is.EqualTo("orders"),
      "The step's input should be the source endpoint, sharing the source item's label.");
    Assert.That(step.Outputs.Single().Label, Is.EqualTo("warehouse"),
      "The step's output should be the target endpoint, sharing the target item's label.");
  }

  [Test]
  public async Task AddBulkTransfer_StreamingRung_MovesAllRows_OnDag()
  {
    // A real composed JSON source (streams via ISupportsStreamingView)
    // into a sink-capable target — the streaming fallback end-to-end.
    var path = Path.Combine(_tempDir, "orders.json");
    var source = ItemFactory.Enumerable.Json<TestRow>("orders", path);
    var rows = new[]
    {
      new TestRow { Id = 1, Name = "alpha" },
      new TestRow { Id = 2, Name = "beta" },
      new TestRow { Id = 3, Name = "gamma" },
    };
    await source.Save(rows).Run();

    var target = new StubSinkTarget<TestRow>("warehouse", batchSize: 2);
    var flow = FlowBuilder.CreateFlow("TransferRun", p => p.AddBulkTransfer(source, target));

    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True, "The transfer flow should complete successfully.");
    Assert.That(target.Sink.Rows.Select(r => r.Id), Is.EqualTo(new[] { 1, 2, 3 }));
    Assert.That(target.Sink.Completed, Is.True);
  }

  [Test]
  public void AddBulkTransfer_InheritsConflictKeysFromBothEndpointItems()
  {
    var sourceDep = ServiceDependency.Of<IStubSourceResource>();
    var targetDep = ServiceDependency.Of<IStubTargetResource>();
    var source = new StubStreamingSource<TestRow>("orders", dependencies: new[] { sourceDep });
    var target = new StubSinkTarget<TestRow>("warehouse", dependencies: new[] { targetDep });

    var flow = FlowBuilder.CreateFlow("TransferConflicts", p => p.AddBulkTransfer(source, target));
    var keys = ConflictKeys.Of(flow.Steps.Single()).ToList();

    Assert.That(keys, Does.Contain((sourceDep, ConflictOp.Read)),
      "The transfer step should inherit the source endpoint's dependency as a read.");
    Assert.That(keys, Does.Contain((targetDep, ConflictOp.Write)),
      "The transfer step should inherit the target endpoint's dependency as a write.");
  }

  // ===========================================================================
  // Rung negotiation — selection and visible reporting
  // ===========================================================================

  [Test]
  public void Negotiate_HeterogeneousPair_SelectsStreamingFallback_AndSaysSo()
  {
    // Neither endpoint declares a bulk capability — the everyday pairing.
    var source = new StubStreamingSource<TestRow>("orders");
    var target = new StubSinkTarget<TestRow>("warehouse");

    var negotiation = BulkTransferNegotiation.Negotiate(source, target);

    Assert.That(negotiation.IsValid, Is.True);
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Streaming));
    Assert.That(decision.Reason, Does.Contain("streaming fallback"),
      "The decision must say the streaming fallback was selected.");
    Assert.That(decision.Reason, Does.Contain("no native capability pair"),
      "The decision must say why native was unavailable — a downgrade is never silent.");
  }

  [Test]
  public void Negotiate_MatchedCapabilityPair_SelectsNativeRung_NamingThePairing()
  {
    // Both endpoints declare the same provider + wire format — the
    // native rung executes it, and the decision names the pairing so the
    // selection is exactly as visible as a downgrade would be.
    var source = new StubStreamingSource<TestRow>("orders",
      export: new StubBulkExport("postgresql", "pgcopy-binary"));
    var target = new StubSinkTarget<TestRow>("warehouse",
      import: new StubBulkImport("postgresql", "pgcopy-binary"));

    var negotiation = BulkTransferNegotiation.Negotiate(source, target);

    Assert.That(negotiation.IsValid, Is.True);
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Native));
    Assert.That(decision.Reason, Does.Contain("native rung selected"),
      "The decision must say the native rung was selected.");
    Assert.That(decision.Reason, Does.Contain("postgresql/pgcopy-binary"),
      "The decision must name the capability pairing it matched on.");
  }

  [Test]
  public void Negotiate_MatchedCapabilityPair_IgnoresStreamingFeasibility()
  {
    // A matched pair executes natively, so the endpoints' inability to
    // stream/sink must not block negotiation — the native rung never
    // touches those capabilities.
    var source = new NonStreamingBulkSource<TestRow>("orders",
      new StubBulkExport("postgresql", "pgcopy-binary"));
    var target = new NonSinkBulkTarget<TestRow>("warehouse",
      new StubBulkImport("postgresql", "pgcopy-binary"));

    var negotiation = BulkTransferNegotiation.Negotiate(source, target);

    Assert.That(negotiation.IsValid, Is.True);
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Native));
  }

  [Test]
  public void Negotiate_MismatchedCapabilityPair_ReportsTheIncompatibility()
  {
    var source = new StubStreamingSource<TestRow>("orders",
      export: new StubBulkExport("postgresql", "pgcopy-binary"));
    var target = new StubSinkTarget<TestRow>("warehouse",
      import: new StubBulkImport("mysql", "mysqldump"));

    var negotiation = BulkTransferNegotiation.Negotiate(source, target);

    Assert.That(negotiation.IsValid, Is.True);
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Streaming));
    Assert.That(decision.Reason, Does.Contain("capability pair incompatible"));
    Assert.That(decision.Reason, Does.Contain("postgresql/pgcopy-binary"));
    Assert.That(decision.Reason, Does.Contain("mysql/mysqldump"));
  }

  // ===========================================================================
  // RequireNative — passes for a matched pair, pre-flight error otherwise
  // ===========================================================================

  [Test]
  public void Negotiate_RequireNative_FailsForHeterogeneousPairs_AndPassesForMatchedPairs()
  {
    var options = new BulkTransferOptions { RequireNative = true };

    // Heterogeneous pair: no capabilities anywhere — a pre-flight error.
    var plain = BulkTransferNegotiation.Negotiate(
      new StubStreamingSource<TestRow>("orders"),
      new StubSinkTarget<TestRow>("warehouse"),
      options
    );
    Assert.That(plain.IsValid, Is.False,
      "RequireNative must fail pre-flight when neither endpoint declares a capability.");
    var errors = ((Validated<PreFlightError, BulkTransferDecision>.Invalid)plain).Errors;
    Assert.That(errors.Single(), Is.InstanceOf<PreFlightError.BulkTransferRungUnavailable>());
    Assert.That(errors.Single().Message, Does.Contain("RequireNative"));

    // Mismatched capabilities — also a pre-flight error, naming both sides.
    var mismatched = BulkTransferNegotiation.Negotiate(
      new StubStreamingSource<TestRow>("orders", export: new StubBulkExport("postgresql", "pgcopy-binary")),
      new StubSinkTarget<TestRow>("warehouse", import: new StubBulkImport("mysql", "mysqldump")),
      options
    );
    Assert.That(mismatched.IsValid, Is.False,
      "RequireNative must fail pre-flight when the capability pair is incompatible.");

    // Matched pair — RequireNative passes and the native rung is selected.
    var matched = BulkTransferNegotiation.Negotiate(
      new StubStreamingSource<TestRow>("orders", export: new StubBulkExport("postgresql", "pgcopy-binary")),
      new StubSinkTarget<TestRow>("warehouse", import: new StubBulkImport("postgresql", "pgcopy-binary")),
      options
    );
    Assert.That(matched.IsValid, Is.True,
      "RequireNative must pass pre-flight for a matched capability pair.");
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)matched).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Native));
  }

  [Test]
  public async Task PreFlightPipeline_RequireNative_SurfacesRungUnavailable_AtHermeticScope()
  {
    // Negotiation is zero-I/O, so an offline (hermetic) smoke test must
    // still catch a transfer whose required rung is unavailable.
    var flow = FlowBuilder.CreateFlow("TransferPreflight", p => p.AddBulkTransfer(
      new StubStreamingSource<TestRow>("orders"),
      new StubSinkTarget<TestRow>("warehouse"),
      new BulkTransferOptions { RequireNative = true }
    ));

    var result = await PreFlightPipeline.Run(flow, scope: PreFlightScope.Hermetic).Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    Assert.That(inner, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var errors = ((Validated<PreFlightError, FlowUnit>.Invalid)inner).Errors;
    var rungError = errors.OfType<PreFlightError.BulkTransferRungUnavailable>().Single();
    Assert.That(rungError.StepLabel, Is.EqualTo("BulkTransfer_orders_to_warehouse"));
  }

  [Test]
  public async Task RunWithoutPreFlight_FailedNegotiation_FailsTheStep_NotSilently()
  {
    // BuiltFlow.RunAsync skips pre-flight entirely; the endpoints re-check
    // the (cached) negotiation at execution time so a host that opted out
    // of validation still fails fast instead of transferring anyway.
    var target = new StubSinkTarget<TestRow>("warehouse");
    var flow = FlowBuilder.CreateFlow("TransferNoPreflight", p => p.AddBulkTransfer(
      new StubStreamingSource<TestRow>("orders"),
      target,
      new BulkTransferOptions { RequireNative = true }
    ));

    var result = await flow.RunAsync();

    Assert.That(result.HasFailures, Is.True);
    // The scheduler attributes the failure to the step; the endpoint's
    // negotiation error must be preserved as the cause.
    var failure = (RuntimeError.StepFailed)result.FirstFailure!.Error;
    var cause = (RuntimeError.PreFlightFailed)failure.Cause;
    Assert.That(cause.Cause, Is.InstanceOf<PreFlightError.BulkTransferRungUnavailable>());
    Assert.That(target.Sink.Rows, Is.Empty, "No rows may move when negotiation failed.");
  }

  // ===========================================================================
  // Native rung execution — byte passthrough, no CLR rows, abort on failure
  // ===========================================================================

  [Test]
  public async Task NativeRung_MovesBytes_WithoutMaterialisingRows_OnDag()
  {
    var payload = new byte[300_000];
    new Random(42).NextBytes(payload);
    var export = new StubBulkExport("postgresql", "pgcopy-binary", payload);
    var import = new StubBulkImport("postgresql", "pgcopy-binary");
    var source = new StubStreamingSource<TestRow>("orders", export: export);
    var target = new StubSinkTarget<TestRow>("warehouse", import: import);

    var flow = FlowBuilder.CreateFlow("NativeTransfer", p => p.AddBulkTransfer(source, target));
    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True,
      "The native transfer should complete. Failures: "
      + string.Join(" | ", result.StepResults.OfType<StepResult.Failed>().Select(f => f.Error.Message)));
    Assert.That(import.Channel.Bytes, Is.EqualTo(payload),
      "Every exported byte must arrive at the import channel unchanged.");
    Assert.That(import.Channel.Completed, Is.True,
      "The pump must complete the import channel after the full payload.");
    Assert.That(import.Channel.Disposed, Is.True,
      "The import channel must be disposed on the success path too.");
    Assert.That(export.StreamDisposed, Is.True,
      "The export stream must be disposed on the success path too.");
    Assert.That(source.StreamOpened, Is.False,
      "The native rung must not open the row-level streaming view — bytes only.");
    Assert.That(target.Sink.Rows, Is.Empty,
      "No CLR row may materialise on the native rung.");
  }

  [Test]
  public async Task NativeRung_ExportFailure_AbortsTheImport_AndDisposesBothChannels()
  {
    var export = new StubBulkExport("postgresql", "pgcopy-binary",
      payload: new byte[200_000], failAfterBytes: 100_000);
    var import = new StubBulkImport("postgresql", "pgcopy-binary");
    var source = new StubStreamingSource<TestRow>("orders", export: export);
    var target = new StubSinkTarget<TestRow>("warehouse", import: import);

    var flow = FlowBuilder.CreateFlow("NativeTransferFailure", p => p.AddBulkTransfer(source, target));
    var result = await flow.RunAsync();

    Assert.That(result.HasFailures, Is.True, "A mid-pump export failure must fail the transfer.");
    Assert.That(import.Channel.Completed, Is.False,
      "The import channel must NOT be completed after a failed pump.");
    Assert.That(import.Channel.Disposed, Is.True,
      "The import channel must be disposed (abort signal) on the failure path.");
    Assert.That(export.StreamDisposed, Is.True,
      "The export stream must be disposed on the failure path.");
  }

  [Test]
  public async Task NativeRung_ImportOpenFailure_DisposesTheExportStream_AndPreservesTheError()
  {
    var export = new StubBulkExport("postgresql", "pgcopy-binary", payload: new byte[16]);
    var import = new StubBulkImport("postgresql", "pgcopy-binary", failOnOpen: true);
    var source = new StubStreamingSource<TestRow>("orders", export: export);
    var target = new StubSinkTarget<TestRow>("warehouse", import: import);

    var flow = FlowBuilder.CreateFlow("NativeTransferOpenFailure", p => p.AddBulkTransfer(source, target));
    var result = await flow.RunAsync();

    Assert.That(result.HasFailures, Is.True);
    Assert.That(export.StreamDisposed, Is.True,
      "An already-open export stream must be released when the import side fails to open.");
    var failure = (RuntimeError.StepFailed)result.FirstFailure!.Error;
    Assert.That(failure.Cause.Message, Does.Contain("import refused"),
      "The import capability's own typed error must survive to the surface.");
  }

  // ===========================================================================
  // Streaming-fallback feasibility — accumulated (applicative) errors
  // ===========================================================================

  [Test]
  public void Negotiate_UnstreamableSourceAndUnsinkableTarget_AccumulateBothErrors()
  {
    // Memory-backed items can neither stream reads nor open a batch sink,
    // so the streaming fallback is infeasible on both ends — and both
    // problems must surface at once, not one per re-run.
    var source = ItemFactory.Enumerable.Memory<TestRow>("orders");
    var target = ItemFactory.Enumerable.Memory<TestRow>("warehouse");

    var negotiation = BulkTransferNegotiation.Negotiate(source, target);

    Assert.That(negotiation.IsValid, Is.False);
    var errors = ((Validated<PreFlightError, BulkTransferDecision>.Invalid)negotiation).Errors;
    Assert.That(errors, Has.Count.EqualTo(2),
      "Source-can't-stream and target-can't-sink are independent checks and must accumulate.");
    Assert.That(errors[0].Message, Does.Contain("'orders'"));
    Assert.That(errors[1].Message, Does.Contain("'warehouse'"));
  }

  // ===========================================================================
  // Caching — endpoints delegate item fingerprints unchanged
  // ===========================================================================

  [Test]
  public async Task EndpointItems_DelegateFingerprints_ToTheWrappedItems()
  {
    var source = new StubStreamingSource<TestRow>("orders", fingerprint: "src-fp");
    var target = new StubSinkTarget<TestRow>("warehouse", fingerprint: "dst-fp");
    var flow = FlowBuilder.CreateFlow("TransferFingerprints", p => p.AddBulkTransfer(source, target));
    var step = flow.Steps.Single();

    var sourceFingerprint = step.Inputs.Single().TryGetFingerprint();
    var targetFingerprint = step.Outputs.Single().TryGetFingerprint();

    Assert.That(sourceFingerprint, Is.Not.Null,
      "A fingerprintable source must stay fingerprintable through the transfer endpoint.");
    Assert.That(targetFingerprint, Is.Not.Null,
      "A fingerprintable target must stay fingerprintable through the transfer endpoint.");
    var src = await sourceFingerprint!.Run();
    var dst = await targetFingerprint!.Run();
    Assert.That(((EffResult<string>.Success)src).Value, Is.EqualTo("src-fp"));
    Assert.That(((EffResult<string>.Success)dst).Value, Is.EqualTo("dst-fp"));
  }

  [Test]
  public void EndpointItems_OnUnfingerprintableItems_StayUnfingerprintable()
  {
    var source = new StubStreamingSource<TestRow>("orders");
    var target = new StubSinkTarget<TestRow>("warehouse");
    var flow = FlowBuilder.CreateFlow("TransferNoFingerprints", p => p.AddBulkTransfer(source, target));
    var step = flow.Steps.Single();

    Assert.That(step.Inputs.Single().TryGetFingerprint(), Is.Null);
    Assert.That(step.Outputs.Single().TryGetFingerprint(), Is.Null);
  }

  // ===========================================================================
  // Host-level reporting — the selected rung appears in validation output
  // ===========================================================================

  [Test]
  public async Task FlowthruService_ReportsSelectedRung_InValidationOutput()
  {
    var path = Path.Combine(_tempDir, "orders.json");
    var source = ItemFactory.Enumerable.Json<TestRow>("orders", path);
    await source.Save(new[] { new TestRow { Id = 1, Name = "alpha" } }).Run();
    var target = new StubSinkTarget<TestRow>("warehouse");

    var capture = new CapturingLoggerProvider();
    var services = new ServiceCollection();
    services.AddLogging(b => b.AddProvider(capture).SetMinimumLevel(LogLevel.Trace));
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("transfer", () =>
        FlowBuilder.CreateFlow("transfer", p => p.AddBulkTransfer(source, target)));
    });
    await using var sp = services.BuildServiceProvider();

    var result = await sp.GetRequiredService<IFlowthruService>().RunAsync();

    Assert.That(result.IsSuccess, Is.True,
      "Transfer run should pass pre-flight and complete. Failures: "
      + string.Join(" | ", result.StepResults.OfType<StepResult.Failed>().Select(f => f.Error.Message)));
    var rungReport = capture.Entries.Where(e => e.Message.Contains("transfer rung")).ToList();
    Assert.That(rungReport, Is.Not.Empty,
      "The selected rung must be reported in the run's validation output. Got: "
      + string.Join(" | ", capture.Entries.Select(e => e.Message)));
    Assert.That(rungReport.Single().Message, Does.Contain("Streaming"),
      "The report must name the selected rung.");
    Assert.That(rungReport.Single().Message, Does.Contain("no native capability pair"),
      "The report must say why the fallback was selected — never a silent downgrade.");
  }

  [Test]
  public async Task FlowthruService_RequireNative_FailsPreFlight_WithTransferLabel()
  {
    var target = new StubSinkTarget<TestRow>("warehouse");
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("transfer", () =>
        FlowBuilder.CreateFlow("transfer", p => p.AddBulkTransfer(
          new StubStreamingSource<TestRow>("orders"),
          target,
          new BulkTransferOptions { RequireNative = true }
        )));
    });
    await using var sp = services.BuildServiceProvider();

    var result = await sp.GetRequiredService<IFlowthruService>().RunAsync();

    Assert.That(result.HasFailures, Is.True);
    var failure = result.StepResults.OfType<StepResult.Failed>().Single();
    Assert.That(failure.StepLabel, Is.EqualTo("preflight:transfer:BulkTransfer_orders_to_warehouse"),
      "The pre-flight failure must be addressed to the transfer step.");
    Assert.That(target.Sink.Rows, Is.Empty, "Pre-flight failure must stop the transfer from running.");
  }

  // ===========================================================================
  // Test doubles
  // ===========================================================================

  public interface IStubSourceResource { }
  public interface IStubTargetResource { }

  /// <summary>
  /// Export capability double: serves <paramref name="payload"/> through a
  /// tracking stream, optionally failing after a byte threshold so pump
  /// failure paths can be exercised.
  /// </summary>
  private sealed class StubBulkExport : ISupportsBulkExport
  {
    private readonly byte[] _payload;
    private readonly long _failAfterBytes;
    private TrackingReadStream? _stream;

    public StubBulkExport(
      string bulkProvider,
      string bulkWireFormat,
      byte[]? payload = null,
      long failAfterBytes = -1
    )
    {
      BulkProvider = bulkProvider;
      BulkWireFormat = bulkWireFormat;
      _payload = payload ?? Array.Empty<byte>();
      _failAfterBytes = failAfterBytes;
    }

    public string BulkProvider { get; }
    public string BulkWireFormat { get; }
    public bool StreamDisposed => _stream?.Disposed ?? false;

    public FlowIO<Stream> OpenBulkExport() =>
      FlowIO.Lift<Stream>(() => _stream = new TrackingReadStream(_payload, _failAfterBytes));
  }

  /// <summary>
  /// Import capability double: records everything the pump writes into a
  /// <see cref="RecordingBulkImportChannel"/>, or fails on open.
  /// </summary>
  private sealed class StubBulkImport : ISupportsBulkImport
  {
    private readonly bool _failOnOpen;

    public StubBulkImport(string bulkProvider, string bulkWireFormat, bool failOnOpen = false)
    {
      BulkProvider = bulkProvider;
      BulkWireFormat = bulkWireFormat;
      _failOnOpen = failOnOpen;
    }

    public string BulkProvider { get; }
    public string BulkWireFormat { get; }
    public RecordingBulkImportChannel Channel { get; } = new();

    public FlowIO<BulkImportChannel> OpenBulkImport() =>
      _failOnOpen
        ? FlowIO.Fail<BulkImportChannel>(new RuntimeError.External(
            "StubBulkImport.OpenBulkImport",
            new InvalidOperationException("import refused")))
        : FlowIO.Pure<BulkImportChannel>(Channel);
  }

  /// <summary>
  /// A <see cref="BulkImportChannel"/> double over a MemoryStream with
  /// observable Completed/Disposed flags — the in-memory stand-in for a
  /// transactional importer.
  /// </summary>
  private sealed class RecordingBulkImportChannel : BulkImportChannel
  {
    private readonly MemoryStream _buffer = new();

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
    public override void Write(byte[] buffer, int offset, int count) =>
      _buffer.Write(buffer, offset, count);

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

  /// <summary>
  /// Read stream double: serves a fixed payload, optionally throwing once
  /// a byte threshold is crossed, and tracks disposal.
  /// </summary>
  private sealed class TrackingReadStream : Stream
  {
    private readonly byte[] _payload;
    private readonly long _failAfterBytes;
    private long _position;

    public TrackingReadStream(byte[] payload, long failAfterBytes)
    {
      _payload = payload;
      _failAfterBytes = failAfterBytes;
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

  /// <summary>
  /// A source that declares a bulk-export capability but cannot stream —
  /// legal for the native rung, infeasible for the streaming fallback.
  /// </summary>
  private sealed class NonStreamingBulkSource<T> : IItem<IEnumerable<T>>
    where T : notnull
  {
    private readonly ISupportsBulkExport _export;

    public NonStreamingBulkSource(string label, ISupportsBulkExport export)
    {
      Label = label;
      _export = export;
    }

    public string Label { get; }
    public NodeTraits Traits => new();

    public FlowIO<IEnumerable<T>> Load() => FlowIO.Pure(Enumerable.Empty<T>());
    public FlowIO<FlowUnit> Save(IEnumerable<T> data) =>
      FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        $"NonStreamingBulkSource[{Label}].Save", new InvalidOperationException("read-only stub")));
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());
    public ISupportsBulkExport? TryGetBulkExport() => _export;
  }

  /// <summary>
  /// A target that declares a bulk-import capability but cannot sink —
  /// legal for the native rung, infeasible for the streaming fallback.
  /// </summary>
  private sealed class NonSinkBulkTarget<T> : IItem<IEnumerable<T>>
    where T : notnull
  {
    private readonly ISupportsBulkImport _import;

    public NonSinkBulkTarget(string label, ISupportsBulkImport import)
    {
      Label = label;
      _import = import;
    }

    public string Label { get; }
    public NodeTraits Traits => new();

    public FlowIO<IEnumerable<T>> Load() => FlowIO.Pure(Enumerable.Empty<T>());
    public FlowIO<FlowUnit> Save(IEnumerable<T> data) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(false);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());
    public ISupportsBulkImport? TryGetBulkImport() => _import;
  }

  /// <summary>
  /// A transfer source double: an eager item that also streams
  /// (<see cref="ISupportsStreamingView{TRow}"/>) and can declare service
  /// dependencies, a fingerprint, and a bulk-export capability.
  /// </summary>
  private sealed class StubStreamingSource<T> : IItem<IEnumerable<T>>, ISupportsStreamingView<T>
    where T : notnull
  {
    private readonly IReadOnlyList<T> _rows;
    private readonly string? _fingerprint;
    private readonly ISupportsBulkExport? _export;

    public StubStreamingSource(
      string label,
      IReadOnlyList<T>? rows = null,
      IReadOnlyList<ServiceDependency>? dependencies = null,
      string? fingerprint = null,
      ISupportsBulkExport? export = null
    )
    {
      Label = label;
      _rows = rows ?? Array.Empty<T>();
      ServiceDependencies = dependencies ?? Array.Empty<ServiceDependency>();
      _fingerprint = fingerprint;
      _export = export;
    }

    public string Label { get; }
    public NodeTraits Traits => new();
    public IReadOnlyList<ServiceDependency> ServiceDependencies { get; }

    public bool StreamOpened { get; private set; }

    public bool SupportsStreaming => true;
    public FlowSource<T> OpenStreamingSource()
    {
      StreamOpened = true;
      return FlowSource.FromEnumerable(_rows);
    }

    public FlowIO<IEnumerable<T>> Load() => FlowIO.Pure<IEnumerable<T>>(_rows);
    public FlowIO<FlowUnit> Save(IEnumerable<T> data) =>
      FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        $"StubStreamingSource[{Label}].Save", new InvalidOperationException("read-only stub")));
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());

    public FlowIO<string>? TryGetFingerprint() =>
      _fingerprint is null ? null : FlowIO.Pure(_fingerprint);
    public ISupportsBulkExport? TryGetBulkExport() => _export;
  }

  /// <summary>
  /// A transfer target double: an eager item that also opens a recording
  /// batch sink (<see cref="ISupportsStreamingSink{TRow}"/>) and can
  /// declare service dependencies, a fingerprint, and a bulk-import
  /// capability.
  /// </summary>
  private sealed class StubSinkTarget<T> : IItem<IEnumerable<T>>, ISupportsStreamingSink<T>
    where T : notnull
  {
    private readonly string? _fingerprint;
    private readonly ISupportsBulkImport? _import;

    public StubSinkTarget(
      string label,
      int batchSize = 2,
      IReadOnlyList<ServiceDependency>? dependencies = null,
      string? fingerprint = null,
      ISupportsBulkImport? import = null
    )
    {
      Label = label;
      Sink = new RecordingSink<T>(batchSize);
      ServiceDependencies = dependencies ?? Array.Empty<ServiceDependency>();
      _fingerprint = fingerprint;
      _import = import;
    }

    public string Label { get; }
    public NodeTraits Traits => new();
    public IReadOnlyList<ServiceDependency> ServiceDependencies { get; }
    public RecordingSink<T> Sink { get; }

    public IFlowSink<T> OpenStreamingSink() => Sink;

    public FlowIO<IEnumerable<T>> Load() => FlowIO.Pure<IEnumerable<T>>(Sink.Rows);
    public FlowIO<FlowUnit> Save(IEnumerable<T> data) =>
      FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        $"StubSinkTarget[{Label}].Save", new InvalidOperationException("sink-only stub")));
    public FlowIO<bool> Exists() => FlowIO.Pure(false);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());

    public FlowIO<string>? TryGetFingerprint() =>
      _fingerprint is null ? null : FlowIO.Pure(_fingerprint);
    public ISupportsBulkImport? TryGetBulkImport() => _import;
  }

  private sealed class RecordingSink<T> : IFlowSink<T>
  {
    public RecordingSink(int batchSize) => BatchSize = batchSize;

    public List<T> Rows { get; } = new();
    public bool Completed { get; private set; }
    public int BatchSize { get; }

    public ValueTask OpenAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask WriteBatchAsync(IReadOnlyList<T> batch, CancellationToken cancellationToken)
    {
      Rows.AddRange(batch);
      return ValueTask.CompletedTask;
    }

    public ValueTask CompleteAsync(CancellationToken cancellationToken)
    {
      Completed = true;
      return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }
}
