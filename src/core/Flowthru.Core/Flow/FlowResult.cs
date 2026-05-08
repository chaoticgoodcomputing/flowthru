namespace Flowthru.Flow;

/// <summary>
/// Aggregate outcome of running a <see cref="BuiltFlow"/>: per-step
/// results plus an overall pass/fail. Per §2.4, the engine emits one
/// of these per <c>Run</c> regardless of <see cref="ExecutionOptions.StopOnFirstError"/>.
/// </summary>
public sealed record FlowResult
{
  public FlowResult(IReadOnlyList<StepResult> stepResults)
  {
    StepResults = stepResults ?? throw new ArgumentNullException(nameof(stepResults));
  }

  /// <summary>Outcome for each step in declared order.</summary>
  public IReadOnlyList<StepResult> StepResults { get; }

  /// <summary>
  /// True if no <see cref="StepResults"/> entry is
  /// <see cref="StepResult.Failed"/>. A dry run (every step
  /// <see cref="StepResult.Skipped"/>) and a successful run (every
  /// step <see cref="StepResult.Succeeded"/>) both report
  /// <c>IsSuccess = true</c> — only failures break the success
  /// invariant.
  /// </summary>
  public bool IsSuccess => StepResults.All(r => r is not StepResult.Failed);

  /// <summary>True if any <see cref="StepResults"/> entry is <see cref="StepResult.Failed"/>.</summary>
  public bool HasFailures => StepResults.Any(r => r is StepResult.Failed);

  /// <summary>The first failure encountered, or <c>null</c> if every step succeeded or was skipped.</summary>
  public StepResult.Failed? FirstFailure =>
    StepResults.OfType<StepResult.Failed>().FirstOrDefault();
}
