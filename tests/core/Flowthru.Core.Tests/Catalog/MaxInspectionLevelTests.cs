using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using SysIO = System.IO;

namespace Flowthru.Core.Tests.Catalog;

[FlowthruSchema]
public partial record MaxInspectionTestRow
{
  public required int Id { get; init; }
  public required string Name { get; init; }
}

/// <summary>
/// Tests for <see cref="CatalogItemExtensions.WithMaxInspectionLevel{T}"/>
/// — per-item inspection-depth cap. The pipeline's effective level
/// for any item is <c>min(globalLevel, item.MaxInspectionLevel)</c>;
/// items without a cap use the global level.
/// </summary>
[TestFixture]
public class MaxInspectionLevelTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-cap-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_root))
    {
      try { SysIO.Directory.Delete(_root, recursive: true); }
      catch { /* best effort */ }
    }
  }

  // ── Property and pure-function semantics ────────────────────────────

  [Test]
  public void DefaultCapIsNull_GlobalLevelApplies()
  {
    var item = ItemFactory.Singleton.Memory<int>("uncapped");
    Assert.That(item.MaxInspectionLevel, Is.Null,
      "Items default to no cap — pipeline uses the global level.");
  }

  [Test]
  public void WithMaxInspectionLevel_SetsTheCap()
  {
    var item = ItemFactory.Singleton.Memory<int>("capped")
      .WithMaxInspectionLevel(InspectionLevel.Shallow);
    Assert.That(item.MaxInspectionLevel, Is.EqualTo(InspectionLevel.Shallow));
  }

  [Test]
  public void WithMaxInspectionLevel_DoesNotMutateOriginal()
  {
    var original = ItemFactory.Singleton.Memory<int>("original");
    _ = original.WithMaxInspectionLevel(InspectionLevel.Shallow);
    Assert.That(original.MaxInspectionLevel, Is.Null,
      "WithMaxInspectionLevel must not mutate the source item.");
  }

  [Test]
  public void WithMaxInspectionLevel_TightensInsteadOfOverwriting()
  {
    // Chained caps narrow rather than overwrite. Setting a less-strict
    // cap on an already-capped item keeps the tighter of the two.
    var shallow = ItemFactory.Singleton.Memory<int>("chained")
      .WithMaxInspectionLevel(InspectionLevel.Shallow);
    var attempted = shallow.WithMaxInspectionLevel(InspectionLevel.Deep);
    Assert.That(attempted.MaxInspectionLevel, Is.EqualTo(InspectionLevel.Shallow),
      "Chained caps narrow — Deep ∩ Shallow = Shallow.");
  }

  [Test]
  public void WithMaxInspectionLevel_NullItem_Throws()
  {
    Assert.That(
      () => CatalogItemExtensions.WithMaxInspectionLevel<int>(null!, InspectionLevel.Shallow),
      Throws.ArgumentNullException
    );
  }

  // ── Pipeline integration ────────────────────────────────────────────

  [Test]
  public async Task PreFlight_GlobalDeep_UncappedItem_RunsInspectDeep()
  {
    var probe = new InspectionProbeItem<int>("uncapped");

    await PreFlightPipeline
      .Run(BuildSingleStepFlow(probe), inspectionLevel: InspectionLevel.Deep)
      .Run();

    Assert.That(probe.LastInvoked, Is.EqualTo("InspectDeep"),
      "Without a cap, the global Deep level reaches the item's InspectDeep method.");
  }

  [Test]
  public async Task PreFlight_GlobalDeep_ItemCappedAtShallow_RunsInspectShallow()
  {
    // The cap forces the pipeline to call InspectShallow even though
    // the global level is Deep. This is the load-bearing cap behaviour:
    // expensive items can opt down without changing the global setting.
    var probe = new InspectionProbeItem<int>("capped");
    var capped = probe.WithMaxInspectionLevel(InspectionLevel.Shallow);

    await PreFlightPipeline
      .Run(BuildSingleStepFlow(capped), inspectionLevel: InspectionLevel.Deep)
      .Run();

    Assert.That(probe.LastInvoked, Is.EqualTo("InspectShallow"),
      "The cap narrows Deep → Shallow for this item; InspectDeep is NOT called.");
  }

  [Test]
  public async Task PreFlight_ItemCappedAtNone_SkipsInspection()
  {
    var probe = new InspectionProbeItem<int>("none-capped");
    var capped = probe.WithMaxInspectionLevel(InspectionLevel.None);

    await PreFlightPipeline
      .Run(BuildSingleStepFlow(capped), inspectionLevel: InspectionLevel.Deep)
      .Run();

    Assert.That(probe.LastInvoked, Is.Null,
      "A None cap should skip inspection entirely — the item's Inspect* methods are not called.");
  }

  // ── Probe item that records which inspection method was invoked. ────

  private sealed class InspectionProbeItem<T> : IItem<T>
  {
    public InspectionProbeItem(string label) { Label = label; }

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

  // ── Helpers ─────────────────────────────────────────────────────────

  /// <summary>Build a flow with a single side-effect step that consumes <paramref name="input"/>.</summary>
  private static BuiltFlow BuildSingleStepFlow<T>(IItem<T> input) =>
    FlowBuilder.CreateFlow("cap-test", b =>
      b.AddStep<T>("consume", _ => { }, input)
    );
}
