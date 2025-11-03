using Flowthru.Abstractions;

namespace RetailData.Data._04_Metadata.Schemas;

/// <summary>
/// Metadata about the processed retail dataset
/// </summary>
public record DatasetMetadata : IStructuredSerializable
{
  [SerializedLabel("uniqueCountries")]
  public List<string> UniqueCountries { get; init; } = new();

  [SerializedLabel("countryCount")]
  public int CountryCount { get; init; }

  [SerializedLabel("dateRange")]
  public DateRangeInfo DateRange { get; init; } = new();

  [SerializedLabel("totalRecords")]
  public int TotalRecords { get; init; }
}

public record DateRangeInfo
{
  [SerializedLabel("startDate")]
  public string StartDate { get; init; } = null!;

  [SerializedLabel("endDate")]
  public string EndDate { get; init; } = null!;

  [SerializedLabel("totalDays")]
  public int TotalDays { get; init; }
}
