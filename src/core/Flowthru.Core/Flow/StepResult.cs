namespace Flowthru.Flow;

/// <summary>
/// Per-step outcome inside a <see cref="FlowResult"/>. A closed sum:
/// either the step ran to completion, was skipped (e.g., due to a
/// dry run or because an upstream failure stopped the engine), or
/// failed with a <see cref="RuntimeError"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Timing.</strong> <see cref="Succeeded"/> and
/// <see cref="Failed"/> carry the wall-clock <c>Duration</c>
/// the scheduler measured for the step's load → transform → save
/// chain. <see cref="Skipped"/> has no duration — by definition the
/// step did not run, so a duration would be misleading. Diagnostic
/// providers (Mermaid heat-map, RunSummary, StepTimings) consume
/// this directly; consumers that don't care about timing can ignore
/// it.
/// </para>
/// </remarks>
public abstract record StepResult
{
  private StepResult() { }

  /// <summary>The step's label.</summary>
  public abstract string StepLabel { get; }

  /// <summary>The step ran to completion.</summary>
  public sealed record Succeeded(string StepLabel, TimeSpan Duration) : StepResult
  {
    public override string StepLabel { get; } = StepLabel;
  }

  /// <summary>
  /// The step did not run — either dry-run mode is on, or the
  /// engine stopped earlier and skipped this step under
  /// fail-fast semantics.
  /// </summary>
  public sealed record Skipped(string StepLabel, string Reason) : StepResult
  {
    public override string StepLabel { get; } = StepLabel;
  }

  /// <summary>The step's transform (or its load/save plumbing) failed.</summary>
  public sealed record Failed(string StepLabel, RuntimeError Error, TimeSpan Duration) : StepResult
  {
    public override string StepLabel { get; } = StepLabel;
  }

  /// <summary>True if this is a <see cref="Succeeded"/>.</summary>
  public bool IsSuccess => this is Succeeded;

  /// <summary>True if this is a <see cref="Failed"/>.</summary>
  public bool IsFailure => this is Failed;
}
