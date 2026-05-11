namespace Flowthru.Diagnostics;

/// <summary>
/// Pattern-matches a <see cref="RuntimeError"/> closed sum to its FT
/// diagnostic code and human-readable category. Open
/// <see cref="RuntimeError.ExtensionError"/> values are routed to the
/// wrapped <see cref="IExtensionRuntimeError"/> for diagnostic-code
/// resolution.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.5, this is the boundary where structured runtime errors
/// become diagnostic codes consumers can render or aggregate. The
/// closed-sum cases hit known FT4xxx codes; extension errors carry
/// their own <see cref="IExtensionRuntimeError.DiagnosticCode"/> in
/// the FT4xxx range.
/// </para>
/// </remarks>
public static class RuntimeErrorClassifier
{
  /// <summary>
  /// Classify <paramref name="error"/> into a
  /// <see cref="RuntimeErrorReport"/>.
  /// </summary>
  public static RuntimeErrorReport Classify(RuntimeError error)
  {
    if (error is null) throw new ArgumentNullException(nameof(error));
    return error switch
    {
      RuntimeError.External e => new RuntimeErrorReport(
        FlowthruDiagnosticCodes.RuntimeExternalFailure,
        "External",
        e.Message,
        e
      ),
      RuntimeError.StepFailed s => new RuntimeErrorReport(
        FlowthruDiagnosticCodes.RuntimeStepFailed,
        "StepFailed",
        s.Message,
        s
      ),
      RuntimeError.Cancelled c => new RuntimeErrorReport(
        FlowthruDiagnosticCodes.RuntimeCancelled,
        "Cancelled",
        c.Message,
        c
      ),
      RuntimeError.InvariantViolated v => new RuntimeErrorReport(
        FlowthruDiagnosticCodes.RuntimeInvariantViolated,
        "InvariantViolated",
        v.Message,
        v
      ),
      RuntimeError.PreFlightFailed p => PreFlightDelegated(p),
      RuntimeError.SchemaMismatch sm => new RuntimeErrorReport(
        FlowthruDiagnosticCodes.RuntimeSchemaMismatch,
        "SchemaMismatch",
        sm.Message,
        sm
      ),
      RuntimeError.ConstraintViolated cv => new RuntimeErrorReport(
        FlowthruDiagnosticCodes.RuntimeConstraintViolated,
        "ConstraintViolated",
        cv.Message,
        cv
      ),
      RuntimeError.ExtensionError x => new RuntimeErrorReport(
        x.Cause.DiagnosticCode,
        x.Cause.Category,
        x.Message,
        x
      ),
      _ => throw new InvalidOperationException("Unreachable: RuntimeError is a closed sum"),
    };
  }

  private static RuntimeErrorReport PreFlightDelegated(RuntimeError.PreFlightFailed wrapped)
  {
    var inner = PreFlightErrorClassifier.Classify(wrapped.Cause);
    return new RuntimeErrorReport(inner.DiagnosticCode, inner.Category, inner.Message, wrapped);
  }
}
