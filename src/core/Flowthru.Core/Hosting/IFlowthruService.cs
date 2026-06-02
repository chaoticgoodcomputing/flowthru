using Flowthru.Flow;

namespace Flowthru.Hosting;

/// <summary>
/// Runtime-side façade over a configured Flowthru host. Resolved
/// from DI; carries the materialised catalog, the flow registry,
/// and the orchestration logic for pre-flight + execution +
/// post-run metadata.
/// </summary>
/// <remarks>
/// <para>
/// End users hold an <see cref="IFlowthruService"/> via DI rather
/// than naming a concrete <c>FlowthruService</c>. That separation
/// keeps the public surface small and lets test fixtures supply
/// their own implementation.
/// </para>
/// </remarks>
public interface IFlowthruService
{
  /// <summary>
  /// Run the merged DAG of every registered flow. When
  /// <paramref name="flowLabel"/> is non-null, the merged DAG is
  /// sliced (per §2.4) to the subgraph reachable from that label's
  /// declared output items via <c>FlowSliceStrategy</c>; a null
  /// label runs the entire merged DAG.
  /// </summary>
  /// <remarks>
  /// Pre-flight runs first (per <paramref name="options"/>); on
  /// success, execution proceeds and the
  /// <see cref="FlowResult"/> is returned. Pre-flight failures
  /// return a <see cref="FlowResult"/> whose first
  /// <see cref="StepResult.Failed"/> wraps the aggregated
  /// <see cref="PreFlightError"/> as a
  /// <see cref="RuntimeError.InvariantViolated"/> — calling code
  /// can pattern-match the <see cref="FlowResult"/> uniformly.
  /// </remarks>
  Task<FlowResult> RunAsync(
    string? flowLabel = null,
    ExecutionOptions? options = null,
    CancellationToken cancellationToken = default
  );

  /// <summary>
  /// Run the merged DAG sliced by <paramref name="strategy"/>. The
  /// strategy supports the closed-sum algebra (<c>From</c>, <c>To</c>,
  /// <c>Only</c>, <c>Flows</c>, <c>All</c>, <c>None</c>, <c>And</c>,
  /// <c>Or</c>) and may use glob wildcards in step / item labels.
  /// </summary>
  Task<FlowResult> RunAsync(
    FlowSliceStrategy strategy,
    ExecutionOptions? options = null,
    CancellationToken cancellationToken = default
  );

  /// <summary>
  /// Run every registered <see cref="IRegistrationValidationHook"/> whose
  /// <see cref="IRegistrationValidationHook.MinimumDepth"/> is at or below
  /// <paramref name="depth"/>. The first <c>RunAsync</c> call invokes this
  /// internally at the run's depth; callers can also invoke it during
  /// <c>Main</c> for fail-fast-at-startup behaviour.
  /// </summary>
  /// <param name="depth">
  /// The depth to validate at — hooks above it are skipped. Defaults to
  /// <see cref="ValidationDepth.Shallow"/> (run every hook). Pass
  /// <see cref="ValidationDepth.Hermetic"/> to run only the zero-I/O wiring
  /// hooks, e.g. an offline startup check.
  /// </param>
  /// <remarks>
  /// The result is cached per the highest depth that has succeeded — a
  /// repeat call at that depth or lower is a no-op; a deeper call re-runs
  /// the hooks its depth newly admits. Failed hooks re-run on each call so
  /// transient failures eventually clear without requiring a process restart.
  /// </remarks>
  Task<Validated<PreFlightError, FlowUnit>> ValidateRegistrationAsync(
    ValidationDepth depth = ValidationDepth.Shallow,
    CancellationToken cancellationToken = default
  );

  /// <summary>The labels of every registered flow in declaration order.</summary>
  IReadOnlyList<string> RegisteredFlowLabels { get; }
}
