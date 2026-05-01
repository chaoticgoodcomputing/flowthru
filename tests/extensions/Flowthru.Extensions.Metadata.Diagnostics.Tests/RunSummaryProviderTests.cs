using Flowthru.Core.Flows;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Meta.Diagnostics;
using Flowthru.Meta.Diagnostics.Providers;
using Flowthru.Meta.Diagnostics.Tests.Fixtures;

namespace Flowthru.Meta.Diagnostics.Tests;

[TestFixture]
[Category("Diagnostics")]
[Category("RunSummary")]
public class RunSummaryProviderTests
{
  private RecordingLogger _logger = null!;

  [SetUp]
  public void SetUp() => _logger = new RecordingLogger();

  [Test]
  public void Consume_Success_ReportsStatusDurationAndSlowest()
  {
    var stepResults = new Dictionary<string, StepResult>
    {
      ["Fast"] = StepResult.CreateSuccess("Fast", TimeSpan.FromMilliseconds(50)),
      ["Slow"] = StepResult.CreateSuccess("Slow", TimeSpan.FromSeconds(2)),
    };
    var run = new RunMetadata
    {
      Dag = new DagMetadata { FlowName = "TestFlow" },
      Result = FlowResult.CreateSuccess(TimeSpan.FromSeconds(3), stepResults, "TestFlow"),
    };
    var provider = new RunSummaryProvider(logger: _logger);

    provider.Consume(run);

    var allMessages = string.Join("\n", _logger.Messages);
    Assert.That(allMessages, Does.Contain("TestFlow"));
    Assert.That(allMessages, Does.Contain("success"));
    Assert.That(allMessages, Does.Contain("2 succeeded"));
    Assert.That(allMessages, Does.Contain("0 failed"));
    Assert.That(allMessages, Does.Contain("Slow"), "Slowest step should be named");
  }

  [Test]
  public void Consume_Failure_ReportsFailureStatus()
  {
    var stepResults = new Dictionary<string, StepResult>
    {
      ["StepA"] = StepResult.CreateSuccess("StepA", TimeSpan.FromMilliseconds(50)),
      ["StepB"] = StepResult.CreateFailure(
        "StepB",
        TimeSpan.FromMilliseconds(10),
        new InvalidOperationException("nope")
      ),
    };
    var run = new RunMetadata
    {
      Dag = new DagMetadata { FlowName = "TestFlow" },
      Result = FlowResult.CreateFailure(
        TimeSpan.FromSeconds(1),
        new InvalidOperationException("flow failed"),
        stepResults,
        "TestFlow"
      ),
    };
    var provider = new RunSummaryProvider(logger: _logger);

    provider.Consume(run);

    var allMessages = string.Join("\n", _logger.Messages);
    Assert.That(allMessages, Does.Contain("failure"));
    Assert.That(allMessages, Does.Contain("1 succeeded"));
    Assert.That(allMessages, Does.Contain("1 failed"));
  }

  [Test]
  public void Consume_Disabled_EmitsNothing()
  {
    var run = new RunMetadata
    {
      Dag = new DagMetadata { FlowName = "X" },
      Result = FlowResult.CreateSuccess(TimeSpan.Zero, new(), "X"),
    };
    var provider = new RunSummaryProvider(new RunSummaryOptions { Enabled = false }, _logger);

    provider.Consume(run);

    Assert.That(_logger.Entries, Is.Empty);
  }
}
