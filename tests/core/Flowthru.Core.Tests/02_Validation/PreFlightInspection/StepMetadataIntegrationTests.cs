using Flowthru.Core.Data;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Flows;
using Flowthru.Core.Services;
using Flowthru.Core.Steps;
using Flowthru.Core.Tests.Fixtures.TestCatalogs;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

// ─────────────────────────────────────────────────────────────────────────
// Top-level fixtures (the metadata generator only emits for top-level classes;
// nesting inside the test class would not produce a discoverable _Metadata sibling).
// ─────────────────────────────────────────────────────────────────────────

public interface IIntegrationFakeService { }

public sealed class IntegrationFakeService : IIntegrationFakeService { }

/// <summary>
/// Step factory that injects <see cref="IIntegrationFakeService"/>. The metadata
/// generator emits a <c>ServiceConsumingStep_Metadata</c> sibling — verified by
/// <see cref="StepMetadataIntegrationTests.FlowBuilder_AddStep_AttributedStepWithService_PopulatesServiceDependencies"/>.
/// </summary>
[FlowthruStep(IsIdempotent = true, HasSideEffects = true)]
public static class ServiceConsumingStep
{
  public static Func<IEnumerable<TestData>, IEnumerable<TestData>> Create(
    IIntegrationFakeService _service
  ) => input => input;
}

/// <summary>
/// End-to-end Phase 4 integration tests verifying that the source-generated metadata
/// flows from <c>[FlowthruStep]</c>-attributed classes through <c>FlowBuilder.AddStep</c>
/// into <c>FlowStep.ServiceDependencies</c>, and that the engine's preflight loop runs
/// the registered inspectors.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("PreFlight")]
[Category("StepMetadataIntegration")]
public class StepMetadataIntegrationTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // FlowBuilder.AddStep with attributed step → ServiceDependencies populated
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task FlowBuilder_AddStep_AttributedStepWithService_PopulatesServiceDependencies()
  {
    var probeRan = false;
    var services = new ServiceCollection();
    services.AddSingleton<IIntegrationFakeService, IntegrationFakeService>();
    services.AddFlowthruInspect<IIntegrationFakeService>(
      (IIntegrationFakeService svc, CancellationToken ct) =>
      {
        probeRan = true;
        return FlowIO.Pure(ValidationResult.Success());
      }
    );
    var sp = services.BuildServiceProvider();

    var catalog = new SimpleThreeStepCatalog();
    await catalog.Input.Save(new[]
    {
      new TestData
      {
        Id = 1,
        Name = "x",
        Value = 1.0,
      },
    }).Run();

    var fakeService = sp.GetRequiredService<IIntegrationFakeService>();
    var flow = FlowBuilder.CreateFlow(b =>
    {
      b.AddStep(
        label: "ServiceConsumingStep",
        transform: ServiceConsumingStep.Create(fakeService),
        input: catalog.Input,
        output: catalog.Output
      );
    });
    flow.ServiceProvider = sp;
    flow.ValidationOptions.Inspect(catalog.Input, InspectionLevel.None);
    flow.Build();

    // Assertion 1: metadata flowed through — the step has ISpaceflightsClient-style deps.
    var step = flow.Steps.Single();
    Assert.That(
      step.ServiceDependencies,
      Is.EquivalentTo(
        new Flowthru.Core.Effects.ServiceRef[]
        {
          Flowthru.Core.Effects.ServiceRef.Of<IIntegrationFakeService>(),
        }
      ),
      "ServiceDependencies should be populated from the source-generated metadata"
    );

    // Assertion 2: preflight runs the registered inspector.
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);
    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.True);
      Assert.That(probeRan, Is.True, "the registered inspector should have been invoked");
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // FlowBuilder.AddStep with inline lambda → empty ServiceDependencies
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task FlowBuilder_AddStep_InlineLambda_LeavesServiceDependenciesEmpty()
  {
    var probeRan = false;
    var services = new ServiceCollection();
    services.AddSingleton<IIntegrationFakeService, IntegrationFakeService>();
    services.AddFlowthruInspect<IIntegrationFakeService>(
      (IIntegrationFakeService svc, CancellationToken ct) =>
      {
        probeRan = true;
        return FlowIO.Pure(ValidationResult.Success());
      }
    );
    var sp = services.BuildServiceProvider();

    var catalog = new SimpleThreeStepCatalog();
    await catalog.Input.Save(new[]
    {
      new TestData
      {
        Id = 1,
        Name = "x",
        Value = 1.0,
      },
    }).Run();

    var flow = FlowBuilder.CreateFlow(b =>
    {
      b.AddStep<IEnumerable<TestData>, IEnumerable<TestData>>(
        label: "InlineLambdaStep",
        transform: x => x,
        input: catalog.Input,
        output: catalog.Output
      );
    });
    flow.ServiceProvider = sp;
    flow.ValidationOptions.Inspect(catalog.Input, InspectionLevel.None);
    flow.Build();

    var step = flow.Steps.Single();
    Assert.That(
      step.ServiceDependencies,
      Is.Empty,
      "inline lambdas have no metadata; ServiceDependencies should be empty"
    );

    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);
    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.True);
      Assert.That(probeRan, Is.False, "no service deps → inspector should not run");
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Flow.Merge preserves ServiceDependencies on prefixed steps
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Regression test: when multiple flows are merged into a unified DAG, each prefixed
  /// step must carry its source step's <see cref="FlowStep.ServiceDependencies"/>
  /// forward. Without this, DagBuilder reads empty deps and the Mermaid renderer
  /// emits a graph missing the service-dependency edges that should appear in the
  /// metadata for service-consuming steps.
  /// </summary>
  [Test]
  public void Flow_Merge_PreservesServiceDependenciesOnPrefixedSteps()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IIntegrationFakeService, IntegrationFakeService>();
    var sp = services.BuildServiceProvider();

    var catalog = new SimpleThreeStepCatalog();
    var fakeService = sp.GetRequiredService<IIntegrationFakeService>();

    var flow = FlowBuilder.CreateFlow(b =>
    {
      b.AddStep(
        label: "ServiceConsumingStep",
        transform: ServiceConsumingStep.Create(fakeService),
        input: catalog.Input,
        output: catalog.Output
      );
    });

    var merged = Flow.Merge(new Dictionary<string, Flow> { ["A"] = flow });

    var mergedStep = merged.Steps.Single();
    Assert.Multiple(() =>
    {
      Assert.That(mergedStep.Label, Is.EqualTo("A.ServiceConsumingStep"));
      Assert.That(
        mergedStep.ServiceDependencies,
        Is.EquivalentTo(
          new Flowthru.Core.Effects.ServiceRef[]
          {
            Flowthru.Core.Effects.ServiceRef.Of<IIntegrationFakeService>(),
          }
        ),
        "Flow.Merge must forward ServiceDependencies; DagBuilder/Mermaid metadata depends on this"
      );
    });
  }
}
