using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Raw;
using Flowthru.Spaceflights.Data.Schemas.Processed;

namespace Flowthru.Spaceflights.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Preprocesses raw shuttle data by converting string values to proper types.
/// Converts price strings ($1,234,567) to decimals and "t"/"f" to booleans.
/// 
/// Stateless node with implicit parameterless constructor,
/// compatible with type reference instantiation for distributed/parallel execution.
/// </summary>
public class PreprocessShuttlesNode : NodeBase<ShuttleRawSchema, ShuttleSchema>
{
  protected override Task<IEnumerable<ShuttleSchema>> Transform(
      IEnumerable<ShuttleRawSchema> input)
  {
    var processed = input
        .Select(s => TryParse(s))
        .Where(s => s != null)
        .Cast<ShuttleSchema>();

    return Task.FromResult(processed);
  }

  /// <summary>
  /// Attempts to parse a raw shuttle record into a processed shuttle.
  /// Returns null if any required field is missing or invalid.
  /// </summary>
  private static ShuttleSchema? TryParse(ShuttleRawSchema raw)
  {
    // Parse fields that might fail
    var engines = ParseInt(raw.Engines);
    var passengerCapacity = ParseInt(raw.PassengerCapacity);
    var crew = ParseInt(raw.Crew);

    // Validation: all required fields must be present
    if (!engines.HasValue
        || !passengerCapacity.HasValue
        || !crew.HasValue
        || string.IsNullOrWhiteSpace(raw.ShuttleLocation)
        || string.IsNullOrWhiteSpace(raw.ShuttleType)
        || string.IsNullOrWhiteSpace(raw.EngineType)
        || string.IsNullOrWhiteSpace(raw.EngineVendor)
        || string.IsNullOrWhiteSpace(raw.CancellationPolicy))
    {
      return null; // Parse failed - incomplete record
    }

    // Parse succeeded - return validated, non-nullable type
    return new ShuttleSchema
    {
      Id = raw.Id,
      CompanyId = raw.CompanyId,
      ShuttleLocation = raw.ShuttleLocation,
      ShuttleType = raw.ShuttleType,
      EngineType = raw.EngineType,
      EngineVendor = raw.EngineVendor,
      Engines = engines.Value,
      PassengerCapacity = passengerCapacity.Value,
      Crew = crew.Value,
      CancellationPolicy = raw.CancellationPolicy,
      Price = ParseMoney(raw.Price),
      DCheckComplete = IsTrue(raw.DCheckComplete),
      MoonClearanceComplete = IsTrue(raw.MoonClearanceComplete)
    };
  }

  /// <summary>
  /// Converts "t" to true, "f" to false
  /// </summary>
  private static bool IsTrue(string value) => value == "t";

  /// <summary>
  /// Parses money string (e.g., "$1,234,567") to decimal
  /// </summary>
  private static decimal ParseMoney(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return 0m;

    var cleaned = value.Replace("$", "").Replace(",", "").Trim();
    if (decimal.TryParse(cleaned, out var result))
      return result;

    return 0m;
  }

  /// <summary>
  /// Parses integer from string, returns null if empty/invalid
  /// </summary>
  private static int? ParseInt(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;

    if (int.TryParse(value, out var result))
      return result;

    return null;
  }
}
