using Flowthru.Core.Steps;
using Flowthru.DataFrames;
using Flowthru.Extensions.Spark;
using KedroSpaceflightsSpark.Data._01_Raw.Schemas;
using KedroSpaceflightsSpark.Data._02_Intermediate.Schemas;

namespace KedroSpaceflightsSpark.Flows.DataProcessing.Steps;

/// <summary>
/// Parses raw company strings into a typed Spark DataFrame.
/// The parsing (percentage strings, boolean flags) runs in C# before the rows are
/// pushed into Spark, keeping Spark focused on the distributed join and aggregation work
/// rather than format-specific cleaning.
/// </summary>
[FlowthruStep]
public static class PreprocessCompaniesStep
{
  public static Func<IEnumerable<CompanySchema>, TypedFrame<PreprocessedCompanySchema>> Create(
    SparkFrameProvider frameProvider
  )
  {
    return (input) =>
    {
      var parsed = input
        .Select(Parse)
        .Where(item => item != null)
        .Cast<PreprocessedCompanySchema>();

      return frameProvider.CreateFromEnumerable<PreprocessedCompanySchema>(parsed);
    };
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

  private static bool TryParsePercentage(string value, out double result)
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
