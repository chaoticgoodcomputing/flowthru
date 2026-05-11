namespace Flowthru.Data.Storage;

/// <summary>
/// A single validation finding from inspecting a catalog item — the
/// adapter-internal counterpart to <see cref="Validation.PreFlight.PreFlightError"/>.
/// Stays domain-specific (storage/format/medium) rather than mapping
/// directly into the closed-sum pre-flight categories so adapters can
/// report fine-grained context (column name, row number, expected vs.
/// actual) without inventing new closed-sum cases per failure mode.
/// </summary>
public sealed class ValidationError
{
  public ValidationError(
    string catalogKey,
    ValidationErrorType errorType,
    string message,
    string? details = null
  )
  {
    CatalogKey = catalogKey ?? throw new ArgumentNullException(nameof(catalogKey));
    ErrorType = errorType;
    Message = message ?? throw new ArgumentNullException(nameof(message));
    Details = details;
  }

  /// <summary>The catalog item label where the error occurred.</summary>
  public string CatalogKey { get; }

  /// <summary>The category of error.</summary>
  public ValidationErrorType ErrorType { get; }

  /// <summary>Human-readable description.</summary>
  public string Message { get; }

  /// <summary>
  /// Optional additional context — file path, row number, column name,
  /// expected vs actual values, stack trace.
  /// </summary>
  public string? Details { get; }
}

/// <summary>
/// Categories of validation findings the storage-inspection layer can
/// produce. Adapters translate provider-specific exceptions into one of
/// these categories so downstream classifiers don't need to know every
/// provider's exception types.
/// </summary>
public enum ValidationErrorType
{
  /// <summary>The data source does not exist (file missing, URL unreachable).</summary>
  NotFound,

  /// <summary>The data format is invalid or corrupted.</summary>
  InvalidFormat,

  /// <summary>Headers / column names don't match the expected schema.</summary>
  SchemaMismatch,

  /// <summary>Data types in the source don't match the expected types.</summary>
  TypeMismatch,

  /// <summary>A row failed to deserialize (missing required field, invalid value).</summary>
  DeserializationError,

  /// <summary>The data source is empty when data was expected.</summary>
  EmptyDataset,

  /// <summary>An unexpected exception occurred during inspection.</summary>
  InspectionFailure,

  /// <summary>The write destination exists but cannot be written to (permissions, read-only).</summary>
  WriteAccessDenied,
}
