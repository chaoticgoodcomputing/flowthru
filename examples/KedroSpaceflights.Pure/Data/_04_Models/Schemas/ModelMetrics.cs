using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._04_Models.Schemas;

public record ModelMetrics : IStructuredSerializable
{
  public decimal R2Score { get; init; }
  public decimal MeanAbsoluteError { get; init; }
  public decimal MaxError { get; init; }
}
