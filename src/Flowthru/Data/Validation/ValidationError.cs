namespace Flowthru.Data.Validation;

/// <summary>
/// Represents a single validation error discovered during catalog entry inspection.
/// </summary>
/// <remarks>
/// ValidationError provides structured information about what went wrong during
/// inspection, making it easier to diagnose and fix data issues.
/// </remarks>
public class ValidationError {
  /// <summary>
  /// Creates a new validation error.
  /// </summary>
  /// <param name="catalogKey">The catalog entry key where the error occurred</param>
  /// <param name="errorType">The category of error</param>
  /// <param name="message">Human-readable description of the error</param>
  /// <param name="details">Optional additional context (file path, row number, column name, etc.)</param>
  public ValidationError(
    string catalogKey,
    ValidationErrorType errorType,
    string message,
    string? details = null) {
    CatalogKey = catalogKey ?? throw new ArgumentNullException(nameof(catalogKey));
    ErrorType = errorType;
    Message = message ?? throw new ArgumentNullException(nameof(message));
    Details = details;
  }

  /// <summary>
  /// The catalog entry key where the error occurred.
  /// </summary>
  public string CatalogKey { get; }

  /// <summary>
  /// The category of error that occurred.
  /// </summary>
  public ValidationErrorType ErrorType { get; }

  /// <summary>
  /// Human-readable description of the error.
  /// </summary>
  public string Message { get; }

  /// <summary>
  /// Optional additional context about the error.
  /// </summary>
  /// <remarks>
  /// May contain:
  /// - File path
  /// - Row number
  /// - Column name
  /// - Expected vs actual values
  /// - Stack trace (for exceptions)
  /// </remarks>
  public string? Details { get; }

  /// <summary>
  /// Returns a formatted string representation of the error.
  /// </summary>
  public override string ToString() {
    var result = $"[{ErrorType}] {CatalogKey}: {Message}";
    if (!string.IsNullOrEmpty(Details)) {
      result += $"\n  Details: {Details}";
    }
    return result;
  }
}

/// <summary>
/// Categories of validation errors that can occur during catalog entry inspection.
/// </summary>
public enum ValidationErrorType {
  /// <summary>
  /// The data source does not exist (file not found, URL unreachable, etc.).
  /// </summary>
  NotFound,

  /// <summary>
  /// The data format is invalid or corrupted (malformed CSV, corrupt Parquet, etc.).
  /// </summary>
  InvalidFormat,

  /// <summary>
  /// Headers or column names don't match the expected schema.
  /// </summary>
  SchemaMismatch,

  /// <summary>
  /// Data types in the source don't match the expected types.
  /// </summary>
  TypeMismatch,

  /// <summary>
  /// A row failed to deserialize (missing required field, invalid value, etc.).
  /// </summary>
  DeserializationError,

  /// <summary>
  /// The data source is empty when data was expected.
  /// </summary>
  EmptyDataset,

  /// <summary>
  /// An unexpected exception occurred during inspection.
  /// </summary>
  InspectionFailure
}
