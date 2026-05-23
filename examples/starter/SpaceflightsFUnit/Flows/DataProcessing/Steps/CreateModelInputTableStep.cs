using Flowthru.Step;
using Flowthru.Step.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceflightsFUnit.Data._01_Raw.Schemas;
using SpaceflightsFUnit.Data._02_Intermediate.Schemas;
using SpaceflightsFUnit.Data._03_Primary.Schemas;

namespace SpaceflightsFUnit.Flows.DataProcessing.Steps;

/// <summary>
/// Joins preprocessed shuttle and company data with review scores to create a unified model input table.
/// </summary>
[FlowthruStep]
public static class CreateModelInputTableStep
{
  /// <summary>
  /// Creates a join function that combines shuttle, company, and review data into a single table for modeling.
  /// </summary>
  /// <returns>
  /// A function that performs inner joins to produce <see cref="ModelInputTableSchema"/> records.
  /// Records are filtered to include only reviews with valid numeric scores.
  /// </returns>
  public static Func<
    (
      IEnumerable<PreprocessedShuttleSchema>,
      IEnumerable<PreprocessedCompanySchema>,
      IEnumerable<ReviewSchema>
    ),
    IEnumerable<ModelInputTableSchema>
  > Create(ILogger logger)
  {
    return (input) =>
    {
      var (shuttles, companies, reviews) = input;
      var shuttleList = shuttles.ToList();
      var companyList = companies.ToList();
      var reviewList = reviews.ToList();

      // Parse reviews to have decimal scores
      var parsedReviews = reviewList
        .Select(r => new
        {
          r.ShuttleId,
          Score = decimal.TryParse(r.ReviewScoresRating, out var score) ? score : (decimal?)null,
        })
        .Where(r => r.Score.HasValue)
        .ToList();

      var droppedReviews = reviewList.Count - parsedReviews.Count;
      if (droppedReviews > 0)
      {
        logger.LogWarning(
          "Dropped {Dropped}/{Total} reviews with unparseable rating scores",
          droppedReviews, reviewList.Count
        );
      }

      // Join reviews to shuttles
      var ratedShuttles = parsedReviews
        .Join(
          shuttleList,
          r => r.ShuttleId,
          s => s.Id,
          (r, s) => new { Shuttle = s, ReviewScore = r.Score!.Value }
        )
        .ToList();

      // Join with companies
      var modelInputTable = ratedShuttles
        .Join(
          companyList,
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
        .ToList(); // Materialize query to ensure LINQ execution completes

      logger.LogInformation(
        "Joined {Shuttles} shuttle rows × {Companies} company rows × {Reviews} reviews "
        + "→ {Out} model-input rows",
        shuttleList.Count, companyList.Count, parsedReviews.Count, modelInputTable.Count
      );

      return modelInputTable;
    };
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="CreateModelInputTableStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static PreprocessedShuttleSchema Shuttle(string id, string companyId) =>
      new()
      {
        Id = id,
        ShuttleType = "Type A",
        CompanyId = companyId,
        Engines = 4,
        PassengerCapacity = 100,
        Crew = 8,
        Price = 1000m,
        DCheckComplete = true,
        MoonClearanceComplete = false,
      };

    private static PreprocessedCompanySchema Company(string id) =>
      new()
      {
        Id = id,
        CompanyRating = 0.90m,
        IataApproved = true,
        CompanyLocation = "London",
      };

    private static ReviewSchema Review(string shuttleId, string score) =>
      new() { ShuttleId = shuttleId, ReviewScoresRating = score };

    /// <summary>
    /// A matched shuttle, company, and review should produce exactly one output row.
    /// </summary>
    [FUnitStepTest(typeof(CreateModelInputTableStep))]
    public void MatchedRow_ProducesOneOutput()
    {
      // Arrange
      var shuttles = Samples.Of(Shuttle("S1", "C1"));
      var companies = Samples.Of(Company("C1"));
      var reviews = Samples.Of(Review("S1", "90"));

      // Apply
      var result = Invoke(Create(NullLogger.Instance), (shuttles, companies, reviews)).ToList();

      // Assert
      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].ShuttleId, Is.EqualTo("S1"));
      Assert.That(result[0].ReviewScoresRating, Is.EqualTo(90m));
    }

    /// <summary>
    /// A shuttle with no matching company should not appear in the output.
    /// </summary>
    [FUnitStepTest(typeof(CreateModelInputTableStep))]
    public void UnmatchedCompany_ShuttleExcluded()
    {
      // Arrange
      var shuttles = Samples.Of(Shuttle("S1", "MISSING"));
      var companies = Samples.Of(Company("C1"));
      var reviews = Samples.Of(Review("S1", "90"));

      // Apply
      var result = Invoke(Create(NullLogger.Instance), (shuttles, companies, reviews)).ToList();

      // Assert
      Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// A review with a non-numeric score should be excluded, dropping the shuttle row.
    /// </summary>
    [FUnitStepTest(typeof(CreateModelInputTableStep))]
    public void NonNumericReviewScore_RowExcluded()
    {
      // Arrange
      var shuttles = Samples.Of(Shuttle("S1", "C1"));
      var companies = Samples.Of(Company("C1"));
      var reviews = Samples.Of(Review("S1", "not-a-number"));

      // Apply
      var result = Invoke(Create(NullLogger.Instance), (shuttles, companies, reviews)).ToList();

      // Assert
      Assert.That(result, Is.Empty);
    }
  }
#endif
}
