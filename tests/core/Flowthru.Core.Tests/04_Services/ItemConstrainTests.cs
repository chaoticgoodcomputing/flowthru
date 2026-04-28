using Flowthru.Core.Data;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Tests.Services;

/// <summary>
/// Tests for <see cref="Item{T}.Constrain"/> — the public API for tightening storage trait
/// capabilities on a catalog item. Constraints are one-way: capabilities can be revoked but
/// never granted.
/// </summary>
[TestFixture]
[Category("Services")]
public class ItemConstrainTests
{
  [Test]
  public void Constrain_TightensCanWrite_Succeeds()
  {
    var item = MakeItem(canRead: true, canWrite: true, canInspect: true);

    var result = item.Constrain(t => t with { CanWrite = false });

    Assert.That(result, Is.SameAs(item), "Constrain should return the same item for chaining.");
    Assert.That(result.Traits.CanWrite, Is.False);
    Assert.That(result.Traits.CanRead, Is.True, "Other traits should be unchanged.");
  }

  [Test]
  public void Constrain_TightensMultipleTraits_Succeeds()
  {
    var item = MakeItem(canRead: true, canWrite: true, canInspect: true);

    item.Constrain(t => t with { CanWrite = false, CanInspect = false });

    Assert.That(item.Traits.CanRead, Is.True);
    Assert.That(item.Traits.CanWrite, Is.False);
    Assert.That(item.Traits.CanInspect, Is.False);
  }

  [Test]
  public void Constrain_AttemptToGrantCanRead_ThrowsInvalidOperation()
  {
    var item = MakeItem(canRead: false, canWrite: true, canInspect: true);

    Assert.That(
      () => item.Constrain(t => t with { CanRead = true }),
      Throws.InvalidOperationException.With.Message.Contains("CanRead")
    );
  }

  [Test]
  public void Constrain_AttemptToGrantCanWrite_ThrowsInvalidOperation()
  {
    var item = MakeItem(canRead: true, canWrite: false, canInspect: true);

    Assert.That(
      () => item.Constrain(t => t with { CanWrite = true }),
      Throws.InvalidOperationException.With.Message.Contains("CanWrite")
    );
  }

  [Test]
  public void Constrain_AttemptToGrantCanInspect_ThrowsInvalidOperation()
  {
    var item = MakeItem(canRead: true, canWrite: true, canInspect: false);

    Assert.That(
      () => item.Constrain(t => t with { CanInspect = true }),
      Throws.InvalidOperationException.With.Message.Contains("CanInspect")
    );
  }

  [Test]
  public void Constrain_NullFunction_ThrowsArgumentNull()
  {
    var item = MakeItem(canRead: true, canWrite: true, canInspect: true);

    Assert.That(
      () => item.Constrain(null!),
      Throws.ArgumentNullException
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static Item<int> MakeItem(bool canRead, bool canWrite, bool canInspect)
  {
    var storage = new TestStorageAdapter<int>(
      new StorageTraits
      {
        CanRead = canRead,
        CanWrite = canWrite,
        CanInspect = canInspect,
      }
    );
    return new Item<int>("test-item", storage);
  }

  private sealed class TestStorageAdapter<T> : IStorageAdapter<T>
  {
    public TestStorageAdapter(StorageTraits traits) => Traits = traits;
    public StorageTraits Traits { get; }
    public FlowIO<T> Load() => FlowIO.Pure<T>(default!);
    public FlowIO<FlowUnit> Save(T data) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(false);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }
}
