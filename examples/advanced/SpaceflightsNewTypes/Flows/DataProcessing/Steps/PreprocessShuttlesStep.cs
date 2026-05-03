using Flowthru.Core.Steps;
using SpaceflightsNewTypes.Data._01_Raw.Schemas;
using SpaceflightsNewTypes.Data._02_Intermediate.Schemas;

namespace SpaceflightsNewTypes.Flows.DataProcessing.Steps;

/// <summary>
/// Preprocesses raw shuttle data by parsing numeric fields, boolean flags, and currency values.
/// </summary>
[FlowthruStep]
public static class PreprocessShuttlesStep
{
  /// <summary>
  /// Creates a preprocessing function that transforms raw shuttle records into strongly-typed records.
  /// </summary>
  /// <returns>
  /// A function that converts <see cref="ShuttleSchema"/> records to <see cref="PreprocessedShuttleSchema"/> records.
  /// Records with invalid numeric fields or currency values are filtered out.
  /// </returns>
  public static Func<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>> Create()
  {
    return (input) =>
    {
      var processed = input
        .Select(raw => Parse(raw))
        .Where(item => item != null)
        .Cast<PreprocessedShuttleSchema>();

      return processed;
    };
  }

  /// <summary>
  /// Parses a raw shuttle record into a preprocessed record with strongly-typed fields.
  /// </summary>
  /// <param name="raw">The raw shuttle record to parse.</param>
  /// <returns>
  /// A <see cref="PreprocessedShuttleSchema"/> if all fields parse successfully; otherwise, <c>null</c>.
  /// </returns>
  private static PreprocessedShuttleSchema? Parse(ShuttleSchema raw)
  {
    // Parse status flag fields. The raw "t"/"f" string is mapped to CheckStatus via its
    // [SerializedEnum] attribute when round-tripped through Flowthru's JSON/CSV adapters,
    // but here we're constructing the typed value from a raw string field, so the mapping
    // is explicit.
    static CheckStatus ParseFlag(string raw) =>
      raw.Trim().ToLowerInvariant() == "t" ? CheckStatus.Complete : CheckStatus.Incomplete;

    var dCheckComplete = ParseFlag(raw.DCheckComplete);
    var moonClearanceComplete = ParseFlag(raw.MoonClearanceComplete);

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

  /// <summary>
  /// Parses a currency string (e.g., "$1,234.56") to a decimal value (e.g., 1234.56).
  /// </summary>
  /// <param name="value">The currency string to parse. Expected format: optional "$", digits with optional commas, optional decimal point.</param>
  /// <param name="result">
  /// When this method returns, contains the decimal value if parsing succeeded,
  /// or zero if parsing failed.
  /// </param>
  /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
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
