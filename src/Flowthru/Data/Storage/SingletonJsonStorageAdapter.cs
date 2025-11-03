using System.Text.Json;
using Flowthru.Abstractions;
using LanguageExt;

namespace Flowthru.Data.Storage;

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
/// <strong>Capabilities:</strong>
/// </para>
/// <list type="bullet">
/// <item>ISeedable: true if file exists</item>
/// <item>IReadOnly: false</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var storage = new SingletonJsonStorageAdapter&lt;LinearRegressionModel&gt;("model.json");
/// var entry = new CatalogEntry&lt;LinearRegressionModel&gt;("model", storage);
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
  /// </summary>
  /// <param name="filePath">Path to JSON file</param>
  public SingletonJsonStorageAdapter(string filePath)
    : this(
      filePath,
      new JsonSerializerOptions
      {
        WriteIndented = true, // Pretty-print by default for readability
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
      }
    ) { }

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
  public IO<T> Load()
  {
    return IO.liftAsync(async () =>
    {
      if (!File.Exists(_filePath))
      {
        throw new FileNotFoundException($"JSON file not found at '{_filePath}'", _filePath);
      }

      await using var stream = File.OpenRead(_filePath);
      var result = await JsonSerializer.DeserializeAsync<T>(stream, _options);
      return result
        ?? throw new InvalidOperationException($"Failed to deserialize JSON from '{_filePath}'");
    });
  }

  /// <inheritdoc />
  public IO<Unit> Save(T data)
  {
    return IO.liftAsync(async () =>
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
        await JsonSerializer.SerializeAsync(stream, data, _options);
      }

      // Atomic rename
      if (File.Exists(_filePath))
      {
        File.Delete(_filePath);
      }
      File.Move(tempPath, _filePath);

      return Unit.Default;
    });
  }

  /// <inheritdoc />
  public IO<bool> Exists()
  {
    return IO.liftAsync(async () => File.Exists(_filePath));
  }

  /// <summary>
  /// Gets the file path used by this adapter.
  /// </summary>
  public string FilePath => _filePath;

  /// <summary>
  /// Gets the JSON serialization options.
  /// </summary>
  public JsonSerializerOptions Options => _options;
}
