using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Phase 3 done-criterion: a stub flow with one step (declared via
/// the <c>FlowBuilderGenerator</c>-emitted <c>AddStep</c> overload)
/// builds via <c>FlowBuilder.CreateFlow(p =&gt; p.AddStep(…))</c>,
/// runs via <c>BuiltFlow.RunAsync</c>, and produces expected output.
/// </summary>
[TestFixture]
public class StubFlowEndToEndTests
{
  [Test]
  public async Task SingleStepRoundTripsThroughEngine()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");

    await input.Save(21).Run();

    var flow = FlowBuilder.CreateFlow("stub", b =>
      b.AddStep<int, int>(
        "double",
        x => x * 2,
        input,
        output
      )
    );

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True,
      "Stub flow should run successfully end-to-end.");
    Assert.That(result.StepResults, Has.Count.EqualTo(1));
    Assert.That(result.StepResults[0], Is.InstanceOf<StepResult.Succeeded>());

    var loaded = await output.Load().Run();
    Assert.That(loaded, Is.InstanceOf<EffResult<int>.Success>());
    Assert.That(((EffResult<int>.Success)loaded).Value, Is.EqualTo(42));
  }

  [Test]
  public async Task TwoInputOneOutputArityWiresThroughTuple()
  {
    var a = ItemFactory.Singleton.Memory<int>("a");
    var b = ItemFactory.Singleton.Memory<int>("b");
    var sum = ItemFactory.Singleton.Memory<int>("sum");

    await a.Save(3).Run();
    await b.Save(5).Run();

    var flow = FlowBuilder.CreateFlow("tuple", builder =>
      builder.AddStep<int, int, int>(
        "sum",
        pair => pair.Item1 + pair.Item2,
        a,
        b,
        sum
      )
    );

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);

    var loaded = await sum.Load().Run();
    Assert.That(((EffResult<int>.Success)loaded).Value, Is.EqualTo(8));
  }

  [Test]
  public async Task TwoStepsExecuteInDependencyOrder()
  {
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var doubled = ItemFactory.Singleton.Memory<int>("doubled");
    var plusOne = ItemFactory.Singleton.Memory<int>("plusOne");

    await raw.Save(10).Run();

    var flow = FlowBuilder.CreateFlow("chain", b =>
    {
      // Declared in reverse order on purpose — analyser must reorder.
      b.AddStep<int, int>("plus-one", x => x + 1, doubled, plusOne);
      b.AddStep<int, int>("double", x => x * 2, raw, doubled);
    });

    Assert.That(flow.Steps.Select(s => s.Label), Is.EqualTo(new[] { "double", "plus-one" }),
      "DependencyAnalyzer should topologically reorder so 'double' precedes 'plus-one'.");

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);

    var loaded = await plusOne.Load().Run();
    Assert.That(((EffResult<int>.Success)loaded).Value, Is.EqualTo(21));
  }

  [Test]
  public async Task FailedTransformProducesStepFailedRuntimeError()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");

    await input.Save(0).Run();

    var flow = FlowBuilder.CreateFlow("boom", b =>
      b.AddStep<int, int>(
        "explode",
        x => 100 / x, // DivideByZero
        input,
        output
      )
    );

    var result = await flow.RunAsync();
    Assert.That(result.HasFailures, Is.True);
    Assert.That(result.FirstFailure, Is.Not.Null);
    Assert.That(result.FirstFailure!.Error, Is.InstanceOf<RuntimeError.StepFailed>(),
      "Failed transforms should be wrapped as RuntimeError.StepFailed for attribution.");
    var stepFailed = (RuntimeError.StepFailed)result.FirstFailure!.Error;
    Assert.That(stepFailed.StepId, Is.EqualTo("explode"));
  }

  [Test]
  public void DependencyAnalyzerRejectsCycle()
  {
    var a = ItemFactory.Singleton.Memory<int>("a");
    var b = ItemFactory.Singleton.Memory<int>("b");

    Assert.Throws<FlowBuildException>(() =>
      FlowBuilder.CreateFlow("cyclic", builder =>
      {
        builder.AddStep<int, int>("a-to-b", x => x, a, b);
        builder.AddStep<int, int>("b-to-a", x => x, b, a);
      }),
      "Two-step cycle should be detected at Build time, not Run time."
    );
  }

  [Test]
  public void DependencyAnalyzerRejectsDuplicateProducer()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("shared");

    Assert.Throws<FlowBuildException>(() =>
      FlowBuilder.CreateFlow("dup", builder =>
      {
        builder.AddStep<int, int>("first", x => x + 1, input, output);
        builder.AddStep<int, int>("second", x => x + 2, input, output);
      }),
      "Two steps writing the same item should fail the single-producer law (§2.4)."
    );
  }

  [Test]
  public async Task DryRunSkipsTransforms()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");

    await input.Save(7).Run();

    var flow = FlowBuilder.CreateFlow("dry", b =>
      b.AddStep<int, int>("noop", x => x * 1000, input, output)
    );

    var result = await flow.RunAsync(new ExecutionOptions { DryRun = DryRunOption.On });
    Assert.That(result.StepResults, Has.Count.EqualTo(1));
    Assert.That(result.StepResults[0], Is.InstanceOf<StepResult.Skipped>(),
      "DryRun.On should skip every step without invoking its transform.");

    // Output should remain empty / never-saved.
    var existed = await output.Exists().Run();
    Assert.That(existed, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)existed).Value, Is.False,
      "Dry run should not write outputs.");
  }

  [Test]
  public async Task AsyncTransformOverloadIsCoveredByGeneratedAddStep()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");

    await input.Save(11).Run();

    var flow = FlowBuilder.CreateFlow("async", b =>
      b.AddStep<int, int>(
        "async-double",
        async x => { await Task.Yield(); return x * 2; },
        input,
        output
      )
    );

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(((EffResult<int>.Success)await output.Load().Run()).Value, Is.EqualTo(22));
  }

  [Test]
  public async Task AsyncWithCtTransformOverloadIsCoveredByGeneratedAddStep()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");

    await input.Save(7).Run();

    var flow = FlowBuilder.CreateFlow("async-ct", b =>
      b.AddStep<int, int>(
        "async-ct-double",
        async (x, ct) => { await Task.Yield(); return x * 2; },
        input,
        output
      )
    );

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(((EffResult<int>.Success)await output.Load().Run()).Value, Is.EqualTo(14));
  }

  [Test]
  public async Task MultiOutputArityWritesEachOutputFromTuple()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var doubled = ItemFactory.Singleton.Memory<int>("doubled");
    var tripled = ItemFactory.Singleton.Memory<int>("tripled");

    await input.Save(4).Run();

    var flow = FlowBuilder.CreateFlow("multi-out", b =>
      b.AddStep<int, int, int>(
        "split",
        x => (x * 2, x * 3),
        input,
        doubled,
        tripled
      )
    );

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);

    Assert.That(((EffResult<int>.Success)await doubled.Load().Run()).Value, Is.EqualTo(8));
    Assert.That(((EffResult<int>.Success)await tripled.Load().Run()).Value, Is.EqualTo(12));
  }

  [Test]
  public async Task SliceRunsOnlyTheSubgraphProducingTargets()
  {
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var sideOnly = ItemFactory.Singleton.Memory<int>("sideOnly");
    var mainOut = ItemFactory.Singleton.Memory<int>("mainOut");

    await raw.Save(10).Run();

    var flow = FlowBuilder.CreateFlow("multi", b =>
    {
      b.AddStep<int, int>("main", x => x + 1, raw, mainOut);
      b.AddStep<int, int>("side", x => x + 1000, raw, sideOnly);
    });

    var result = await flow.RunSliceAsync(new[] { "mainOut" });
    Assert.That(result.StepResults, Has.Count.EqualTo(1),
      "Slice to mainOut should keep only the 'main' step.");
    Assert.That(result.StepResults[0].StepLabel, Is.EqualTo("main"));

    var sideExists = await sideOnly.Exists().Run();
    Assert.That(((EffResult<bool>.Success)sideExists).Value, Is.False,
      "Side step should not have been executed by the slice.");
  }
}
