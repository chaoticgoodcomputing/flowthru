using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsSpark.Data._08_Reporting.Schemas;

/// <summary>
/// Per-shuttle price ranking within shuttle type.
/// Produced by RankShuttlesByPriceStep using a Spark window function over a
/// PartitionBy(ShuttleType).OrderBy(Price) window spec, annotating each row with
/// its DenseRank and the window-average price for its type.
/// </summary>
[FlowthruSchema]
public partial record ShuttlePriceRankSchema
{
  [SerializedLabel("shuttle_id")]
  public required string ShuttleId { get; init; }

  [SerializedLabel("shuttle_type")]
  public required string ShuttleType { get; init; }

  [SerializedLabel("company_id")]
  public required string CompanyId { get; init; }

  [SerializedLabel("price")]
  public required double Price { get; init; }

  [SerializedLabel("price_rank")]
  public required long PriceRank { get; init; }

  [SerializedLabel("avg_price_for_type")]
  public required double AvgPriceForType { get; init; }
}
