using Flowthru.Abstractions;

namespace RetailData.Data._99_Configuration.Schemas;

/// <summary>
/// Configuration mapping regions to their countries
/// </summary>
public record CountryRegionMapping : IStructuredSerializable
{
  [SerializedLabel("regions")]
  public Dictionary<string, List<string>> Regions { get; set; } = new();
}
