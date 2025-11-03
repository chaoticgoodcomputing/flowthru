using Flowthru.Abstractions;

namespace RetailData.Data._05_Reporting.Schemas;

/// <summary>
/// Unified correlation heatmap schema that works for both country and region groupings
/// </summary>
public record CorrelationHeatmapSchema : IFlatSchema, IBinarySerializable
{
  [SerializedLabel("GroupingType")]
  public string GroupingType { get; init; } = null!; // "Country" or "Region"

  [SerializedLabel("Group1")]
  public string Group1 { get; init; } = null!;

  [SerializedLabel("Group2")]
  public string Group2 { get; init; } = null!;

  [SerializedLabel("DollarsCorrelation")]
  public double DollarsCorrelation { get; init; }

  [SerializedLabel("TransactionsCorrelation")]
  public double TransactionsCorrelation { get; init; }

  [SerializedLabel("UsersCorrelation")]
  public double UsersCorrelation { get; init; }
}
