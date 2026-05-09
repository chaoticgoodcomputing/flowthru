using Flowthru.Step;

using SpaceflightsDistributed.DataProcessing.Data._01_Raw.Schemas;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;
using SpaceflightsDistributed.DataProcessing.Data._03_Primary.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Flows.DataProcessing.Steps;

[FlowthruStep]
public static class CreateModelInputTableStep
{
  public static Func<
    (
      IEnumerable<PreprocessedShuttleSchema>,
      IEnumerable<PreprocessedCompanySchema>,
      IEnumerable<ReviewSchema>
    ),
    IEnumerable<ModelInputTableSchema>
  > Create()
  {
    return (input) =>
    {
      var (shuttles, companies, reviews) = input;

      var parsedReviews = reviews
        .Select(r => new
        {
          r.ShuttleId,
          Score = decimal.TryParse(r.ReviewScoresRating, out var score) ? score : (decimal?)null,
        })
        .Where(r => r.Score.HasValue)
        .ToList();

      var ratedShuttles = parsedReviews
        .Join(
          shuttles,
          r => r.ShuttleId,
          s => s.Id,
          (r, s) => new { Shuttle = s, ReviewScore = r.Score!.Value }
        )
        .ToList();

      return ratedShuttles
        .Join(
          companies,
          rs => rs.Shuttle.CompanyId,
          c => c.Id,
          (rs, c) =>
            new ModelInputTableSchema
            {
              ShuttleId = rs.Shuttle.Id,
              ShuttleType = rs.Shuttle.ShuttleType,
              CompanyId = rs.Shuttle.CompanyId,
              Engines = rs.Shuttle.Engines,
              PassengerCapacity = rs.Shuttle.PassengerCapacity,
              Crew = rs.Shuttle.Crew,
              DCheckComplete = rs.Shuttle.DCheckComplete,
              MoonClearanceComplete = rs.Shuttle.MoonClearanceComplete,
              Price = rs.Shuttle.Price,
              IataApproved = c.IataApproved,
              CompanyRating = c.CompanyRating,
              ReviewScoresRating = rs.ReviewScore,
            }
        )
        .ToList();
    };
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="CreateModelInputTableStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static PreprocessedShuttleSchema MakeShuttle(string id, string companyId = "c1") =>
      new()
      {
        Id = id,
        ShuttleType = "TypeA",
        CompanyId = companyId,
        Engines = 2,
        PassengerCapacity = 100,
        Crew = 5,
        Price = 1000m,
        DCheckComplete = true,
        MoonClearanceComplete = false,
      };

    private static PreprocessedCompanySchema MakeCompany(string id) =>
      new()
      {
        Id = id,
        CompanyRating = 0.9m,
        IataApproved = true,
        CompanyLocation = "UK",
      };

    private static ReviewSchema MakeReview(string shuttleId, string score = "4.5") =>
      new() { ShuttleId = shuttleId, ReviewScoresRating = score };

    [StepTest(typeof(CreateModelInputTableStep))]
    public void MatchingShuttleCompanyAndReview_ProducesOneRow()
    {
      var input = (
        Samples.Of(MakeShuttle("s1", "c1")),
        Samples.Of(MakeCompany("c1")),
        Samples.Of(MakeReview("s1", "4.5"))
      );

      var result = Invoke(Create(), input).ToList();

      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].ShuttleId, Is.EqualTo("s1"));
      Assert.That(result[0].ReviewScoresRating, Is.EqualTo(4.5m));
      Assert.That(result[0].IataApproved, Is.True);
    }

    [StepTest(typeof(CreateModelInputTableStep))]
    public void ReviewWithInvalidScore_ShuttleExcluded()
    {
      var input = (
        Samples.Of(MakeShuttle("s1", "c1")),
        Samples.Of(MakeCompany("c1")),
        Samples.Of(MakeReview("s1", "not-a-number"))
      );

      var result = Invoke(Create(), input).ToList();

      Assert.That(result, Is.Empty);
    }

    [StepTest(typeof(CreateModelInputTableStep))]
    public void ShuttleWithNoMatchingCompany_Excluded()
    {
      var input = (
        Samples.Of(MakeShuttle("s1", "c_missing")),
        Samples.Of(MakeCompany("c1")),
        Samples.Of(MakeReview("s1", "4.5"))
      );

      var result = Invoke(Create(), input).ToList();

      Assert.That(result, Is.Empty);
    }
  }
#endif
}
