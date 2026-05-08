using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Hosting;

/// <summary>
/// End-to-end tests for the Phase 4 hosting surface:
/// <c>services.AddFlowthru(...)</c> wires multiple catalog factories +
/// flow factories whose parameter lists declare which catalogs they
/// need, plus optional inspectors. The framework resolves each
/// catalog from DI before invoking the flow factory.
/// </summary>
[TestFixture]
public class FlowthruServiceTests
{
  public sealed class TestCatalog : CatalogAbstract
  {
    public IItem<int> Input => CreateItem(() => ItemFactory.Singleton.Memory<int>("input"));
    public IItem<int> Output => CreateItem(() => ItemFactory.Singleton.Memory<int>("output"));
  }

  /// <summary>A non-CatalogAbstract reference type — proves the constraint is `class`, not CatalogAbstract.</summary>
  public sealed record FlowConfig(int Multiplier);

  public sealed class SecondaryCatalog : CatalogAbstract
  {
    public IItem<int> Sink => CreateItem(() => ItemFactory.Singleton.Memory<int>("secondary-sink"));
  }

  [Test]
  public async Task RegisteredFlow_RunsThroughHosting()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow<TestCatalog>("main", catalog =>
      {
        catalog.Input.Save(21).Run().GetAwaiter().GetResult();
        return FlowBuilder.CreateFlow("main", p =>
          p.AddStep<int, int>("double", x => x * 2, catalog.Input, catalog.Output)
        );
      });
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    Assert.That(flowthru.RegisteredFlowLabels, Is.EquivalentTo(new[] { "main" }));

