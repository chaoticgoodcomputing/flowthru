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
    var processed = input.Select(company => new CompanySchema
    {
      Id = company.Id,
      CompanyRating = ParsePercentage(company.CompanyRating),
      CompanyLocation = company.CompanyLocation,
      TotalFleetCount = ParseDecimal(company.TotalFleetCount),
      IataApproved = IsTrue(company.IataApproved)
    });

    return Task.FromResult(processed);
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
