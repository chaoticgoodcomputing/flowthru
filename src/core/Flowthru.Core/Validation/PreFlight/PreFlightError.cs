namespace Flowthru.Validation.PreFlight;

/// <summary>
/// Closed sum of every way pre-flight validation can fail. Used as the error
/// type of <see cref="Validated{TError, TValue}"/> for pre-flight checks;
/// multiple cases can accumulate into a single Invalid result so the user
/// sees every problem at once instead of one per re-run.
/// </summary>
/// <remarks>
/// <para>
/// The hierarchy is closed via the private constructor — no derived case
/// can be added outside this file. Add new cases here when Core gains a new
/// pre-flight check; consumers pattern-match exhaustively.
/// </para>
/// <para>
/// PreFlightError covers the failure modes the system can detect <em>before
/// any pipeline logic runs</em>. Anything that can only be discovered during
/// step execution belongs in <see cref="Runtime.RuntimeError"/>.
/// </para>
/// <para>
/// Extensions add their own pre-flight failure shapes via the
/// <see cref="External"/> variant: an extension implements
/// <see cref="IExtensionPreFlightError"/> and Core's renderer / classifier
/// dispatches to it. Extensions do not add cases to this closed sum
/// directly — the closed sum is Core's responsibility, the open extension
/// point is the <see cref="External"/> variant.
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
  /// Two registered flows share a flow label, or two steps across the
  /// merged DAG share a step label. Flowthru requires both to be unique:
  /// flow labels key the per-flow slice, step labels key the merged DAG
  /// node set (§2.4). Detected while assembling the merged flow — a
  /// plan-build precondition, surfaced as data so a smoke test reports it
  /// rather than crashing. <see cref="Scope"/> distinguishes the two
  /// namespaces (<c>"flow"</c> / <c>"step"</c>).
  /// </summary>
  public sealed record DuplicateLabel(string Label, string Scope) : PreFlightError
  {
    public override string Message =>
      $"Duplicate {Scope} label '{Label}': {Scope} labels must be unique "
      + (Scope == "flow"
        ? "within a single FlowthruService."
        : "across the merged DAG (§2.4).");
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

  /// <summary>
  /// An adapter-internal inspection check failed for a reason that doesn't
  /// fit the other categories — a malformed file body, a database
  /// configuration mismatch, an unreachable healthcheck endpoint. The
  /// <see cref="Detail"/> carries the adapter-specific explanation.
  /// </summary>
  public sealed record InspectionFailed(string ItemId, string Detail)
    : PreFlightError
  {
    public override string Message =>
      $"Inspection failed for '{ItemId}': {Detail}";
  }

  /// <summary>
  /// A registration-time validation hook reported a failure. Hooks
  /// run once per process at first <c>RunAsync</c> (or eagerly via
  /// <c>IFlowthruService.ValidateRegistrationAsync</c>) and surface
  /// failures as this closed-sum case. Distinct from per-flow checks
  /// (<see cref="DuplicateProducer"/>, <see cref="MissingInput"/>):
  /// these report on host wire-up state — bad connection strings,
  /// misconfigured DI services, schema drift catchable before any
  /// flow runs.
  /// </summary>
  /// <param name="HookId">Stable identifier of the hook that reported the failure.</param>
  /// <param name="CheckMessage">Human-readable description.</param>
  /// <param name="Details">Optional supplementary detail for diagnostic display.</param>
  public sealed record RegistrationCheckFailed(
    string HookId,
    string CheckMessage,
    string? Details = null
  ) : PreFlightError
  {
    public override string Message =>
      Details is null
        ? $"Registration check '{HookId}' failed: {CheckMessage}"
        : $"Registration check '{HookId}' failed: {CheckMessage} ({Details})";
  }

  /// <summary>
  /// An extension-defined pre-flight failure. The wrapped
  /// <see cref="IExtensionPreFlightError"/> carries the extension's
  /// rendering data; Core's classifier and formatter dispatch to it
  /// generically. Extensions speak Core's error language by satisfying
  /// the interface, not by extending the closed sum.
  /// </summary>
  public sealed record External(IExtensionPreFlightError Cause) : PreFlightError
  {
    public override string Message => Cause.Message;
  }
}

/// <summary>
/// Open extension point for extension-defined pre-flight failures. An
/// extension implements this interface to surface its domain-specific
/// pre-flight failures through Core's standard error pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Implementations live in
/// <c>Flowthru.Validation.PreFlight.&lt;ExtensionName&gt;</c> sub-namespaces
/// per the namespace convention from the FP-rewrite spec. The
/// <see cref="Category"/> discriminator lets Core's classifier route
/// renderings without knowing the concrete extension type; the
/// <see cref="DiagnosticCode"/> carries the FT3xxx-range identifier
/// for documentation cross-reference.
/// </para>
/// </remarks>
public interface IExtensionPreFlightError
{
  /// <summary>Human-readable description of the failure.</summary>
  string Message { get; }

  /// <summary>
  /// Stable category discriminator the extension uses. Core's classifier
  /// uses this to route the failure through the appropriate rendering
  /// pipeline without coupling to the concrete extension type.
  /// </summary>
  string Category { get; }

  /// <summary>
  /// FT3xxx-range diagnostic code for documentation cross-reference. Each
  /// extension reserves a sub-range of FT3xxx for its categories; see the
  /// diagnostics docs for the allocation.
  /// </summary>
  string DiagnosticCode { get; }
}
