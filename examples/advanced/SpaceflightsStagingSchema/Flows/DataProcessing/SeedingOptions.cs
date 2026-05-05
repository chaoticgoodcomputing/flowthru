namespace SpaceflightsStagingSchema.Flows.DataProcessing;

/// <summary>
/// Knobs for synthesizing additional rows on top of the real CSV/Excel inputs.
/// Used by the preprocess steps to scale the pipeline up to volumes that
/// actually exercise the bulk-insert path.
/// </summary>
/// <remarks>
/// <para>
/// Statistical fidelity is <strong>not</strong> a goal here. The synthetic
/// rows are deterministic but uncorrelated with the real Spaceflights data —
/// the point is throughput, not predictive validity. Set the counts to zero
/// (the default) for a real-data-only run.
/// </para>
/// <para>
/// FK shape is preserved across synthesized rows: synthetic shuttles
/// reference synthetic companies via <c>"syn-co-{i}"</c>, and synthetic
/// reviews reference synthetic shuttles via <c>"syn-sh-{i}"</c>. Crank
/// <see cref="SyntheticCompanies"/> high enough to cover
/// <see cref="SyntheticShuttles"/>, and so on, or the FK conformance filter
/// at promotion time will trim the orphans (which is itself a useful demo,
/// just not the bulk demo).
/// </para>
/// </remarks>
public record SeedingOptions
{
  /// <summary>Synthetic company rows to append. <c>0</c> disables synthesis.</summary>
  public int SyntheticCompanies { get; init; } = 0;

  /// <summary>Synthetic shuttle rows to append.</summary>
  public int SyntheticShuttles { get; init; } = 0;

  /// <summary>Synthetic review rows to append.</summary>
  public int SyntheticReviews { get; init; } = 0;

  /// <summary>Seed for the deterministic RNG. Same seed → same rows.</summary>
  public int RandomSeed { get; init; } = 42;
}
