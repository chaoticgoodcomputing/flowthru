namespace Flowthru.Core.Graph.Scheduling;

/// <summary>
/// Scheduling strategy that prioritises steps with the longest remaining critical path
/// (Highest Level First / HLF).
/// </summary>
/// <remarks>
/// <para>
/// When multiple steps are ready simultaneously, this strategy dispatches the step
/// with the greatest <see cref="FlowStep.Height"/> first — where height is the length
/// of the longest path from that step to any leaf in the DAG.
/// </para>
/// <para>
/// <strong>Rationale.</strong> Starting a high-height step unblocks more downstream
/// parallelism sooner, keeping worker threads saturated. Graham (1966) proved that any
/// list-scheduling algorithm using this priority order achieves a makespan within a
/// factor of <c>2 − 1/m</c> of optimal on <c>m</c> identical machines — the best
/// polynomial-time guarantee known for <c>P|prec|C_max</c>.
/// </para>
/// <para>
/// <strong>Prerequisites.</strong> <see cref="FlowStep.Height"/> values must have been
/// populated by <see cref="DependencyAnalyzer.ComputeHeights"/> before this strategy
/// is used. Steps with <c>Height == -1</c> are treated as height 0.
/// </para>
/// <para>
/// Steps with equal height retain their relative arrival order (stable sort), so the
/// strategy degrades gracefully to FIFO when all ready steps share the same height.
/// </para>
/// </remarks>
public sealed class CriticalPathSchedulingStrategy : ISchedulingStrategy
{
    /// <inheritdoc/>
    public IReadOnlyList<FlowStep> Prioritize(
      IReadOnlyList<FlowStep> readySteps,
      SchedulingContext context
    )
    {
        // OrderByDescending is a stable sort — equal-height steps keep arrival order.
        return readySteps.OrderByDescending(s => s.Height >= 0 ? s.Height : 0).ToList();
    }
}
