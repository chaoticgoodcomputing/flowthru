using Flowthru.Abstractions;

namespace RetailData.Data._03_Primary.Schemas;

/// <summary>
/// Region-to-region correlation metrics
/// </summary>
public record RegionCorrelationSchema : IFlatSchema, IBinarySerializable, ITextSerializable
{
  [SerializedLabel("Region1")]
  public string Region1 { get; init; } = null!;

  [SerializedLabel("Region2")]
  public string Region2 { get; init; } = null!;

  [SerializedLabel("DollarsCorrelation")]
  public double DollarsCorrelation { get; init; }

  [SerializedLabel("TransactionsCorrelation")]
  public double TransactionsCorrelation { get; init; }

  [SerializedLabel("UsersCorrelation")]
  public double UsersCorrelation { get; init; }
}
