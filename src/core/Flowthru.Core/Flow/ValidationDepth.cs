namespace Flowthru.Flow;

/// <summary>
/// How far pre-flight reaches before a flow runs. The ladder's
/// organizing axis is <em>whether pre-flight performs I/O</em>, not how
/// much data it reads: <see cref="Hermetic"/> validates everything
/// knowable without touching an external resource; <see cref="Shallow"/>
/// and <see cref="Deep"/> additionally probe live resources, and only
/// <em>those</em> two materialise the row-depth levels exposed on
/// <c>IItem</c>'s <c>InspectShallow</c>/<c>InspectDeep</c>/<c>InspectTarget</c>.
/// </summary>
/// <remarks>
/// Assembling the merged DAG and checking it for cycles, the
/// single-producer law, and label uniqueness is a <em>plan-build
/// precondition</em> — a cyclic graph is physically un-runnable — so it
/// happens at every level (including <see cref="None"/>) and its failures
/// surface as <c>FlowResult</c> data regardless of depth. This enum grades
/// only the introspection performed <em>above</em> that precondition.
/// Orthogonal to <see cref="DryRunOption"/>: a smoke test is
/// <see cref="DryRunOption.On"/> paired with <see cref="Hermetic"/>.
/// </remarks>
public enum ValidationDepth
{
  /// <summary>
  /// No introspection above plan-build — go straight to execution.
  /// </summary>
  None,

  /// <summary>
  /// The maximal validation that performs <em>zero I/O</em>: dispatcher
  /// presence for external service refs, C# service-dependency DI
  /// registration, and registration hooks that classify as hermetic. No
  /// socket, file, or database access — answers "is this flow sound and
  /// fully wired, could it start in principle?" Has no <c>IItem</c>
  /// inspection mapping (inspection reads data, which is I/O).
  /// </summary>
  Hermetic,

  /// <summary>
  /// <see cref="Hermetic"/> plus live-resource probes: existence +
  /// small-sample schema check on inputs, connectivity checks, etc.
  /// (default).
  /// </summary>
  Shallow,

  /// <summary>
  /// <see cref="Hermetic"/> plus full-dataset schema validation on inputs.
  /// </summary>
  Deep,
}
