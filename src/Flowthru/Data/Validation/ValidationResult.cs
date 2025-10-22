namespace Flowthru.Data.Validation;

/// <summary>
/// Represents the result of inspecting one or more catalog entries.
/// </summary>
/// <remarks>
/// <para>
/// ValidationResult provides a structured way to collect and report validation errors
/// discovered during catalog entry inspection. It supports both single-entry and
/// multi-entry validation scenarios.
/// </para>
/// </remarks>
public class ValidationResult {
  private readonly List<ValidationError> _errors;

  /// <summary>
  /// Creates a successful validation result with no errors.
  /// </summary>
  public ValidationResult() {
    _errors = new List<ValidationError>();
  }

  /// <summary>
  /// Creates a validation result with the specified errors.
  /// </summary>
  /// <param name="errors">Collection of validation errors</param>
  public ValidationResult(IEnumerable<ValidationError> errors) {
    _errors = new List<ValidationError>(errors ?? throw new ArgumentNullException(nameof(errors)));
  }

  /// <summary>
  /// True if no validation errors were found.
  /// </summary>
  public bool IsValid => _errors.Count == 0;

  /// <summary>
  /// True if one or more validation errors were found.
  /// </summary>
  public bool HasErrors => _errors.Count > 0;

  /// <summary>
  /// Read-only collection of all validation errors.
  /// </summary>
  public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

  /// <summary>
  /// Number of validation errors found.
  /// </summary>
  public int ErrorCount => _errors.Count;

  /// <summary>
  /// Adds a validation error to this result.
  /// </summary>
  /// <param name="error">The error to add</param>
  internal void AddError(ValidationError error) {
    if (error == null) {
      throw new ArgumentNullException(nameof(error));
    }
    _errors.Add(error);
  }

  /// <summary>
  /// Merges another validation result into this one.
  /// </summary>
  /// <param name="other">The validation result to merge</param>
  internal void Merge(ValidationResult other) {
    if (other == null) {
      throw new ArgumentNullException(nameof(other));
    }
    _errors.AddRange(other.Errors);
  }

  /// <summary>
  /// Creates a successful validation result.
  /// </summary>
  public static ValidationResult Success() => new ValidationResult();

  /// <summary>
  /// Creates a failed validation result with a single error.
  /// </summary>
  /// <param name="catalogKey">The catalog entry key where the error occurred</param>
  /// <param name="errorType">The category of error</param>
  /// <param name="message">Human-readable description of the error</param>
  /// <param name="details">Optional additional context</param>
  public static ValidationResult Failure(
    string catalogKey,
    ValidationErrorType errorType,
    string message,
    string? details = null) {
    return new ValidationResult(new[]
    {
      new ValidationError(catalogKey, errorType, message, details)
    });
  }

  /// <summary>
  /// Creates a failed validation result from an exception.
  /// </summary>
  /// <param name="catalogKey">The catalog entry key where the error occurred</param>
  /// <param name="exception">The exception that occurred during inspection</param>
  public static ValidationResult FromException(string catalogKey, Exception exception) {
    return Failure(
      catalogKey,
      ValidationErrorType.InspectionFailure,
      exception.Message,
      exception.ToString()
    );
  }

  /// <summary>
  /// Returns a formatted string representation of all errors.
  /// </summary>
  public override string ToString() {
    if (IsValid) {
      return "Validation successful - no errors found";
    }

    return $"Validation failed with {ErrorCount} error(s):\n" +
           string.Join("\n", _errors.Select(e => $"  • {e}"));
  }

  /// <summary>
  /// Throws a ValidationException if this result has errors.
  /// </summary>
  /// <exception cref="ValidationException">Thrown if validation failed</exception>
  public void ThrowIfInvalid() {
    if (HasErrors) {
      throw new ValidationException(this);
    }
  }
}
