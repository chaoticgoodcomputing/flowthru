using Flowthru.Step;
using Flowthru.Step.Testing;
using SpaceflightsFUnit.Data._01_Raw.Schemas;
using SpaceflightsFUnit.Data._02_Intermediate.Schemas;

namespace SpaceflightsFUnit.Flows.DataProcessing.Steps;

/// <summary>
/// Preprocesses raw company data by parsing rating percentages and IATA approval flags.
/// </summary>
[FlowthruStep]
public static class PreprocessCompaniesStep
{
  /// <summary>
  /// Creates a preprocessing function that transforms raw company records into strongly-typed records.
  /// </summary>
  /// <returns>
  /// A function that converts <see cref="CompanySchema"/> records to <see cref="PreprocessedCompanySchema"/> records.
  /// Records with invalid rating percentages are filtered out.
  /// </returns>
  public static Func<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>> Create()
  {
    return (input) =>
    {
      var processed = input
        .Select(raw => Parse(raw))
        .Where(item => item != null)
        .Cast<PreprocessedCompanySchema>();

      return processed;
    };
  }

  /// <summary>
  /// Parses a raw company record into a preprocessed record with strongly-typed fields.
  /// </summary>
  /// <param name="raw">The raw company record to parse.</param>
  /// <returns>
  /// A <see cref="PreprocessedCompanySchema"/> if parsing succeeds; otherwise, <c>null</c>.
  /// </returns>
  private static PreprocessedCompanySchema? Parse(CompanySchema raw)
  {
    // Parse "t" or "f" to boolean
    bool iataApproved = raw.IataApproved.Trim().ToLowerInvariant() == "t";

    // Parse percentage string (e.g., "90%" -> 0.90)
    if (!TryParsePercentage(raw.CompanyRating, out var rating))
    {
      return null;
    }

    return new PreprocessedCompanySchema
    {
      Id = raw.Id,
      CompanyRating = rating,
      IataApproved = iataApproved,
      CompanyLocation = raw.CompanyLocation,
    };
  }

  /// <summary>
  /// Parses a percentage string (e.g., "90%") to a decimal ratio (e.g., 0.90).
  /// </summary>
  /// <param name="value">The percentage string to parse. Expected format: digits followed by optional "%".</param>
  /// <param name="result">
  /// When this method returns, contains the decimal ratio (0.0 to 1.0) if parsing succeeded,
  /// or zero if parsing failed.
  /// </param>
  /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
  private static bool TryParsePercentage(string value, out decimal result)
  {
    result = 0;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    var cleaned = value.Replace("%", "").Trim();
    if (!decimal.TryParse(cleaned, out var parsed))
    {
      return false;
    }

    result = parsed / 100m;
    return true;
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="PreprocessCompaniesStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static CompanySchema ValidRaw =>
      new()
      {
        Id = "C1",
        CompanyRating = "90%",
        IataApproved = "t",
        CompanyLocation = "London",
      };

    /// <summary>
    /// A well-formed record should produce one output with correctly parsed fields.
    /// </summary>
    [FUnitStepTest(typeof(PreprocessCompaniesStep))]
    public void ValidRecord_ParsesCorrectly()
    {
      // Arrange
      var input = Samples.Of(ValidRaw);

      // Apply
      var result = Invoke(Create(), input).ToList();

      // Assert
      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].Id, Is.EqualTo("C1"));
      Assert.That(result[0].CompanyRating, Is.EqualTo(0.90m));
      Assert.That(result[0].IataApproved, Is.True);
      Assert.That(result[0].CompanyLocation, Is.EqualTo("London"));
    }

    /// <summary>
    /// "f" in IataApproved should parse to false.
    /// </summary>
    [FUnitStepTest(typeof(PreprocessCompaniesStep))]
    public void IataApprovedFalse_ParsesCorrectly()
    {
      // Arrange
      var input = Samples.Of(ValidRaw with { IataApproved = "f" });

      // Apply
      var result = Invoke(Create(), input).ToList();

      // Assert
      Assert.That(result[0].IataApproved, Is.False);
    }

    /// <summary>
    /// A record whose company_rating is not a valid percentage string should be filtered out.
    /// </summary>
    [FUnitStepTest(typeof(PreprocessCompaniesStep))]
    public void InvalidRating_RecordIsFiltered()
    {
      // Arrange
      var input = Samples.Of(ValidRaw with { CompanyRating = "not-a-percent" });

      // Apply
      var result = Invoke(Create(), input).ToList();

      // Assert
      Assert.That(result, Is.Empty);
    }
  }
#endif
}
