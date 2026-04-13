using Flowthru.Core.Steps;
using KedroSpaceflightsGQL.Data._01_Raw.Schemas;
using KedroSpaceflightsGQL.Data._02_Intermediate.Schemas;

namespace KedroSpaceflightsGQL.Flows.Ingest.Steps;

/// <summary>
/// Preprocesses raw company data by parsing rating percentages and IATA approval flags.
/// Runs during Ingest so the GQL server stores typed values rather than raw strings.
/// </summary>
[FlowthruStep]
public static class PreprocessCompaniesStep
{
  /// <summary>
  /// Creates a preprocessing function that transforms raw company records into strongly-typed records.
  /// Records with invalid rating percentages are filtered out.
  /// </summary>
  public static Func<
    IEnumerable<CompanySchema>,
    IEnumerable<PreprocessedCompanySchema>
  > Create() =>
    input =>
      input
        .Select(raw => Parse(raw))
        .Where(item => item != null)
        .Cast<PreprocessedCompanySchema>();

  private static PreprocessedCompanySchema? Parse(CompanySchema raw)
  {
    bool iataApproved = raw.IataApproved.Trim().ToLowerInvariant() == "t";

    if (!TryParsePercentage(raw.CompanyRating, out var rating))
      return null;

    return new PreprocessedCompanySchema
    {
      Id = raw.Id,
      CompanyRating = rating,
      IataApproved = iataApproved,
      CompanyLocation = raw.CompanyLocation,
    };
  }

  private static bool TryParsePercentage(string value, out decimal result)
  {
    result = 0;
    if (string.IsNullOrWhiteSpace(value))
      return false;

    var cleaned = value.Replace("%", "").Trim();
    if (!decimal.TryParse(cleaned, out var parsed))
      return false;

    result = parsed / 100m;
    return true;
  }
}
