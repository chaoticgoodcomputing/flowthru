namespace Flowthru.Core.Graph.Scheduling;

/// <summary>
/// Read-only graph context passed to <see cref="ISchedulingStrategy.Prioritize"/> on
/// each dispatch cycle.
/// </summary>
/// <remarks>
/// Carries structural information about the DAG that strategies may use to make ordering
/// decisions. Designed to be extended: future fields (e.g., historical step durations)
/// can be added here without changing the <see cref="ISchedulingStrategy"/> signature.
/// </remarks>
/// <param name="Dependents">
/// Reverse adjacency map: for each step, the list of steps that depend on it.
/// A step with an empty list is a sink (no descendants).
/// </param>
public sealed record SchedulingContext(
  IReadOnlyDictionary<FlowStep, IReadOnlyList<FlowStep>> Dependents
);
