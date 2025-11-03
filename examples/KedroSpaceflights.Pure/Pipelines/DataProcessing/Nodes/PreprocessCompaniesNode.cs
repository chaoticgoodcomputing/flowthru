using KedroSpaceflights.Pure.Data._01_Raw.Schemas;
using KedroSpaceflights.Pure.Data._02_Intermediate.Schemas;

namespace KedroSpaceflights.Pure.Pipelines.DataProcessing.Nodes;

public static class PreprocessCompaniesNode
{
  public static Func<
    IEnumerable<CompanySchema>,
    Task<IEnumerable<PreprocessedCompanySchema>>
  > Create()
  {
    return async (input) =>
    {
      var processed = input
        .Select(raw => Parse(raw))
        .Where(item => item != null)
        .Cast<PreprocessedCompanySchema>();

      return await Task.FromResult(processed);
    };
  }

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
}
