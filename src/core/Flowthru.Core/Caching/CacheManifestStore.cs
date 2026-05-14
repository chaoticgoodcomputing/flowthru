using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Validation.Runtime;

namespace Flowthru.Caching;

/// <summary>
/// Load + upsert helpers for the framework-managed
/// <see cref="IItem{T}"/> over <see cref="CacheManifest"/>. Bridges
/// pre-flight and post-run paths in <c>FlowthruService</c> without
/// scattering raw <c>FlowIO</c> handling across the host.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Schema mismatch.</strong> A loaded manifest whose
/// <see cref="CacheManifest.SchemaVersion"/> does not equal
/// <see cref="CacheManifestSchema.CurrentVersion"/> is returned to the
/// caller as <see cref="CacheManifest.Empty"/>, with the mismatch
/// silently absorbed. Net effect: every cacheable step re-records on
/// the first run after a schema bump; no migration logic in v1.
/// </para>
/// <para>
/// <strong>Concurrent runs.</strong>
/// <see cref="UpsertEntriesAsync"/> re-loads the on-disk manifest at
/// save time and merges per-entry by greater
/// <see cref="NodeFingerprint.RecordedAt"/> before writing. Two
/// concurrent processes recording disjoint steps both have their
/// writes preserved; overlapping writes resolve to the later
/// timestamp. No file-locking required.
/// </para>
/// </remarks>
public static class CacheManifestStore
{
  /// <summary>
  /// Load the manifest, returning <see cref="CacheManifest.Empty"/>
  /// when the item doesn't exist, fails to load, or carries a stale
  /// schema version. Never throws — failures collapse to "no prior
  /// cache."
  /// </summary>
  public static async Task<CacheManifest> LoadAsync(
    IItem<CacheManifest> item,
    CancellationToken cancellationToken = default
  )
  {
    if (item is null) throw new ArgumentNullException(nameof(item));

    var existsResult = await item.Exists().Run(cancellationToken).ConfigureAwait(false);
    if (existsResult is not EffResult<bool>.Success existsOk || !existsOk.Value)
    {
      return CacheManifest.Empty;
    }

    var loadResult = await item.Load().Run(cancellationToken).ConfigureAwait(false);
    if (loadResult is EffResult<CacheManifest>.Success ok)
    {
      return ok.Value.IsCurrentSchema() ? ok.Value : CacheManifest.Empty;
    }
    return CacheManifest.Empty;
  }

  /// <summary>
  /// Upsert <paramref name="newEntries"/> into the on-disk manifest
  /// with last-write-wins per <see cref="NodeFingerprint.RecordedAt"/>.
  /// Re-loads the manifest immediately before saving so concurrent
  /// runs never drop disjoint entries.
  /// </summary>
  /// <param name="item">The framework-registered manifest item.</param>
  /// <param name="newEntries">
  /// New label → fingerprint pairs to record. Timestamps are stamped
  /// at <paramref name="recordedAt"/>; callers typically pass
  /// <see cref="DateTimeOffset.UtcNow"/>.
  /// </param>
  /// <param name="recordedAt">Timestamp to apply to every new entry.</param>
  /// <param name="cancellationToken">Cancellation.</param>
  public static async Task UpsertEntriesAsync(
    IItem<CacheManifest> item,
    IReadOnlyDictionary<string, string> newStepEntries,
    IReadOnlyDictionary<string, string> newItemEntries,
    DateTimeOffset recordedAt,
    CancellationToken cancellationToken = default
  )
  {
    if (item is null) throw new ArgumentNullException(nameof(item));
    if (newStepEntries is null) throw new ArgumentNullException(nameof(newStepEntries));
    if (newItemEntries is null) throw new ArgumentNullException(nameof(newItemEntries));
    if (newStepEntries.Count == 0 && newItemEntries.Count == 0) return;

    var current = await LoadAsync(item, cancellationToken).ConfigureAwait(false);
    var mergedSteps = MergeWithLww(current.Steps, newStepEntries, recordedAt);
    var mergedItems = MergeWithLww(current.Items, newItemEntries, recordedAt);

    var updated = new CacheManifest(
      CacheManifestSchema.CurrentVersion,
      mergedSteps,
      mergedItems
    );
    // Save failures are non-fatal — the run already succeeded; a missed
    // cache write is at worst a redundant rerun next time.
    _ = await item.Save(updated).Run(cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Per-entry last-write-wins merge: each incoming pair replaces the
  /// existing entry iff its timestamp is greater (or no entry exists).
  /// Disjoint keys from the existing dict are preserved unchanged.
  /// </summary>
  private static Dictionary<string, NodeFingerprint> MergeWithLww(
    IReadOnlyDictionary<string, NodeFingerprint> existing,
    IReadOnlyDictionary<string, string> incoming,
    DateTimeOffset recordedAt
  )
  {
    var merged = new Dictionary<string, NodeFingerprint>(existing, StringComparer.Ordinal);
    foreach (var (label, value) in incoming)
    {
      var fresh = new NodeFingerprint(value, recordedAt);
      if (!merged.TryGetValue(label, out var prior) || fresh.RecordedAt > prior.RecordedAt)
      {
        merged[label] = fresh;
      }
    }
    return merged;
  }

  /// <summary>
  /// Walk <paramref name="flow"/> in topological order, computing the
  /// post-run per-step composite fingerprint and per-item leaf
  /// fingerprint for every eligible node touched by the run. Returns
  /// two maps — one for <c>manifest.Steps</c>, one for
  /// <c>manifest.Items</c> — that the caller passes to
  /// <see cref="UpsertEntriesAsync"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the post-run twin of
  /// <see cref="CachePlanBuilder.BuildAsync"/>: same eligibility rules,
  /// same composite derivation (Phase 8: input <em>item</em>
  /// fingerprints, not parent step composites), but no manifest
  /// comparison and no output-existence check — every item touched by
  /// the run has its current fingerprint read fresh from disk.
  /// </para>
  /// <para>
  /// Only entries for steps that appear in
  /// <paramref name="succeededStepLabels"/> are surfaced; failed or
  /// skipped steps contribute nothing. Items consumed by succeeded
  /// steps are also captured (and any output of a succeeded step), so
  /// the manifest fully describes the post-run node state.
  /// </para>
  /// </remarks>
  public static async Task<PostRunFingerprints>
    ComputePostRunFingerprintsAsync(
      BuiltFlow flow,
      IReadOnlySet<string> succeededStepLabels,
      CancellationToken cancellationToken = default
    )
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));
    if (succeededStepLabels is null) throw new ArgumentNullException(nameof(succeededStepLabels));

