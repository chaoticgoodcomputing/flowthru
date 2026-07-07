using Flowthru.FUnit.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Step.Testing;
using Flowthru.Validation.Runtime;

namespace Flowthru.FUnit.Tests;

/// <summary>
/// Exercises the FUnit streaming affordance (issue #118) against
/// <see cref="FlowSource{A}"/>: lifting samples into a stream, the async
/// compile/drain invoke that unwinds the <see cref="EffResult{A}"/>, assertions
/// for BOTH error modes (terminal <see cref="RuntimeError"/> and per-item
/// dead-letter), and the hooks that observe bracket release and mid-stream
/// cancellation (<see cref="TrackingSource{T}"/>) and sink lifecycle
/// (<see cref="RecordingSink{T}"/>).
/// </summary>
[TestFixture]
[Category("FUnit")]
#pragma warning disable FU002 // FUnitContext subclass not guarded by #if FUNIT_ENABLED — this IS the test suite.
public class FUnitStreamingTests : FUnitContext
{
  // ===========================================================================
  // Lifting samples into a stream
  // ===========================================================================

  [Test]
  public async Task Samples_Source_LiftsIntoFlowSource()
  {
    var input = Samples.Source(new NumberRow(1.0), new NumberRow(2.0));

    var result = await InvokeStream(input);

    Assert.That(result.Select(r => r.Value), Is.EqualTo(new[] { 1.0, 2.0 }));
  }

