using Flowthru.Flow;

namespace Flowthru.Validation.Runtime;

/// <summary>
/// A set of steps that contend on one finite-capacity conflict resource
/// (ADR-0019) — the steps the scheduler will serialize against each other
/// because they share a constrained resource under the same operation.
/// Surfaced in DAG metadata so the concurrency model is legible: a
/// diagram or manifest can show <em>why</em> two independent-looking steps
/// won't run in parallel.
/// </summary>
public sealed record ConflictGroup
{
  /// <summary>The scheduler conflict key — <c>"{Op}:{ResourceDagId}"</c>.</summary>
  public required string Key { get; init; }

  /// <summary>The operation that contends — <see cref="ConflictOp.Use"/> / <see cref="ConflictOp.Read"/> / <see cref="ConflictOp.Write"/>.</summary>
  public required ConflictOp Op { get; init; }

  /// <summary>Stable identity of the shared resource (the dependency's <see cref="IExtensionServiceDependency.DagId"/>).</summary>
  public required string ResourceDagId { get; init; }

  /// <summary>Human-readable resource name for rendering.</summary>
  public required string ResourceDisplayName { get; init; }

  /// <summary>Concurrent holders the resource admits under this op (the resolved capacity; always finite here).</summary>
  public required int Capacity { get; init; }

  /// <summary>Labels of the steps that touch this resource under this op, in flow order.</summary>
  public required IReadOnlyList<string> StepLabels { get; init; }

  /// <summary>
  /// True when more steps share the resource than its capacity admits —
  /// i.e. the group will actually serialize at runtime. A group whose
  /// member count is within capacity is declared-but-uncontended.
  /// </summary>
  public bool Serializes => StepLabels.Count > Capacity;
}

/// <summary>
/// Computes the <see cref="ConflictGroup"/>s of a flow by resolving each
/// step's <see cref="ConflictKeys"/> against an
/// <see cref="IServiceProfileProvider"/>. Only finite-capacity keys form a
/// group — an unbounded shared dependency never serializes, so it isn't a
/// conflict.
/// </summary>
public static class ConflictGroupAnalyzer
{
  /// <summary>
  /// The conflict groups in <paramref name="flow"/> under
  /// <paramref name="profiles"/>, one per finite-capacity conflict key,
  /// ordered by key. Returns an empty list when no resource declares a
  /// finite capacity (the default — gating is opt-in per resource).
  /// </summary>
  public static IReadOnlyList<ConflictGroup> Analyze(BuiltFlow flow, IServiceProfileProvider profiles)
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));
    if (profiles is null) throw new ArgumentNullException(nameof(profiles));

    // key -> accumulating group state. Capacity meets to the minimum if
    // sources disagree (mirrors the scheduler's capacityByKey).
    var byKey = new Dictionary<string, Builder>(StringComparer.Ordinal);

    foreach (var step in flow.Steps)
    {
      // A step can reach one key through several deps (e.g. two inputs on
      // one database) — only record it once per key.
      var seenForStep = new HashSet<string>(StringComparer.Ordinal);
      foreach (var (dep, op) in ConflictKeys.Of(step))
      {
        var capacity = profiles.Resolve(dep).CapacityFor(op);
        if (capacity >= int.MaxValue) continue; // unbounded — not a conflict
        var key = ConflictKeys.KeyFor(dep, op);
        if (!byKey.TryGetValue(key, out var builder))
        {
          builder = new Builder(key, op, dep.DagId, dep.DisplayName, capacity);
          byKey[key] = builder;
        }
        else
        {
          builder.Capacity = Math.Min(builder.Capacity, capacity);
        }
        if (seenForStep.Add(key)) builder.StepLabels.Add(step.Label);
      }
    }

    return byKey.Values
      .OrderBy(b => b.Key, StringComparer.Ordinal)
      .Select(b => new ConflictGroup
      {
        Key = b.Key,
        Op = b.Op,
        ResourceDagId = b.ResourceDagId,
        ResourceDisplayName = b.ResourceDisplayName,
        Capacity = b.Capacity,
        StepLabels = b.StepLabels,
      })
      .ToList();
  }

  private sealed class Builder(string key, ConflictOp op, string dagId, string displayName, int capacity)
  {
    public string Key { get; } = key;
    public ConflictOp Op { get; } = op;
    public string ResourceDagId { get; } = dagId;
    public string ResourceDisplayName { get; } = displayName;
    public int Capacity { get; set; } = capacity;
    public List<string> StepLabels { get; } = new();
  }
}
