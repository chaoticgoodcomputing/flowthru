using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Meta.Diagnostics;
using Flowthru.Meta.Diagnostics.Providers;
using Flowthru.Meta.Diagnostics.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Diagnostics.Tests;

[TestFixture]
[Category("Diagnostics")]
[Category("OutputExistence")]
public class OutputExistenceProviderTests
{
  private RecordingLogger _logger = null!;

  [SetUp]
  public void SetUp() => _logger = new RecordingLogger();

  private static (RunMetadata Run, IServiceProvider Services) BuildContext(params IItem[] items)
  {
    var dag = new DagMetadata
    {
      FlowName = "TestFlow",
      Steps = new()
      {
        new StepMetadata
        {
          Id = "stepA",
          Label = "StepA",
          StepType = "FakeStep",
          FlowName = "TestFlow",
          Outputs = items.Select(i => i.Label).ToList(),
        },
      },
    };
    var run = new RunMetadata
    {
      Dag = dag,
      Result = FlowResult.CreateSuccess(TimeSpan.Zero, new(), "TestFlow"),
    };

    var sc = new ServiceCollection();
    sc.AddSingleton<CatalogAbstract>(new FakeCatalog(items));
    return (run, sc.BuildServiceProvider());
  }

  [Test]
  public void Consume_MissingOutput_LogsWarning()
  {
    var present = new FakeItem { Label = "Present", ExistsResult = true };
    var missing = new FakeItem { Label = "Missing", ExistsResult = false };
    var (run, services) = BuildContext(present, missing);

    var provider = new OutputExistenceProvider(new OutputExistenceOptions { Enabled = true }, _logger);
    provider.Consume(run, services);

    var warnings = _logger.Entries.Where(e => e.Level == LogLevel.Warning).ToList();
    Assert.That(string.Join("\n", warnings.Select(w => w.Message)), Does.Contain("Missing"));
    Assert.That(string.Join("\n", warnings.Select(w => w.Message)), Does.Not.Contain("Present"));
  }

  [Test]
  public void Consume_AllPresent_NoWarnings()
  {
    var a = new FakeItem { Label = "A", ExistsResult = true };
    var b = new FakeItem { Label = "B", ExistsResult = true };
    var (run, services) = BuildContext(a, b);

    var provider = new OutputExistenceProvider(new OutputExistenceOptions { Enabled = true }, _logger);
    provider.Consume(run, services);

    Assert.That(_logger.Entries.Where(e => e.Level == LogLevel.Warning), Is.Empty);
  }

  [Test]
  public void Consume_FullAudit_LogsEveryOutput()
  {
    var present = new FakeItem { Label = "Present", ExistsResult = true };
    var missing = new FakeItem { Label = "Missing", ExistsResult = false };
    var (run, services) = BuildContext(present, missing);

    var provider = new OutputExistenceProvider(
      new OutputExistenceOptions { Enabled = true, ReportMissingOnly = false },
      _logger
    );
    provider.Consume(run, services);

    var allMessages = string.Join("\n", _logger.Messages);
    Assert.That(allMessages, Does.Contain("full audit"));
    Assert.That(allMessages, Does.Contain("Present"));
    Assert.That(allMessages, Does.Contain("Missing"));
  }

  [Test]
  public void Consume_ExistsThrows_LogsWarningAndContinues()
  {
    var ok = new FakeItem { Label = "OK", ExistsResult = true };
    var bad = new FakeItem
    {
      Label = "Bad",
      ExistsThrows = new IOException("storage flapped"),
    };
    var (run, services) = BuildContext(ok, bad);

    var provider = new OutputExistenceProvider(new OutputExistenceOptions { Enabled = true }, _logger);

    Assert.DoesNotThrow(() => provider.Consume(run, services));
    Assert.That(string.Join("\n", _logger.Messages), Does.Contain("Exists() failed for Bad"));
  }

  [Test]
  public void Consume_Disabled_EmitsNothing()
  {
    var item = new FakeItem { Label = "X", ExistsResult = false };
    var (run, services) = BuildContext(item);

    var provider = new OutputExistenceProvider(
      new OutputExistenceOptions { Enabled = false },
      _logger
    );
    provider.Consume(run, services);

    Assert.That(_logger.Entries, Is.Empty);
  }
}
