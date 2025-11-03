using KedroSpaceflights.Pure.Data._01_Raw.Schemas;
using KedroSpaceflights.Pure.Data._02_Intermediate.Schemas;
using KedroSpaceflights.Pure.Data._03_Primary.Schemas;

namespace KedroSpaceflights.Pure.Pipelines.DataProcessing.Nodes;

public static class CreateModelInputTableNode
{
  public static Func<
    (
      IEnumerable<PreprocessedShuttleSchema>,
      IEnumerable<PreprocessedCompanySchema>,
      IEnumerable<ReviewSchema>
    ),
    Task<IEnumerable<ModelInputTableSchema>>
  > Create()
  {
    return async (input) =>
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
      var ratedShuttles = parsedReviews.Join(
        shuttles,
        r => r.ShuttleId,
        s => s.Id,
        (r, s) => new { Shuttle = s, ReviewScore = r.Score!.Value }
      );

      // Join with companies
      var modelInputTable = ratedShuttles.Join(
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
      );

      return await Task.FromResult(modelInputTable);
    };
  }
}
