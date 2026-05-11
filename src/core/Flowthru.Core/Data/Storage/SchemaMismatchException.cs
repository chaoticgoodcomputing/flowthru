namespace Flowthru.Data.Storage;

/// <summary>
/// Provider-agnostic exception type adapters throw to signal that the
/// underlying source's schema diverges from the schema the catalog item
/// declared. <see cref="ValidationResult.FromException"/> classifies it
/// as <see cref="ValidationErrorType.SchemaMismatch"/>.
/// </summary>
/// <remarks>
/// Format extensions catch their provider's native schema-mismatch
/// exception (CsvHelper's <c>HeaderValidationException</c>, Parquet's
/// schema-mismatch errors, etc.) and re-throw as this type. The
/// translation happens at the adapter boundary so Core can stay agnostic
/// of every provider's exception hierarchy.
/// </remarks>
public sealed class SchemaMismatchException : Exception
{
  public SchemaMismatchException(string message) : base(message) { }

  public SchemaMismatchException(string message, Exception innerException)
    : base(message, innerException) { }
}
