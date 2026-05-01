using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Meta.Diagnostics;
using Flowthru.Meta.Diagnostics.Providers;
using Flowthru.Meta.Diagnostics.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Meta.Diagnostics.Tests;

[TestFixture]
[Category("Diagnostics")]
[Category("RowCounts")]
public class RowCountProviderTests
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
  public void Consume_OnlyEfficient_SkipsItemsWithoutEfficientCount()
  {
    var efficient = new FakeItem { Label = "Cheap", HasEfficientCount = true, Count = 42 };
    var expensive = new FakeItem { Label = "Expensive", HasEfficientCount = false, Count = 999 };
    var (run, services) = BuildContext(efficient, expensive);

    var provider = new RowCountProvider(new RowCountOptions { Enabled = true }, _logger);
    provider.Consume(run, services);

    Assert.That(efficient.GetCountCalls, Is.EqualTo(1), "Efficient item should be counted");
    Assert.That(expensive.GetCountCalls, Is.EqualTo(0), "Expensive item must not trigger materialization");
    Assert.That(string.Join("\n", _logger.Messages), Does.Contain("42"));
    Assert.That(string.Join("\n", _logger.Messages), Does.Contain("?"));
  }

  [Test]
  public void Consume_ForceCountAll_CountsEveryItem()
  {
    var efficient = new FakeItem { Label = "Cheap", HasEfficientCount = true, Count = 1 };
    var expensive = new FakeItem { Label = "Expensive", HasEfficientCount = false, Count = 7 };
    var (run, services) = BuildContext(efficient, expensive);

    var provider = new RowCountProvider(
      new RowCountOptions { Enabled = true, ForceCountAll = true },
      _logger
    );
    provider.Consume(run, services);

    Assert.That(efficient.GetCountCalls, Is.EqualTo(1));
    Assert.That(expensive.GetCountCalls, Is.EqualTo(1));
  }

  [Test]
  public void Consume_CountThrows_LogsWarningAndContinues()
  {
    var ok = new FakeItem { Label = "OK", HasEfficientCount = true, Count = 5 };
    var bad = new FakeItem
    {
      Label = "Bad",
      HasEfficientCount = true,
      CountThrows = new InvalidOperationException("boom"),
    };
    var (run, services) = BuildContext(ok, bad);

    var provider = new RowCountProvider(new RowCountOptions { Enabled = true }, _logger);
    provider.Consume(run, services);

    Assert.That(string.Join("\n", _logger.Messages), Does.Contain("count failed for Bad"));
    Assert.That(string.Join("\n", _logger.Messages), Does.Contain("5"));
  }

  [Test]
  public void Consume_Disabled_EmitsNothing()
  {
    var item = new FakeItem { Label = "X", HasEfficientCount = true, Count = 1 };
    var (run, services) = BuildContext(item);

    var provider = new RowCountProvider(new RowCountOptions { Enabled = false }, _logger);
    provider.Consume(run, services);

    Assert.That(item.GetCountCalls, Is.EqualTo(0));
    Assert.That(_logger.Entries.Where(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Information), Is.Empty);
  }

  [Test]
  public void Consume_NoCatalogServices_LogsDebugAndExits()
  {
    var run = new RunMetadata
    {
      Dag = new DagMetadata { FlowName = "X" },
      Result = FlowResult.CreateSuccess(TimeSpan.Zero, new(), "X"),
    };
    var services = new ServiceCollection().BuildServiceProvider();

    var provider = new RowCountProvider(new RowCountOptions { Enabled = true }, _logger);

    Assert.DoesNotThrow(() => provider.Consume(run, services));
  }

  [Test]
  public void Consume_WithoutServiceProvider_FallsBackQuietly()
  {
    // The bare Consume(RunMetadata) overload should not throw when called without DI;
    // it logs at Debug level and exits.
    var run = new RunMetadata
    {
      Dag = new DagMetadata { FlowName = "X" },
      Result = FlowResult.CreateSuccess(TimeSpan.Zero, new(), "X"),
    };
    var provider = new RowCountProvider(new RowCountOptions { Enabled = true }, _logger);

    Assert.DoesNotThrow(() => provider.Consume(run));
  }
}
