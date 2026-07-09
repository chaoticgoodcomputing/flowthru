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
  /// The maximal validation that reaches <em>nothing outside the
  /// process</em>: dispatcher presence for external service refs, C#
  /// service-dependency DI registration, and registration/flow hooks that
  /// classify as hermetic. No socket, no data file, no external database
  /// or service — nothing whose availability or state could differ
  /// between environments — answers "is this flow sound and fully wired,
  /// could it start in principle?" on a machine with no live environment
  /// at all. Has no <c>IItem</c> inspection mapping (inspection reads
  /// data, which is I/O).
  /// </summary>
  /// <remarks>
  /// The promise is <em>no external reach</em>, not "no syscalls":
  /// process-local computation over declared metadata qualifies even when
  /// it loads code shipped inside the application's own deployment — the
  /// CLR loading an assembly, or an <em>embedded in-memory engine</em>
  /// (e.g. DuckDB) instantiated purely to type-check declared schemas,
  /// touching no real data and no state outside the process. An embedded
  /// engine that reads or writes any file, or reaches any endpoint, is
  /// <em>not</em> hermetic and must classify <see cref="Shallow"/> or
  /// above.
  /// </remarks>
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
