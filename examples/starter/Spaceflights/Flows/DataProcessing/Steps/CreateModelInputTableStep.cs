using Flowthru.Step;
using Spaceflights.Data._01_Raw.Schemas;
using Spaceflights.Data._02_Intermediate.Schemas;
using Spaceflights.Data._03_Primary.Schemas;

namespace Spaceflights.Flows.DataProcessing.Steps;

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
  > Create()
  {
    return (input) =>
    {
      var (shuttles, companies, reviews) = input;

      // Parse reviews to have decimal scores
      var parsedReviews = reviews
        .Select(r => new
        {
          r.ShuttleId,
          Score = decimal.TryParse(r.ReviewScoresRating, out var score) ? score : (decimal?)null,
        })
        .Where(r => r.Score.HasValue)
        .ToList();

      // Join reviews to shuttles
      var ratedShuttles = parsedReviews
        .Join(
          shuttles,
          r => r.ShuttleId,
          s => s.Id,
          (r, s) => new { Shuttle = s, ReviewScore = r.Score!.Value }
        )
        .ToList();

      // Join with companies
      var modelInputTable = ratedShuttles
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
        .ToList(); // Materialize query to ensure LINQ execution completes

      return modelInputTable;
    };
  }
}
