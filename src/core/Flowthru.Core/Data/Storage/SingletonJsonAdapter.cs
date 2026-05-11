using System.Text.Json;
using System.Text.Json.Serialization;
using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;

namespace Flowthru.Data.Storage;

/// <summary>
/// Storage adapter for a single <typeparamref name="T"/> value persisted
/// as a JSON object (not wrapped in an array). Used for items like
/// trained models, computed metrics, or configuration documents — single
/// values that don't fit the row-oriented enumerable shape.
/// </summary>
/// <typeparam name="T">The schema type stored as a single value.</typeparam>
public sealed class SingletonJsonAdapter<T> : IStorageAdapter<T>
  where T : notnull, IStructuredSerializable
{
  private readonly string _filePath;
  private readonly FileStorageMedium _medium;
  private readonly JsonSerializerOptions _options;

  public SingletonJsonAdapter(string filePath)
    : this(
      filePath,
      new JsonSerializerOptions
      {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      }
    ) { }

  public SingletonJsonAdapter(string filePath, JsonSerializerOptions options)
  {
    if (string.IsNullOrWhiteSpace(filePath))
    {
      throw new ArgumentException("File path cannot be null or whitespace", nameof(filePath));
    }
    _filePath = filePath;
    _medium = new FileStorageMedium(filePath);
    _options = options ?? throw new ArgumentNullException(nameof(options));
    _options.Converters.Add(new SerializedLabelJsonConverterFactory());
    // [SerializedEnum]-decorated enum properties honor their declared
    // mapping (parallel to JsonFormatSerializer). Without this, enums
    // round-trip as ordinals/member names and the on-disk wire format
    // diverges from every other Flowthru format adapter.
    _options.Converters.Add(new SerializedEnumJsonConverterFactory());
  }

  /// <inheritdoc/>
  public StorageTraits Traits => _medium.Traits;

  /// <inheritdoc/>
  public FlowIO<T> Load() =>
    FlowIO.LiftAsync(async ct =>
    {
      await using var stream = new FileStream(
        _filePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 4096,
        useAsync: true
      );
      var value = await JsonSerializer.DeserializeAsync<T>(stream, _options, ct).ConfigureAwait(false);
      if (value is null)
      {
        throw new InvalidOperationException(
          $"Deserialized null value from singleton JSON file at '{_filePath}'."
        );
      }
      return value;
    });

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data) =>
    FlowIO.LiftAsync(async ct =>
    {
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }
      var tempPath = $"{_filePath}.tmp.{Guid.NewGuid():N}";
      try
      {
        await using (
          var fs = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true
          )
        )
        {
          await JsonSerializer.SerializeAsync(fs, data, _options, ct).ConfigureAwait(false);
        }
        File.Move(tempPath, _filePath, overwrite: true);
        return FlowUnit.Default;
      }
      catch
      {
        if (File.Exists(tempPath))
        {
          try { File.Delete(tempPath); } catch { /* cleanup best-effort */ }
        }
        throw;
      }
    });

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => _medium.Exists();

  /// <summary>
  /// Cached set of required field names from the planner — built once
  /// per adapter instance so repeated inspections don't re-reflect.
  /// Case-insensitive comparison matches the planner's
  /// <see cref="PropertyMappingPlan{TRow}.ByFieldName"/> lookup policy
  /// — if <c>Load</c> would find a field, <c>InspectShallow</c> reports
  /// it present.
  /// </summary>
  private static readonly IReadOnlySet<string> _requiredFieldNames =
    PropertyMappingPlanner.Build<T>().RequiredFieldNames
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

  /// <inheritdoc/>
  /// <remarks>
  /// Verifies that the on-disk JSON object's top-level properties
  /// include every field the schema declares as required (non-nullable).
  /// Extra fields are tolerated — they're silently ignored on Load —
  /// so the rule is <em>data ⊇ schema</em>. Outcomes:
  /// <list type="bullet">
  ///   <item>Source missing → <c>NotFound</c>.</item>
  ///   <item>Source unreadable / malformed JSON → <c>DeserializationError</c>.</item>
  ///   <item>Top-level is not an object → <c>SchemaMismatch</c>.</item>
  ///   <item>Required fields absent → <c>SchemaMismatch</c> with the missing-field diff.</item>
  ///   <item>All required fields present → <c>Success</c> (values are NOT
  ///     value-validated; that's <c>InspectDeep</c>'s contract).</item>
  /// </list>
  /// </remarks>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync<ValidationResult>(async ct =>
    {
      var label = typeof(T).Name;
      if (!File.Exists(_filePath))
      {
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.NotFound,
          message: $"Singleton JSON file not found at '{_filePath}'"
        );
      }
      JsonDocument? document = null;
      try
      {
        await using var stream = new FileStream(
          _filePath,
          FileMode.Open,
          FileAccess.Read,
          FileShare.Read,
          bufferSize: 4096,
          useAsync: true
        );
        document = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.DeserializationError,
          message: $"Failed to parse singleton JSON for '{label}'",
          details: ex.Message
        );
      }

      using (document)
      {
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
          return ValidationResult.Failure(
            catalogKey: label,
            errorType: ValidationErrorType.SchemaMismatch,
            message: $"Singleton JSON for '{label}' must be a JSON object at the top level",
            details: $"Found {document.RootElement.ValueKind} at the top level. "
              + "Singleton JSON adapters expect an object shape, not an array or scalar."
          );
        }

        var presentFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
          presentFields.Add(property.Name);
        }

        var missing = new List<string>();
        foreach (var required in _requiredFieldNames)
        {
          if (!presentFields.Contains(required))
          {
            missing.Add(required);
          }
        }
        if (missing.Count > 0)
        {
          return ValidationResult.Failure(
            catalogKey: label,
            errorType: ValidationErrorType.SchemaMismatch,
            message: $"Singleton JSON for '{label}' is missing required field(s): "
              + string.Join(", ", missing.Select(f => $"'{f}'")),
            details: $"Required by schema: {string.Join(", ", _requiredFieldNames.Select(f => $"'{f}'"))}. "
              + $"Present in data: {string.Join(", ", presentFields.Select(f => $"'{f}'"))}."
          );
        }
        return ValidationResult.Success();
      }
    });

  /// <inheritdoc/>
  /// <remarks>
  /// Deep inspection deserializes the full document into a
  /// <typeparamref name="T"/> instance — verifying every value is
  /// type-compatible with the schema, not just that the required
  /// field names are present. Use for critical inputs; the cost
  /// scales with file size.
  /// </remarks>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync<ValidationResult>(async ct =>
    {
      var label = typeof(T).Name;
      if (!File.Exists(_filePath))
      {
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.NotFound,
          message: $"Singleton JSON file not found at '{_filePath}'"
        );
      }
      try
      {
        await using var stream = new FileStream(
          _filePath,
          FileMode.Open,
          FileAccess.Read,
          FileShare.Read,
          bufferSize: 4096,
          useAsync: true
        );
        _ = await JsonSerializer.DeserializeAsync<T>(stream, _options, ct).ConfigureAwait(false);
        return ValidationResult.Success();
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.DeserializationError,
          message: $"Failed to deserialize singleton JSON for '{label}'",
          details: ex.Message
        );
      }
    });

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => _medium.InspectTarget();
}
