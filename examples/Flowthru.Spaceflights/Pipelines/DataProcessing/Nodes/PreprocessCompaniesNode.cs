using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Raw;
using Flowthru.Spaceflights.Data.Schemas.Processed;

namespace Flowthru.Spaceflights.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Preprocesses raw company data by converting string values to proper types.
/// Converts percentage strings to decimals and "t"/"f" to booleans.
/// 
/// Stateless node with implicit parameterless constructor,
/// compatible with type reference instantiation for distributed/parallel execution.
/// </summary>
public class PreprocessCompaniesNode : NodeBase<CompanyRawSchema, CompanySchema>
{
  protected override Task<IEnumerable<CompanySchema>> Transform(
      IEnumerable<CompanyRawSchema> input)
  {
    var processed = input
        .Select(c => Parse(c))
        .Where(c => c != null)
        .Cast<CompanySchema>();

    return Task.FromResult(processed);
  }

  /// <summary>
  /// Attempts to parse a raw company record into a processed company.
  /// Returns null if any required field is missing or invalid.
  /// </summary>
  private static CompanySchema? Parse(CompanyRawSchema raw)
  {
    // Parse fields that might fail
    var companyRating = ParsePercentage(raw.CompanyRating);
    var totalFleetCount = ParseDecimal(raw.TotalFleetCount);

    // Validation: all required fields must be present
    if (!companyRating.HasValue
        || !totalFleetCount.HasValue
        || string.IsNullOrWhiteSpace(raw.CompanyLocation))
    {
      return null; // Parse failed - incomplete record
    }

    // Parse succeeded - return validated, non-nullable type
    return new CompanySchema
    {
      Id = raw.Id,
      CompanyRating = companyRating.Value,
      CompanyLocation = raw.CompanyLocation,
      TotalFleetCount = totalFleetCount.Value,
      IataApproved = IsTrue(raw.IataApproved)
    };
  }

  /// <summary>
  /// Converts "t" to true, "f" to false
  /// </summary>
  private static bool IsTrue(string value) => value == "t";

  /// <summary>
  /// Parses percentage string (e.g., "100%") to decimal (e.g., 1.0)
  /// Returns null for empty/invalid values to match Kedro's NaN handling
  /// </summary>
  private static decimal? ParsePercentage(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;

    var cleaned = value.Replace("%", "").Trim();
    if (decimal.TryParse(cleaned, out var result))
      return result / 100m;

    return null;
  }

  /// <summary>
  /// Parses decimal from string, returns null if empty/invalid
  /// </summary>
  private static decimal? ParseDecimal(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;

    if (decimal.TryParse(value, out var result))
      return result;

    return null;
  }
}
