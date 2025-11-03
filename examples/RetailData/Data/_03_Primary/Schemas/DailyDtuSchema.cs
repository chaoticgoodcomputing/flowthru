using Flowthru.Abstractions;

namespace RetailData.Data._03_Primary.Schemas;

/// <summary>
/// Daily DTU (Dollars, Transactions, Users) metrics by country
/// </summary>
public record DailyDtuSchema : IFlatSchema, IBinarySerializable, ITextSerializable
{
  [SerializedLabel("Date")]
  public string Date { get; init; } = null!;

  [SerializedLabel("Country")]
  public string Country { get; init; } = null!;

  [SerializedLabel("Dollars")]
  public decimal Dollars { get; init; }

  [SerializedLabel("Transactions")]
  public int Transactions { get; init; }

  [SerializedLabel("Users")]
  public int Users { get; init; }
}
