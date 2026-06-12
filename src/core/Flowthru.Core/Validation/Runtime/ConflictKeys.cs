using Flowthru.Data.Catalog;

namespace Flowthru.Validation.Runtime;

/// <summary>
/// Canonical derivation of a step's scheduler conflict keys (ADR-0019).
/// The single source of truth shared by <c>ParallelFlowScheduler</c> (which
/// gates on these keys) and the diagnostics layer (which surfaces them as
/// conflict groups) — so what a metadata diagram shows can never drift from
/// what the scheduler actually enforces.
/// </summary>
public static class ConflictKeys
{
  /// <summary>
  /// A step's conflict-relevant dependencies: its own services
  /// (<see cref="ConflictOp.Use"/>), the items it reads
  /// (<see cref="ConflictOp.Read"/>), and the items it writes
  /// (<see cref="ConflictOp.Write"/>). An item surfaces a shared resource
  /// (a database, a rate-limited endpoint), so the step touching it
  /// inherits that resource's key.
  /// </summary>
  public static IEnumerable<(ServiceDependency Dep, ConflictOp Op)> Of(IStepNode step)
  {
    if (step is null) throw new ArgumentNullException(nameof(step));
    foreach (var dep in step.ServiceDependencies) yield return (dep, ConflictOp.Use);
    foreach (var input in step.Inputs)
      foreach (var dep in input.ServiceDependencies) yield return (dep, ConflictOp.Read);
    foreach (var output in step.Outputs)
      foreach (var dep in output.ServiceDependencies) yield return (dep, ConflictOp.Write);
  }

  /// <summary>
  /// The conflict key for a <paramref name="dep"/> touched under
  /// <paramref name="op"/>. The op-class is part of the key, so
  /// <c>Read:X</c> and <c>Write:X</c> are distinct — concurrent readers
  /// don't conflict with one writer.
  /// </summary>
  public static string KeyFor(ServiceDependency dep, ConflictOp op) =>
    $"{op}:{(dep ?? throw new ArgumentNullException(nameof(dep))).DagId}";
}
