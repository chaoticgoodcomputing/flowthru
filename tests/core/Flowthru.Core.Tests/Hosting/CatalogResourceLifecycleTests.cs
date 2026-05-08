using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Hosting;

/// <summary>
/// Integration tests verifying that <see cref="FlowthruService"/>
/// acquires every registered catalog's <see cref="IFlowResource"/>
/// before pre-flight, runs the flow, and releases LIFO afterwards —
/// the bracket invariant from §2.6 / Phase 1.
/// </summary>
[TestFixture]
public class CatalogResourceLifecycleTests
{
  [Test]
  public async Task RegisteredCatalogsWithResources_AcquireBeforeRun_ReleaseLifoAfter()
  {
    var sequence = new List<string>();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new CatalogA(sequence));
      b.RegisterCatalog(_ => new CatalogB(sequence));
      b.RegisterFlow("noop", () =>
        FlowBuilder.CreateFlow("noop", p =>
          p.AddStep("step", () => sequence.Add("step"))
        )
      );
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync();
    Assert.That(result.IsSuccess, Is.True);

    // Acquire order = registration order; release order = reverse (LIFO).
    Assert.That(sequence, Is.EqualTo(new[]
    {
      "acquire(catalogA)",
      "acquire(catalogB)",
      "step",
      "release(catalogB)",
      "release(catalogA)",
    }), "Catalog resources should acquire in registration order, release LIFO.");
  }

  [Test]
  public async Task ReleaseAlwaysRuns_EvenWhenStepFails()
  {
    var sequence = new List<string>();
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new CatalogA(sequence));
      b.RegisterFlow("boom", () =>
        FlowBuilder.CreateFlow("boom", p =>
          p.AddStep("explode", () =>
          {
            sequence.Add("step");
            throw new InvalidOperationException("kaboom");
          })
        )
      );
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync();
    Assert.That(result.HasFailures, Is.True);
    Assert.That(sequence.Last(), Is.EqualTo("release(catalogA)"),
      "Resource must release even when the flow fails — bracket invariant per §2.6."
    );
  }

  [Test]
  public async Task BodyFailure_ReleaseClosureSeesActualBodyError()
  {
    // FlowResource.Use spec: "release closure receives the body's
    // primary RuntimeError, or null on success." Verify the runtime's
    // acquire/release driver honours that contract — release closures
    // implementing PreserveOnFailure need this.
    var captured = new List<RuntimeError?>();
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new ErrorObservingCatalog(captured));
      b.RegisterFlow("boom", () =>
        FlowBuilder.CreateFlow("boom", p =>
          p.AddStep("explode", () => throw new InvalidOperationException("body kaboom"))
        )
      );
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync();
    Assert.That(result.HasFailures, Is.True);

    Assert.That(captured, Has.Count.EqualTo(1));
    Assert.That(captured[0], Is.Not.Null,
      "Release closure must receive the body's RuntimeError, not null, when the flow failed."
    );
    Assert.That(captured[0], Is.InstanceOf<RuntimeError.StepFailed>());
  }

  [Test]
  public async Task BodySuccess_ReleaseFailure_SurfacesAsFlowResultStepFailure()
  {
    // FlowResource.Use spec: "Body succeeds, release fails → returned
    // effect fails with the release error." On success path, a
    // failing release surfaces in the FlowResult as a synthetic
    // StepResult.Failed("resource.release[i]", ...).
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new FailingReleaseCatalog());
      b.RegisterFlow("ok", () =>
        FlowBuilder.CreateFlow("ok", p => p.AddStep("noop", () => { }))
      );
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync();
    Assert.That(result.HasFailures, Is.True,
      "Release failure on body-success path should be visible in the FlowResult."
    );
    var failure = result.StepResults.OfType<StepResult.Failed>().FirstOrDefault();
    Assert.That(failure, Is.Not.Null);
    Assert.That(failure!.StepLabel, Does.StartWith("resource.release"),
      "Release failure should be labelled distinctly so consumers can see it came from cleanup."
    );
    Assert.That(failure.Error.Message, Does.Contain("release-kaboom"));
  }

  [Test]
  public async Task BodyFailure_ReleaseFailure_SuppressesReleaseError()
  {
    // FlowResource.Use spec: "Body fails (regardless of release) →
    // returned effect fails with the body error; release errors are
    // suppressed." Verify the runtime suppresses release errors on
    // the body-failure path.
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new FailingReleaseCatalog());
      b.RegisterFlow("boom", () =>
        FlowBuilder.CreateFlow("boom", p =>
          p.AddStep("explode", () => throw new InvalidOperationException("body kaboom"))
        )
      );
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync();
    Assert.That(result.HasFailures, Is.True);

    var failures = result.StepResults.OfType<StepResult.Failed>().ToList();
    Assert.That(
      failures.Any(f => f.StepLabel.StartsWith("resource.release")),
      Is.False,
      "Release errors must NOT surface on the body-failure path — body diagnostic wins."
    );
    Assert.That(
      failures.Any(f => f.StepLabel == "explode"),
      Is.True,
      "Body error must be the user-visible failure on the body-failure path."
    );
  }

  [Test]
  public async Task CatalogWithoutResource_NoAcquireOrReleaseEvents()
  {
    // CatalogAbstract.Resource defaults to null — most catalogs hold
    // no managed resources (in-memory items, JSON files, etc.).
    // Verify the runtime doesn't try to acquire anything in that case.
    var sequence = new List<string>();
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new ResourcelessCatalog());
      b.RegisterFlow("plain", () =>
        FlowBuilder.CreateFlow("plain", p => p.AddStep("step", () => sequence.Add("step")))
      );
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync();

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(sequence, Is.EqualTo(new[] { "step" }),
      "Catalogs returning Resource = null contribute no acquire/release events.");
  }

  /// <summary>
  /// A catalog whose <see cref="Resource"/> records acquire/release
  /// events into the supplied trace list. Subclassed so each
  /// catalog gets its own DI-singleton slot (DI keys by type).
  /// </summary>
  private abstract class TracedCatalog : CatalogAbstract
  {
    private readonly string _name;
    private readonly List<string> _trace;

    protected TracedCatalog(string name, List<string> trace)
    {
      _name = name;
      _trace = trace;
    }

    public override IFlowResource? Resource => FlowResource.Make<string>(
      acquire: FlowIO.Lift(() => { _trace.Add($"acquire({_name})"); return _name; }),
      release: (scope, _) => FlowIO.Lift(() =>
      {
        _trace.Add($"release({scope})");
        return FlowUnit.Default;
      })
    );
  }

  private sealed class ResourcelessCatalog : CatalogAbstract { }

  private sealed class CatalogA : TracedCatalog
  {
    public CatalogA(List<string> trace) : base("catalogA", trace) { }
  }

  private sealed class CatalogB : TracedCatalog
  {
    public CatalogB(List<string> trace) : base("catalogB", trace) { }
  }

  /// <summary>
  /// A catalog whose release closure captures the
  /// <c>bodyError</c> argument it receives — used to verify the
  /// runtime forwards the actual body error to release.
  /// </summary>
  private sealed class ErrorObservingCatalog : CatalogAbstract
  {
    private readonly List<RuntimeError?> _capturedBodyErrors;

    public ErrorObservingCatalog(List<RuntimeError?> capturedBodyErrors)
    {
      _capturedBodyErrors = capturedBodyErrors;
    }

    public override IFlowResource? Resource => FlowResource.Make<int>(
      acquire: FlowIO.Pure(0),
      release: (_, error) => FlowIO.Lift(() =>
      {
        _capturedBodyErrors.Add(error);
        return FlowUnit.Default;
      })
    );
  }

  /// <summary>
  /// A catalog whose release effect always fails with a typed
  /// <see cref="RuntimeError.External"/> wrapping a known sentinel —
  /// used to verify release-error surfacing on body success and
  /// suppression on body failure.
  /// </summary>
  private sealed class FailingReleaseCatalog : CatalogAbstract
  {
    public override IFlowResource? Resource => FlowResource.Make<int>(
      acquire: FlowIO.Pure(0),
      release: (_, _) => FlowIO<FlowUnit>.Fail(
        new RuntimeError.External("release", new Exception("release-kaboom"))
      )
    );
  }
}
