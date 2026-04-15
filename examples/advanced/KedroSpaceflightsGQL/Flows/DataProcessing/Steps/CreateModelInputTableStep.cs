using Flowthru.Core.Steps;
using KedroSpaceflightsGQL.Data._03_Primary.Schemas;
using KedroSpaceflightsGQL.Infra.GqlClient;

namespace KedroSpaceflightsGQL.Flows.DataProcessing.Steps;

/// <summary>
/// Joins typed shuttle, company, and review data from the GQL server into a unified model
/// input table. All fields are already strongly-typed — no parsing required.
/// The <c>bool</c> first input is the GqlDatabaseSeeded gate; it is consumed only to
/// express the DAG dependency on Ingest and is otherwise unused.
/// </summary>
[FlowthruStep]
public static class CreateModelInputTableStep
{
    public static Func<
      (
        bool,
        IEnumerable<IGetShuttles_Shuttles>,
        IEnumerable<IGetCompanies_Companies>,
        IEnumerable<IGetReviews_Reviews>
      ),
      IEnumerable<ModelInputTableSchema>
    > Create()
    {
        return (input) =>
        {
            var (_, shuttles, companies, reviews) = input;

            // Join reviews to shuttles
            var ratedShuttles = reviews
          .Join(
            shuttles,
            r => r.ShuttleId,
            s => s.Id,
            (r, s) => new { Shuttle = s, ReviewScore = r.ReviewScoresRating }
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
          .ToList();

            return modelInputTable;
        };
    }
}
