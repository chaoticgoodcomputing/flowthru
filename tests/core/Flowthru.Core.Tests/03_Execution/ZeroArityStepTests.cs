using Flowthru.Core.Data;
using Flowthru.Core.Flows;

namespace Flowthru.Core.Tests.Execution;

/// <summary>
/// Tests verifying execution of steps with zero inputs and/or zero outputs across
/// all transform variants (sync, async, async-with-CancellationToken). These shapes
/// replace the legacy <c>NoData</c>/<c>NullStorageAdapter</c> sentinel pattern.
/// </summary>
[TestFixture]
[Category("Execution")]
[Category("StepExecution")]
public class ZeroArityStepTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // 0×0 — no inputs, no outputs
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ZeroByZero_Sync_ExecutesSideEffect()
  {
    var counter = new Counter();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(label: "Tick", transform: () => counter.Increment());
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    Assert.That(counter.Value, Is.EqualTo(1));
  }

  [Test]
  public async Task ZeroByZero_Async_ExecutesAndAwaitsSideEffect()
  {
    var counter = new Counter();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "TickAsync",
        transform: async () =>
        {
          await Task.Yield();
          counter.Increment();
        }
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    Assert.That(counter.Value, Is.EqualTo(1));
  }

  [Test]
  public async Task ZeroByZero_AsyncWithCancellation_ReceivesToken()
  {
    var captured = default(CancellationToken);
    using var cts = new CancellationTokenSource();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "TickWithToken",
        transform: async (CancellationToken ct) =>
        {
          captured = ct;
          await Task.Yield();
        }
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(cts.Token);

    // The engine creates its own linked token internally; we can verify only that
    // a non-default CT was threaded through (not its identity).
    Assert.That(captured, Is.Not.EqualTo(default(CancellationToken)));
  }

  [Test]
  public async Task ZeroByZero_WithDescription_RecordsDescription()
  {
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "TickWithDesc",
        transform: () => { },
        description: "Smoke-test step with no IO."
      );
    });

    pipeline.Build();
    var step = pipeline.Steps.Single(s => s.Label == "TickWithDesc");

    Assert.That(step.Description, Is.EqualTo("Smoke-test step with no IO."));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // 0×N — no inputs, one or more outputs
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ZeroByOne_Sync_ProducesOutput()
  {
    var catalog = new ZeroArityCatalog();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(label: "Produce", transform: () => 42, output: catalog.OutputInt);
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    var result = await catalog.OutputInt.Load().Run();
    Assert.That(result, Is.EqualTo(42));
  }

  [Test]
  public async Task ZeroByOne_Async_ProducesOutput()
  {
    var catalog = new ZeroArityCatalog();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "ProduceAsync",
        transform: async () =>
        {
          await Task.Yield();
          return 99;
        },
        output: catalog.OutputInt
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    var result = await catalog.OutputInt.Load().Run();
    Assert.That(result, Is.EqualTo(99));
  }

  [Test]
  public async Task ZeroByThree_Sync_ProducesAllOutputs()
  {
    var catalog = new ZeroArityCatalog();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "ProduceTuple",
        transform: () => (1, "two", 3.0),
        output: (catalog.OutputInt, catalog.OutputString, catalog.OutputDouble)
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    Assert.That(await catalog.OutputInt.Load().Run(), Is.EqualTo(1));
    Assert.That(await catalog.OutputString.Load().Run(), Is.EqualTo("two"));
    Assert.That(await catalog.OutputDouble.Load().Run(), Is.EqualTo(3.0));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // N×0 — one or more inputs, no outputs
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task OneByZero_Sync_ConsumesInput()
  {
    var catalog = new ZeroArityCatalog();
    await catalog.OutputInt.Save(7).Run();

    var captured = -1;
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "Consume",
        transform: (int v) => captured = v,
        input: catalog.OutputInt
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    Assert.That(captured, Is.EqualTo(7));
  }

  [Test]
  public async Task OneByZero_Async_ConsumesInput()
  {
    var catalog = new ZeroArityCatalog();
    await catalog.OutputInt.Save(11).Run();

    var captured = -1;
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "ConsumeAsync",
        transform: async (int v) =>
        {
          await Task.Yield();
          captured = v;
        },
        input: catalog.OutputInt
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    Assert.That(captured, Is.EqualTo(11));
  }

  [Test]
  public async Task ThreeByZero_Sync_ConsumesAllInputs()
  {
    var catalog = new ZeroArityCatalog();
    await catalog.OutputInt.Save(5).Run();
    await catalog.OutputString.Save("hello").Run();
    await catalog.OutputDouble.Save(2.5).Run();

    int? capturedInt = null;
    string? capturedString = null;
    double? capturedDouble = null;

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "ConsumeTuple",
        transform: ((int i, string s, double d) tuple) =>
        {
          capturedInt = tuple.i;
          capturedString = tuple.s;
          capturedDouble = tuple.d;
        },
        input: (catalog.OutputInt, catalog.OutputString, catalog.OutputDouble)
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    Assert.That(capturedInt, Is.EqualTo(5));
    Assert.That(capturedString, Is.EqualTo("hello"));
    Assert.That(capturedDouble, Is.EqualTo(2.5));
  }

  [Test]
  public async Task ThreeByZero_Async_ConsumesAllInputs()
  {
    var catalog = new ZeroArityCatalog();
    await catalog.OutputInt.Save(15).Run();
    await catalog.OutputString.Save("world").Run();
    await catalog.OutputDouble.Save(7.5).Run();

    var sum = 0.0;

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "ConsumeTupleAsync",
        transform: async ((int i, string s, double d) tuple) =>
        {
          await Task.Yield();
          sum = tuple.i + tuple.d;
        },
        input: (catalog.OutputInt, catalog.OutputString, catalog.OutputDouble)
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    Assert.That(sum, Is.EqualTo(22.5));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Multi-arity async-with-CancellationToken — exercises the engine's
  // dataParameterCount logic that strips trailing CT from multi-input invokes.
  // Previously unreachable: pre-Phase-1, the generator emitted no CT variants
  // for non-1×1 arities.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task TwoByOne_AsyncWithCancellation_StripsTrailingCancellationToken()
  {
    var catalog = new ZeroArityCatalog();
    await catalog.OutputInt.Save(2).Run();
    await catalog.OutputDouble.Save(2.5).Run();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "MultiplyWithToken",
        transform: async ((int i, double d) tuple, CancellationToken ct) =>
        {
          await Task.Yield();
          ct.ThrowIfCancellationRequested();
          return tuple.i * tuple.d;
        },
        input: (catalog.OutputInt, catalog.OutputDouble),
        output: catalog.OutputDouble
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    var result = await catalog.OutputDouble.Load().Run();
    Assert.That(result, Is.EqualTo(5.0));
  }

  [Test]
  public async Task OneByTwo_AsyncWithCancellation_ReceivesToken()
  {
    var catalog = new ZeroArityCatalog();
    await catalog.OutputInt.Save(7).Run();

    var capturedToken = default(CancellationToken);

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "SplitWithToken",
        transform: async (int input, CancellationToken ct) =>
        {
          await Task.Yield();
          capturedToken = ct;
          return (input * 2, $"value-{input}");
        },
        input: catalog.OutputInt,
        output: (catalog.OutputInt, catalog.OutputString)
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    int resultInt = await catalog.OutputInt.Load().Run();
    string resultString = await catalog.OutputString.Load().Run();
    Assert.Multiple(() =>
    {
      Assert.That(resultInt, Is.EqualTo(14));
      Assert.That(resultString, Is.EqualTo("value-7"));
      Assert.That(capturedToken, Is.Not.EqualTo(default(CancellationToken)));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Cancellation honored, not just received
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void ZeroByZero_AsyncWithCancellation_HonorsCancellation()
  {
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "RespectsToken",
        transform: (CancellationToken ct) => Task.Delay(TimeSpan.FromMinutes(5), ct)
      );
    });

    pipeline.Build();

    using var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromMilliseconds(50));

    Assert.ThrowsAsync<OperationCanceledException>(async () =>
      await pipeline.RunAsync(cts.Token));
  }

  [Test]
  public void OneByZero_AsyncWithCancellation_HonorsCancellation()
  {
    var catalog = new ZeroArityCatalog();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "ConsumeWithToken",
        transform: (int value, CancellationToken ct) => Task.Delay(TimeSpan.FromMinutes(5), ct),
        input: catalog.OutputInt
      );
    });

    pipeline.Build();

    using var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromMilliseconds(50));

    Assert.ThrowsAsync<OperationCanceledException>(async () =>
    {
      await catalog.OutputInt.Save(7).Run();
      await pipeline.RunAsync(cts.Token);
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Completing the async-with-CancellationToken arity matrix
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ZeroByOne_AsyncWithCancellation_ProducesAndReceivesToken()
  {
    var catalog = new ZeroArityCatalog();
    var capturedToken = default(CancellationToken);

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "ProduceWithToken",
        transform: async (CancellationToken ct) =>
        {
          await Task.Yield();
          capturedToken = ct;
          return 123;
        },
        output: catalog.OutputInt
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    int result = await catalog.OutputInt.Load().Run();
    Assert.Multiple(() =>
    {
      Assert.That(result, Is.EqualTo(123));
      Assert.That(capturedToken, Is.Not.EqualTo(default(CancellationToken)));
    });
  }

  [Test]
  public async Task OneByZero_AsyncWithCancellation_ConsumesAndReceivesToken()
  {
    var catalog = new ZeroArityCatalog();
    await catalog.OutputInt.Save(99).Run();

    var capturedValue = -1;
    var capturedToken = default(CancellationToken);

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "ConsumeWithToken",
        transform: async (int value, CancellationToken ct) =>
        {
          await Task.Yield();
          capturedValue = value;
          capturedToken = ct;
        },
        input: catalog.OutputInt
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(capturedValue, Is.EqualTo(99));
      Assert.That(capturedToken, Is.Not.EqualTo(default(CancellationToken)));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Mixed flow integration — 0-arity steps alongside conventional 1×1 steps
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ZeroArity_MixedWithConventionalSteps_AllExecute()
  {
    var catalog = new ZeroArityCatalog();
    var sideEffectFired = false;

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      // 0×1 — produces seed value (no upstream dependency).
      builder.AddStep(label: "Seed", transform: () => 10, output: catalog.OutputInt);

      // 1×1 — conventional transform consuming the seed.
      builder.AddStep(
        label: "Double",
        transform: (int v) => v * 2,
        input: catalog.OutputInt,
        output: catalog.OutputDouble
      );

      // 1×0 — sink that consumes the doubled value (no downstream dependency).
      builder.AddStep(
        label: "Notify",
        transform: (double _) => sideEffectFired = true,
        input: catalog.OutputDouble
      );
    });

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    int seedResult = await catalog.OutputInt.Load().Run();
    double doubledResult = await catalog.OutputDouble.Load().Run();
    Assert.Multiple(() =>
    {
      Assert.That(seedResult, Is.EqualTo(10));
      Assert.That(doubledResult, Is.EqualTo(20.0));
      Assert.That(sideEffectFired, Is.True);
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Error propagation — exceptions inside 0-arity transforms surface cleanly
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ZeroByZero_TransformThrows_ResultReportsFailure()
  {
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "Boom",
        transform: () => throw new InvalidOperationException("boom from 0×0")
      );
    });

    pipeline.Build();
    FlowResult result = await pipeline.RunAsync(CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.Success, Is.False);
      Assert.That(result.Exception, Is.Not.Null);
      Assert.That(result.Exception!.Message, Does.Contain("boom from 0×0"));
    });
  }

  [Test]
  public async Task ZeroByOne_AsyncTransformThrows_ResultReportsFailure()
  {
    var catalog = new ZeroArityCatalog();

    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "ProduceBoom",
        transform: () =>
          Task.FromException<int>(new InvalidOperationException("boom from 0×1")),
        output: catalog.OutputInt
      );
    });

    pipeline.Build();
    FlowResult result = await pipeline.RunAsync(CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.Success, Is.False);
      Assert.That(result.Exception, Is.Not.Null);
      Assert.That(result.Exception!.Message, Does.Contain("boom from 0×1"));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private sealed class Counter
  {
    public int Value { get; private set; }

    public void Increment() => Value++;
  }

  private sealed class ZeroArityCatalog : CatalogAbstract
  {
    public ZeroArityCatalog()
    {
      InitializeCatalogProperties();
    }

    public IItem<int> OutputInt =>
      CreateItem(() => ItemFactory.Single.Memory<int>(label: "output_int"));

    public IItem<string> OutputString =>
      CreateItem(() => ItemFactory.Single.Memory<string>(label: "output_string"));

    public IItem<double> OutputDouble =>
      CreateItem(() => ItemFactory.Single.Memory<double>(label: "output_double"));
  }
}
