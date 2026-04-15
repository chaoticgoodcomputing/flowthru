using Flowthru.Core.Steps;
using Flowthru.DataFrames;
using Flowthru.Extensions.Spark;
using KedroSpaceflightsSpark.Data._01_Raw.Schemas;
using KedroSpaceflightsSpark.Data._02_Intermediate.Schemas;

namespace KedroSpaceflightsSpark.Flows.DataProcessing.Steps;

/// <summary>
/// Parses raw shuttle strings into a typed Spark DataFrame.
/// </summary>
[FlowthruStep]
public static class PreprocessShuttlesStep
{
  public static Func<IEnumerable<ShuttleSchema>, TypedFrame<PreprocessedShuttleSchema>> Create(
    SparkFrameProvider frameProvider
  )
  {
    return (input) =>
    {
      var parsed = input
        .Select(Parse)
        .Where(item => item != null)
        .Cast<PreprocessedShuttleSchema>();

      return frameProvider.CreateFromEnumerable(parsed);
    };
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

  private static bool TryParseMoney(string value, out double result)
  {
    result = 0;
    if (string.IsNullOrWhiteSpace(value))
      return false;

    var cleaned = value.Replace("$", "").Replace(",", "").Trim();
    return double.TryParse(cleaned, out result);
  }
}
