namespace Flowthru.Validation;

/// <summary>
/// Closed sum of every way pre-flight validation can fail. Used as the error
/// type of <see cref="Prelude.Validated{TError, TValue}"/> for pre-flight
/// checks; multiple cases can accumulate into a single Invalid result so the
/// user sees every problem at once instead of one per re-run.
/// </summary>
/// <remarks>
/// <para>
/// The hierarchy is closed via the private constructor — no derived case can
/// be added outside this file. Add new cases here when adding a new
/// pre-flight check; consumers pattern-match exhaustively.
/// </para>
/// <para>
/// PreFlightError covers the failure modes the system can detect <em>before
/// any pipeline logic runs</em>. Anything that can only be discovered during
/// step execution belongs in <see cref="Error.RuntimeError"/>.
/// </para>
/// </remarks>
public abstract record PreFlightError
{
  private PreFlightError() { }

  /// <summary>Human-readable description of the failure.</summary>
  public abstract string Message { get; }

  /// <summary>
  /// A catalog item is written to by more than one step. Flowthru's
  /// single-producer rule — a catalog item has at most one step that
  /// produces it — is mechanised here.
  /// </summary>
  public sealed record DuplicateProducer(string ItemId, IReadOnlyList<string> StepIds)
    : PreFlightError
  {
    public override string Message =>
      $"Catalog item '{ItemId}' has multiple producers: {string.Join(", ", StepIds)}";
  }

  /// <summary>
  /// The flow's DAG contains a cycle. <see cref="Cycle"/> lists the step IDs
  /// involved in dependency order so the user can see the loop.
  /// </summary>
  public sealed record CircularDependency(IReadOnlyList<string> Cycle) : PreFlightError
  {
    public override string Message => $"Circular dependency: {string.Join(" → ", Cycle)}";
  }

  /// <summary>
  /// A required external input is not accessible. Detected by inspecting
  /// each external input's storage medium before any step runs — even if
  /// the input is consumed only by the last step in the flow.
  /// </summary>
  public sealed record MissingInput(string ItemId, string Source) : PreFlightError
  {
    public override string Message =>
      $"Required input '{ItemId}' is not accessible at '{Source}'";
  }

  /// <summary>
  /// An external input's schema does not match the schema declared by the
  /// catalog item that consumes it.
  /// </summary>
  public sealed record SchemaDrift(string ItemId, string Expected, string Actual)
    : PreFlightError
  {
    public override string Message =>
      $"Schema drift on '{ItemId}': expected {Expected}, found {Actual}";
  }
}
