using Flowthru.Step;
using SpaceflightsGQL.Data._02_Intermediate.Schemas;
using SpaceflightsGQL.Infra.GqlClient;
using StrawberryShake;

namespace SpaceflightsGQL.Flows.Ingest.Steps;

/// <summary>
/// Seeds the GQL server with preprocessed, typed company, shuttle, and review data via mutations.
/// Receives already-parsed records — no string conversion needed.
/// </summary>
[FlowthruStep]
public static class SeedGqlDatabaseStep
{
  /// <summary>
  /// Creates the seeding transform. Receives preprocessed typed collections and calls
  /// the corresponding add-mutations; returns <c>true</c> when seeding is complete.
  /// </summary>
  public static Func<
    (
      IEnumerable<PreprocessedCompanySchema>,
      IEnumerable<PreprocessedShuttleSchema>,
      IEnumerable<PreprocessedReviewSchema>
    ),
    Task<bool>
  > Create(ISpaceflightsClient client) =>
    async (inputs) =>
    {
      var (companies, shuttles, reviews) = inputs;

      foreach (var c in companies)
      {
        var result = await client.AddCompany.ExecuteAsync(
          new AddCompanyInput
          {
            Id = c.Id,
            CompanyRating = c.CompanyRating,
            IataApproved = c.IataApproved,
            CompanyLocation = c.CompanyLocation,
          }
        );
        result.EnsureNoErrors();
      }

      foreach (var s in shuttles)
      {
        var result = await client.AddShuttle.ExecuteAsync(
          new AddShuttleInput
          {
            Id = s.Id,
            ShuttleType = s.ShuttleType,
            CompanyId = s.CompanyId,
            Engines = s.Engines,
            PassengerCapacity = s.PassengerCapacity,
            Crew = s.Crew,
            Price = s.Price,
            DCheckComplete = s.DCheckComplete,
            MoonClearanceComplete = s.MoonClearanceComplete,
          }
        );
        result.EnsureNoErrors();
      }

      foreach (var r in reviews)
      {
        var result = await client.AddReview.ExecuteAsync(
          new AddReviewInput { ShuttleId = r.ShuttleId, ReviewScoresRating = r.ReviewScoresRating }
        );
        result.EnsureNoErrors();
      }

      return true;
    };
}
