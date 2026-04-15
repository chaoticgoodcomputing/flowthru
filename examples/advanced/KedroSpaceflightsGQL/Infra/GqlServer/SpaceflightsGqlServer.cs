using KedroSpaceflightsGQL.Infra.GqlServer.Types;

namespace KedroSpaceflightsGQL.Infra.GqlServer;

// ── Query root ─────────────────────────────────────────────────────────────

/// <summary>
/// HotChocolate query root. Returns the full collection of each entity.
/// </summary>
public class Query
{
    public IReadOnlyList<CompanyRecord> GetCompanies([Service] SpaceflightsRepository repo) =>
      repo.GetCompanies();

    public IReadOnlyList<ShuttleRecord> GetShuttles([Service] SpaceflightsRepository repo) =>
      repo.GetShuttles();

    public IReadOnlyList<ReviewRecord> GetReviews([Service] SpaceflightsRepository repo) =>
      repo.GetReviews();
}

// ── Mutation root ──────────────────────────────────────────────────────────

/// <summary>
/// HotChocolate mutation root. Each mutation appends one record to the repository.
/// </summary>
public class Mutation
{
    public CompanyRecord AddCompany(AddCompanyInput input, [Service] SpaceflightsRepository repo) =>
      repo.AddCompany(
        new CompanyRecord(input.Id, input.CompanyRating, input.IataApproved, input.CompanyLocation)
      );

    public ShuttleRecord AddShuttle(AddShuttleInput input, [Service] SpaceflightsRepository repo) =>
      repo.AddShuttle(
        new ShuttleRecord(
          input.Id,
          input.ShuttleType,
          input.CompanyId,
          input.Engines,
          input.PassengerCapacity,
          input.Crew,
          input.Price,
          input.DCheckComplete,
          input.MoonClearanceComplete
        )
      );

    public ReviewRecord AddReview(AddReviewInput input, [Service] SpaceflightsRepository repo) =>
      repo.AddReview(new ReviewRecord(input.ShuttleId, input.ReviewScoresRating));
}

// ── Server factory ─────────────────────────────────────────────────────────

/// <summary>
/// Configures and builds the in-process HotChocolate GraphQL server.
/// </summary>
/// <remarks>
/// This class exists solely to provide a realistic, swap-out-for-production GQL endpoint.
/// To point at a real GQL server instead, remove <c>SpaceflightsGqlServer</c> entirely and
/// configure <c>AddSpaceflightsClient</c> with the real endpoint URL in
/// <c>Program.ConfigureServices</c>.
/// </remarks>
public static class SpaceflightsGqlServer
{
    /// <summary>
    /// Registers all services required by the Spaceflights GQL server.
    /// Shared between the standalone <see cref="Build"/> path and the in-process
    /// <see cref="TestServer"/> path so both stay in sync.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        services
          .AddSingleton<SpaceflightsRepository>()
          .AddGraphQLServer()
          .AddQueryType<Query>()
          .AddMutationType<Mutation>();
        services.AddRouting();
    }

    /// <summary>
    /// Wires up the GQL middleware on an <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapGraphQL());
    }

    /// <summary>
    /// Builds a standalone <see cref="WebApplication"/> bound to a real port (Kestrel).
    /// </summary>
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);
        ConfigureServices(builder.Services);
        var app = builder.Build();
        app.MapGraphQL();
        return app;
    }
}
