namespace Flowthru.Core.Graph.Scheduling;

/// <summary>
/// Scheduling strategy that preserves arrival order (first-in, first-out).
/// </summary>
/// <remarks>
/// Equivalent to the behaviour of the original <c>ConcurrentQueue</c>-based
/// dispatcher: steps become eligible for dispatch in the order their last
/// dependency completes, and that order is preserved when claiming worker slots.
/// </remarks>
public sealed class FifoSchedulingStrategy : ISchedulingStrategy
{
    /// <inheritdoc/>
    public IReadOnlyList<FlowStep> Prioritize(
      IReadOnlyList<FlowStep> readySteps,
      SchedulingContext context
    ) => readySteps;
}