    var producerByItemLabel = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var step in flow.Steps)
    {
      foreach (var output in step.Outputs)
      {
        producerByItemLabel[output.Label] = step.Label;
      }
    }

    var stepComposites = new Dictionary<string, string>(StringComparer.Ordinal);
    var itemFingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
    var ineligibleStepLabels = new HashSet<string>(StringComparer.Ordinal);
    var itemFingerprintCache = new Dictionary<string, string?>(StringComparer.Ordinal);

    async Task<string?> FingerprintOnce(IItem item)
    {
      if (itemFingerprintCache.TryGetValue(item.Label, out var cached)) return cached;
      var fp = await TryReadFingerprintAsync(item, cancellationToken).ConfigureAwait(false);
      itemFingerprintCache[item.Label] = fp;
      if (fp is not null) itemFingerprints[item.Label] = fp;
      return fp;
    }

    foreach (var step in flow.Steps)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (step.CodeVersion is null || step.ServiceDependencies.Count > 0)
      {
        ineligibleStepLabels.Add(step.Label);
        continue;
      }

      var inputContributions = new List<(string Label, string Value)>(step.Inputs.Count);
      var blocked = false;

      foreach (var input in step.Inputs)
      {
        // Cascade rule: if a parent step is ineligible and we don't
        // already have a fingerprint for the item, treat the step as
        // ineligible. The item-fingerprint path handles every other
        // case uniformly.
        var fp = await FingerprintOnce(input).ConfigureAwait(false);
        if (fp is null)
        {
          blocked = true;
          break;
        }
        if (producerByItemLabel.TryGetValue(input.Label, out var parentLabel)
          && ineligibleStepLabels.Contains(parentLabel))
        {
          blocked = true;
          break;
        }
        inputContributions.Add((input.Label, fp));
      }

      if (blocked)
      {
        ineligibleStepLabels.Add(step.Label);
        continue;
      }

      stepComposites[step.Label] = CachePlanBuilder
        .ComposeStepFingerprint(step.CodeVersion!, inputContributions);

      // Also fingerprint outputs — fresh on disk now that the step ran.
      foreach (var output in step.Outputs)
      {
        await FingerprintOnce(output).ConfigureAwait(false);
      }
    }

    var stepResult = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var (label, composite) in stepComposites)
    {
      if (succeededStepLabels.Contains(label)) stepResult[label] = composite;
    }
    return new PostRunFingerprints(stepResult, itemFingerprints);
  }

  /// <summary>
  /// Result shape for
  /// <see cref="ComputePostRunFingerprintsAsync"/>: per-step composite
  /// hashes plus per-item leaf fingerprints, both keyed by label and
  /// ready to pass into <see cref="UpsertEntriesAsync"/>.
  /// </summary>
  public sealed record PostRunFingerprints(
    IReadOnlyDictionary<string, string> Steps,
    IReadOnlyDictionary<string, string> Items
  );

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
