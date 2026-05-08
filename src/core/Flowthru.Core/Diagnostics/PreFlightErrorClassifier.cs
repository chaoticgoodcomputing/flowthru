namespace Flowthru.Diagnostics;

/// <summary>
/// Pattern-matches a <see cref="PreFlightError"/> closed sum to its
/// FT diagnostic code and human-readable category. Mirrors
/// <see cref="RuntimeErrorClassifier"/> for the pre-flight phase.
/// </summary>
public sealed record PreFlightErrorReport(
  string DiagnosticCode,
  string Category,
  string Message,
  PreFlightError Error
);

/// <summary>
/// Static classifier that turns a <see cref="PreFlightError"/> into a
/// <see cref="PreFlightErrorReport"/> with FT3xxx-range diagnostic
/// codes.
/// </summary>
public static class PreFlightErrorClassifier
{
  /// <summary>
  /// Classify <paramref name="error"/> into a
  /// <see cref="PreFlightErrorReport"/>.
  /// </summary>
  public static PreFlightErrorReport Classify(PreFlightError error)
  {
    if (error is null) throw new ArgumentNullException(nameof(error));
    return error switch
    {
      PreFlightError.DuplicateProducer d => new PreFlightErrorReport(
        FlowthruDiagnosticCodes.PreFlightDuplicateProducer,
        "DuplicateProducer",
        d.Message,
        d
      ),
      PreFlightError.CircularDependency c => new PreFlightErrorReport(
        FlowthruDiagnosticCodes.PreFlightCircularDependency,
        "CircularDependency",
        c.Message,
        c
      ),
      PreFlightError.MissingInput m => new PreFlightErrorReport(
        FlowthruDiagnosticCodes.PreFlightMissingInput,
        "MissingInput",
        m.Message,
        m
      ),
      PreFlightError.SchemaDrift s => new PreFlightErrorReport(
        FlowthruDiagnosticCodes.PreFlightSchemaDrift,
        "SchemaDrift",
        s.Message,
        s
      ),
      PreFlightError.InspectionFailed i => new PreFlightErrorReport(
        FlowthruDiagnosticCodes.PreFlightInspectionFailed,
        "InspectionFailed",
        i.Message,
        i
      ),
      PreFlightError.RegistrationCheckFailed r => new PreFlightErrorReport(
        FlowthruDiagnosticCodes.PreFlightRegistrationCheckFailed,
        "RegistrationCheckFailed",
        r.Message,
        r
      ),
      PreFlightError.External e => new PreFlightErrorReport(
        e.Cause.DiagnosticCode,
        e.Cause.Category,
        e.Message,
        e
      ),
      _ => throw new InvalidOperationException("Unreachable: PreFlightError is a closed sum"),
    };
  }
}
