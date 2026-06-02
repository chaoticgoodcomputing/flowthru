using System.Security.Cryptography;
using System.Text;
using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Validation.Runtime;

namespace Flowthru.Caching;

/// <summary>
/// Walks the effective flow's DAG in topological order and produces a
/// <see cref="CachePlan"/> describing which steps are fresh, stale, or
/// uncacheable.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Uniform DAG-node walk (Phase 8).</strong> Items and steps
/// are treated as a single node space. Each item has a leaf fingerprint
/// (sourced from its storage adapter's <c>ISupportsFingerprint</c>); each
/// step has a composite fingerprint composed of its <c>CodeVersion</c>
/// and its inputs' fingerprints. Both kinds of fingerprint are persisted
/// in <c>CacheManifest.{Steps, Items}</c>, and the walk compares the
/// current value against the recorded value to decide freshness.
/// </para>
/// <para>
/// <strong>Eligibility.</strong> A step is eligible for caching only
/// when every one of these holds:
/// </para>
/// <list type="bullet">
/// <item>The step's <see cref="IStepNode.CodeVersion"/> is non-null.</item>
/// <item>The step has no <see cref="IStepNode.ServiceDependencies"/>.</item>
/// <item>Every input either is produced by another step in this flow
/// (its freshness rolls down from the producer) or has a non-null
/// <see cref="IItem.TryGetFingerprint"/> that succeeds.</item>
/// </list>
/// <para>
/// <strong>Cascade rule.</strong> If any input's producer is marked
/// stale or uncacheable, the consumer cascades into the same bucket —
/// downstream of any non-fresh parent cannot be fresh because the
/// parent's outputs will be regenerated and we don't know their
/// post-run fingerprints at pre-flight time.
/// </para>
/// </remarks>
public static class CachePlanBuilder
{
  /// <summary>
  /// Compute the cache plan for <paramref name="flow"/> against
  /// <paramref name="manifest"/>. Reads each external input's
  /// fingerprint and each cacheable step's output existence; if
  /// <paramref name="manifest"/> isn't on the current schema version,
  /// every cacheable step is marked stale (forcing a re-record).
  /// </summary>
  public static async Task<CachePlan> BuildAsync(
    BuiltFlow flow,
    CacheManifest manifest,
    IServiceProfileProvider? profiles = null,
    CancellationToken cancellationToken = default
  )
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));
    if (manifest is null) throw new ArgumentNullException(nameof(manifest));

    var serviceProfiles = profiles ?? new DefaultServiceProfileProvider();

    // Schema-version mismatch: treat every recorded entry as absent so
    // every cacheable step re-records on this run.
    var recordedSteps = manifest.IsCurrentSchema()
      ? manifest.Steps
      : (IReadOnlyDictionary<string, NodeFingerprint>)
        new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal);
    var recordedItems = manifest.IsCurrentSchema()
      ? manifest.Items
      : (IReadOnlyDictionary<string, NodeFingerprint>)
        new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal);

    var fresh = new HashSet<string>(StringComparer.Ordinal);
    var stale = new HashSet<string>(StringComparer.Ordinal);
    var uncacheable = new HashSet<string>(StringComparer.Ordinal);
    var newStepFingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
    var newItemFingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
    // Per-step explanation for every label landed in `uncacheable`.
    // Without this, "step X uncacheable" was invisible to developers —
    // MagicAtlas spent ~2 hours bisecting a 7-step cascade because the
    // signal was unobservable. Populated alongside every uncacheable.Add.
    var uncacheableReasons = new Dictionary<string, StepUncacheableReason>(StringComparer.Ordinal);

    // Producer index — same shape DependencyAnalyzer builds, but we
    // store the producing step's label rather than the step itself so
    // we can look up the parent's verdict by name.
    var producerByItemLabel = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var step in flow.Steps)
    {
      foreach (var output in step.Outputs)
      {
        producerByItemLabel[output.Label] = step.Label;
      }
    }

    // Track each item's current leaf fingerprint and look it up at
    // most once per pre-flight pass — items consumed by multiple
    // steps probe disk only once.
    var itemFingerprints = new Dictionary<string, string?>(StringComparer.Ordinal);
    async Task<string?> FingerprintOnce(IItem item)
    {
      if (itemFingerprints.TryGetValue(item.Label, out var cached)) return cached;
      var fp = await TryReadFingerprintAsync(item, cancellationToken).ConfigureAwait(false);
      itemFingerprints[item.Label] = fp;
      if (fp is not null) newItemFingerprints[item.Label] = fp;
      return fp;
    }

    foreach (var step in flow.Steps)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // Phase 1 — eligibility checks that don't require I/O.
      if (step.CodeVersion is null)
      {
        uncacheable.Add(step.Label);
        uncacheableReasons[step.Label] = new StepUncacheableReason.NoCodeVersion();
        continue;
      }
      // Cache-affecting deps only. A dep is cache-neutral when it's an
      // ObservationOnly variant (e.g. ILogger — observation surfaces can't
      // change output values) OR its resolved profile declares
      // AffectsOutputs=false (e.g. the Python worker, whose determinism is
      // captured by CodeVersion rather than its identity). Without this
      // filter, declaring such a dep would uncacheabilise the step and
      // cascade through every downstream consumer.
      var cacheAffectingDeps = step.ServiceDependencies
        .Count(r => r is not ServiceDependency.ObservationOnly
                    && serviceProfiles.Resolve(r).AffectsOutputs);
      if (cacheAffectingDeps > 0)
      {
        uncacheable.Add(step.Label);
        uncacheableReasons[step.Label] = new StepUncacheableReason.HasServiceDependencies(
          cacheAffectingDeps);
        continue;
      }

      // Phase 2 — collect fingerprints for every input. If any input is
      // unfingerprintable, or its parent is non-fresh, we cascade.
      var inputContributions = new List<(string Label, string Value)>(step.Inputs.Count);
      var cascadeStale = false;
      StepUncacheableReason? cascadeReason = null;

      foreach (var input in step.Inputs)
      {
        if (producerByItemLabel.TryGetValue(input.Label, out var parentLabel))
        {
          if (uncacheable.Contains(parentLabel))
          {
            cascadeReason = new StepUncacheableReason.CascadeFromStep(parentLabel);
            break;
          }
          if (stale.Contains(parentLabel))
          {
            cascadeStale = true;
            break;
          }
        }

        var fp = await FingerprintOnce(input).ConfigureAwait(false);
        if (fp is null)
        {
          cascadeReason = new StepUncacheableReason.UnfingerprintableInput(input.Label);
          break;
        }

        // External (no producer): compare against manifest. A mismatch
        // makes the consumer stale (cascade carries it downstream).
        if (!producerByItemLabel.ContainsKey(input.Label))
        {
          var matchesRecorded =
            recordedItems.TryGetValue(input.Label, out var recordedFp)
            && string.Equals(recordedFp!.Value, fp, StringComparison.Ordinal);
          if (!matchesRecorded)
          {
            cascadeStale = true;
            break;
          }
        }

        inputContributions.Add((input.Label, fp));
      }

      if (cascadeReason is not null)
      {
        uncacheable.Add(step.Label);
        uncacheableReasons[step.Label] = cascadeReason;
        continue;
      }
      if (cascadeStale)
      {
        stale.Add(step.Label);
        continue;
      }

      // Phase 3 — derive the step's composite identity.
      var composite = ComposeStepFingerprint(step.CodeVersion!, inputContributions);

      // Phase 4 — fresh iff manifest entry matches AND every output exists.
      var manifestMatches =
        recordedSteps.TryGetValue(step.Label, out var recorded)
        && string.Equals(recorded!.Value, composite, StringComparison.Ordinal);

      var outputsExist = true;
      if (manifestMatches)
      {
        foreach (var output in step.Outputs)
        {
          var existsResult = await output.Exists().Run(cancellationToken).ConfigureAwait(false);
          var exists = existsResult is EffResult<bool>.Success ok && ok.Value;
          if (!exists) { outputsExist = false; break; }
        }
      }

      if (manifestMatches && outputsExist)
      {
        fresh.Add(step.Label);
        newStepFingerprints[step.Label] = composite;
        // Probe each output fingerprint so the post-run upsert records
        // an up-to-date Items entry alongside the refreshed Steps entry.
        // Without this, intermediate items would only get recorded the
        // very first time their producer runs — the FRESH path would
        // never refresh them, and any out-of-band touch (formatter,
        // mtime bump) on a still-cached intermediate would silently
        // stale every downstream step.
        foreach (var output in step.Outputs)
        {
          await FingerprintOnce(output).ConfigureAwait(false);
        }
      }
      else
      {
        stale.Add(step.Label);
      }
    }

    return new CachePlan(fresh, stale, uncacheable, newStepFingerprints, newItemFingerprints, uncacheableReasons);
  }

  /// <summary>
  /// Compose a step's composite fingerprint from its
  /// <c>CodeVersion</c> and its inputs' contributions. The inputs are
  /// sorted by label for stability across runs.
  /// </summary>
  internal static string ComposeStepFingerprint(
    string codeVersion,
    IReadOnlyList<(string Label, string Value)> inputs
  )
  {
    var sorted = inputs.OrderBy(c => c.Label, StringComparer.Ordinal).ToList();
    using var sha = SHA256.Create();
    var builder = new StringBuilder();
    builder.Append("code:").Append(codeVersion).Append('|');
    foreach (var (label, value) in sorted)
    {
      builder.Append(label).Append('=').Append(value).Append('|');
    }
    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
    return Convert.ToHexString(bytes).Substring(0, 16);
  }

  private static async Task<string?> TryReadFingerprintAsync(
    IItem input,
    CancellationToken cancellationToken
  )
  {
    var fingerprintIO = input.TryGetFingerprint();
    if (fingerprintIO is null) return null;
    var result = await fingerprintIO.Run(cancellationToken).ConfigureAwait(false);
    return result switch
    {
      EffResult<string>.Success ok => ok.Value,
      _ => null,
    };
  }
}
