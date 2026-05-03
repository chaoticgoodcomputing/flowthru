using Flowthru.Core.Steps;
using SpaceflightsNewTypes.Data._01_Raw.Schemas;
using SpaceflightsNewTypes.Data._02_Intermediate.Schemas;
using SpaceflightsNewTypes.Data._03_Primary.Schemas;

namespace SpaceflightsNewTypes.Flows.DataProcessing.Steps;

/// <summary>
/// Joins preprocessed shuttle and company data with review scores to create a unified model input table.
/// </summary>
/// <remarks>
/// <para>
/// The two joins below use NewType keys (<c>ShuttleId</c>, <c>CompanyId</c>) instead of raw
/// <c>string</c>. The compiler enforces correctness at every join site:
/// </para>
/// <list type="bullet">
/// <item>
/// <c>r.ShuttleId</c> joined on <c>s.Id</c>: both are <c>ShuttleId</c>. Swapping in
/// <c>c.Id</c> (which is <c>CompanyId</c>) would not compile — the type parameters of
/// <c>Enumerable.Join</c> cannot unify.
/// </item>
/// <item>
/// <c>rs.Shuttle.CompanyId</c> joined on <c>c.Id</c>: both are <c>CompanyId</c>.
/// </item>
/// </list>
/// <para>
/// This is the <em>cross-join guard</em>: the kind of bug that silently produces an empty
/// (or wrong) table at runtime in stringly-typed pipelines is caught at build time.
/// </para>
/// </remarks>
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
