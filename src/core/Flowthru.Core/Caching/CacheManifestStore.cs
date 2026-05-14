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
    IReadOnlyDictionary<string, string> newEntries,
    DateTimeOffset recordedAt,
    CancellationToken cancellationToken = default
  )
  {
    if (item is null) throw new ArgumentNullException(nameof(item));
    if (newEntries is null) throw new ArgumentNullException(nameof(newEntries));
    if (newEntries.Count == 0) return;

    var current = await LoadAsync(item, cancellationToken).ConfigureAwait(false);
    var merged = new Dictionary<string, NodeFingerprint>(current.Entries, StringComparer.Ordinal);

    foreach (var (label, value) in newEntries)
    {
      var fresh = new NodeFingerprint(value, recordedAt);
      // Last-write-wins: only replace if our timestamp is greater.
      if (!merged.TryGetValue(label, out var existing) || fresh.RecordedAt > existing.RecordedAt)
      {
        merged[label] = fresh;
      }
    }

    var updated = new CacheManifest(CacheManifestSchema.CurrentVersion, merged);
    // Save failures are non-fatal — the run already succeeded; a missed
    // cache write is at worst a redundant rerun next time.
    _ = await item.Save(updated).Run(cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Walk <paramref name="flow"/> in topological order, computing the
  /// post-run composite fingerprint for every eligible step whose
  /// label appears in <paramref name="succeededStepLabels"/>. Returns
  /// the label → composite map suitable for
  /// <see cref="UpsertEntriesAsync"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the post-run twin of
  /// <see cref="CachePlanBuilder.BuildAsync"/>: same eligibility rules,
  /// same composite derivation, but no manifest comparison and no
  /// output-existence check (the step already ran). External inputs
  /// re-fingerprint here because intermediate items produced by stale
  /// steps may have changed during the run.
  /// </para>
  /// </remarks>
  public static async Task<IReadOnlyDictionary<string, string>>
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
    var ineligibleStepLabels = new HashSet<string>(StringComparer.Ordinal);
    var externalFingerprintCache = new Dictionary<string, string?>(StringComparer.Ordinal);

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
        if (producerByItemLabel.TryGetValue(input.Label, out var parentLabel))
        {
          if (ineligibleStepLabels.Contains(parentLabel))
          {
            blocked = true;
            break;
          }
          if (!stepComposites.TryGetValue(parentLabel, out var parentComposite))
          {
            blocked = true;
            break;
          }
          inputContributions.Add((input.Label, parentComposite));
        }
        else
        {
          if (!externalFingerprintCache.TryGetValue(input.Label, out var cachedFp))
          {
            cachedFp = await TryReadFingerprintAsync(input, cancellationToken)
              .ConfigureAwait(false);
            externalFingerprintCache[input.Label] = cachedFp;
          }
          if (cachedFp is null)
          {
            blocked = true;
            break;
          }
          inputContributions.Add((input.Label, cachedFp));
        }
      }

      if (blocked)
      {
        ineligibleStepLabels.Add(step.Label);
        continue;
      }

      stepComposites[step.Label] = CachePlanBuilder
        .ComposeStepFingerprint(step.CodeVersion!, inputContributions);
    }

    // Only return entries for steps that actually ran successfully.
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var (label, composite) in stepComposites)
    {
      if (succeededStepLabels.Contains(label)) result[label] = composite;
    }
    return result;
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
