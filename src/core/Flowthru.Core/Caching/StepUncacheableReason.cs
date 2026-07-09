namespace Flowthru.Caching;

/// <summary>
/// Structured explanation for why a step ended up in
/// <see cref="CachePlan.UncacheableStepLabels"/>. Surfaces in pre-flight
/// logging, in the JSON metadata projection's per-step <c>cache.reason</c>
/// field, and (eventually) in Mermaid tooltips. Without this signal,
/// "step X is uncacheable" was invisible to flow authors — every cascade
/// looked the same as a fundamental eligibility miss, and the only
/// debugging path was reading the cache-plan-builder source.
/// </summary>
/// <remarks>
/// Closed sum. Adding a new case requires updating every
/// <c>switch</c> expression that consumes this type so callers can't
/// silently drop the new case.
/// </remarks>
public abstract record StepUncacheableReason
{
  private StepUncacheableReason() { }

  /// <summary>
  /// The step's <see cref="Flowthru.Step.IStepNode.CodeVersion"/> is
  /// <c>null</c>. Typical when a Python step lacks
  /// <c>@step(cacheable=True)</c>, when an inline-Func step has no
  /// <c>[FlowthruStep]</c> companion to source identity from, or when
  /// the source generator failed to register the step (see Bug 1 in the
  /// MagicAtlas report — multi-decorator files).
  /// </summary>
  public sealed record NoCodeVersion : StepUncacheableReason;

  /// <summary>
  /// The step declared one or more <see cref="Flowthru.Step.IStepNode.ServiceDependencies"/>.
  /// The cache plan can't fingerprint runtime service state, so any
  /// service-dep makes the step uncacheable.
  /// </summary>
  /// <param name="Count">How many service dependencies the step declared.</param>
  public sealed record HasServiceDependencies(int Count) : StepUncacheableReason;

  /// <summary>
  /// At least one input is produced by another step in this flow, and
  /// that producer is itself uncacheable — the cascade rule carries the
  /// state downstream. <paramref name="ParentStepLabel"/> is the
  /// nearest uncacheable ancestor; deeper roots still chain through it.
  /// </summary>
  /// <param name="ParentStepLabel">
  /// Label of the uncacheable parent step whose output blocks this
  /// step from caching.
  /// </param>
  public sealed record CascadeFromStep(string ParentStepLabel) : StepUncacheableReason;

  /// <summary>
  /// One of the step's inputs has no
  /// <see cref="Flowthru.Data.Storage.ISupportsFingerprint"/> capability
  /// (or the fingerprint probe failed). Most commonly hit by items
  /// backed by <c>.Memory()</c> adapters — they're deliberately
  /// non-fingerprintable because in-process memory has no cross-run
  /// identity.
  /// </summary>
  /// <param name="ItemLabel">
  /// Label of the input catalog item that couldn't be fingerprinted.
  /// </param>
  public sealed record UnfingerprintableInput(string ItemLabel) : StepUncacheableReason;

  /// <summary>
  /// The step itself declared that it must not be cached, via
  /// <see cref="Flowthru.Step.IStepNode.DeclaredUncacheableReason"/>.
  /// Used by step types whose transform identity isn't fully captured
  /// by <see cref="Flowthru.Step.IStepNode.CodeVersion"/> — e.g. a step
  /// whose behaviour is driven by wire-up data (a remote script, an
  /// opaque callback) that cannot be reduced to a stable
  /// <see cref="Flowthru.Step.IStepNode.DeclaredCacheIdentity"/> token.
  /// Caching such a step would risk serving stale output after the
  /// wire-up data changes, so the step opts out loudly instead of
  /// silently. Steps whose wire-up data <em>can</em> be fingerprinted
  /// (e.g. SQL text hashed into the identity) should declare it through
  /// <c>DeclaredCacheIdentity</c> and stay cacheable.
  /// </summary>
  /// <param name="Reason">
  /// Human-readable explanation supplied by the step — rendered
  /// verbatim wherever uncacheable reasons surface.
  /// </param>
  public sealed record DeclaredByStep(string Reason) : StepUncacheableReason;

  /// <summary>
  /// Render this reason as a single-line human-readable string suitable
  /// for log lines and CLI output. Per-case formatting keeps the
  /// structured information accessible without a switch at every call
  /// site.
  /// </summary>
  public string Describe() => this switch
  {
    NoCodeVersion => "no CodeVersion (mark @step(cacheable=True) or add [FlowthruStep] to the factory class)",
    HasServiceDependencies sd => $"declares {sd.Count} service dependency(ies) — service state can't be fingerprinted",
    CascadeFromStep cs => $"cascaded from uncacheable parent step '{cs.ParentStepLabel}'",
    UnfingerprintableInput ui => $"input '{ui.ItemLabel}' is unfingerprintable (likely a .Memory() adapter)",
    DeclaredByStep db => db.Reason,
    _ => throw new InvalidOperationException(
      $"Unreachable: StepUncacheableReason is a closed sum, got {GetType().Name}."),
  };
}
