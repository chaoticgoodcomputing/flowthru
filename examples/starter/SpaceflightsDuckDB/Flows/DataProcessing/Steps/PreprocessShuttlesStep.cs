using Flowthru.Step;
using Flowthru.Step.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceflightsDuckDB.Data._01_Raw.Schemas;
using SpaceflightsDuckDB.Data._02_Intermediate.Schemas;

namespace SpaceflightsDuckDB.Flows.DataProcessing.Steps;

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
  public static Func<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>> Create(
    ILogger logger)
  {
    return (input) =>
    {
      var rows = input.ToList();
      var processed = rows
        .Select(raw => Parse(raw))
        .Where(item => item != null)
        .Cast<PreprocessedShuttleSchema>()
        .ToList();

      var dropped = rows.Count - processed.Count;
      if (dropped > 0)
      {
        logger.LogWarning(
          "Dropped {Dropped}/{Total} shuttle rows with invalid numeric/currency fields",
          dropped, rows.Count
        );
      }
      else
      {
        logger.LogInformation("Preprocessed {Count} shuttle rows", processed.Count);
      }

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

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="PreprocessShuttlesStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static ShuttleSchema ValidRaw =>
      new()
      {
        Id = "S1",
        ShuttleType = "Type A",
        CompanyId = "C1",
        Engines = "4",
        PassengerCapacity = "100",
        Crew = "8",
        Price = "$1,234.56",
        DCheckComplete = "t",
        MoonClearanceComplete = "f",
      };

    /// <summary>
    /// A well-formed record should produce one output with all fields parsed correctly.
    /// </summary>
    [FUnitStepTest(typeof(PreprocessShuttlesStep))]
    public void ValidRecord_ParsesCorrectly()
    {
      // Arrange
      var input = Samples.Of(ValidRaw);

      // Apply
      var result = Invoke(Create(NullLogger.Instance), input).ToList();

      // Assert
      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].Engines, Is.EqualTo(4));
      Assert.That(result[0].PassengerCapacity, Is.EqualTo(100));
      Assert.That(result[0].Price, Is.EqualTo(1234.56m));
      Assert.That(result[0].DCheckComplete, Is.EqualTo(CheckStatus.Complete));
      Assert.That(result[0].MoonClearanceComplete, Is.EqualTo(CheckStatus.Incomplete));
    }

    /// <summary>
    /// A record with a non-numeric engines field should be filtered out.
    /// </summary>
    [FUnitStepTest(typeof(PreprocessShuttlesStep))]
    public void NonNumericEngines_RecordIsFiltered()
    {
      // Arrange
      var input = Samples.Of(ValidRaw with { Engines = "many" });

      // Apply
      var result = Invoke(Create(NullLogger.Instance), input).ToList();

      // Assert
      Assert.That(result, Is.Empty);
    }
  }
#endif
}
