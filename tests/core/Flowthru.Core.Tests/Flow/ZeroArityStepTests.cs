using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Tests for the (0,0), (0,N), and (M,0) <c>AddStep</c> overload
/// shapes — pure side-effect steps, source steps, and sink steps.
/// Reactivates the legacy <c>ZeroArityStepTests</c> against the
/// new <c>FlowBuilder</c> arity matrix.
/// </summary>
[TestFixture]
public class ZeroArityStepTests
{
  // ── (0, 0) — pure side-effect ─────────────────────────────────────────

  [Test]
  public async Task SideEffect_Sync_RunsOnce()
  {
    var counter = 0;
    var flow = FlowBuilder.CreateFlow("tick", b =>
      b.AddStep("tick", () => counter++)
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(counter, Is.EqualTo(1));
  }

  [Test]
  public async Task SideEffect_Async_AwaitsAndRuns()
  {
    var counter = 0;
    var flow = FlowBuilder.CreateFlow("tick-async", b =>
      b.AddStep("tick-async", async () =>
      {
        await Task.Yield();
        counter++;
      })
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(counter, Is.EqualTo(1));
  }

  [Test]
  public async Task SideEffect_AsyncWithToken_PassesCancellationToken()
  {
    CancellationToken? captured = null;
    var flow = FlowBuilder.CreateFlow("tick-ct", b =>
      b.AddStep("tick-ct", async (CancellationToken ct) =>
      {
        captured = ct;
        await Task.Yield();
      })
    );
    using var cts = new CancellationTokenSource();
    var result = await flow.RunAsync(ExecutionOptions.Default, cts.Token);
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(captured, Is.EqualTo(cts.Token));
  }

  // ── (0, N) — source steps (no input, produce outputs) ─────────────────

  [Test]
  public async Task Source_OneOutput_Sync_WritesValue()
  {
    var output = ItemFactory.Singleton.Memory<int>("source-1");
    var flow = FlowBuilder.CreateFlow("produce-one", b =>
      b.AddStep<int>("produce", () => 42, output)
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    var loaded = await output.Load().Run();
    Assert.That(((EffResult<int>.Success)loaded).Value, Is.EqualTo(42));
  }

  [Test]
  public async Task Source_OneOutput_Async_WritesValue()
  {
    var output = ItemFactory.Singleton.Memory<int>("source-async");
    var flow = FlowBuilder.CreateFlow("produce-async", b =>
      b.AddStep<int>(
        "produce",
        async () => { await Task.Yield(); return 99; },
        output
      )
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    var loaded = await output.Load().Run();
    Assert.That(((EffResult<int>.Success)loaded).Value, Is.EqualTo(99));
  }

  [Test]
  public async Task Source_TwoOutputs_FansOut()
  {
    var a = ItemFactory.Singleton.Memory<int>("source-a");
    var b = ItemFactory.Singleton.Memory<string>("source-b");
    var flow = FlowBuilder.CreateFlow("produce-two", builder =>
      builder.AddStep<int, string>(
        "produce",
        () => (123, "hello"),
        a,
        b
      )
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(((EffResult<int>.Success)await a.Load().Run()).Value, Is.EqualTo(123));
    Assert.That(((EffResult<string>.Success)await b.Load().Run()).Value, Is.EqualTo("hello"));
  }

  // ── (M, 0) — sink steps (consume inputs, no output) ────────────────────

  [Test]
  public async Task Sink_OneInput_Sync_RunsTransform()
  {
    var input = ItemFactory.Singleton.Memory<int>("sink-in");
    await input.Save(7).Run();

    var collected = -1;
    var flow = FlowBuilder.CreateFlow("consume-one", b =>
      b.AddStep<int>("consume", x => collected = x, input)
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(collected, Is.EqualTo(7));
  }

  [Test]
  public async Task Sink_OneInput_Async_AwaitsTransform()
  {
    var input = ItemFactory.Singleton.Memory<int>("sink-async-in");
    await input.Save(11).Run();

    var collected = -1;
    var flow = FlowBuilder.CreateFlow("consume-async", b =>
      b.AddStep<int>(
        "consume-async",
        async x => { await Task.Yield(); collected = x; },
        input
      )
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(collected, Is.EqualTo(11));
  }

  [Test]
  public async Task Sink_TwoInputs_DeconstructsTuple()
  {
    var a = ItemFactory.Singleton.Memory<int>("sink-a");
    var b = ItemFactory.Singleton.Memory<int>("sink-b");
    await a.Save(3).Run();
    await b.Save(4).Run();

    var sum = 0;
    var flow = FlowBuilder.CreateFlow("consume-two", builder =>
      builder.AddStep<int, int>(
        "consume-two",
        pair => { sum = pair.Item1 + pair.Item2; },
        a,
        b
      )
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(sum, Is.EqualTo(7));
  }

  // ── Mixed shapes in one flow ───────────────────────────────────────────

  [Test]
  public async Task SourceThenSink_ComposesEndToEnd()
  {
    var produced = ItemFactory.Singleton.Memory<int>("composed-mid");
    var collected = -1;

    var flow = FlowBuilder.CreateFlow("source-then-sink", b =>
    {
      b.AddStep<int>("produce-15", () => 15, produced);
      b.AddStep<int>("read-15", x => collected = x, produced);
    });

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(collected, Is.EqualTo(15));
  }
}
