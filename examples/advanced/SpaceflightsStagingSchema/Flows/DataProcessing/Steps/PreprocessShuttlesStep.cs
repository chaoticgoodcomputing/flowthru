using Flowthru.Core.Steps;
using SpaceflightsStagingSchema.Data._01_Raw.Schemas;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Flows.DataProcessing.Steps;

[FlowthruStep]
public static class PreprocessShuttlesStep
{
  public static Func<
    (IEnumerable<ShuttleSchema> Raw, SeedingOptions Options),
    IEnumerable<PreprocessedShuttleSchema>
  > Create()
  {
    return (input) =>
    {
      var (raw, options) = input;
      var real = raw.Select(Parse).Where(item => item is not null).Cast<PreprocessedShuttleSchema>();
      var synthetic = SyntheticDataSeeder.Shuttles(
        options.SyntheticShuttles,
        options.SyntheticCompanies,
        options.RandomSeed
      );
      return real.Concat(synthetic);
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

  private static bool TryParseMoney(string value, out decimal result)
  {
    result = 0;
    if (string.IsNullOrWhiteSpace(value))
      return false;

    var cleaned = value.Replace("$", "").Replace(",", "").Trim();
    return decimal.TryParse(cleaned, out result);
  }
}
