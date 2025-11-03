using RetailData.Data._03_Primary.Schemas;
using RetailData.Data._04_Metadata.Schemas;

namespace RetailData.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Generates metadata about the processed dataset
/// </summary>
public static class GenerateMetadataNode
{
  public static Func<IEnumerable<DailyDtuSchema>, Task<DatasetMetadata>> Create()
  {
    return async (input) =>
    {
      var dataList = input.ToList();

      // Get unique countries
      var uniqueCountries = dataList.Select(d => d.Country).Distinct().OrderBy(c => c).ToList();

      // Get date range
      var dates = dataList.Select(d => d.Date).Distinct().OrderBy(d => d).ToList();
      var startDate = dates.FirstOrDefault() ?? "";
      var endDate = dates.LastOrDefault() ?? "";

      var metadata = new DatasetMetadata
      {
        UniqueCountries = uniqueCountries,
        CountryCount = uniqueCountries.Count,
        DateRange = new DateRangeInfo
        {
          StartDate = startDate,
          EndDate = endDate,
          TotalDays = dates.Count,
        },
        TotalRecords = dataList.Count,
      };

      return await Task.FromResult(metadata);
    };
  }
}
