using System.Text.Json;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Direct JSON file storage for singleton objects (not collections).
/// </summary>
/// <typeparam name="T">The object type to serialize</typeparam>
/// <remarks>
/// <para>
/// <strong>Design Rationale:</strong> Singleton objects don't need the full
/// medium/format/container composition since they don't stream rows. This adapter
/// provides direct JSON serialization for single objects.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>ML models (LinearRegressionModel)</item>
/// <item>Metrics objects (ModelMetrics, CrossValidationResults)</item>
/// <item>Configuration files</item>
/// <item>Any single object (not a collection)</item>
/// </list>
/// <para>
/// <strong>Serialization Format:</strong> JSON object (not wrapped in array)
/// </para>
/// <para>
/// <strong>Storage Traits:</strong> All traits use filesystem baseline defaults
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var storage = new SingletonJsonStorageAdapter&lt;LinearRegressionModel&gt;("model.json");
/// var entry = new Item&lt;LinearRegressionModel&gt;("model", storage);
///
/// // Save
/// await entry.Save(model).RunAsync();
///
/// // Load
/// var loadedModel = await entry.Load().RunAsync();
/// </code>
/// </example>
public sealed class SingletonJsonStorageAdapter<T> : IStorageAdapter<T>
  where T : IStructuredSerializable
{
  private readonly string _filePath;
  private readonly JsonSerializerOptions _options;

  /// <summary>
  /// Creates a new singleton JSON storage adapter with default options.
  /// Uses JsonFormatSerializer's default options to ensure consistent behavior,
  /// including <c>[SerializedLabel]</c> and <c>[SerializedEnum]</c> attribute support.
  /// </summary>
  /// <param name="filePath">Path to JSON file</param>
  /// <remarks>
  /// Mirrors <see cref="Format.JsonFormatSerializer{TRow}"/>'s converter registration shape:
  /// build the base options first, then add converters via <c>Add()</c> in the constructor
  /// body. The enum-aware converter must be registered before the label-aware converter so
  /// that enum-typed properties dispatch to <c>SerializedEnumJsonConverter</c> rather than
  /// the generic label converter.
  /// </remarks>
  public SingletonJsonStorageAdapter(string filePath)
    : this(filePath, BuildDefaultOptions()) { }

  private static JsonSerializerOptions BuildDefaultOptions()
  {
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      PropertyNamingPolicy = null,
      DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
    // Enum factory first so [SerializedEnum] mappings (e.g. CheckStatus.Complete → "t")
    // are honored end-to-end. Registering only the label factory — as this adapter did
    // pre-Phase-C — silently dropped [SerializedEnum] mappings, with enum values
    // round-tripping as their C# member names instead of declared serialized strings.
    options.Converters.Add(new Format.SerializedEnumJsonConverterFactory());
    options.Converters.Add(new Format.SerializedLabelJsonConverterFactory());
    return options;
  }

  /// <summary>
  /// Creates a new singleton JSON storage adapter with custom options.
  /// </summary>
  /// <param name="filePath">Path to JSON file</param>
  /// <param name="options">JSON serialization options</param>
  public SingletonJsonStorageAdapter(string filePath, JsonSerializerOptions options)
  {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    _options = options ?? throw new ArgumentNullException(nameof(options));
  }

  /// <inheritdoc />
  public StorageTraits Traits => new StorageTraits();

  /// <inheritdoc />
  public FlowIO<T> Load()
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!File.Exists(_filePath))
        {
          throw new FileNotFoundException($"JSON file not found at '{_filePath}'", _filePath);
        }

        await using var stream = File.OpenRead(_filePath);
        var result = await JsonSerializer.DeserializeAsync<T>(stream, _options, ct);
        return result
          ?? throw new InvalidOperationException($"Failed to deserialize JSON from '{_filePath}'");
      }
    );
  }

  /// <inheritdoc />
  public FlowIO<FlowUnit> Save(T data)
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (data == null)
        {
          throw new ArgumentNullException(nameof(data));
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        // Write to temp file then rename for atomicity
        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
          await JsonSerializer.SerializeAsync(stream, data, _options, ct);
        }

        // Atomic rename
        if (File.Exists(_filePath))
        {
          File.Delete(_filePath);
        }
        File.Move(tempPath, _filePath);

        return FlowUnit.Default;
      }
    );
  }

  /// <inheritdoc />
  public FlowIO<bool> Exists()
  {
    return FlowIO.Lift(() => File.Exists(_filePath));
  }

  /// <inheritdoc />
  public FlowIO<Data.Validation.ValidationResult> InspectShallow(int sampleSize)
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        if (!File.Exists(_filePath))
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"JSON file not found: {_filePath}",
            details: "File does not exist or is not accessible"
          );
        }

        try
        {
          // Attempt to deserialize to verify valid JSON and schema
          await using var stream = File.OpenRead(_filePath);
          await JsonSerializer.DeserializeAsync<T>(stream, _options, ct);
          return Data.Validation.ValidationResult.Success();
        }
        catch (JsonException ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: Data.Validation.ValidationErrorType.DeserializationError,
            message: $"Invalid JSON in file: {_filePath}",
            details: ex.Message
          );
        }
        catch (Exception ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: Path.GetFileName(_filePath),
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"Failed to access JSON file: {_filePath}",
            details: ex.Message
          );
        }
      }
    );
  }

  /// <inheritdoc />
  public FlowIO<Data.Validation.ValidationResult> InspectDeep()
  {
    // For singleton objects, deep inspection is equivalent to shallow
    // since we must deserialize the entire object anyway
    return InspectShallow(sampleSize: 0);
  }

  /// <inheritdoc />
  /// <remarks>
  /// Validates that the write destination is accessible.
  /// Walks up to the nearest existing ancestor to check write permissions,
  /// so a missing intermediate directory is not itself a failure.
  /// </remarks>
  public FlowIO<Data.Validation.ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(ct => LocalFileWriteProbe.ProbeAsync(_filePath, ct));

  /// <summary>
  /// Gets the file path used by this adapter.
  /// </summary>
  public string FilePath => _filePath;

  /// <summary>
  /// Gets the JSON serialization options.
  /// </summary>
  public JsonSerializerOptions Options => _options;
}
