using Flowthru.Abstractions;

namespace RetailData.Data._05_Reporting.Schemas;

/// <summary>
/// Unified DTU time series schema that works for both country and region groupings
/// </summary>
public record DtuTimeSeriesSchema : IFlatSchema, IBinarySerializable
{
  [SerializedLabel("GroupingType")]
  public string GroupingType { get; init; } = null!; // "Country" or "Region"

  [SerializedLabel("GroupingValue")]
  public string GroupingValue { get; init; } = null!; // Actual country/region name

  [SerializedLabel("Date")]
  public string Date { get; init; } = null!;

  [SerializedLabel("Dollars")]
  public decimal Dollars { get; init; }

  [SerializedLabel("Transactions")]
  public int Transactions { get; init; }

  [SerializedLabel("Users")]
  public int Users { get; init; }
}
