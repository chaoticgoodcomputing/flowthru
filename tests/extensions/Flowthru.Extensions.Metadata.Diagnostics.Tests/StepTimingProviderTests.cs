using Flowthru.Core.Flows;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Meta.Diagnostics;
using Flowthru.Meta.Diagnostics.Providers;
using Flowthru.Meta.Diagnostics.Tests.Fixtures;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Diagnostics.Tests;

[TestFixture]
[Category("Diagnostics")]
[Category("StepTimings")]
public class StepTimingProviderTests
{
  private RecordingLogger _logger = null!;

  [SetUp]
  public void SetUp() => _logger = new RecordingLogger();

  private static RunMetadata BuildRun(params (string Name, double Seconds)[] steps)
  {
    var stepResults = steps.ToDictionary(
      s => s.Name,
      s => StepResult.CreateSuccess(s.Name, TimeSpan.FromSeconds(s.Seconds))
    );

    return new RunMetadata
    {
      Dag = new DagMetadata { FlowName = "TestFlow" },
      Result = FlowResult.CreateSuccess(TimeSpan.FromSeconds(10), stepResults, "TestFlow"),
    };
  }

  [Test]
  public void Consume_TopN_LogsSlowestStepsInOrder()
  {
    var run = BuildRun(("Fast", 0.1), ("Slow", 5.0), ("Medium", 1.5));
    var provider = new StepTimingProvider(new StepTimingOptions { TopSlowest = 2 }, _logger);

    provider.Consume(run);

    var indexOfSlow = _logger.Messages.ToList().FindIndex(m => m.Contains("Slow"));
    var indexOfMedium = _logger.Messages.ToList().FindIndex(m => m.Contains("Medium"));
    Assert.That(indexOfSlow, Is.GreaterThanOrEqualTo(0));
    Assert.That(indexOfMedium, Is.GreaterThanOrEqualTo(0));
    Assert.That(indexOfSlow, Is.LessThan(indexOfMedium), "Slowest step should be reported first");
    Assert.That(_logger.Messages, Has.None.Contains("Fast"), "Top-2 should exclude the fastest step");
  }

  [Test]
  public void Consume_SlowThreshold_FlagsExcessSteps()
  {
    var run = BuildRun(("Fast", 0.1), ("Slow", 5.0));
    var provider = new StepTimingProvider(
      new StepTimingOptions { TopSlowest = 0, SlowThreshold = TimeSpan.FromSeconds(1) },
      _logger
    );

    provider.Consume(run);

    var warnings = _logger.Entries.Where(e => e.Level == LogLevel.Warning).ToList();
    Assert.That(warnings, Has.Count.EqualTo(1));
    Assert.That(warnings[0].Message, Does.Contain("Slow"));
    Assert.That(warnings[0].Message, Does.Contain("exceeded threshold"));
  }

  [Test]
  public void Consume_Disabled_EmitsNothing()
  {
    var run = BuildRun(("A", 0.1));
    var provider = new StepTimingProvider(new StepTimingOptions { Enabled = false }, _logger);

    provider.Consume(run);

    Assert.That(_logger.Entries, Is.Empty);
  }

  [Test]
  public void Consume_NoLogger_DoesNotThrow()
  {
    var run = BuildRun(("A", 0.1));
    var provider = new StepTimingProvider(logger: null);

    Assert.DoesNotThrow(() => provider.Consume(run));
  }

  [Test]
  public void Consume_Dag_NoOp()
  {
    var provider = new StepTimingProvider(logger: _logger);

    provider.Consume(new DagMetadata { FlowName = "X" });

    Assert.That(_logger.Entries, Is.Empty);
  }
}
