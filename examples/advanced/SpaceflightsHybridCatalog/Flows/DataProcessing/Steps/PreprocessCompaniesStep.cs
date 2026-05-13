using Flowthru.Step;
using SpaceflightsHybridCatalog.Data._01_Raw.Schemas;
using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;

namespace SpaceflightsHybridCatalog.Flows.DataProcessing.Steps;

/// <summary>
/// Preprocesses raw company data by parsing rating percentages and IATA approval flags.
/// </summary>
[FlowthruStep]
public static class PreprocessCompaniesStep
{
  public static Func<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>> Create()
  {
    return input =>
    {
      var processed = input
        .Select(Parse)
        .Where(item => item != null)
        .Cast<PreprocessedCompanySchema>();

      return processed;
    };
  }

  private static PreprocessedCompanySchema? Parse(CompanySchema raw)
  {
    bool iataApproved = raw.IataApproved.Trim().ToLowerInvariant() == "t";

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

  private static bool TryParsePercentage(string value, out decimal result)
  {
    result = 0;
    if (string.IsNullOrWhiteSpace(value)) return false;

    var cleaned = value.Replace("%", "").Trim();
    if (!decimal.TryParse(cleaned, out var parsed)) return false;

    result = parsed / 100m;
    return true;
  }
}
