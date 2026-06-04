using Flowthru.Flow;
using Flowthru.Step;

namespace Flowthru.Validation.Runtime;

/// <summary>One step's use of a service, under a given conflict op.</summary>
public sealed record ServiceUsageMember(string StepLabel, ConflictOp Op);

/// <summary>
/// A distinct service used across a flow, with its resolved
/// <see cref="ServiceProfile"/> and the steps that touch it (ADR-0019). The
/// unified signal behind DAG metadata — the Mermaid services legend and the
/// JSON <c>services</c> array both project this. A "conflict group" is the
/// derived view: a service whose capacity is finite and exceeded by the
/// steps using it (<see cref="Serializes"/>).
/// </summary>
public sealed record ServiceUsage
{
  /// <summary>Stable resource identity (the dependency's <see cref="IExtensionServiceDependency.DagId"/>).</summary>
  public required string DagId { get; init; }

  /// <summary>Human-readable resource name for rendering.</summary>
  public required string DisplayName { get; init; }

  /// <summary>Distinct ops this service is touched under, ordered Use, Read, Write.</summary>
  public required IReadOnlyList<ConflictOp> Ops { get; init; }

  /// <summary>Concurrent holders for <see cref="ConflictOp.Use"/>/<see cref="ConflictOp.Write"/> (the profile capacity). <see cref="int.MaxValue"/> = unbounded.</summary>
  public required int WriteCapacity { get; init; }

  /// <summary>Concurrent holders for <see cref="ConflictOp.Read"/>. <see cref="int.MaxValue"/> = unbounded.</summary>
  public required int ReadCapacity { get; init; }

  /// <summary>
  /// Whether using this service keeps dependent steps cacheable (it's
  /// cache-neutral). <c>null</c> when the service is reached only through
  /// items (Read/Write) — cacheability is a step-injected-service concern,
  /// so it doesn't apply to an item's backing resource.
  /// </summary>
  public required bool? Cacheable { get; init; }

  /// <summary>The steps that touch this service, with the op each uses.</summary>
  public required IReadOnlyList<ServiceUsageMember> UsedBy { get; init; }

  /// <summary>
  /// True when this service is an <see cref="ServiceDependency.ObservationOnly"/>
  /// surface (e.g. <c>ILogger</c>) — it can't change outputs or contend on
  /// concurrency, so it's pure observation. Consumers that render a
  /// readability-first view (the Mermaid diagram) filter these out.
  /// </summary>
  public required bool IsObservationOnly { get; init; }

  /// <summary>
  /// True when a finite capacity is exceeded by the steps using this service
  /// under that op — the scheduler will serialize them. The
  /// declared-but-uncontended case (members within capacity) is false.
  /// </summary>
  public bool Serializes =>
    UsedBy.GroupBy(m => m.Op).Any(g =>
    {
      var capacity = g.Key == ConflictOp.Read ? ReadCapacity : WriteCapacity;
      return capacity < int.MaxValue
        && g.Select(m => m.StepLabel).Distinct(StringComparer.Ordinal).Count() > capacity;
    });
}

/// <summary>
/// Aggregates a flow's <see cref="ServiceUsage"/>s by resolving every step's
/// <see cref="ConflictKeys"/> against an <see cref="IServiceProfileProvider"/>.
/// Unlike the scheduler (which keeps only finite-capacity keys for gating),
/// this keeps <em>every</em> service so the metadata layer can render a
/// complete legend — capacity and cacheability then tell the reader which
/// ones actually constrain anything.
/// </summary>
public static class ServiceUsageAnalyzer
{
  /// <summary>
  /// Every distinct service used in <paramref name="flow"/> under
  /// <paramref name="profiles"/>, ordered by resource identity. Empty when
  /// the flow declares no service dependencies.
  /// </summary>
  public static IReadOnlyList<ServiceUsage> Analyze(BuiltFlow flow, IServiceProfileProvider profiles)
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));
    return Analyze(flow.Steps, profiles);
  }

  /// <summary>
  /// As <see cref="Analyze(BuiltFlow, IServiceProfileProvider)"/>, over an
  /// explicit set of steps — used to scope the analysis to one flow's local
  /// steps in a per-flow diagram.
  /// </summary>
  public static IReadOnlyList<ServiceUsage> Analyze(
    IEnumerable<IStepNode> steps, IServiceProfileProvider profiles)
  {
    if (steps is null) throw new ArgumentNullException(nameof(steps));
    if (profiles is null) throw new ArgumentNullException(nameof(profiles));

    var byId = new Dictionary<string, Builder>(StringComparer.Ordinal);

    foreach (var step in steps)
    {
      foreach (var (dep, op) in ConflictKeys.Of(step))
      {
        var profile = profiles.Resolve(dep);
        if (!byId.TryGetValue(dep.DagId, out var b))
        {
          b = new Builder(dep.DagId, dep.DisplayName, profile.Capacity, profile.ReadCapacity);
          byId[dep.DagId] = b;
        }
        else
        {
          // If sources disagree on a resource's capacity, the most
          // restrictive wins — same meet the scheduler applies.
          b.WriteCapacity = Math.Min(b.WriteCapacity, profile.Capacity);
          b.ReadCapacity = Math.Min(b.ReadCapacity, profile.ReadCapacity);
        }
        b.Ops.Add(op);
        b.Members.Add(new ServiceUsageMember(step.Label, op));
        if (dep is ServiceDependency.ObservationOnly) b.IsObservationOnly = true;
        if (op == ConflictOp.Use)
        {
          // Cacheability only applies to step-injected services. Cache-
          // neutral when the dep is observation-only (e.g. a logger) or its
          // profile declares it doesn't affect outputs (e.g. the Python
          // worker, fingerprinted by CodeVersion).
          b.Cacheable = dep is ServiceDependency.ObservationOnly || !profile.AffectsOutputs;
        }
      }
    }

    return byId.Values
      .OrderBy(b => b.DagId, StringComparer.Ordinal)
      .Select(b => new ServiceUsage
      {
        DagId = b.DagId,
        DisplayName = b.DisplayName,
        Ops = b.Ops.OrderBy(o => o).ToList(),
        WriteCapacity = b.WriteCapacity,
        ReadCapacity = b.ReadCapacity,
        Cacheable = b.Cacheable,
        IsObservationOnly = b.IsObservationOnly,
        UsedBy = b.Members
          .OrderBy(m => m.StepLabel, StringComparer.Ordinal)
          .ThenBy(m => m.Op)
          .ToList(),
      })
      .ToList();
  }

  private sealed class Builder(string dagId, string displayName, int writeCapacity, int readCapacity)
  {
    public string DagId { get; } = dagId;
    public string DisplayName { get; } = displayName;
    public int WriteCapacity { get; set; } = writeCapacity;
    public int ReadCapacity { get; set; } = readCapacity;
    public bool? Cacheable { get; set; }
    public bool IsObservationOnly { get; set; }
    public HashSet<ConflictOp> Ops { get; } = new();
    public HashSet<ServiceUsageMember> Members { get; } = new();
  }
}
