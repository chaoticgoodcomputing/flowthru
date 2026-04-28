using Flowthru.Core.Data;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Graph;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

/// <summary>
/// Tests for the <c>IItem.INode.Validate</c> default interface dispatch logic. The DIM
/// routes to <c>InspectShallow</c>, <c>InspectDeep</c>, or returns trivial success based
/// on the implementor's <c>PreferredInspectionLevel</c>. This is the live pre-flight
/// validation path for catalog items that don't override <c>Validate()</c> directly.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("PreFlightInspection")]
public class IItemValidateDimDispatchTests
{
  [Test]
  public async Task Validate_PreferredLevelNone_ReturnsSuccessWithoutInspection()
  {
    var item = new RecordingItem(InspectionLevel.None);

    var result = await ((INode)item).Validate().Run();

    Assert.That(result.IsValid, Is.True);
    Assert.That(item.ShallowCalls, Is.EqualTo(0));
    Assert.That(item.DeepCalls, Is.EqualTo(0));
  }

  [Test]
  public async Task Validate_PreferredLevelShallow_DispatchesToInspectShallow()
  {
    var item = new RecordingItem(InspectionLevel.Shallow);

    await ((INode)item).Validate().Run();

    Assert.That(item.ShallowCalls, Is.EqualTo(1));
    Assert.That(item.DeepCalls, Is.EqualTo(0));
  }

  [Test]
  public async Task Validate_PreferredLevelDeep_DispatchesToInspectDeep()
  {
    var item = new RecordingItem(InspectionLevel.Deep);

    await ((INode)item).Validate().Run();

    Assert.That(item.ShallowCalls, Is.EqualTo(0));
    Assert.That(item.DeepCalls, Is.EqualTo(1));
  }

  [Test]
  public async Task Validate_PreferredLevelNull_DefaultsToShallow()
  {
    var item = new RecordingItem(preferredLevel: null);

    await ((INode)item).Validate().Run();

    Assert.That(item.ShallowCalls, Is.EqualTo(1));
    Assert.That(item.DeepCalls, Is.EqualTo(0));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Stub IItem implementor that records which Inspect method gets called.
  // ─────────────────────────────────────────────────────────────────────────

  private sealed class RecordingItem : IItem
  {
    private readonly InspectionLevel? _preferredLevel;
    public int ShallowCalls { get; private set; }
    public int DeepCalls { get; private set; }

    public RecordingItem(InspectionLevel? preferredLevel) => _preferredLevel = preferredLevel;

    public string Label => "test-item";
    public Type DataType => typeof(int);
    public NodeTraits Traits => new() { CanInspect = true };
    public InspectionLevel? PreferredInspectionLevel => _preferredLevel;

    public FlowIO<object> LoadUntyped() => FlowIO.Pure<object>(0);
    public FlowIO<FlowUnit> SaveUntyped(object data) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
    public FlowIO<int> GetCountAsync() => FlowIO.Pure(0);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100)
    {
      ShallowCalls++;
      return FlowIO.Pure(ValidationResult.Success());
    }

    public FlowIO<ValidationResult> InspectDeep()
    {
      DeepCalls++;
      return FlowIO.Pure(ValidationResult.Success());
    }

    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }
}
