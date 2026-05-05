namespace Flowthru.Error;

/// <summary>
/// Closed sum of every way Flowthru execution can fail at runtime. The failure
/// type of <see cref="Prelude.Eff{TRuntime, T}"/>; consumers pattern-match on
/// the cases to distinguish user-environment failures, step failures,
/// cancellation, and Flowthru bugs (invariant violations).
/// </summary>
/// <remarks>
/// <para>
/// The hierarchy is closed via the private constructor — no derived case can
/// be added outside this file. Pattern-match exhaustively; new cases added
/// here will surface as compile warnings at every consumer until handled.
/// </para>
/// <para>
/// <see cref="InvariantViolated"/> deserves special handling: its presence at
/// runtime means a pre-flight check that should have caught the condition was
/// missing or wrong. CONTRIBUTING.md's invariant — "a flow that passes
/// pre-flight should always complete successfully" — is materialised here.
/// Surfacing this case as a typed value (rather than an untyped exception)
/// is what makes that invariant operationally checkable.
/// </para>
/// </remarks>
public abstract record RuntimeError
{
  private RuntimeError() { }

  /// <summary>Human-readable description of the failure.</summary>
  public abstract string Message { get; }

  /// <summary>
  /// A failure originating outside Flowthru — network drop, OOM, disk full,
  /// permission denied, malformed external input encountered mid-stream.
  /// The <see cref="Cause"/> is the underlying exception captured at the
  /// boundary by <see cref="Prelude.Eff{TRuntime, T}.Lift"/> /
  /// <see cref="Prelude.Eff{TRuntime, T}.LiftAsync"/>.
  /// </summary>
  public sealed record External(string Source, Exception Cause) : RuntimeError
  {
    public override string Message => $"External failure in '{Source}': {Cause.Message}";
  }

  /// <summary>
  /// A specific step's transform raised the contained <see cref="Cause"/>.
  /// Used to attribute a deeper RuntimeError to the step that produced it
  /// while preserving the original cause for diagnostic display.
  /// </summary>
  public sealed record StepFailed(string StepId, RuntimeError Cause) : RuntimeError
  {
    public override string Message => $"Step '{StepId}' failed: {Cause.Message}";
  }

  /// <summary>
  /// The flow was cancelled via <see cref="CancellationToken"/>. Distinct
  /// from <see cref="External"/> because cancellation is a control-flow
  /// signal, not a failure mode the user can fix in their flow code.
  /// </summary>
  public sealed record Cancelled(string Reason) : RuntimeError
  {
    public override string Message => $"Cancelled: {Reason}";
  }

  /// <summary>
  /// A pre-flight invariant was violated at runtime — i.e. a flow that
  /// passed pre-flight failed during execution in a way pre-flight should
  /// have predicted. <strong>This indicates a bug in Flowthru itself</strong>
  /// (a missing pre-flight check or a missing compile-time constraint), not
  /// in the user's flow. Surfacing this as a distinct case lets reporting
  /// code render it with a "please file an issue" affordance rather than a
  /// generic stack trace.
  /// </summary>
  public sealed record InvariantViolated(string CheckName, string Detail) : RuntimeError
  {
    public override string Message =>
      $"Pre-flight invariant '{CheckName}' violated at runtime — this is a bug in Flowthru. {Detail}";
  }
}
