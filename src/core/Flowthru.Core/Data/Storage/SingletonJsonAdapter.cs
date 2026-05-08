using System.Text.Json;
using System.Text.Json.Serialization;
using Flowthru.Data.Schema;

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

  /// <inheritdoc/>
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
  public FlowIO<ValidationResult> InspectDeep() => InspectShallow(sampleSize: 0);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => _medium.InspectTarget();
}
