using KedroSpaceflights.Pure.Data._01_Raw.Schemas;
using KedroSpaceflights.Pure.Data._02_Intermediate.Schemas;

namespace KedroSpaceflights.Pure.Pipelines.DataProcessing.Nodes;

public static class PreprocessShuttlesNode
{
  public static Func<
    IEnumerable<ShuttleSchema>,
    Task<IEnumerable<PreprocessedShuttleSchema>>
  > Create()
  {
    return async (input) =>
    {
      var processed = input
        .Select(raw => Parse(raw))
        .Where(item => item != null)
        .Cast<PreprocessedShuttleSchema>();

      return await Task.FromResult(processed);
    };
  }

  private static PreprocessedShuttleSchema? Parse(ShuttleSchema raw)
  {
    // Parse boolean fields
    bool dCheckComplete = raw.DCheckComplete.Trim().ToLowerInvariant() == "t";
    bool moonClearanceComplete = raw.MoonClearanceComplete.Trim().ToLowerInvariant() == "t";

    // Parse numeric fields
    if (!int.TryParse(raw.Engines, out var engines))
    {
      return null;
    }

    if (!int.TryParse(raw.PassengerCapacity, out var passengerCapacity))
    {
      return null;
    }

    if (!int.TryParse(raw.Crew, out var crew))
    {
      return null;
    }

    // Parse money string (e.g., "$1,234.56" -> 1234.56)
    if (!TryParseMoney(raw.Price, out var price))
    {
      return null;
    }

    return new PreprocessedShuttleSchema
    {
      Id = raw.Id,
      ShuttleType = raw.ShuttleType,
      CompanyId = raw.CompanyId,
      Engines = engines,
      PassengerCapacity = passengerCapacity,
      Crew = crew,
      Price = price,
      DCheckComplete = dCheckComplete,
      MoonClearanceComplete = moonClearanceComplete,
    };
  }

  private static bool TryParseMoney(string value, out decimal result)
  {
    result = 0;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    var cleaned = value.Replace("$", "").Replace(",", "").Trim();
    return decimal.TryParse(cleaned, out result);
  }
}
