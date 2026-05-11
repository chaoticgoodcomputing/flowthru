using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

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
        (a, b)
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
        (a, b)
      )
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(sum, Is.EqualTo(7));
  }

  [Test]
  public async Task Source_OneOutput_AsyncWithToken_PassesCancellationToken()
  {
    var output = ItemFactory.Singleton.Memory<int>("source-ct");
    CancellationToken? captured = null;
    var flow = FlowBuilder.CreateFlow("produce-ct", b =>
      b.AddStep<int>(
        "produce-ct",
        async (CancellationToken ct) =>
        {
          captured = ct;
          await Task.Yield();
          return 7;
        },
        output
      )
    );
    using var cts = new CancellationTokenSource();
    var result = await flow.RunAsync(ExecutionOptions.Default, cts.Token);
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(captured, Is.EqualTo(cts.Token));
    Assert.That(((EffResult<int>.Success)await output.Load().Run()).Value, Is.EqualTo(7));
  }

  [Test]
  public async Task Source_TwoOutputs_Async_FansOut()
  {
    var a = ItemFactory.Singleton.Memory<int>("source-2async-a");
    var b = ItemFactory.Singleton.Memory<string>("source-2async-b");
    var flow = FlowBuilder.CreateFlow("produce-two-async", builder =>
      builder.AddStep<int, string>(
        "produce",
        async () => { await Task.Yield(); return (5, "five"); },
        (a, b)
      )
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(((EffResult<int>.Success)await a.Load().Run()).Value, Is.EqualTo(5));
    Assert.That(((EffResult<string>.Success)await b.Load().Run()).Value, Is.EqualTo("five"));
  }

  [Test]
  public async Task Source_ThreeOutputs_Sync_FansOutToAllThree()
  {
    var a = ItemFactory.Singleton.Memory<int>("source-3a");
    var b = ItemFactory.Singleton.Memory<int>("source-3b");
    var c = ItemFactory.Singleton.Memory<int>("source-3c");
    var flow = FlowBuilder.CreateFlow("produce-three", builder =>
      builder.AddStep<int, int, int>(
        "produce-three",
        () => (1, 2, 3),
        (a, b, c)
      )
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(((EffResult<int>.Success)await a.Load().Run()).Value, Is.EqualTo(1));
    Assert.That(((EffResult<int>.Success)await b.Load().Run()).Value, Is.EqualTo(2));
    Assert.That(((EffResult<int>.Success)await c.Load().Run()).Value, Is.EqualTo(3));
  }

  // ── (M, 0) — additional sink variants ──────────────────────────────────

  [Test]
  public async Task Sink_OneInput_AsyncWithToken_PassesCancellationToken()
  {
    var input = ItemFactory.Singleton.Memory<int>("sink-ct-in");
    await input.Save(99).Run();

    CancellationToken? captured = null;
    var collected = -1;
    var flow = FlowBuilder.CreateFlow("consume-ct", b =>
      b.AddStep<int>(
        "consume-ct",
        async (int x, CancellationToken ct) =>
        {
          captured = ct;
          collected = x;
          await Task.Yield();
        },
        input
      )
    );
    using var cts = new CancellationTokenSource();
    var result = await flow.RunAsync(ExecutionOptions.Default, cts.Token);
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(collected, Is.EqualTo(99));
    Assert.That(captured, Is.EqualTo(cts.Token));
  }

  [Test]
  public async Task Sink_TwoInputs_Async_DeconstructsAndAwaits()
  {
    var a = ItemFactory.Singleton.Memory<int>("sink-2async-a");
    var b = ItemFactory.Singleton.Memory<int>("sink-2async-b");
    await a.Save(10).Run();
    await b.Save(20).Run();

    var sum = 0;
    var flow = FlowBuilder.CreateFlow("consume-two-async", builder =>
      builder.AddStep<int, int>(
        "consume-two-async",
        async pair => { await Task.Yield(); sum = pair.Item1 + pair.Item2; },
        (a, b)
      )
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(sum, Is.EqualTo(30));
  }

  [Test]
  public async Task Sink_ThreeInputs_Sync_DeconstructsAllThree()
  {
    var a = ItemFactory.Singleton.Memory<int>("sink-3a");
    var b = ItemFactory.Singleton.Memory<int>("sink-3b");
    var c = ItemFactory.Singleton.Memory<int>("sink-3c");
    await a.Save(1).Run();
    await b.Save(2).Run();
    await c.Save(3).Run();

    var sum = 0;
    var flow = FlowBuilder.CreateFlow("consume-three", builder =>
      builder.AddStep<int, int, int>(
        "consume-three",
        triple => { sum = triple.Item1 + triple.Item2 + triple.Item3; },
        (a, b, c)
      )
    );
    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(sum, Is.EqualTo(6));
  }

  // ── Transform-throws — error surfaces, downstream cannot continue ──────

  [Test]
  public async Task SideEffect_TransformThrows_SurfacesAsFailure()
  {
    var flow = FlowBuilder.CreateFlow("throwing-tick", b =>
      b.AddStep("throwing-tick", () => throw new InvalidOperationException("boom"))
    );
    var result = await flow.RunAsync();
    Assert.That(result.HasFailures, Is.True,
      "A throwing zero-arity transform must surface as a step failure, "
        + "not silently complete or escape the FlowIO boundary.");
  }

  [Test]
  public async Task Source_TransformThrows_SurfacesAsFailure_NoOutputWritten()
  {
    var output = ItemFactory.Singleton.Memory<int>("throw-source");
    Func<int> bomb = () => throw new InvalidOperationException("source bombed");
    var flow = FlowBuilder.CreateFlow("throwing-source", b =>
      b.AddStep<int>("throwing-source", bomb, output)
    );
    var result = await flow.RunAsync();
    Assert.That(result.HasFailures, Is.True);
    var loaded = await output.Load().Run();
    Assert.That(loaded, Is.InstanceOf<EffResult<int>.Failure>(),
      "Throwing source must not partially-commit its output — the memory adapter "
        + "stays empty when the transform throws.");
  }

  [Test]
  public async Task Sink_TransformThrows_SurfacesAsFailure()
  {
    var input = ItemFactory.Singleton.Memory<int>("throw-sink-in");
    await input.Save(42).Run();
    Action<int> bomb = _ => throw new InvalidOperationException("sink bombed");
    var flow = FlowBuilder.CreateFlow("throwing-sink", b =>
      b.AddStep<int>("throwing-sink", bomb, input)
    );
    var result = await flow.RunAsync();
    Assert.That(result.HasFailures, Is.True);
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

  [Test]
  public async Task MixedWithConventionalSteps_AllArityShapesCoexist()
  {
    // Exercises that zero-arity AddStep overloads compose with the regular
    // (M, N) matrix in a single flow — i.e. the generator's zero-arity
    // emissions don't clash with the matrix cells.
    var src = ItemFactory.Singleton.Memory<int>("mixed-source");
    var mid = ItemFactory.Singleton.Memory<int>("mixed-mid");
    var sideEffectRan = false;

    var flow = FlowBuilder.CreateFlow("mixed-arity", b =>
    {
      b.AddStep("warm-up", () => { sideEffectRan = true; });            // (0, 0)
      b.AddStep<int>("seed", () => 10, src);                            // (0, 1)
      b.AddStep<int, int>("double", x => x * 2, src, mid);              // (1, 1)
      b.AddStep<int>("consume", _ => { /* sink, no output */ }, mid);   // (1, 0)
    });

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(sideEffectRan, Is.True);
    Assert.That(((EffResult<int>.Success)await mid.Load().Run()).Value, Is.EqualTo(20));
  }

  // ── Null-arg validation ────────────────────────────────────────────────

  [Test]
  public void SideEffect_NullTransform_ThrowsArgumentNullException()
  {
    Action nullTransform = null!;
    Assert.That(
      () => FlowBuilder.CreateFlow("null-action", b =>
        b.AddStep("tick", nullTransform)
      ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Source_NullTransform_ThrowsArgumentNullException()
  {
    var output = ItemFactory.Singleton.Memory<int>("null-source");
    Func<int> nullTransform = null!;
    Assert.That(
      () => FlowBuilder.CreateFlow("null-source", b =>
        b.AddStep<int>("seed", nullTransform, output)
      ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Sink_NullTransform_ThrowsArgumentNullException()
  {
    var input = ItemFactory.Singleton.Memory<int>("null-sink");
    Action<int> nullTransform = null!;
    Assert.That(
      () => FlowBuilder.CreateFlow("null-sink", b =>
        b.AddStep<int>("consume", nullTransform, input)
      ),
      Throws.TypeOf<ArgumentNullException>()
    );
  }
}
