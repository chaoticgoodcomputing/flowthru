using Flowthru.Core.Tests.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Tests.Step;

/// <summary>
/// Fixture step that demonstrates the canonical
/// <c>Create(ILogger)</c> shape. Filters out zeros and
/// logs how many it dropped — the same data-quality pattern
/// PreprocessCompaniesStep will adopt under Issue 3.
/// </summary>
/// <remarks>
/// Declared at namespace scope (not nested inside the test fixture)
/// because the source generator's emitted
/// <c>{ClassName}_Registration</c> companion lives in the same
/// namespace and would not resolve a nested type by simple name.
/// </remarks>
[FlowthruStep]
public static class FixtureLoggingStep
{
  public static Func<IEnumerable<int>, IEnumerable<int>> Create(ILogger logger)
  {
    return input =>
    {
      var rows = input.ToList();
      var kept = rows.Where(x => x != 0).ToList();
      var dropped = rows.Count - kept.Count;
      if (dropped > 0)
      {
        logger.LogWarning("Dropped {Count} zero rows", dropped);
      }
      return kept;
    };
  }
}

/// <summary>
/// End-to-end coverage for the canonical step-logging convention:
/// a <c>[FlowthruStep]</c> class declares
/// <see cref="ILogger"/> as a parameter on its <c>Create()</c>
/// factory; the source generator extracts it as a
/// <see cref="ServiceDependency.ObservationOnly"/>; the host
/// resolves it through DI; the step's logged lines hit the captured
/// provider.
/// </summary>
[TestFixture]
public class StepLoggerInjectionTests
{
  public sealed class TestCatalog : CatalogAbstract
  {
    public IItem<IEnumerable<int>> Input =>
      CreateItem(() => ItemFactory.Singleton.Memory<IEnumerable<int>>("sli-input"));

    public IItem<IEnumerable<int>> Output =>
      CreateItem(() => ItemFactory.Singleton.Memory<IEnumerable<int>>("sli-output"));
  }

  [Test]
  public void SourceGenerator_ExtractsILoggerParameter_AsObservationOnlyServiceDependency()
  {
    // The [FlowthruStep] generator inspects Create() parameter types
    // and registers interface-typed params as ServiceDependencies on the
    // step's metadata. ILogger (non-generic) must round-trip through
    // this path so the engine's shared "Flowthru"-category logger is
    // resolvable at flow-construction time — AND it must be emitted
    // as the ObservationOnly variant so the cache planner
    // doesn't cascade uncacheability from steps that only declare a
    // logger.
    var entry = StepMetadataRegistry.TryGetEntry(typeof(FixtureLoggingStep));
    Assert.That(entry, Is.Not.Null,
      "ModuleInitializer should have registered the fixture step before any test runs.");
    Assert.That(
      entry!.Services.OfType<ServiceDependency.ObservationOnly>()
        .Any(r => r.ServiceType == typeof(ILogger)),
      Is.True,
      "Step metadata must record ILogger as an ObservationOnly ServiceDependency. Found: "
        + string.Join(", ", entry.Services.Select(s => $"{s.GetType().Name}({s.DisplayName})"))
    );
  }

  [Test]
  public async Task DI_Resolves_SharedILogger_AndStepLogsAreCaptured()
  {
    // End-to-end: a host that called AddLogging(...) and registered a
    // flow factory consuming the shared ILogger sees the step's
    // LogWarning in the captured provider after RunAsync completes.
    // This is the contract Flow Developers will rely on under the
    // new canonical Create(ILogger) convention.
    var capture = new CapturingLoggerProvider();
    var services = new ServiceCollection();
    services.AddLogging(b => b.AddProvider(capture).SetMinimumLevel(LogLevel.Trace));
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow<TestCatalog, ILogger>(
        "step-log",
        (catalog, stepLogger) =>
        {
          catalog.Input.Save(new[] { 1, 0, 2, 0, 3 }.AsEnumerable())
            .Run().GetAwaiter().GetResult();
          return FlowBuilder.CreateFlow("step-log", p =>
            p.AddStep<IEnumerable<int>, IEnumerable<int>>(
              "filter-zeros",
              FixtureLoggingStep.Create(stepLogger),
              catalog.Input,
              catalog.Output)
          );
        });
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync();
    Assert.That(result.IsSuccess, Is.True);

    var flowthruLogs = capture.EntriesForCategory("Flowthru").ToList();
    Assert.That(flowthruLogs, Is.Not.Empty,
      "Step's logger should write under the shared 'Flowthru' category. "
      + "Captured categories: "
      + string.Join(", ", capture.Entries.Select(e => e.Category).Distinct())
    );
    Assert.That(
      flowthruLogs.Any(e =>
        e.Level == LogLevel.Warning
        && e.Message.Contains("Dropped 2 zero rows")),
      Is.True,
      "Step should emit a Warning naming the dropped-row count. Got: "
        + string.Join(" | ", flowthruLogs.Select(e => $"[{e.Level}] {e.Message}"))
    );
  }

  [Test]
  public async Task SharedLoggerCategory_EngineAndStepEmitUnderSameCategory()
  {
    // Every Flowthru log collapses into one "Flowthru" category —
    // engine internals (FlowthruService,
    // ParallelFlowScheduler) and user-authored step logs share an
    // identity. This test pins that contract: both engine lifecycle
    // messages and the step's own LogWarning land under the same
    // captured category.
    var capture = new CapturingLoggerProvider();
    var services = new ServiceCollection();
    services.AddLogging(b => b.AddProvider(capture).SetMinimumLevel(LogLevel.Trace));
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog());
      b.RegisterFlow<TestCatalog, ILogger>(
        "shared",
        (catalog, stepLogger) =>
        {
          catalog.Input.Save(new[] { 0, 1 }.AsEnumerable())
            .Run().GetAwaiter().GetResult();
          return FlowBuilder.CreateFlow("shared", p =>
            p.AddStep<IEnumerable<int>, IEnumerable<int>>(
              "filter-zeros",
              FixtureLoggingStep.Create(stepLogger),
              catalog.Input,
              catalog.Output)
          );
        });
    });

    await using var sp = services.BuildServiceProvider();
    await sp.GetRequiredService<IFlowthruService>().RunAsync();

    var categories = capture.Entries.Select(e => e.Category).Distinct().ToList();
    Assert.That(categories, Is.EqualTo(new[] { "Flowthru" }),
      "Engine and step logs must share the single 'Flowthru' category. Categories seen: "
        + string.Join(", ", categories)
    );

    var sharedCategoryLogs = capture.EntriesForCategory("Flowthru").ToList();
    Assert.That(
      sharedCategoryLogs.Any(e => e.Message.Contains("→ Running")),
      Is.True,
      "Engine run-start log should appear under the shared 'Flowthru' category.");
    Assert.That(
      sharedCategoryLogs.Any(e => e.Message.Contains("Dropped")),
      Is.True,
      "Step LogWarning should appear under the same shared 'Flowthru' category.");
  }
}
