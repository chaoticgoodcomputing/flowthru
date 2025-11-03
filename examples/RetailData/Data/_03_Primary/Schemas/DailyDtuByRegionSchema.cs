using Flowthru.Abstractions;

namespace RetailData.Data._03_Primary.Schemas;

/// <summary>
/// Daily DTU metrics aggregated by region
/// </summary>
public record DailyDtuByRegionSchema : IFlatSchema, IBinarySerializable, ITextSerializable
{
  [SerializedLabel("Date")]
  public string Date { get; init; } = null!;

  [SerializedLabel("Region")]
  public string Region { get; init; } = null!;

  [SerializedLabel("Dollars")]
  public decimal Dollars { get; init; }

  [SerializedLabel("Transactions")]
  public int Transactions { get; init; }

  [SerializedLabel("Users")]
  public int Users { get; init; }
}
