using Flowthru.Core.Tests.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Tests.Hosting;

/// <summary>
/// Asserts <see cref="FlowthruService"/> emits lifecycle, pre-flight,
/// and run-finished logs directly through its injected
/// <see cref="ILogger{FlowthruService}"/>. Replaces the equivalent
/// bridge-rendering tests that were retired with
/// <c>FlowthruActivityLogger</c>.
/// </summary>
[TestFixture]
public class FlowthruServiceLoggingTests
{
  public sealed class TestCatalog : CatalogAbstract
  {
    public IItem<int> Input => CreateItem(() => ItemFactory.Singleton.Memory<int>("fsl-input"));
    public IItem<int> Output => CreateItem(() => ItemFactory.Singleton.Memory<int>("fsl-output"));
  }

  private static (ServiceProvider Provider, CapturingLoggerProvider Capture) BuildHostWithCapture(
    Action<IFlowthruBuilder> configureFlowthru
  )
  {
    var capture = new CapturingLoggerProvider();
    var services = new ServiceCollection();
    services.AddLogging(b => b.AddProvider(capture).SetMinimumLevel(LogLevel.Trace));
    services.AddFlowthru(configureFlowthru);
    return (services.BuildServiceProvider(), capture);
  }

  [Test]
  public async Task SuccessfulMergedRun_EmitsStartAndFinishedLogs()
  {
    var (sp, capture) = BuildHostWithCapture(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow<TestCatalog>("only", catalog =>
      {
        catalog.Input.Save(7).Run().GetAwaiter().GetResult();
        return FlowBuilder.CreateFlow("only", p =>
          p.AddStep<int, int>("noop", x => x, catalog.Input, catalog.Output)
        );
      });
    });
    await using var _ = sp;

    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync();
    Assert.That(result.IsSuccess, Is.True);

    var entries = capture.Entries.ToList();
    Assert.That(
      entries.Any(e => e.Message.Contains("→ Running merged DAG")),
      Is.True,
      "Unsliced run should log '→ Running merged DAG (N step(s))'. Got: "
        + string.Join(" | ", entries.Select(e => e.Message))
    );
    Assert.That(
      entries.Any(e => e.Message.Contains("Flow run finished") && e.Message.Contains("ms")),
      Is.True,
      "Successful run should log 'Flow run finished in {ms} ms'. Got: "
        + string.Join(" | ", entries.Select(e => e.Message))
    );
  }

  [Test]
  public async Task SlicedRun_LogsFlowLabelAndStepCount()
  {
    var (sp, capture) = BuildHostWithCapture(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow<TestCatalog>("sliced", catalog =>
      {
        catalog.Input.Save(7).Run().GetAwaiter().GetResult();
        return FlowBuilder.CreateFlow("sliced", p =>
          p.AddStep<int, int>("noop", x => x, catalog.Input, catalog.Output)
        );
      });
    });
    await using var _ = sp;

    var flowthru = sp.GetRequiredService<IFlowthruService>();
    await flowthru.RunAsync("sliced");

    var entries = capture.Entries.ToList();
    Assert.That(
      entries.Any(e =>
        e.Message.Contains("→ Running flow 'sliced'")
        && e.Message.Contains("after slicing")),
      Is.True,
      "Sliced run should log \"→ Running flow '{label}' (N step(s) after slicing)\". Got: "
        + string.Join(" | ", entries.Select(e => e.Message))
    );
  }

  [Test]
  public async Task PreFlightPhase_LogsStartAndPass()
  {
    var (sp, capture) = BuildHostWithCapture(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow<TestCatalog>("pf", catalog =>
      {
        catalog.Input.Save(1).Run().GetAwaiter().GetResult();
        return FlowBuilder.CreateFlow("pf", p =>
          p.AddStep<int, int>("noop", x => x, catalog.Input, catalog.Output)
        );
      });
    });
    await using var _ = sp;

    var flowthru = sp.GetRequiredService<IFlowthruService>();
    await flowthru.RunAsync();

    var entries = capture.Entries.ToList();
    Assert.That(
      entries.Any(e => e.Message.Contains("→ Pre-flight checks")),
      Is.True,
      "Pre-flight start should log '→ Pre-flight checks…'. Got: "
        + string.Join(" | ", entries.Select(e => e.Message))
    );
    Assert.That(
      entries.Any(e =>
        e.Message.Contains("✓ Pre-flight passed")
        && e.Message.Contains("ms")),
      Is.True,
      "Pre-flight success should log '✓ Pre-flight passed ({ms} ms)'. Got: "
        + string.Join(" | ", entries.Select(e => e.Message))
    );
  }

  [Test]
  public async Task RunWithFailedStep_LogsWarningRunFinished()
  {
    var (sp, capture) = BuildHostWithCapture(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow<TestCatalog>("boom", catalog =>
      {
        catalog.Input.Save(0).Run().GetAwaiter().GetResult();
        return FlowBuilder.CreateFlow("boom", p =>
          p.AddStep<int, int>("explode", x => 100 / x, catalog.Input, catalog.Output)
        );
      });
    });
    await using var _ = sp;

    var flowthru = sp.GetRequiredService<IFlowthruService>();
    await flowthru.RunAsync();

    var warnings = capture.Entries
      .Where(e => e.Level == LogLevel.Warning && e.Message.Contains("finished with failures"))
      .ToList();
    Assert.That(warnings, Is.Not.Empty,
      "Failed run should log a Warning 'Flow run finished with failures in {ms} ms: …'. Got: "
        + string.Join(" | ", capture.Entries.Select(e => $"[{e.Level}] {e.Message}"))
    );
  }

  [Test]
  public async Task NoAddLoggingHostBuild_RunsSilentlyWithNullLoggerFallback()
  {
    // Without AddLogging(), AddFlowthru's TryAdd<NullLoggerFactory>
    // fallback kicks in. The run should still succeed and produce
    // no captured entries (because there's no capturing provider
    // wired, period).
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow<TestCatalog>("silent", catalog =>
      {
        catalog.Input.Save(1).Run().GetAwaiter().GetResult();
        return FlowBuilder.CreateFlow("silent", p =>
          p.AddStep<int, int>("noop", x => x, catalog.Input, catalog.Output)
        );
      });
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync();

    Assert.That(result.IsSuccess, Is.True,
      "Host that didn't call AddLogging() must still resolve IFlowthruService "
      + "via the NullLoggerFactory fallback and run flows successfully.");
  }
}
