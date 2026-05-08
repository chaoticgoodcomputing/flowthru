using System.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Tests for <see cref="ParallelFlowScheduler"/> — Core's shipped
/// <see cref="IFlowScheduler"/>. Covers Parallelism=1 (sequential
/// FIFO) and Parallelism>1 (intra-layer concurrency) as specified
/// in §2.4.
/// </summary>
[TestFixture]
public class ParallelFlowSchedulerTests
{
  // ── Parallelism = 1 (sequential default) ─────────────────────────────

  [Test]
  public async Task Parallelism1_LinearChain_RunsInTopologicalOrder()
  {
    var s1 = ItemFactory.Singleton.Memory<int>("p1-1");
    var s2 = ItemFactory.Singleton.Memory<int>("p1-2");
    var s3 = ItemFactory.Singleton.Memory<int>("p1-3");
    await s1.Save(10).Run();

    var observed = new List<string>();
    var flow = FlowBuilder.CreateFlow("p1-chain", b =>
    {
      // Declared in reverse — analyser must reorder.
      b.AddStep<int, int>("c", x => { observed.Add("c"); return x; }, s2, s3);
      b.AddStep<int, int>("a", x => { observed.Add("a"); return x; }, s1, s2);
    });

    var scheduler = new ParallelFlowScheduler();
    var result = await scheduler.ExecuteAsync(flow, ExecutionOptions.Default);
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(observed, Is.EqualTo(new[] { "a", "c" }),
      "Sequential scheduler should respect topological order, not declaration order."
    );
  }

  [Test]
  public async Task Parallelism1_StopOnFirstError_SkipsRemaining()
  {
    var s1 = ItemFactory.Singleton.Memory<int>("p1-fail-1");
    var s2 = ItemFactory.Singleton.Memory<int>("p1-fail-2");
    var s3 = ItemFactory.Singleton.Memory<int>("p1-fail-3");
    await s1.Save(0).Run();

    var flow = FlowBuilder.CreateFlow("p1-fail", b =>
    {
      b.AddStep<int, int>("explode", x => 100 / x, s1, s2);
      b.AddStep<int, int>("downstream", x => x + 1, s2, s3);
    });

    var scheduler = new ParallelFlowScheduler();
    var result = await scheduler.ExecuteAsync(flow, ExecutionOptions.Default);
    Assert.That(result.HasFailures, Is.True);
    Assert.That(result.StepResults.Where(r => r is StepResult.Skipped).ToList(),
      Has.Count.EqualTo(1),
      "Downstream step should be Skipped after upstream failure under StopOnFirstError."
    );
  }

  [Test]
  public async Task Parallelism1_DryRun_AllStepsSkipped()
  {
    var input = ItemFactory.Singleton.Memory<int>("p1-dry-in");
    var output = ItemFactory.Singleton.Memory<int>("p1-dry-out");
    await input.Save(1).Run();

    var flow = FlowBuilder.CreateFlow("p1-dry", b =>
      b.AddStep<int, int>("dry-step", x => x, input, output)
    );

    var scheduler = new ParallelFlowScheduler();
    var result = await scheduler.ExecuteAsync(
      flow,
      new ExecutionOptions { DryRun = DryRunOption.On }
    );
    Assert.That(result.StepResults.All(r => r is StepResult.Skipped), Is.True);
  }

  // ── Parallelism > 1 (intra-layer concurrency) ────────────────────────

  [Test]
  public async Task Parallelism4_DiamondDag_IndependentStepsRunConcurrently()
  {
    // Diamond shape:
    //   raw → A → out_a
    //   raw → B → out_b
    // Steps A and B share no data dependency, so under Parallelism>=2
    // they should run concurrently.
    var raw = ItemFactory.Singleton.Memory<int>("d-raw");
    var outA = ItemFactory.Singleton.Memory<int>("d-out-a");
    var outB = ItemFactory.Singleton.Memory<int>("d-out-b");
    await raw.Save(0).Run();

    var aGate = new TaskCompletionSource();
    var bSawAStarted = false;

    var flow = FlowBuilder.CreateFlow("diamond", builder =>
    {
      builder.AddStep<int, int>(
        "branch-a",
        async x =>
        {
          await aGate.Task; // Held open until B signals it's started.
          return x + 1;
        },
        raw,
        outA
      );
      builder.AddStep<int, int>(
        "branch-b",
        x =>
        {
          // If we get here while A is still waiting on aGate, the
          // scheduler ran us concurrently (good). Release A so the
          // run completes.
          bSawAStarted = true;
          aGate.SetResult();
          return x + 2;
        },
        raw,
        outB
      );
    });

    var scheduler = new ParallelFlowScheduler();
    var result = await scheduler.ExecuteAsync(
      flow,
      new ExecutionOptions { Parallelism = 4 }
    );

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(bSawAStarted, Is.True,
      "Under Parallelism=4, branch-b should have run while branch-a was awaiting aGate — proves intra-layer concurrency."
    );
  }

