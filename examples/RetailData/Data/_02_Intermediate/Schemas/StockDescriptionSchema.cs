using Flowthru.Abstractions;

namespace RetailData.Data._02_Intermediate.Schemas;

/// <summary>
/// Stock code to description mapping
/// </summary>
public record StockDescriptionSchema : IFlatSchema, IBinarySerializable
{
  [SerializedLabel("StockCode")]
  public string StockCode { get; init; } = null!;

  [SerializedLabel("Description")]
  public string Description { get; init; } = null!;
}
