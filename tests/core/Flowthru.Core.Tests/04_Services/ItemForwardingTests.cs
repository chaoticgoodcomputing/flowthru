using Flowthru.Core.Data;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Tests.Services;

/// <summary>
/// Tests for <see cref="Item{T}"/>'s pass-through forwarding to its underlying storage
/// adapter. Each forwarder is a thin wrapper, but they're public API surface — exercising
/// them with a recording stub adapter confirms the delegation contract.
/// </summary>
[TestFixture]
[Category("Services")]
public class ItemForwardingTests
{
  [Test]
  public async Task InspectDeep_ForwardsToStorageAdapter()
  {
    var storage = new RecordingAdapter<int>();
    var item = new Item<int>("test-item", storage);

    var result = await item.InspectDeep().Run();

    Assert.That(storage.InspectDeepCalls, Is.EqualTo(1));
    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_ForwardsSampleSizeToStorageAdapter()
  {
    var storage = new RecordingAdapter<int>();
    var item = new Item<int>("test-item", storage);

    await item.InspectShallow(sampleSize: 25).Run();

    Assert.That(storage.LastShallowSampleSize, Is.EqualTo(25));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Recording stub
  // ─────────────────────────────────────────────────────────────────────────

  private sealed class RecordingAdapter<T> : IStorageAdapter<T>
  {
    public int InspectDeepCalls { get; private set; }
    public int? LastShallowSampleSize { get; private set; }

    public StorageTraits Traits => new StorageTraits();

    public FlowIO<T> Load() => FlowIO.Pure<T>(default!);

    public FlowIO<FlowUnit> Save(T data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(false);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize)
    {
      LastShallowSampleSize = sampleSize;
      return FlowIO.Pure(ValidationResult.Success());
    }

    public FlowIO<ValidationResult> InspectDeep()
    {
      InspectDeepCalls++;
      return FlowIO.Pure(ValidationResult.Success());
    }

    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }
}