  [Test]
  public async Task Parallelism4_LinearChain_StillRespectsTopologicalOrder()
  {
    // Even at high parallelism, dependent steps cannot run
    // concurrently with their producer.
    var s1 = ItemFactory.Singleton.Memory<int>("p4-1");
    var s2 = ItemFactory.Singleton.Memory<int>("p4-2");
    var s3 = ItemFactory.Singleton.Memory<int>("p4-3");
    await s1.Save(1).Run();

    int? observedAtB = null;
    var flow = FlowBuilder.CreateFlow("p4-chain", b =>
    {
      b.AddStep<int, int>("a", x => x + 10, s1, s2);
      b.AddStep<int, int>(
        "b",
        x =>
        {
          observedAtB = x;
          return x;
        },
        s2,
        s3
      );
    });

    var scheduler = new ParallelFlowScheduler();
    var result = await scheduler.ExecuteAsync(
      flow,
      new ExecutionOptions { Parallelism = 4 }
    );

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(observedAtB, Is.EqualTo(11),
      "Step b must see a's output, even with high parallelism — topological order trumps concurrency."
    );
  }

  // ── Hosting integration ──────────────────────────────────────────────

  [Test]
  public async Task IFlowScheduler_DefaultRegistration_IsParallelFlowScheduler()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new Flowthru.Core.Tests.Hosting.FlowthruServiceTests.TestCatalog());
      b.RegisterFlow("plain", () =>
        FlowBuilder.CreateFlow("plain", p => p.AddStep("noop", () => { }))
      );
    });

    await using var sp = services.BuildServiceProvider();
    var scheduler = sp.GetRequiredService<IFlowScheduler>();
    Assert.That(scheduler, Is.InstanceOf<ParallelFlowScheduler>(),
      "AddFlowthru should register ParallelFlowScheduler as the default IFlowScheduler via TryAddSingleton."
    );
  }

  [Test]
  public async Task IFlowScheduler_HostCanReplaceDefault_UsingTryAddSingletonSemantics()
  {
    var customRanFor = new List<string>();
    var services = new ServiceCollection();
    // Host registers their own scheduler BEFORE AddFlowthru.
    services.AddSingleton<IFlowScheduler>(_ => new TracingScheduler(customRanFor));
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new Flowthru.Core.Tests.Hosting.FlowthruServiceTests.TestCatalog());
      b.RegisterFlow("plain", () =>
        FlowBuilder.CreateFlow("plain", p => p.AddStep("noop", () => { }))
      );
    });

    await using var sp = services.BuildServiceProvider();
    var resolved = sp.GetRequiredService<IFlowScheduler>();
    Assert.That(resolved, Is.InstanceOf<TracingScheduler>(),
      "TryAddSingleton in AddFlowthru should NOT overwrite a host-supplied IFlowScheduler."
    );

    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync(
      "plain",
      new ExecutionOptions { ValidationDepth = ValidationDepth.None }
    );
    Assert.That(customRanFor, Has.Count.EqualTo(1),
      "Custom scheduler should have driven the run."
    );
  }

  /// <summary>
  /// A trivial alternative scheduler used by the
  /// host-replacement test — proves <see cref="IFlowScheduler"/>
  /// is a real extension point.
  /// </summary>
  private sealed class TracingScheduler : IFlowScheduler
  {
    private readonly List<string> _trace;
    public TracingScheduler(List<string> trace) { _trace = trace; }

    public Task<FlowResult> ExecuteAsync(
      BuiltFlow flow,
      ExecutionOptions options,
      CancellationToken cancellationToken = default
    )
    {
      _trace.Add(flow.Label);
      return new ParallelFlowScheduler().ExecuteAsync(flow, options, cancellationToken);
    }
  }
}
