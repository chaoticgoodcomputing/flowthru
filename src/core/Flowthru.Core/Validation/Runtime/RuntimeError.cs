using Flowthru.Validation.PreFlight;

namespace Flowthru.Validation.Runtime;

/// <summary>
/// Closed sum of every way Flowthru execution can fail at runtime. The
/// failure type of <see cref="Prelude.FlowIO{A}"/>; consumers pattern-match
/// on the cases to distinguish user-environment failures, step failures,
/// cancellation, Flowthru bugs (invariant violations), and extension-defined
/// failures.
/// </summary>
/// <remarks>
/// <para>
/// The hierarchy is closed via the private constructor — no derived case
/// can be added outside this file. Pattern-match exhaustively; new cases
/// added here will surface as compile diagnostics at every consumer until
/// handled.
/// </para>
/// <para>
/// <see cref="InvariantViolated"/> deserves special handling: its presence
/// at runtime means a pre-flight check that should have caught the
/// condition was missing or wrong. CONTRIBUTING.md's invariant — "a flow
/// that passes pre-flight should always complete successfully" — is
/// materialised here. Surfacing this case as a typed value (rather than
/// an untyped exception) is what makes that invariant operationally
/// checkable.
/// </para>
/// <para>
/// Extensions add their own runtime failure shapes via the
/// <see cref="ExtensionError"/> variant: an extension implements
/// <see cref="IExtensionRuntimeError"/> and Core's renderer / classifier
/// dispatches to it. Extensions do not add cases to this closed sum
/// directly — the closed sum is Core's responsibility, the open
/// extension point is the <see cref="ExtensionError"/> variant.
/// </para>
/// </remarks>
public abstract record RuntimeError
{
  private RuntimeError() { }

  /// <summary>Human-readable description of the failure.</summary>
  public abstract string Message { get; }

  /// <summary>
  /// A failure originating outside Flowthru — network drop, OOM, disk
  /// full, permission denied, malformed external input encountered
  /// mid-stream. The <see cref="Cause"/> is the underlying exception
  /// captured at the boundary by <see cref="Prelude.FlowIO{A}.Lift"/> /
  /// <see cref="Prelude.FlowIO{A}.LiftAsync"/>.
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
  /// (a missing pre-flight check or a missing compile-time constraint),
  /// not in the user's flow. Surfacing this as a distinct case lets
  /// reporting code render it with a "please file an issue" affordance
  /// rather than a generic stack trace.
  /// </summary>
  public sealed record InvariantViolated(string CheckName, string Detail) : RuntimeError
  {
    public override string Message =>
      $"Pre-flight invariant '{CheckName}' violated at runtime — this is a bug in Flowthru. {Detail}";
  }

  /// <summary>
  /// A legitimate, user-actionable pre-flight failure surfaced into the
  /// runtime error channel. Distinct from <see cref="InvariantViolated"/>:
  /// this is <em>not</em> a Flowthru bug. It exists so pre-flight outcomes
  /// can be reported through the same <see cref="StepResult.Failed"/>
  /// envelope as runtime errors, while still surfacing the inner FT3xxx
  /// diagnostic code (missing input, schema drift, duplicate producer,
  /// etc.) instead of being misclassified as FT4004 "bug in Flowthru".
  /// </summary>
  /// <remarks>
  /// The classifier delegates this case to <c>PreFlightErrorClassifier</c>
  /// so the surfaced report carries the inner <see cref="PreFlightError"/>'s
  /// FT3xxx code and category, not a runtime FT4xxx wrapper. Users with
  /// a missing CSV input see FT3003, not "file an issue with Flowthru".
  /// </remarks>
  public sealed record PreFlightFailed(PreFlightError Cause) : RuntimeError
  {
    public override string Message => $"Pre-flight failure: {Cause.Message}";
  }

