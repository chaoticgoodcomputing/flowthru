using Flowthru.Cli;
using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Cli.Tests;

/// <summary>
/// Phase 4 done-criterion: a smoke-test program runs end-to-end
/// through <see cref="FlowthruCli.RunStandaloneAsync"/>, executing a
/// stub flow and producing a <see cref="FlowResult"/> with expected
/// step outcomes.
/// </summary>
[TestFixture]
public class FlowthruCliEndToEndTests
{
  public sealed class TestCatalog : CatalogAbstract
  {
    public IItem<int> Input => CreateItem(() => ItemFactory.Singleton.Memory<int>("input"));
    public IItem<int> Output => CreateItem(() => ItemFactory.Singleton.Memory<int>("output"));
  }

  [Test]
  public async Task SingleFlowAndDefaults_ExitsZero()
  {
    var consoleOut = new StringWriter();
    var consoleErr = new StringWriter();
    Console.SetOut(consoleOut);
    Console.SetError(consoleErr);

    var exit = await FlowthruCli.RunStandaloneAsync(
      Array.Empty<string>(),
      services => services.AddFlowthru(b =>
      {
        b.RegisterCatalog(_ => new TestCatalog());
        b.RegisterFlow<TestCatalog>("the-only-flow", catalog =>
        {
          catalog.Input.Save(7).Run().GetAwaiter().GetResult();
          return FlowBuilder.CreateFlow("the-only-flow", p =>
            p.AddStep<int, int>("double", x => x * 2, catalog.Input, catalog.Output)
          );
        });
      })
    );
    Assert.That(exit, Is.EqualTo(0),
      "Single registered flow should run by default and exit 0 on success.");
    Assert.That(consoleOut.ToString(), Does.Contain("double"),
      "Successful step should be reported on stdout.");
  }

  [Test]
  public async Task NoFlowFlag_WithMultipleFlows_RunsMergedDag()
  {
    // Per §2.4, all flows registered with the same FlowthruService
    // merge into a single DAG. No --flow flag → run the full merged
    // DAG; the CLI does NOT treat this as a usage error.
    var consoleOut = new StringWriter();
    Console.SetOut(consoleOut);
    Console.SetError(new StringWriter());

    var exit = await FlowthruCli.RunStandaloneAsync(
      Array.Empty<string>(),
      services => services.AddFlowthru(b =>
      {
        b.RegisterCatalog(_ => new TestCatalog());
        b.RegisterFlow("a", () => FlowBuilder.CreateFlow("a", p => p.Add(new NoOpStep("step-a"))));
        b.RegisterFlow("b", () => FlowBuilder.CreateFlow("b", p => p.Add(new NoOpStep("step-b"))));
      })
    );
    Assert.That(exit, Is.EqualTo(0));
    var stdout = consoleOut.ToString();
    Assert.That(stdout, Does.Contain("step-a"),
      "Merged-DAG run should include the step from flow 'a'.");
    Assert.That(stdout, Does.Contain("step-b"),
      "Merged-DAG run should include the step from flow 'b'.");
  }

  [Test]
  public async Task FlowFlag_SlicesMergedDag()
  {
    // --flow <label> → slice the merged DAG to that label's
    // declared output items. With two non-overlapping flows,
    // slicing to one drops the steps that belong only to the other.
    var consoleOut = new StringWriter();
    Console.SetOut(consoleOut);
    Console.SetError(new StringWriter());

    var exit = await FlowthruCli.RunStandaloneAsync(
      new[] { "--flow", "alpha" },
      services => services.AddFlowthru(b =>
      {
        b.RegisterCatalog(_ => new TestCatalog());
        b.RegisterFlow<TestCatalog>("alpha", c =>
        {
          c.Input.Save(7).Run().GetAwaiter().GetResult();
          return FlowBuilder.CreateFlow("alpha", p =>
            p.AddStep<int, int>("alpha-step", x => x + 1, c.Input, c.Output)
          );
        });
        b.RegisterFlow("beta", () =>
          FlowBuilder.CreateFlow("beta", p => p.Add(new NoOpStep("beta-step")))
        );
      })
    );
    Assert.That(exit, Is.EqualTo(0));
    var stdout = consoleOut.ToString();
    Assert.That(stdout, Does.Contain("alpha-step"),
      "Slice to 'alpha' should run alpha-step.");
    Assert.That(stdout, Does.Not.Contain("beta-step"),
      "Slice to 'alpha' should NOT run beta-step (belongs only to flow 'beta').");
  }

  [Test]
  public async Task ListFlag_PrintsRegisteredLabelsAndExitsZero()
  {
    var consoleOut = new StringWriter();
    Console.SetOut(consoleOut);
    Console.SetError(new StringWriter());

    var exit = await FlowthruCli.RunStandaloneAsync(
      new[] { "--list" },
      services => services.AddFlowthru(b =>
      {
        b.RegisterCatalog(_ => new TestCatalog());
        b.RegisterFlow("alpha", () => FlowBuilder.CreateFlow("alpha", p => p.Add(new NoOpStep("alpha-step"))));
        b.RegisterFlow("beta", () => FlowBuilder.CreateFlow("beta", p => p.Add(new NoOpStep("beta-step"))));
      })
    );
    Assert.That(exit, Is.EqualTo(0));
    var stdout = consoleOut.ToString();
    Assert.That(stdout, Does.Contain("alpha"));
    Assert.That(stdout, Does.Contain("beta"));
  }