    var result = await flowthru.RunAsync("main");
    Assert.That(result.IsSuccess, Is.True);
  }

  [Test]
  public async Task MultiCatalogFlow_ResolvesEachCatalogFromDi()
  {
    // Two catalogs and a non-CatalogAbstract config record — the canonical
    // multi-domain authoring shape from SpaceflightsDistributed/Program.cs.
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterCatalog(_ => new SecondaryCatalog());
      b.RegisterCatalog(_ => new FlowConfig(Multiplier: 5));
      b.RegisterFlow<TestCatalog, SecondaryCatalog, FlowConfig>(
        "multi",
        (primary, secondary, cfg) =>
        {
          primary.Input.Save(2).Run().GetAwaiter().GetResult();
          return FlowBuilder.CreateFlow("multi", p =>
            p.AddStep<int, int>(
              "scale",
              x => x * cfg.Multiplier,
              primary.Input,
              secondary.Sink
            )
          );
        }
      );
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var secondary = sp.GetRequiredService<SecondaryCatalog>();

    var result = await flowthru.RunAsync("multi");
    Assert.That(result.IsSuccess, Is.True);

    var loaded = await secondary.Sink.Load().Run();
    Assert.That(((EffResult<int>.Success)loaded).Value, Is.EqualTo(10),
      "Cross-catalog flow should write to the secondary catalog using the config-supplied multiplier.");
  }

  [Test]
  public async Task RunAsync_NullLabel_RunsMergedDagAcrossEveryRegisteredFlow()
  {
    // Per §2.4, all flows registered with the same FlowthruService
    // merge into a single DAG. RunAsync(null) runs the entire
    // merged DAG; both flows' steps must execute.
    var stage1 = ItemFactory.Singleton.Memory<int>("stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("stage3");
    await stage1.Save(10).Run();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow("flowA", () => FlowBuilder.CreateFlow("flowA", p =>
        p.AddStep<int, int>("a-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("flowB", () => FlowBuilder.CreateFlow("flowB", p =>
        p.AddStep<int, int>("b-step", x => x * 2, stage2, stage3)
      ));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync(); // No label → run merged DAG.
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.StepResults.Select(r => r.StepLabel),
      Is.EquivalentTo(new[] { "a-step", "b-step" }),
      "Merged DAG should run every step across registered flows.");

    var final = await stage3.Load().Run();
    Assert.That(((EffResult<int>.Success)final).Value, Is.EqualTo(22),
      "End-to-end value: (10 + 1) * 2 = 22 — proves the cross-flow data dependency was honoured.");
  }

  [Test]
  public async Task RunAsync_FlowLabel_SlicesMergedDagToThatLabelsOutputs()
  {
    // --flow X / RunAsync("X") slices the merged DAG to the
    // subgraph reachable from flow X's declared output items.
    // Steps belonging only to other flows are skipped.
    var stage1 = ItemFactory.Singleton.Memory<int>("stage1-slice");
    var stage2 = ItemFactory.Singleton.Memory<int>("stage2-slice");
    var stage3 = ItemFactory.Singleton.Memory<int>("stage3-slice");
    await stage1.Save(10).Run();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow("upstream", () => FlowBuilder.CreateFlow("upstream", p =>
        p.AddStep<int, int>("upstream-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("downstream", () => FlowBuilder.CreateFlow("downstream", p =>
        p.AddStep<int, int>("downstream-step", x => x * 2, stage2, stage3)
      ));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    // Slice to "upstream" — should run upstream-step but NOT downstream-step.
    var result = await flowthru.RunAsync("upstream");
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.StepResults.Select(r => r.StepLabel),
      Is.EquivalentTo(new[] { "upstream-step" }),
      "Slice to 'upstream' should run only upstream-step.");

    var stage3Existed = await stage3.Exists().Run();
    Assert.That(((EffResult<bool>.Success)stage3Existed).Value, Is.False,
      "stage3 should not be written by the upstream-only slice.");
  }

  [Test]
  public async Task RunAsync_FlowLabel_PullsInUpstreamDependencies()
  {
    // The slice walks BACKWARDS from declared outputs — so requesting
    // a downstream flow's outputs runs the upstream flow's steps too,
    // when intermediate items connect them. This is the §2.4
    // "DataScience pulls in DataEngineering" idiom.
    var stage1 = ItemFactory.Singleton.Memory<int>("stage1-deps");
    var stage2 = ItemFactory.Singleton.Memory<int>("stage2-deps");
    var stage3 = ItemFactory.Singleton.Memory<int>("stage3-deps");
    await stage1.Save(10).Run();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow("upstream", () => FlowBuilder.CreateFlow("upstream", p =>
        p.AddStep<int, int>("u-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("downstream", () => FlowBuilder.CreateFlow("downstream", p =>
        p.AddStep<int, int>("d-step", x => x * 2, stage2, stage3)
      ));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync("downstream");
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.StepResults.Select(r => r.StepLabel),
      Is.EquivalentTo(new[] { "u-step", "d-step" }),
      "Slicing to 'downstream' should pull in 'upstream' too — its outputs feed downstream's inputs.");
  }

  [Test]
  public async Task RegisterFlow_FluentDescription_AttachesToRegistration()
  {
    var services = new ServiceCollection();
    IFlowRegistration? registration = null;
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      registration = b
        .RegisterFlow<TestCatalog>("main", catalog =>
          FlowBuilder.CreateFlow("main", p =>
            p.AddStep<int, int>("noop", x => x, catalog.Input, catalog.Output)
          )
        )
        .WithDescription("Smoke test flow");
    });

    Assert.That(registration, Is.Not.Null);
    Assert.That(registration!.Label, Is.EqualTo("main"));
  }

  [Test]
  public async Task PreFlightFailure_SurfacesAsStepResultFailedWithInvariantViolated()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      // No Save on Input → MissingInput / NotFound during pre-flight inspection.
      b.RegisterFlow<TestCatalog>("missing", catalog =>
        FlowBuilder.CreateFlow("missing", p =>
          p.AddStep<int, int>("double", x => x * 2, catalog.Input, catalog.Output)
        )
      );
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync("missing");
    Assert.That(result.HasFailures, Is.True);
    var failure = result.FirstFailure;
    Assert.That(failure, Is.Not.Null);
    Assert.That(failure!.StepLabel, Is.EqualTo("preflight"));
    Assert.That(failure!.Error, Is.InstanceOf<RuntimeError.InvariantViolated>());
  }

  [Test]
  public async Task UnknownFlowLabel_Throws()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow("only", () => FlowBuilder.CreateFlow("only", p => p.Add(new NoOpStep())));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    Assert.ThrowsAsync<InvalidOperationException>(() => flowthru.RunAsync("missing"));
  }

  [Test]
  public async Task ValidationDepthNone_SkipsPreFlight()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow<TestCatalog>("skip-pf", catalog =>
      {
        catalog.Input.Save(2).Run().GetAwaiter().GetResult();
        return FlowBuilder.CreateFlow("skip-pf", p =>
          p.AddStep<int, int>("double", x => x * 2, catalog.Input, catalog.Output)
        );
      });
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync(
      "skip-pf",
      new ExecutionOptions { ValidationDepth = ValidationDepth.None }
    );
    Assert.That(result.IsSuccess, Is.True);
  }

  private sealed class NoOpStep : IStepNode
  {
    public string Label => "noop";
    public NodeTraits Traits => new();
    public IReadOnlyList<IItem> Inputs => Array.Empty<IItem>();
    public IReadOnlyList<IItem> Outputs => Array.Empty<IItem>();
    public IReadOnlyList<ServiceRef> ServiceDependencies => Array.Empty<ServiceRef>();
    public FlowIO<Flowthru.Data.Storage.ValidationResult> Validate() =>
      FlowIO.Pure(Flowthru.Data.Storage.ValidationResult.Success());
    public FlowIO<FlowUnit> Execute() => FlowIO.Pure(FlowUnit.Default);
  }
}
