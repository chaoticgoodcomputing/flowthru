namespace Flowthru.Caching;

/// <summary>
/// Pre-flight artifact describing which steps in a (sliced) flow can
/// be short-circuited and which must run. Produced once before the
/// scheduler starts and consumed without re-derivation — the scheduler
/// trusts the plan.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Three buckets, not two.</strong> A step is one of:
/// <list type="bullet">
/// <item><b>Fresh</b> — every input's identity matches the recorded
/// manifest entry, the step's <c>CodeVersion</c> matches, and every
/// output's <c>Exists()</c> returns true. Skip at runtime.</item>
/// <item><b>Stale</b> — eligible for caching, but at least one input
/// has changed or an output is missing. Must run.</item>
/// <item><b>Uncacheable</b> — does not qualify for caching at all
/// (no <c>CodeVersion</c>, has <c>ServiceDependencies</c>, or any
/// input lacks <c>ISupportsFingerprint</c> / failed to fingerprint).
/// Must run.</item>
/// </list>
/// The distinction between Stale and Uncacheable matters for telemetry
/// and CLI rendering even though both end up running.
/// </para>
/// <para>
/// <strong>Cascade rule.</strong> A stale or uncacheable step forces
/// every downstream step that consumes its outputs into the same
/// bucket — the cache walk preserves this transitively. Phase 6's
/// design treats this as a feature: a miss in the middle of the DAG
/// implies its outputs will differ; assuming downstream cacheability
/// would compound silently-stale data.
/// </para>
/// <para>
/// <strong>NewFingerprints.</strong> The plan also carries the
/// composite hashes the framework will write back to the manifest for
/// every cacheable step the plan computed (whether fresh or stale).
/// On successful execution, fresh steps' entries are preserved with
/// updated <c>RecordedAt</c>; stale steps' entries are replaced with
/// the new composite. Uncacheable steps contribute no entries.
/// </para>
/// </remarks>
public sealed record CachePlan(
  IReadOnlySet<string> FreshStepLabels,
  IReadOnlySet<string> StaleStepLabels,
  IReadOnlySet<string> UncacheableStepLabels,
  IReadOnlyDictionary<string, string> NewFingerprints
)
{
  /// <summary>
  /// The empty plan — no fresh steps, nothing recorded. Used when
  /// caching is disabled or no <c>UseCacheStorage</c> registration was
  /// made on the builder.
  /// </summary>
  public static CachePlan Empty { get; } =
    new(
      new HashSet<string>(StringComparer.Ordinal),
      new HashSet<string>(StringComparer.Ordinal),
      new HashSet<string>(StringComparer.Ordinal),
      new Dictionary<string, string>(StringComparer.Ordinal)
    );

  /// <summary>
  /// True iff every step in <paramref name="stepLabel"/> is fresh —
  /// the scheduler should emit a cached <c>StepResult.Succeeded</c>
  /// without dispatching the transform.
  /// </summary>
  public bool IsFresh(string stepLabel) => FreshStepLabels.Contains(stepLabel);
}
