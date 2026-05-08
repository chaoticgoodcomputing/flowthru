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
  /// Run every registered <see cref="IRegistrationValidationHook"/>.
  /// The first <see cref="RunAsync"/> call invokes this internally;
  /// callers can also invoke it during <c>Main</c> for fail-fast-at-
  /// startup behaviour.
  /// </summary>
  /// <remarks>
  /// Successful hooks are cached after the first pass — re-running is
  /// a no-op. Failed hooks re-run on each call so transient failures
  /// eventually clear without requiring a process restart.
  /// </remarks>
  Task<Validated<PreFlightError, FlowUnit>> ValidateRegistrationAsync(
    CancellationToken cancellationToken = default
  );

  /// <summary>The labels of every registered flow in declaration order.</summary>
  IReadOnlyList<string> RegisteredFlowLabels { get; }
}
