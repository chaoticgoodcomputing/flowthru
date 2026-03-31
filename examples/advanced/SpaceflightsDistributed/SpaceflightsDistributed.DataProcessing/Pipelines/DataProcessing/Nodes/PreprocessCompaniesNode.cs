using SpaceflightsDistributed.DataProcessing.Data._01_Raw.Schemas;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Pipelines.DataProcessing.Nodes;

public static class PreprocessCompaniesNode
{
  public static Func<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>> Create()
  {
    return (input) =>
      input.Select(raw => Parse(raw)).Where(item => item != null).Cast<PreprocessedCompanySchema>();
  }

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
