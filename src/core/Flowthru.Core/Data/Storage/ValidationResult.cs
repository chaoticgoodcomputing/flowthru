namespace Flowthru.Data.Storage;

/// <summary>
/// Result of inspecting a catalog item — a list of <see cref="ValidationError"/>s
/// (empty = valid). Storage adapters return <c>FlowIO&lt;ValidationResult&gt;</c>
/// from <see cref="IStorageAdapter{T}.InspectShallow"/> and friends.
/// </summary>
/// <remarks>
/// <para>
/// ValidationResult is the storage-inspection-specific shape, kept distinct
/// from the FP <see cref="Validated{TError, TValue}"/> applicative. The
/// pre-flight pipeline (Phase 4) lifts adapter ValidationResults into the
/// applicative for accumulation across the full flow; the adapter itself
/// returns this simpler container.
/// </para>
/// </remarks>
public sealed class ValidationResult
{
  private readonly List<ValidationError> _errors;

  public ValidationResult()
  {
    _errors = new List<ValidationError>();
  }

  public ValidationResult(IEnumerable<ValidationError> errors)
  {
    _errors = new List<ValidationError>(errors ?? throw new ArgumentNullException(nameof(errors)));
  }

  public bool IsValid => _errors.Count == 0;

  public bool HasErrors => _errors.Count > 0;

  public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

  public int ErrorCount => _errors.Count;

  /// <summary>Build a successful result.</summary>
  public static ValidationResult Success() => new();

  /// <summary>Build a failed result with a single error.</summary>
  public static ValidationResult Failure(
    string catalogKey,
    ValidationErrorType errorType,
    string message,
    string? details = null
  ) => new(new[] { new ValidationError(catalogKey, errorType, message, details) });

  /// <summary>
  /// Build a failed result from an exception, picking the most-specific
  /// <see cref="ValidationErrorType"/> available.
  /// <see cref="SchemaMismatchException"/> maps to
  /// <see cref="ValidationErrorType.SchemaMismatch"/>; everything else
  /// becomes <see cref="ValidationErrorType.InspectionFailure"/>.
  /// </summary>
  public static ValidationResult FromException(string catalogKey, Exception exception)
  {
    var errorType = exception switch
    {
      SchemaMismatchException => ValidationErrorType.SchemaMismatch,
      _ => ValidationErrorType.InspectionFailure,
    };
    return Failure(catalogKey, errorType, exception.Message, exception.ToString());
  }

  /// <summary>Add an error to this result (internal — used by adapters during inspection).</summary>
  internal void AddError(ValidationError error)
  {
    if (error is null)
    {
      throw new ArgumentNullException(nameof(error));
    }
    _errors.Add(error);
  }

  /// <summary>Merge another result into this one.</summary>
  internal void Merge(ValidationResult other)
  {
    if (other is null)
    {
      throw new ArgumentNullException(nameof(other));
    }
    _errors.AddRange(other.Errors);
  }
}
