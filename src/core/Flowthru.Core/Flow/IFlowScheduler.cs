namespace Flowthru.Flow;

/// <summary>
/// The interpretation surface for "how to execute a built flow."
/// Core ships exactly one implementation —
/// <see cref="ParallelFlowScheduler"/> — the same way it ships one
/// format serializer (<c>JsonFormatSerializer</c>) and one step
/// archetype (the <c>[FlowthruStep]</c> C# Func factory). Extensions
/// can register alternative <see cref="IFlowScheduler"/> implementations
/// (e.g., a Dataflow-based scheduler, a remote scheduler that
/// delegates to a worker pool) by replacing the registration during
/// host wiring.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.4 / Phase 7.5, scheduling is the algebra interpretation
/// extension point. The <see cref="BuiltFlow"/> description plus
/// <see cref="ExecutionOptions"/> configuration is everything a
/// scheduler needs; the result is a <see cref="FlowResult"/>
/// aggregating per-step outcomes.
/// </para>
/// <para>
/// Implementations should be stateless (or carry only configuration)
/// — the same scheduler instance may run many flows over its
/// lifetime. Per-run state lives on the stack of
/// <see cref="ExecuteAsync"/>.
/// </para>
/// </remarks>
public interface IFlowScheduler
{
  /// <summary>
  /// Execute <paramref name="flow"/> under <paramref name="options"/>.
  /// Returns a <see cref="FlowResult"/> regardless of outcome —
  /// step failures become <see cref="StepResult.Failed"/> entries,
  /// not exceptions.
  /// </summary>
  Task<FlowResult> ExecuteAsync(
    BuiltFlow flow,
    ExecutionOptions options,
    CancellationToken cancellationToken = default
  );
}
