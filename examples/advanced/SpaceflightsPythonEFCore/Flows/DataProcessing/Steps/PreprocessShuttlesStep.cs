using Flowthru.Step;
using SpaceflightsPythonEFCore.Data._01_Raw.Schemas;
using SpaceflightsPythonEFCore.Data._02_Intermediate.Schemas;

namespace SpaceflightsPythonEFCore.Flows.DataProcessing.Steps;

/// <summary>
/// Preprocesses raw shuttle data into strongly-typed records.
/// Parses numeric fields, booleans, currency strings, and passes engine type through.
/// Records with unparseable required fields are filtered out.
/// </summary>
[FlowthruStep]
public static class PreprocessShuttlesStep
{
  public static Func<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>> Create()
  {
    return (input) =>
      input.Select(Parse).Where(item => item != null).Cast<PreprocessedShuttleSchema>();
  }

  private static PreprocessedShuttleSchema? Parse(ShuttleSchema raw)
  {
    if (!int.TryParse(raw.Id?.Trim(), out var id))
    {
      return null;
    }

    if (!int.TryParse(raw.CompanyId?.Trim(), out var companyId))
    {
      return null;
    }

    if (!int.TryParse(raw.Engines?.Trim(), out var engines))
    {
      return null;
    }

    if (!int.TryParse(raw.PassengerCapacity?.Trim(), out var passengerCapacity))
    {
      return null;
    }

    if (!int.TryParse(raw.Crew?.Trim(), out var crew))
    {
      return null;
    }

    if (!TryParseMoney(raw.Price, out var price))
    {
      return null;
    }

    bool dCheckComplete = raw.DCheckComplete?.Trim().ToLowerInvariant() == "t";
    bool moonClearanceComplete = raw.MoonClearanceComplete?.Trim().ToLowerInvariant() == "t";

    return new PreprocessedShuttleSchema
    {
      Id = id,
      ShuttleType = raw.ShuttleType ?? string.Empty,
      EngineType = raw.EngineType ?? string.Empty,
      CompanyId = companyId,
      Engines = engines,
      PassengerCapacity = passengerCapacity,
      Crew = crew,
      Price = price,
      DCheckComplete = dCheckComplete,
      MoonClearanceComplete = moonClearanceComplete,
    };
  }

  private static bool TryParseMoney(string? value, out double result)
  {
    result = 0;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    var cleaned = value.Replace("$", "").Replace(",", "").Trim();
    return double.TryParse(cleaned, out result);
  }
}