  [Test]
  public async Task UnknownFlag_PrintsHelpAndExitsTwo()
  {
    var consoleErr = new StringWriter();
    Console.SetError(consoleErr);
    Console.SetOut(new StringWriter());

    var exit = await FlowthruCli.RunStandaloneAsync(
      new[] { "--bogus" },
      services => services.AddFlowthru(b =>
      {
        b.RegisterCatalog(_ => new TestCatalog());
        b.RegisterFlow("x", () => FlowBuilder.CreateFlow("x", p => p.Add(new NoOpStep())));
      })
    );
    Assert.That(exit, Is.EqualTo(2));
    Assert.That(consoleErr.ToString(), Does.Contain("Usage"));
  }

  [Test]
  public async Task ExcludeFlag_DropsExcludedFlowFromMergedRun()
  {
    // --exclude flows:beta over a two-flow registration should run
    // alpha's step but skip beta's step at execution time. We use
    // AddStep<int, int> so the framework's concrete Step type stamps
    // FlowLabel automatically via OnAddedToFlow.
    var consoleOut = new StringWriter();
    Console.SetOut(consoleOut);
    Console.SetError(new StringWriter());

    var alphaSrc = ItemFactory.Singleton.Memory<int>("excl-alpha-src");
    var alphaSink = ItemFactory.Singleton.Memory<int>("excl-alpha-sink");
    var betaSrc = ItemFactory.Singleton.Memory<int>("excl-beta-src");
    var betaSink = ItemFactory.Singleton.Memory<int>("excl-beta-sink");
    await alphaSrc.Save(1).Run();
    await betaSrc.Save(1).Run();

    var exit = await FlowthruCli.RunStandaloneAsync(
      new[] { "--exclude", "flows:beta" },
      services => services.AddFlowthru(b =>
      {
        b.RegisterCatalog(_ => new TestCatalog());
        b.RegisterFlow("alpha", () =>
          FlowBuilder.CreateFlow("alpha", p =>
            p.AddStep<int, int>("alpha-step", x => x + 1, alphaSrc, alphaSink)
          )
        );
        b.RegisterFlow("beta", () =>
          FlowBuilder.CreateFlow("beta", p =>
            p.AddStep<int, int>("beta-step", x => x + 1, betaSrc, betaSink)
          )
        );
      })
    );
    Assert.That(exit, Is.EqualTo(0));
    var stdout = consoleOut.ToString();
    Assert.That(stdout, Does.Contain("alpha-step"),
      "alpha-step should run when only flow 'beta' is excluded.");
    Assert.That(stdout, Does.Not.Contain("beta-step"),
      "beta-step should not run — --exclude flows:beta drops it from the slice.");
  }

  [Test]
  public async Task DryRun_RunsWithoutWritingOutputs()
  {
    Console.SetOut(new StringWriter());
    Console.SetError(new StringWriter());

    TestCatalog? capturedCatalog = null;
    var exit = await FlowthruCli.RunStandaloneAsync(
      new[] { "--dry-run", "--validation-depth", "none" },
      services => services.AddFlowthru(b =>
      {
        b.RegisterCatalog(_ =>
        {
          capturedCatalog = new TestCatalog();
          return capturedCatalog;
        });
        b.RegisterFlow<TestCatalog>("dry", catalog =>
          FlowBuilder.CreateFlow("dry", p =>
            p.AddStep<int, int>("noop", x => x, catalog.Input, catalog.Output)
          )
        );
      })
    );
    Assert.That(exit, Is.EqualTo(0));
    Assert.That(capturedCatalog, Is.Not.Null);
    var existed = await capturedCatalog!.Output.Exists().Run();
    Assert.That(((EffResult<bool>.Success)existed).Value, Is.False,
      "Dry run should not write outputs.");
  }

  private sealed class NoOpStep : IStepNode
  {
    public NoOpStep(string label = "noop") { Label = label; }
    public string Label { get; }
    public NodeTraits Traits => new();
    public IReadOnlyList<IItem> Inputs => Array.Empty<IItem>();
    public IReadOnlyList<IItem> Outputs => Array.Empty<IItem>();
    public IReadOnlyList<ServiceDependency> ServiceDependencies => Array.Empty<ServiceDependency>();
    public FlowIO<Flowthru.Data.Storage.ValidationResult> Validate() =>
      FlowIO.Pure(Flowthru.Data.Storage.ValidationResult.Success());
    public FlowIO<FlowUnit> Execute() => FlowIO.Pure(FlowUnit.Default);
  }
}
