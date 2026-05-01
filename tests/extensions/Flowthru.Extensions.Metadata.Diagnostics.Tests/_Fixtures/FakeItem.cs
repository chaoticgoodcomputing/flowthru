using Flowthru.Core.Data;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Graph;

namespace Flowthru.Meta.Diagnostics.Tests.Fixtures;

/// <summary>
/// Minimal IItem stub for diagnostics tests. Lets tests control HasEfficientCount,
/// the count returned by GetCountAsync, and the bool returned by Exists.
/// </summary>
internal sealed class FakeItem : IItem
{
  public required string Label { get; init; }
  public Type DataType { get; init; } = typeof(int);
  public NodeTraits Traits => new() { CanInspect = true };
  public InspectionLevel? PreferredInspectionLevel => null;
  public bool HasEfficientCount { get; init; }
  public int Count { get; init; }
  public bool ExistsResult { get; init; } = true;
  public Exception? CountThrows { get; init; }
  public Exception? ExistsThrows { get; init; }

  public int GetCountCalls { get; private set; }
  public int LoadUntypedCalls { get; private set; }

  public FlowIO<object> LoadUntyped()
  {
    LoadUntypedCalls++;
    return FlowIO.Pure<object>(0);
  }

  public FlowIO<FlowUnit> SaveUntyped(object data) => FlowIO.Pure(FlowUnit.Default);

  public FlowIO<bool> Exists() =>
    ExistsThrows is not null
      ? FlowIO.Fail<bool>(ExistsThrows)
      : FlowIO.Pure(ExistsResult);

  public FlowIO<int> GetCountAsync()
  {
    GetCountCalls++;
    return CountThrows is not null
      ? FlowIO.Fail<int>(CountThrows)
      : FlowIO.Pure(Count);
  }

  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
    FlowIO.Pure(ValidationResult.Success());
  public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
  public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
}
