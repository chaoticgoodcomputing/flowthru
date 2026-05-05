using Flowthru.Core.Data;
using Flowthru.Core.Effects;
using Flowthru.Tests.Kits.Lifecycle;

namespace Flowthru.Core.Tests.Execution;

/// <summary>
/// Self-test for the kit's <see cref="CatalogLifecycleConformance"/> harness.
/// Uses an in-memory traceable catalog so the kit's framework-integration
/// scenarios can be validated without any external dependencies. A failure
/// here points at the kit or at <c>FlowthruService</c>'s lifecycle wiring,
/// not at any extension.
/// </summary>
[TestFixture]
[Category("Execution")]
public class CatalogLifecycleConformanceTests : CatalogLifecycleConformance
{
  protected override CatalogAbstract BuildCatalog(LifecycleTracker tracker) =>
    new TraceableCatalog(tracker);

  private sealed class TraceableCatalog : CatalogAbstract
  {
    private readonly LifecycleTracker _tracker;

    public TraceableCatalog(LifecycleTracker tracker)
    {
      _tracker = tracker;
      InitializeCatalogProperties();
    }

    public override IFlowResource? Resource =>
      TraceableResources.Make(_tracker, label: "TraceableCatalog");
  }
}
