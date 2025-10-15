using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Raw;
using Flowthru.Spaceflights.Data.Schemas.Processed;

namespace Flowthru.Spaceflights.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Preprocesses raw shuttle data by converting string values to proper types.
/// Converts price strings ($1,234,567) to decimals and "t"/"f" to booleans.
/// </summary>
public class PreprocessShuttlesNode : Node<ShuttleRawSchema, ShuttleSchema>
{
  protected override Task<IEnumerable<ShuttleSchema>> TransformInternal(
      IEnumerable<ShuttleRawSchema> input)
  {
    var processed = input.Select(shuttle => new ShuttleSchema
    {
      Id = shuttle.Id,
      CompanyId = shuttle.CompanyId,
      ShuttleType = shuttle.ShuttleType,
      Engines = ParseInt(shuttle.Engines),
      PassengerCapacity = ParseInt(shuttle.PassengerCapacity),
      Crew = ParseInt(shuttle.Crew),
      Price = ParseMoney(shuttle.Price),
      DCheckComplete = IsTrue(shuttle.DCheckComplete),
      MoonClearanceComplete = IsTrue(shuttle.MoonClearanceComplete)
    });

    return Task.FromResult(processed);
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
