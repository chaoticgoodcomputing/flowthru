using Flowthru.Core.Steps;
using Flowthru.FUnit;
using SpaceflightsDistributed.DataProcessing.Data._01_Raw.Schemas;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Flows.DataProcessing.Steps;

[FlowthruStep]
public static class PreprocessCompaniesStep
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

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="PreprocessCompaniesStep"/>.</summary>
  public class Tests : Flowthru.FUnit.FunitContext
  {
    private static readonly CompanySchema ValidRaw =
      new()
      {
        Id = "1",
        CompanyRating = "90%",
        IataApproved = "t",
        CompanyLocation = "UK",
      };

    [StepTest(typeof(PreprocessCompaniesStep))]
    public void ValidRecord_ParsesCorrectly()
    {
      var result = Invoke(Create(), Samples.Of(ValidRaw)).ToList();

      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].CompanyRating, Is.EqualTo(0.90m));
      Assert.That(result[0].IataApproved, Is.True);
    }

    [StepTest(typeof(PreprocessCompaniesStep))]
    public void IataApprovedFalse_ParsesCorrectly()
    {
      var result = Invoke(Create(), Samples.Of(ValidRaw with { IataApproved = "f" })).ToList();

      Assert.That(result[0].IataApproved, Is.False);
    }

    [StepTest(typeof(PreprocessCompaniesStep))]
    public void InvalidRating_RecordIsFiltered()
    {
      var result = Invoke(Create(), Samples.Of(ValidRaw with { CompanyRating = "not-a-percent" }))
        .ToList();

      Assert.That(result, Is.Empty);
    }
  }
#endif
}
