using Flowthru.Step;
using Flowthru.Data.Storage.Gql;
using KedroSpaceflightsGQL.Data._03_Primary.Schemas;
using KedroSpaceflightsGQL.Infra.GqlClient;

namespace KedroSpaceflightsGQL.Flows.DataProcessing.Steps;

/// <summary>
/// Materializes deferred GQL query handles and joins the results into a unified model
/// input table. All fields are already strongly-typed — no parsing required.
/// </summary>
/// <remarks>
/// <para>
/// The <c>bool</c> first input is the <c>GqlDatabaseSeeded</c> gate; it is consumed only
/// to express the DAG dependency on Ingest and is otherwise unused.
/// </para>
/// <para>
/// The remaining three inputs are <see cref="GqlQuery{TResult,T}"/> handles — deferred
/// query descriptors that carry the connection details and pagination config but have not
/// yet executed any network calls. The calls to <c>ToList()</c> below are the
/// materialization points: each one fires the corresponding GQL query (paginating as
/// needed) and pulls the full dataset into memory before the join.
/// </para>
/// <para>
/// This is the step-level analog of <c>TypedFrame&lt;T&gt;.ToList()</c> in the Spark
/// extension: the catalog declares <em>what</em> to query; the step decides <em>when</em>
/// to materialize and <em>how</em> to combine the results.
/// </para>
/// </remarks>
[FlowthruStep]
public static class CreateModelInputTableStep
{
  public static Func<
    (
      bool,
      GqlQuery<IGetShuttlesResult, IGetShuttles_Shuttles>,
      GqlQuery<IGetCompaniesResult, IGetCompanies_Companies>,
      GqlQuery<IGetReviewsResult, IGetReviews_Reviews>
    ),
    IEnumerable<ModelInputTableSchema>
  > Create()
  {
    return (input) =>
    {
      var (_, shuttlesQuery, companiesQuery, reviewsQuery) = input;

      // Materialization — each call fires a GQL query (with pagination if configured).
      // Network I/O happens here, not in the catalog.
      var shuttles = shuttlesQuery.ToList();
      var companies = companiesQuery.ToList();
      var reviews = reviewsQuery.ToList();

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
