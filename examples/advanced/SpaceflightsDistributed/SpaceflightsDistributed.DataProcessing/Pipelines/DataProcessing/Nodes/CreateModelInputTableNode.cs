using SpaceflightsDistributed.DataProcessing.Data._01_Raw.Schemas;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;
using SpaceflightsDistributed.DataProcessing.Data._03_Primary.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Pipelines.DataProcessing.Nodes;

public static class CreateModelInputTableNode
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
}
