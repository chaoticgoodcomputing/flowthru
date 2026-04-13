using Flowthru.Core.Steps;
using KedroSpaceflightsGQL.Data._01_Raw.Schemas;
using KedroSpaceflightsGQL.Infra.GqlClient;
using StrawberryShake;

namespace KedroSpaceflightsGQL.Flows.Ingest.Steps;

/// <summary>
/// Seeds the GQL server with company, shuttle, and review data via mutations.
/// Reads from the three raw CSV/Excel catalog entries and calls one mutation per record.
/// </summary>
[FlowthruStep]
public static class SeedGqlDatabaseStep
{
  /// <summary>
  /// Creates the seeding transform. Receives all three raw collections and calls
  /// the corresponding add-mutations; returns <c>true</c> when seeding is complete.
  /// </summary>
  public static Func<
    (IEnumerable<CompanySchema>, IEnumerable<ShuttleSchema>, IEnumerable<ReviewSchema>),
    Task<bool>
  > Create(ISpaceflightsClient client) =>
    async (inputs) =>
    {
      var (companies, shuttles, reviews) = inputs;

      // Seed all three collections — run sequentially to avoid overwhelming
      // an in-process server; switch to parallel execution for a production endpoint.
      foreach (var c in companies)
      {
        // Skip records with missing required fields (sparse rows from source files)
        if (
          c.Id is null
          || c.CompanyRating is null
          || c.IataApproved is null
          || c.CompanyLocation is null
        )
          continue;

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
        // Skip records with missing required fields (sparse rows from source Excel files)
        if (
          s.Id is null
          || s.ShuttleType is null
          || s.CompanyId is null
          || s.Engines is null
          || s.PassengerCapacity is null
          || s.Crew is null
          || s.Price is null
          || s.DCheckComplete is null
          || s.MoonClearanceComplete is null
        )
          continue;

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
        // Skip records with missing required fields (sparse rows from source files)
        if (r.ShuttleId is null || r.ReviewScoresRating is null)
          continue;

        var result = await client.AddReview.ExecuteAsync(
          new AddReviewInput { ShuttleId = r.ShuttleId, ReviewScoresRating = r.ReviewScoresRating }
        );
        result.EnsureNoErrors();
      }

      return true;
    };
}
