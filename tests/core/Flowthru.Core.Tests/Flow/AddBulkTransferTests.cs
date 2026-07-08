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
/// Tests for the bulk-transfer intent verb (#134): the on-DAG identity
/// step <c>AddBulkTransfer</c> wires between two endpoint items, the
/// pre-flight rung negotiation, its visible reporting, and the
/// <c>RequireNative</c> pre-flight error.
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
  public void Negotiate_MatchedCapabilityPair_StillDowngradesVisibly()
  {
    // Both endpoints declare the same provider + wire format. The pairing
    // decision point matches for real — but the native rung's execution
    // machinery hasn't shipped, so selection still lands on Streaming and
    // the reason says exactly that.
    var source = new StubStreamingSource<TestRow>("orders",
      export: new StubBulkExport("postgresql", "pgcopy-binary"));
    var target = new StubSinkTarget<TestRow>("warehouse",
      import: new StubBulkImport("postgresql", "pgcopy-binary"));

    var negotiation = BulkTransferNegotiation.Negotiate(source, target);

    Assert.That(negotiation.IsValid, Is.True);
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Streaming));
    Assert.That(decision.Reason, Does.Contain("capability pair matched"));
    Assert.That(decision.Reason, Does.Contain("not available"),
      "A matched pair must still report that the native rung is unavailable in this version.");
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
  // RequireNative — pre-flight error for every pair in this slice
  // ===========================================================================

  [Test]
  public void Negotiate_RequireNative_IsAlwaysAPreFlightError()
  {
    var options = new BulkTransferOptions { RequireNative = true };

    // Pair 1: no capabilities anywhere.
    var plain = BulkTransferNegotiation.Negotiate(
      new StubStreamingSource<TestRow>("orders"),
      new StubSinkTarget<TestRow>("warehouse"),
      options
    );
    Assert.That(plain.IsValid, Is.False,
      "RequireNative must fail pre-flight when neither endpoint declares a capability.");

    // Pair 2: even a matched capability pair fails, because the native
    // rung's execution machinery does not exist in this version.
    var matched = BulkTransferNegotiation.Negotiate(
      new StubStreamingSource<TestRow>("orders", export: new StubBulkExport("postgresql", "pgcopy-binary")),
      new StubSinkTarget<TestRow>("warehouse", import: new StubBulkImport("postgresql", "pgcopy-binary")),
      options
    );
    Assert.That(matched.IsValid, Is.False,
      "RequireNative must fail pre-flight for every pair while no native rung exists.");
    var errors = ((Validated<PreFlightError, BulkTransferDecision>.Invalid)matched).Errors;
    Assert.That(errors.Single(), Is.InstanceOf<PreFlightError.BulkTransferRungUnavailable>());
    Assert.That(errors.Single().Message, Does.Contain("RequireNative"));
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

  private sealed record StubBulkExport(string BulkProvider, string BulkWireFormat) : ISupportsBulkExport
  {
    public FlowIO<Stream> OpenBulkExport() =>
      FlowIO.Pure<Stream>(new MemoryStream());
  }

  private sealed record StubBulkImport(string BulkProvider, string BulkWireFormat) : ISupportsBulkImport
  {
    public FlowIO<Stream> OpenBulkImport() =>
      FlowIO.Pure<Stream>(new MemoryStream());
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

    public bool SupportsStreaming => true;
    public FlowSource<T> OpenStreamingSource() => FlowSource.FromEnumerable(_rows);

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
