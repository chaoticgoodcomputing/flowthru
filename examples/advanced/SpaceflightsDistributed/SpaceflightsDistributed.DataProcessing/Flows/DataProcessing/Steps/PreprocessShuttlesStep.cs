using Flowthru.Core.Steps;
using SpaceflightsDistributed.DataProcessing.Data._01_Raw.Schemas;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Flows.DataProcessing.Steps;

[FlowthruStep]
public static class PreprocessShuttlesStep
{
  public static Func<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>> Create()
  {
    return (input) =>
      input.Select(raw => Parse(raw)).Where(item => item != null).Cast<PreprocessedShuttleSchema>();
  }

  private static PreprocessedShuttleSchema? Parse(ShuttleSchema raw)
  {
    bool dCheckComplete = raw.DCheckComplete.Trim().ToLowerInvariant() == "t";
    bool moonClearanceComplete = raw.MoonClearanceComplete.Trim().ToLowerInvariant() == "t";

    if (!int.TryParse(raw.Engines, out var engines))
      return null;
    if (!int.TryParse(raw.PassengerCapacity, out var passengerCapacity))
      return null;
    if (!int.TryParse(raw.Crew, out var crew))
      return null;
    if (!TryParseMoney(raw.Price, out var price))
      return null;

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
      return false;

    var cleaned = value.Replace("$", "").Replace(",", "").Trim();
    return decimal.TryParse(cleaned, out result);
  }
}
