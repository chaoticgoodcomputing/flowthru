using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Flows.DataProcessing;

/// <summary>
/// Deterministic synthetic-row generators for scaling DataProcessing inputs
/// to bulk-test volumes. Yields lazily — pipe into <c>Concat</c> alongside
/// real preprocessed rows and let the bulk save adapter consume the joint
/// stream without intermediate materialization.
/// </summary>
internal static class SyntheticDataSeeder
{
  private static readonly string[] Locations =
  {
    "USA",
    "Russia",
    "China",
    "Japan",
    "France",
    "UK",
    "Germany",
    "Brazil",
    "India",
    "Australia",
  };

  private static readonly string[] ShuttleTypes =
  {
    "Capsule",
    "Lander",
    "Shuttle",
    "Cruiser",
    "Frigate",
    "Liner",
  };

  /// <summary>
  /// Yields <paramref name="count"/> synthetic preprocessed company rows.
  /// IDs are <c>"syn-co-{i}"</c> for <c>i</c> in <c>0..count-1</c>.
  /// </summary>
  public static IEnumerable<PreprocessedCompanySchema> Companies(int count, int seed)
  {
    if (count <= 0)
      yield break;

    var rng = new Random(seed);
    for (int i = 0; i < count; i++)
    {
      yield return new PreprocessedCompanySchema
      {
        Id = $"syn-co-{i}",
        CompanyRating = (decimal)Math.Round(rng.NextDouble(), 6),
        IataApproved = rng.NextDouble() < 0.7,
        CompanyLocation = Locations[rng.Next(Locations.Length)],
      };
    }
  }

  /// <summary>
  /// Yields <paramref name="count"/> synthetic preprocessed shuttle rows.
  /// IDs are <c>"syn-sh-{i}"</c>; <c>CompanyId</c> cycles through
  /// <c>"syn-co-{i}"</c> bounded by <paramref name="syntheticCompanyCount"/>
  /// so the FK conformance filter retains them when companies are seeded
  /// at matching scale.
  /// </summary>
  public static IEnumerable<PreprocessedShuttleSchema> Shuttles(
    int count,
    int syntheticCompanyCount,
    int seed
  )
  {
    if (count <= 0)
      yield break;

    var rng = new Random(seed + 1);
    var maxCompany = Math.Max(syntheticCompanyCount, 1);
    for (int i = 0; i < count; i++)
    {
      yield return new PreprocessedShuttleSchema
      {
        Id = $"syn-sh-{i}",
        ShuttleType = ShuttleTypes[rng.Next(ShuttleTypes.Length)],
        CompanyId = $"syn-co-{i % maxCompany}",
        Engines = rng.Next(1, 8),
        PassengerCapacity = rng.Next(2, 50),
        Crew = rng.Next(1, 10),
        Price = (decimal)Math.Round(rng.NextDouble() * 5000 + 100, 6),
        DCheckComplete = rng.NextDouble() < 0.5,
        MoonClearanceComplete = rng.NextDouble() < 0.5,
      };
    }
  }

  /// <summary>
  /// Yields <paramref name="count"/> synthetic preprocessed review rows.
  /// <c>ShuttleId</c> cycles through <c>"syn-sh-{i}"</c> bounded by
  /// <paramref name="syntheticShuttleCount"/>.
  /// </summary>
  public static IEnumerable<PreprocessedReviewSchema> Reviews(
    int count,
    int syntheticShuttleCount,
    int seed
  )
  {
    if (count <= 0)
      yield break;

    var rng = new Random(seed + 2);
    var maxShuttle = Math.Max(syntheticShuttleCount, 1);
    for (int i = 0; i < count; i++)
    {
      yield return new PreprocessedReviewSchema
      {
        ShuttleId = $"syn-sh-{i % maxShuttle}",
        ReviewScoresRating = (decimal)Math.Round(rng.NextDouble() * 10, 6),
      };
    }
  }
}