  [Test]
  public async Task Samples_Of_AsStream_LiftsIntoFlowSource()
  {
    var input = Samples.Of(new NumberRow(3.0)).AsStream();

    var result = await InvokeStream(input);

    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].Value, Is.EqualTo(3.0));
  }

  // ===========================================================================
  // InvokeStream over a streaming step transform (Func<FlowSource, FlowSource>)
  // ===========================================================================

  [Test]
  public async Task InvokeStream_Step_TransformsLazily()
  {
    Func<FlowSource<NumberRow>, FlowSource<NumberRow>> doubler =
      src => src.Where(r => r.Value > 0).Map(r => r with { Value = r.Value * 2 });

    var result = await InvokeStream(
      doubler,
      Samples.Of(new NumberRow(-1.0), new NumberRow(2.0), new NumberRow(3.0))
    );

    Assert.That(result.Select(r => r.Value), Is.EqualTo(new[] { 4.0, 6.0 }));
  }

  [Test]
  public async Task InvokeStream_EmptyInput_ReturnsEmpty()
  {
    var result = await InvokeStream(Samples.Source<NumberRow>());

    Assert.That(result, Is.Empty);
  }

  // ===========================================================================
  // Error mode 1 — terminal RuntimeError (a failed compile)
  // ===========================================================================

  [Test]
  public async Task RunStream_TerminalFailure_SurfacesAsEffResultFailure()
  {
    var source = FlowSource.Lift<int>(Boom);

    var result = await RunStream(source);

    Assert.That(result, Is.InstanceOf<EffResult<IReadOnlyList<int>>.Failure>());
    var error = ((EffResult<IReadOnlyList<int>>.Failure)result).Error;
    Assert.That(error, Is.InstanceOf<RuntimeError.External>());
  }

  [Test]
  public void InvokeStream_TerminalFailure_Throws()
  {
    var source = FlowSource.Lift<int>(Boom);

    Assert.CatchAsync<InvalidOperationException>(() => InvokeStream(source));
  }

  [Test]
  public async Task RunStream_Success_YieldsRows()
  {
    var result = await RunStream(Samples.Source(1, 2, 3));

    Assert.That(result, Is.InstanceOf<EffResult<IReadOnlyList<int>>.Success>());
    Assert.That(((EffResult<IReadOnlyList<int>>.Success)result).Value, Is.EqualTo(new[] { 1, 2, 3 }));
  }

  // ===========================================================================
  // Error mode 2 — per-item dead-letter (FlowSource<EffResult<T>>)
  // ===========================================================================

  [Test]
  public async Task InvokeStream_DeadLetter_MaterialisesPerItemOutcomes()
  {
    // A per-item stream: two good rows and one quarantined bad row.
    var deadLettered = Samples.Source<EffResult<int>>(
      new EffResult<int>.Success(1),
      new EffResult<int>.Failure(new RuntimeError.External("bad-row", new Exception("corrupt"))),
      new EffResult<int>.Success(3)
    );

    var outcomes = await InvokeStream(deadLettered);

    Assert.That(outcomes, Has.Count.EqualTo(3));
    Assert.That(outcomes[0], Is.InstanceOf<EffResult<int>.Success>());
    Assert.That(outcomes[1], Is.InstanceOf<EffResult<int>.Failure>());
    Assert.That(outcomes[2], Is.InstanceOf<EffResult<int>.Success>());
  }

  [Test]
  public async Task InvokeStream_DeadLetter_SkipErrors_KeepsGoodRows()
  {
    var reported = new List<RuntimeError>();
    var deadLettered = Samples.Source<EffResult<int>>(
      new EffResult<int>.Success(1),
      new EffResult<int>.Failure(new RuntimeError.External("bad-row", new Exception("corrupt"))),
      new EffResult<int>.Success(3)
    );

    var kept = await InvokeStream(deadLettered.SkipErrors(reported.Add));

    Assert.That(kept, Is.EqualTo(new[] { 1, 3 }));
    Assert.That(reported, Has.Count.EqualTo(1));
  }

  // ===========================================================================
  // Bracket release + mid-stream cancellation observability
  // ===========================================================================

  [Test]
  public async Task TrackingSource_ReleasesBracket_OnCompletion()
  {
    var tracked = new TrackingSource<int>(new[] { 1, 2, 3 });

    var rows = await InvokeStream(tracked.Source);

    Assert.That(rows, Is.EqualTo(new[] { 1, 2, 3 }));
    Assert.That(tracked.AcquireCount, Is.EqualTo(1));
    Assert.That(tracked.Released, Is.True, "The pull-scoped bracket must release on completion.");
    Assert.That(tracked.ReleaseCount, Is.EqualTo(1));
    Assert.That(tracked.LastReleaseError, Is.Null, "Clean completion releases with a null error.");
  }

  [Test]
  public async Task TrackingSource_ReleasesBracket_AndStopsRead_OnMidStreamCancellation()
  {
    using var cts = new CancellationTokenSource();
    // Cancel after the second element is pulled — a mid-stream cancellation.
    var tracked = new TrackingSource<int>(
      new[] { 1, 2, 3, 4, 5 },
      onPulled: index => { if (index == 2) cts.Cancel(); }
    );

    var result = await RunStream(tracked.Source, cts.Token);

    Assert.That(result, Is.InstanceOf<EffResult<IReadOnlyList<int>>.Failure>());
    Assert.That(((EffResult<IReadOnlyList<int>>.Failure)result).Error,
      Is.InstanceOf<RuntimeError.Cancelled>());
    Assert.That(tracked.Released, Is.True, "The bracket must release even when cancelled mid-stream.");
    Assert.That(tracked.LastReleaseError, Is.InstanceOf<RuntimeError.Cancelled>());
    Assert.That(tracked.PulledCount, Is.LessThan(5),
      "A mid-stream cancel must stop the read before draining every element.");
  }

  // ===========================================================================
  // Sink lifecycle observability (DrainInto + RecordingSink)
  // ===========================================================================

  [Test]
  public async Task DrainInto_RecordingSink_CompletesAndDisposes()
  {
    var sink = new RecordingSink<int>(batchSize: 2);

    var result = await DrainInto(Samples.Source(1, 2, 3, 4, 5), sink);

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    Assert.That(sink.Written, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    Assert.That(sink.Events, Is.EqualTo(new[] { "open", "write(2)", "write(2)", "write(1)", "complete", "dispose" }));
    Assert.That(sink.Completed, Is.True);
    Assert.That(sink.Disposed, Is.True);
  }

  [Test]
  public async Task DrainInto_OnMidStreamFailure_DisposesWithoutCompleting()
  {
    var sink = new RecordingSink<int>(batchSize: 2);

    var result = await DrainInto(FlowSource.Lift<int>(Boom), sink);

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
    Assert.That(sink.Completed, Is.False, "A mid-stream failure must not complete the sink.");
    Assert.That(sink.Disposed, Is.True, "Dispose (rollback) must still run on the failure path.");
  }

  // ── helpers ────────────────────────────────────────────────────────────

  private static async IAsyncEnumerable<int> BoomImpl()
  {
    await Task.CompletedTask.ConfigureAwait(false);
    yield return 1;
    throw new InvalidOperationException("boom");
  }

  private static IAsyncEnumerable<int> Boom(CancellationToken ct) => BoomImpl();
}
#pragma warning restore FU002
