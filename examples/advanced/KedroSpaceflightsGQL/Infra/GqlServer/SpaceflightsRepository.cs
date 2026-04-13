using KedroSpaceflightsGQL.Infra.GqlServer.Types;

namespace KedroSpaceflightsGQL.Infra.GqlServer;

/// <summary>
/// Thread-safe in-memory store for Company, Shuttle, and Review records.
/// Acts as the data layer for the HotChocolate server in this example.
/// </summary>
/// <remarks>
/// In a real-world application, replace this class with your own data access layer
/// (e.g., EF Core, Dapper, or a remote data source).
/// </remarks>
public class SpaceflightsRepository
{
  private readonly List<CompanyRecord> _companies = [];
  private readonly List<ShuttleRecord> _shuttles = [];
  private readonly List<ReviewRecord> _reviews = [];

  private readonly Lock _lock = new();

  // ── Queries ─────────────────────────────────────────────────────────────

  public IReadOnlyList<CompanyRecord> GetCompanies()
  {
    lock (_lock)
      return _companies.ToList();
  }

  public IReadOnlyList<ShuttleRecord> GetShuttles()
  {
    lock (_lock)
      return _shuttles.ToList();
  }

  public IReadOnlyList<ReviewRecord> GetReviews()
  {
    lock (_lock)
      return _reviews.ToList();
  }

  // ── Mutations ────────────────────────────────────────────────────────────

  public CompanyRecord AddCompany(CompanyRecord company)
  {
    lock (_lock)
      _companies.Add(company);
    return company;
  }

  public ShuttleRecord AddShuttle(ShuttleRecord shuttle)
  {
    lock (_lock)
      _shuttles.Add(shuttle);
    return shuttle;
  }

  public ReviewRecord AddReview(ReviewRecord review)
  {
    lock (_lock)
      _reviews.Add(review);
    return review;
  }
}
