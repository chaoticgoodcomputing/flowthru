using SpaceflightsPythonEFCore.Data._01_Raw.Schemas;
using SpaceflightsPythonEFCore.Data._02_Intermediate.Schemas;

namespace SpaceflightsPythonEFCore.Flows.DataProcessing.Steps;

/// <summary>
/// Preprocesses raw company data into strongly-typed records.
/// Parses IATA flag, company rating percentage, and optional fleet count.
/// Records with unparseable company_rating are filtered out.
/// </summary>
public static class PreprocessCompaniesStep
{
  public static Func<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>> Create()
  {
    return (input) =>
      input.Select(Parse).Where(item => item != null).Cast<PreprocessedCompanySchema>();
  }

  private static PreprocessedCompanySchema? Parse(CompanySchema raw)
  {
    if (!int.TryParse(raw.Id?.Trim(), out var id))
      return null;

    if (!TryParsePercentage(raw.CompanyRating, out var rating))
      return null;

    bool iataApproved = raw.IataApproved?.Trim().ToLowerInvariant() == "t";

    double? totalFleetCount = null;
    if (
      !string.IsNullOrWhiteSpace(raw.TotalFleetCount)
      && double.TryParse(raw.TotalFleetCount.Trim(), out var fleetCount)
    )
    {
      totalFleetCount = fleetCount;
    }

    return new PreprocessedCompanySchema
    {
      Id = id,
      CompanyRating = rating,
      IataApproved = iataApproved,
      CompanyLocation = raw.CompanyLocation ?? string.Empty,
      TotalFleetCount = totalFleetCount,
    };
  }

  private static bool TryParsePercentage(string? value, out double result)
  {
    result = 0;
    if (string.IsNullOrWhiteSpace(value))
      return false;

    var cleaned = value.Replace("%", "").Trim();
    if (!double.TryParse(cleaned, out var parsed))
      return false;

    result = parsed / 100.0;
    return true;
  }
}
