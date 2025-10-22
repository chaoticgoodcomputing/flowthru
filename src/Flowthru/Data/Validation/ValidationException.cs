namespace Flowthru.Data.Validation;

/// <summary>
/// Exception thrown when catalog entry validation fails.
/// </summary>
/// <remarks>
/// This exception is thrown by <see cref="ValidationResult.ThrowIfInvalid"/> to halt
/// pipeline execution when external data validation fails.
/// </remarks>
public class ValidationException : Exception {
  /// <summary>
  /// Creates a new validation exception.
  /// </summary>
  /// <param name="validationResult">The validation result containing errors</param>
  public ValidationException(ValidationResult validationResult)
    : base(BuildMessage(validationResult)) {
    ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
  }

  /// <summary>
  /// The validation result containing all errors.
  /// </summary>
  public ValidationResult ValidationResult { get; }

  private static string BuildMessage(ValidationResult result) {
    if (result == null) {
      throw new ArgumentNullException(nameof(result));
    }

    if (result.IsValid) {
      return "Validation exception created with valid result (no errors)";
    }

    var message = $"Catalog validation failed with {result.ErrorCount} error(s):";

    // Group errors by catalog key for better readability
    var errorsByCatalog = result.Errors.GroupBy(e => e.CatalogKey);
    foreach (var group in errorsByCatalog) {
      message += $"\n\n{group.Key}:";
      foreach (var error in group) {
        message += $"\n  • [{error.ErrorType}] {error.Message}";
        if (!string.IsNullOrEmpty(error.Details)) {
          // Indent details for readability
          var indentedDetails = error.Details.Replace("\n", "\n    ");
          message += $"\n    {indentedDetails}";
        }
      }
    }

    return message;
  }
}
