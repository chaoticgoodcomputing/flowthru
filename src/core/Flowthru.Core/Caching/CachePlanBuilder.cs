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
/// <strong>Eligibility.</strong> A step is eligible for caching only
/// when every one of these holds:
/// </para>
/// <list type="bullet">
/// <item>The step's <see cref="IStepNode.CodeVersion"/> is non-null.</item>
/// <item>The step has no <see cref="IStepNode.ServiceDependencies"/>.</item>
/// <item>Every input either is produced by another step in this flow
/// (its composite is then derived recursively) or has a non-null
/// <see cref="IItem.TryGetFingerprint"/> that succeeds.</item>
/// </list>
/// <para>
/// <strong>Cascade rule.</strong> If any input's producer is marked
/// stale or uncacheable, the consumer cascades into stale —
/// downstream of any non-fresh parent cannot be fresh because the
/// parent's outputs will be regenerated and we don't know their
/// post-run fingerprints at pre-flight time.
/// </para>
/// <para>
/// <strong>Composite identity.</strong> A fresh step's composite is
/// <c>SHA256(CodeVersion + "|" + sorted(input-label:fingerprint))</c>.
/// For external inputs, the fingerprint is the leaf
/// <see cref="IItem.TryGetFingerprint"/>; for produced inputs, it's
/// the upstream step's composite. The plan records these composites
/// in <see cref="CachePlan.NewFingerprints"/> so the post-run upsert
/// path can refresh manifest entries without recomputing.
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
    CancellationToken cancellationToken = default
  )
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));
    if (manifest is null) throw new ArgumentNullException(nameof(manifest));

    // Schema-version mismatch: treat the manifest as empty so every
    // cacheable step re-records on this run.
    var effectiveEntries = manifest.IsCurrentSchema()
      ? manifest.Entries
      : (IReadOnlyDictionary<string, NodeFingerprint>)
        new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal);

    var fresh = new HashSet<string>(StringComparer.Ordinal);
    var stale = new HashSet<string>(StringComparer.Ordinal);
    var uncacheable = new HashSet<string>(StringComparer.Ordinal);
    var newFingerprints = new Dictionary<string, string>(StringComparer.Ordinal);

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

    // Fingerprint every external input once and cache the result.
    var externalFingerprintCache = new Dictionary<string, string?>(StringComparer.Ordinal);

    foreach (var step in flow.Steps)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // Phase 1 — eligibility checks that don't require I/O.
      if (step.CodeVersion is null || step.ServiceDependencies.Count > 0)
      {
        uncacheable.Add(step.Label);
        continue;
      }

      // Phase 2 — collect fingerprints for every input. If any input is
      // unfingerprintable or its parent is non-fresh, we cascade.
      var inputContributions = new List<(string Label, string Value)>(step.Inputs.Count);
      var cascadeStale = false;
      var cascadeUncacheable = false;

      foreach (var input in step.Inputs)
      {
        if (producerByItemLabel.TryGetValue(input.Label, out var parentLabel))
        {
          // Internally produced — rely on the parent's verdict.
          if (uncacheable.Contains(parentLabel))
          {
            cascadeUncacheable = true;
            break;
          }
          if (stale.Contains(parentLabel))
          {
            cascadeStale = true;
            break;
          }
          if (!fresh.Contains(parentLabel) || !newFingerprints.TryGetValue(parentLabel, out var parentComposite))
          {
            // Shouldn't happen given topological order, but defend.
            cascadeUncacheable = true;
            break;
          }
          inputContributions.Add((input.Label, parentComposite));
        }
        else
        {
          // External root — fingerprint via the item's leaf capability.
          if (!externalFingerprintCache.TryGetValue(input.Label, out var cachedFp))
          {
            cachedFp = await TryReadFingerprintAsync(input, cancellationToken)
              .ConfigureAwait(false);
            externalFingerprintCache[input.Label] = cachedFp;
          }
          if (cachedFp is null)
          {
            cascadeUncacheable = true;
            break;
          }
          inputContributions.Add((input.Label, cachedFp));
        }
      }

      if (cascadeUncacheable)
      {
        uncacheable.Add(step.Label);
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
        effectiveEntries.TryGetValue(step.Label, out var recorded)
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
        newFingerprints[step.Label] = composite;
      }
      else
      {
        // Stale: composite differs from recorded, OR an output is missing.
        // We don't record a new composite at pre-flight — the post-run
        // upsert path will compute it from the actually-produced outputs.
        stale.Add(step.Label);
      }
    }

    return new CachePlan(fresh, stale, uncacheable, newFingerprints);
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
