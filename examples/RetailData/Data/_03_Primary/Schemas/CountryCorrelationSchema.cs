using Flowthru.Abstractions;

namespace RetailData.Data._03_Primary.Schemas;

/// <summary>
/// Country-to-country correlation metrics
/// </summary>
public record CountryCorrelationSchema : IFlatSchema, IBinarySerializable, ITextSerializable
{
  [SerializedLabel("Country1")]
  public string Country1 { get; init; } = null!;

  [SerializedLabel("Country2")]
  public string Country2 { get; init; } = null!;

  [SerializedLabel("DollarsCorrelation")]
  public double DollarsCorrelation { get; init; }

  [SerializedLabel("TransactionsCorrelation")]
  public double TransactionsCorrelation { get; init; }

  [SerializedLabel("UsersCorrelation")]
  public double UsersCorrelation { get; init; }
}