  /// <summary>
  /// An operation was blocked by a trait constraint applied to the
  /// catalog item — typically because the item was wrapped in a
  /// read-only constraint via <c>IItem&lt;T&gt;.Constrain(...)</c>.
  /// Distinct from <see cref="External"/> (real system error) and
  /// <see cref="InvariantViolated"/> (Flowthru bug): this signals
  /// the catalog author deliberately forbade the operation at
  /// wire-up time, and a step downstream tried it anyway.
  /// </summary>
  /// <param name="ItemLabel">Catalog label of the item the operation targeted.</param>
  /// <param name="Operation">Which operation was blocked (<c>"Load"</c>, <c>"Save"</c>, etc.).</param>
  /// <param name="TraitName">Which trait constraint blocked it (<c>"CanRead"</c>, <c>"CanWrite"</c>, etc.).</param>
  public sealed record ConstraintViolated(
    string ItemLabel,
    string Operation,
    string TraitName
  ) : RuntimeError
  {
    public override string Message =>
      $"Operation '{Operation}' on item '{ItemLabel}' is blocked: "
      + $"the item's '{TraitName}' trait was constrained to false at catalog wire-up.";
  }

  /// <summary>
  /// The format adapter encountered a structural mismatch between the
  /// underlying source and the schema the catalog item declared (a
  /// missing column, a renamed header, a type-shape change). Carries
  /// the adapter <see cref="Source"/> and a human-readable
  /// <see cref="Detail"/> for diagnostic display; the classifier maps
  /// this to <see cref="ValidationErrorType.SchemaMismatch"/> on the
  /// pre-flight side.
  /// </summary>
  /// <remarks>
  /// Translated at the format-adapter boundary from the format
  /// extension's provider-specific exception (CsvHelper's
  /// <c>HeaderValidationException</c>, Parquet's schema-mismatch
  /// errors, etc.) so Core stays agnostic of every provider's
  /// exception hierarchy. The adapter catches the provider exception,
  /// translates to <c>SchemaMismatchException</c>, and the composed
  /// adapter at the <see cref="Prelude.FlowIO{A}"/> boundary lifts it
  /// to this typed variant — preserving typed-error fidelity end to
  /// end while accommodating that the row iterator throws (rather
  /// than failing the <c>FlowIO</c> from inside <c>yield return</c>).
  /// </remarks>
  public sealed record SchemaMismatch(string Source, string Detail, string? InnerExceptionInfo = null)
    : RuntimeError
  {
    public override string Message => $"Schema mismatch in '{Source}': {Detail}";
  }

  /// <summary>
  /// An extension-defined runtime failure. The wrapped
  /// <see cref="IExtensionRuntimeError"/> carries the extension's
  /// rendering data; Core's classifier and formatter dispatch to it
  /// generically. Extensions speak Core's error language by satisfying
  /// the interface, not by extending the closed sum.
  /// </summary>
  public sealed record ExtensionError(IExtensionRuntimeError Cause) : RuntimeError
  {
    public override string Message => Cause.Message;
  }
}

/// <summary>
/// Open extension point for extension-defined runtime failures. An
/// extension implements this interface to surface its domain-specific
/// runtime failures through Core's standard error pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Implementations live in
/// <c>Flowthru.Validation.Runtime.&lt;ExtensionName&gt;</c> sub-namespaces
/// per the namespace convention from the FP-rewrite spec. The
/// <see cref="Category"/> discriminator lets Core's classifier route
/// renderings without knowing the concrete extension type; the
/// <see cref="DiagnosticCode"/> carries the FT4xxx-range identifier
/// for documentation cross-reference.
/// </para>
/// </remarks>
public interface IExtensionRuntimeError
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
  /// FT4xxx-range diagnostic code for documentation cross-reference. Each
  /// extension reserves a sub-range of FT4xxx for its categories; see the
  /// diagnostics docs for the allocation.
  /// </summary>
  string DiagnosticCode { get; }
}
