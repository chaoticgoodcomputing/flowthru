using RetailData.Data._03_Primary.Schemas;

namespace RetailData.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Splits aggregated DTU data by country into separate outputs
/// </summary>
public static class SplitByCountryNode
{
  public static Func<
    IEnumerable<DailyDtuSchema>,
    Task<Dictionary<string, IEnumerable<DailyDtuSchema>>>
  > Create()
  {
    return async (input) =>
    {
      var byCountry = input.GroupBy(d => d.Country).ToDictionary(g => g.Key, g => g.AsEnumerable());

      return await Task.FromResult(byCountry);
    };
  }
}
