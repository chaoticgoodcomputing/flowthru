namespace Flowthru.Core.Data.Validation;

/// <summary>
/// Thrown by format serializers and storage adapters when the underlying source
/// structurally diverges from the schema — missing columns, mismatched column types,
/// extra columns, header-row mismatches, etc.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why this exists.</strong> Provider libraries raise their own exception
/// types for structural mismatches: <c>CsvHelper.HeaderValidationException</c>,
/// Parquet's schema-mismatch errors, JSON property-not-found, EFCore's column-
/// existence checks, etc. <see cref="ValidationResult.FromException"/> would
/// otherwise classify all of them as <see cref="ValidationErrorType.InspectionFailure"/>
/// (the catch-all "an unexpected exception occurred during inspection") because Core
/// can't reference each provider's exception type to translate.
/// </para>
/// <para>
/// Format serializers and adapters that detect a structural mismatch should catch
/// the provider's exception and re-throw as <see cref="SchemaMismatchException"/>
/// with the original as <see cref="Exception.InnerException"/>.
/// <c>ValidationResult.FromException</c> then maps it to
/// <see cref="ValidationErrorType.SchemaMismatch"/> — the canonically-correct
/// category per the enum's own definition ("Headers or column names don't match the
/// expected schema").
/// </para>
/// <para>
/// <strong>Cross-extension uniformity.</strong> Flow developers writing custom
/// error-handling do <c>if (error.ErrorType == SchemaMismatch) { … }</c> and expect
/// that to work the same regardless of underlying storage. This exception type +
/// <c>FromException</c>'s introspection make that uniformity possible without
/// putting provider-specific knowledge in Core.
/// </para>
/// </remarks>
public sealed class SchemaMismatchException : Exception
{
  /// <summary>
  /// Creates a new schema-mismatch exception.
  /// </summary>
  /// <param name="message">A human-readable description of the structural divergence.</param>
  /// <param name="innerException">Optional underlying provider exception.</param>
  public SchemaMismatchException(string message, Exception? innerException = null)
    : base(message, innerException) { }
}
