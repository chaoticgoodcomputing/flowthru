namespace Flowthru.Core.Graph.Scheduling;

/// <summary>
/// Defines the priority ordering for ready steps in the task-graph scheduler.
/// </summary>
/// <remarks>
/// <para>
/// When multiple steps are ready to dispatch simultaneously (all dependencies satisfied),
/// the scheduler delegates to an <see cref="ISchedulingStrategy"/> to determine which
/// step should be dispatched first. This affects which steps claim a worker slot when
/// the degree of parallelism is limited.
/// </para>
/// <para>
/// Implementations receive the currently ready steps and a <see cref="SchedulingContext"/>
/// containing graph structure and any available historical data, then return the steps in
/// dispatch-priority order (highest priority first).
/// </para>
/// <para>
/// The strategy is invoked each time the dispatch loop drains the ready queue, ensuring
/// newly-unblocked steps are ranked relative to any that were already waiting.
/// </para>
/// </remarks>
public interface ISchedulingStrategy
{
  /// <summary>
  /// Returns <paramref name="readySteps"/> sorted in dispatch-priority order,
  /// highest priority first.
  /// </summary>
  /// <param name="readySteps">Steps whose dependencies have all completed and that
  /// are eligible for immediate dispatch.</param>
  /// <param name="context">Read-only graph context available to inform ordering decisions.</param>
  /// <returns>The same steps in priority order. Must contain exactly the same elements
  /// as <paramref name="readySteps"/>.</returns>
  IReadOnlyList<FlowStep> Prioritize(IReadOnlyList<FlowStep> readySteps, SchedulingContext context);
}
