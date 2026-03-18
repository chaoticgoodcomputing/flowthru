using SpaceflightsPythonEFCore.Data._01_Raw.Schemas;
using SpaceflightsPythonEFCore.Data._02_Intermediate.Schemas;
using SpaceflightsPythonEFCore.Data._03_Primary.Schemas;

namespace SpaceflightsPythonEFCore.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Joins preprocessed shuttle and company data with review scores to create the model input table.
/// Mirrors the logic of the Python create_model_input_table node, including string-to-int
/// coercion of review shuttle IDs for the join.
/// </summary>
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

      // Parse review shuttle IDs to int (matching Python's pd.to_numeric coercion)
      var parsedReviews = reviews
        .Select(r => new
        {
          ShuttleId = int.TryParse(r.ShuttleId, out var sid) ? (int?)sid : null,
          Score = double.TryParse(r.ReviewScoresRating, out var score) ? (double?)score : null,
        })
        .Where(r => r.ShuttleId.HasValue && r.Score.HasValue)
        .ToList();

      var ratedShuttles = parsedReviews
        .Join(
          shuttles,
          r => r.ShuttleId!.Value,
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
              ShuttleId = rs.Shuttle.Id.ToString(),
              ShuttleType = rs.Shuttle.ShuttleType,
              EngineType = rs.Shuttle.EngineType,
              CompanyId = rs.Shuttle.CompanyId.ToString(),
              Engines = rs.Shuttle.Engines,
              PassengerCapacity = rs.Shuttle.PassengerCapacity,
              Crew = rs.Shuttle.Crew,
              DCheckComplete = rs.Shuttle.DCheckComplete,
              MoonClearanceComplete = rs.Shuttle.MoonClearanceComplete,
              Price = rs.Shuttle.Price,
              IataApproved = c.IataApproved,
              CompanyRating = c.CompanyRating,
              CompanyLocation = c.CompanyLocation,
              TotalFleetCount = c.TotalFleetCount,
              ReviewScoresRating = rs.ReviewScore,
            }
        )
        .ToList();
    };
  }
}
