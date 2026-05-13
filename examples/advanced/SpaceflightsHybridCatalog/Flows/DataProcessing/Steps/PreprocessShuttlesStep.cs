using Flowthru.Step;
using SpaceflightsHybridCatalog.Data._01_Raw.Schemas;
using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;

namespace SpaceflightsHybridCatalog.Flows.DataProcessing.Steps;

/// <summary>
/// Preprocesses raw shuttle data by parsing numeric fields, boolean flags, and currency values.
/// </summary>
[FlowthruStep]
public static class PreprocessShuttlesStep
{
  public static Func<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>> Create()
  {
    return input =>
    {
      var processed = input
        .Select(Parse)
        .Where(item => item != null)
        .Cast<PreprocessedShuttleSchema>();

      return processed;
    };
  }

  private static PreprocessedShuttleSchema? Parse(ShuttleSchema raw)
  {
    static CheckStatus ParseFlag(string raw) =>
      raw.Trim().ToLowerInvariant() == "t" ? CheckStatus.Complete : CheckStatus.Incomplete;

    var dCheckComplete = ParseFlag(raw.DCheckComplete);
    var moonClearanceComplete = ParseFlag(raw.MoonClearanceComplete);

    if (!int.TryParse(raw.Engines, out var engines)) return null;
    if (!int.TryParse(raw.PassengerCapacity, out var passengerCapacity)) return null;
    if (!int.TryParse(raw.Crew, out var crew)) return null;
    if (!TryParseMoney(raw.Price, out var price)) return null;

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
    if (string.IsNullOrWhiteSpace(value)) return false;

    var cleaned = value.Replace("$", "").Replace(",", "").Trim();
    return decimal.TryParse(cleaned, out result);
  }
}
