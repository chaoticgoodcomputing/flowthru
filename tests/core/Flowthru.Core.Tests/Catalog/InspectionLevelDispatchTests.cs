using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Core.Tests.Catalog;

/// <summary>
/// Tests for <see cref="PreFlightPipeline"/>'s level-based dispatch onto a
/// catalog item's <c>Inspect*</c> methods. The pipeline picks
/// <see cref="IItem.InspectShallow(int)"/>, <see cref="IItem.InspectDeep"/>,
/// or <see cref="IItem.InspectTarget"/> from the caller-requested
/// <see cref="InspectionLevel"/>, capped by the item's optional
/// <see cref="IItem.MaxInspectionLevel"/>. The <c>None</c> level (whether
/// global or via cap) skips inspection entirely.
/// </summary>
/// <remarks>
/// <para>
/// Ports the intent of the old <c>IItemValidateDimDispatchTests</c>. The old
/// dispatch lived on a default interface member on <c>INode.Validate()</c>
/// that read the implementer's <c>PreferredInspectionLevel</c>; the FP
/// rewrite moved the level-selection decision into the pipeline itself.
/// <see cref="MaxInspectionLevelTests"/> covers the cap interaction with a
/// global <c>Deep</c> level — this fixture pins the remaining global-level
/// branches (Shallow, Target, None) and the Math.Min relationship between
/// global and cap.
/// </para>
/// </remarks>
[TestFixture]
public class InspectionLevelDispatchTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Global-level dispatch
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task PreFlight_GlobalShallow_DispatchesToInspectShallow()
  {
    var probe = new InspectionProbeItem<int>("shallow-target");

    await PreFlightPipeline
      .Run(BuildSingleStepFlow(probe), inspectionLevel: InspectionLevel.Shallow)
      .Run();

    Assert.That(probe.LastInvoked, Is.EqualTo("InspectShallow"),
      "A global Shallow level must route to InspectShallow on uncapped items.");
  }

  [Test]
  public async Task PreFlight_GlobalDeep_DispatchesToInspectDeep()
  {
    var probe = new InspectionProbeItem<int>("deep-target");

    await PreFlightPipeline
      .Run(BuildSingleStepFlow(probe), inspectionLevel: InspectionLevel.Deep)
      .Run();

    Assert.That(probe.LastInvoked, Is.EqualTo("InspectDeep"),
      "A global Deep level must route to InspectDeep on uncapped items.");
  }

  [Test]
  public async Task PreFlight_GlobalTarget_DispatchesToInspectTarget()
  {
    var probe = new InspectionProbeItem<int>("target-target");

    await PreFlightPipeline
      .Run(BuildSingleStepFlow(probe), inspectionLevel: InspectionLevel.Target)
      .Run();

    Assert.That(probe.LastInvoked, Is.EqualTo("InspectTarget"),
      "A global Target level must route to InspectTarget on uncapped items.");
  }

  [Test]
  public async Task PreFlight_GlobalNone_SkipsInspection()
  {
    var probe = new InspectionProbeItem<int>("none-global");

    await PreFlightPipeline
      .Run(BuildSingleStepFlow(probe), inspectionLevel: InspectionLevel.None)
      .Run();

    Assert.That(probe.LastInvoked, Is.Null,
      "A global None level must skip inspection on every input — no Inspect* call.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Global × Cap interaction — Math.Min(global, cap)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task PreFlight_GlobalShallow_CapDeep_RunsShallow()
  {
    // The cap is the ceiling, not the floor — when the global level is
    // already below the cap, the global wins.
    var probe = new InspectionProbeItem<int>("shallow-under-deep-cap");
    var capped = probe.WithMaxInspectionLevel(InspectionLevel.Deep);

    await PreFlightPipeline
      .Run(BuildSingleStepFlow(capped), inspectionLevel: InspectionLevel.Shallow)
      .Run();

    Assert.That(probe.LastInvoked, Is.EqualTo("InspectShallow"),
      "min(Shallow=1, Deep=2) = Shallow; the cap should not promote.");
  }

  [Test]
  public async Task PreFlight_GlobalDeep_CapTarget_RunsDeep()
  {
    // Likewise — Deep < Target by enum value, so the global wins.
    var probe = new InspectionProbeItem<int>("deep-under-target-cap");
    var capped = probe.WithMaxInspectionLevel(InspectionLevel.Target);

    await PreFlightPipeline
      .Run(BuildSingleStepFlow(capped), inspectionLevel: InspectionLevel.Deep)
      .Run();

    Assert.That(probe.LastInvoked, Is.EqualTo("InspectDeep"),
      "min(Deep=2, Target=3) = Deep; the cap should not promote.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Probe item — records which Inspect method was invoked.
  // ─────────────────────────────────────────────────────────────────────────

  private sealed class InspectionProbeItem<T> : IItem<T>
  {
    public InspectionProbeItem(string label) => Label = label;

    public string Label { get; }
    public NodeTraits Traits => new() { CanInspect = true };
    public Type DataType => typeof(T);
    public string? LastInvoked { get; private set; }

    public FlowIO<T> Load() => FlowIO.Fail<T>(
      new Flowthru.Validation.Runtime.RuntimeError.External("probe-load", new NotImplementedException())
    );
    public FlowIO<FlowUnit> Save(T data) => FlowIO.Fail<FlowUnit>(
      new Flowthru.Validation.Runtime.RuntimeError.External("probe-save", new NotImplementedException())
    );
    public FlowIO<bool> Exists() => FlowIO.Pure(true);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100)
    {
      LastInvoked = "InspectShallow";
      return FlowIO.Pure(ValidationResult.Success());
    }
    public FlowIO<ValidationResult> InspectDeep()
    {
      LastInvoked = "InspectDeep";
      return FlowIO.Pure(ValidationResult.Success());
    }
    public FlowIO<ValidationResult> InspectTarget()
    {
      LastInvoked = "InspectTarget";
      return FlowIO.Pure(ValidationResult.Success());
    }

    public FlowIO<object> LoadUntyped() => Load().Map(value => (object)value!);
    public FlowIO<FlowUnit> SaveUntyped(object data) => Save((T)data);
    public FlowIO<ValidationResult> Validate() => InspectShallow();
  }

  private static BuiltFlow BuildSingleStepFlow<T>(IItem<T> input) =>
    FlowBuilder.CreateFlow("dispatch-test", b =>
      b.AddStep<T>("consume", _ => { }, input)
    );
}
