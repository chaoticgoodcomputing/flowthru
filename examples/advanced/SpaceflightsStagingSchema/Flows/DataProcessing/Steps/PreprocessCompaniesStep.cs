using Flowthru.Step;
using SpaceflightsStagingSchema.Data._01_Raw.Schemas;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Flows.DataProcessing.Steps;

[FlowthruStep]
public static class PreprocessCompaniesStep
{
  public static Func<
    IEnumerable<CompanySchema>,
    IEnumerable<PreprocessedCompanySchema>
  > Create(SeedingOptions options) => raw =>
  {
    var real = raw.Select(Parse).Where(item => item is not null).Cast<PreprocessedCompanySchema>();
    var synthetic = SyntheticDataSeeder.Companies(
      options.SyntheticCompanies,
      options.RandomSeed
    );
    return real.Concat(synthetic);
  };

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
    if (string.IsNullOrWhiteSpace(value))
      return false;

    var cleaned = value.Replace("%", "").Trim();
    if (!decimal.TryParse(cleaned, out var parsed))
      return false;

    result = parsed / 100m;
    return true;
  }
}
