using System.Text.Json;
using Flowthru.Abstractions;
using Flowthru.Data.Validation;

namespace Flowthru.Data.Implementations;

/// <summary>
/// JSON file-based catalog object for singleton objects using System.Text.Json.
/// </summary>
/// <typeparam name="T">The type of the singleton object to store</typeparam>
/// <remarks>
/// <para>
/// <strong>New in v0.4.0:</strong> This class provides JSON serialization for singleton objects.
/// Use this for complex objects with nested structures that need human-readable storage.
/// </para>
/// <para>
/// <strong>Schema Compatibility:</strong> JSON format supports both <see cref="IFlatSerializable"/> 
/// and <see cref="INestedSerializable"/> schemas. Use JSON when:
/// - Object contains nested structures or collections
/// - Human-readable format is desired for inspection/debugging
/// - Interchange with web APIs or configuration systems
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - Complex model metadata with nested properties (CrossValidationResults with List&lt;FoldMetric&gt;)
/// - Configuration objects with hierarchical structure
/// - Model evaluation reports with nested metrics
/// - Any singleton object where CSV would lose structural information
/// </para>
/// <para>
/// <strong>Comparison with CSV:</strong>
/// CSV cannot preserve nested structures (lists, dictionaries, complex objects).
/// JSON maintains full object hierarchy, making it ideal for rich data models.
/// </para>
/// <para>
/// <strong>Requirements:</strong>
/// Type T should be:
/// - JSON-serializable (public properties with getters/setters)
/// - Compatible with System.Text.Json (or use JsonSerializerOptions for customization)
/// </para>
/// <para>
/// <strong>Dependencies:</strong> Uses System.Text.Json (built into .NET 9.0).
/// </para>
/// <para>
/// <strong>Default Configuration:</strong>
/// - WriteIndented = true (pretty-printed, human-readable)
/// - PropertyNamingPolicy = CamelCase (modern JSON convention)
/// - DefaultIgnoreCondition = WhenWritingNull (cleaner output)
/// - Custom configuration can be provided via constructor
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In catalog - pretty-printed (default)
/// public ICatalogObject&lt;CrossValidationResults&gt; CrossValidationResults =>
///     GetOrCreateObject(() => new JsonCatalogObject&lt;CrossValidationResults&gt;(
///         "cross_validation_results",
///         $"{BasePath}/results.json"));
/// 
/// // In catalog - minified for production
/// public ICatalogObject&lt;ModelConfig&gt; ProdConfig =>
///     GetOrCreateObject(() => new JsonCatalogObject&lt;ModelConfig&gt;(
///         "config",
///         $"{BasePath}/config.json",
///         minified: true));
/// 
/// // Usage
/// var results = await catalog.CrossValidationResults.Load();
/// await catalog.CrossValidationResults.Save(newResults);
/// </code>
/// </example>
public class JsonCatalogObject<T> : CatalogObjectBase<T> {
  private readonly string _filePath;
  private readonly JsonSerializerOptions _options;

  /// <summary>
  /// Creates a new JSON catalog object with default serialization options.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog object</param>
  /// <param name="filePath">Path to the JSON file (absolute or relative to working directory)</param>
  /// <param name="minified">If true, output compact JSON without indentation (smaller files). If false (default), output pretty-printed JSON (human-readable).</param>
  public JsonCatalogObject(string key, string filePath, bool minified = false)
      : this(key, filePath, new JsonSerializerOptions {
        WriteIndented = !minified,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
      }) {
  }

  /// <summary>
  /// Creates a new JSON catalog object with custom serialization options.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog object</param>
  /// <param name="filePath">Path to the JSON file</param>
  /// <param name="options">JsonSerializerOptions for customizing serialization behavior</param>
  public JsonCatalogObject(string key, string filePath, JsonSerializerOptions options)
      : base(key) {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    _options = options ?? throw new ArgumentNullException(nameof(options));
  }

  /// <summary>
  /// Gets the file path for this JSON catalog object.
  /// </summary>
  public string FilePath => _filePath;

  /// <summary>
  /// Gets the JsonSerializerOptions for this catalog object.
  /// </summary>
  public JsonSerializerOptions Options => _options;

  /// <inheritdoc/>
  public override async Task<T> Load() {
    if (!File.Exists(_filePath)) {
      throw new FileNotFoundException(
          $"JSON file not found for catalog object '{Key}'", _filePath);
    }

    await using var stream = new FileStream(
      _filePath,
      FileMode.Open,
      FileAccess.Read,
      FileShare.Read,
      bufferSize: 4096,
      useAsync: true);

    var result = await JsonSerializer.DeserializeAsync<T>(stream, _options);

    if (result == null) {
      throw new InvalidOperationException(
          $"Deserialization resulted in null for catalog object '{Key}' at path: {_filePath}");
    }

    return result;
  }

  /// <inheritdoc/>
  public override async Task Save(T data) {
    if (data == null) {
      throw new ArgumentNullException(nameof(data),
          $"Cannot save null data to catalog object '{Key}'");
    }

    // Ensure directory exists
    var directory = Path.GetDirectoryName(_filePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
      Directory.CreateDirectory(directory);
    }

    await using var stream = new FileStream(
      _filePath,
      FileMode.Create,
      FileAccess.Write,
      FileShare.None,
      bufferSize: 4096,
      useAsync: true);

    await JsonSerializer.SerializeAsync(stream, data, _options);
  }

  /// <inheritdoc/>
  public override Task<bool> Exists() {
    return Task.FromResult(File.Exists(_filePath));
  }

  /// <summary>
  /// Performs shallow inspection of the JSON file.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>JSON-Specific Validation:</strong> This method provides efficient validation:
  /// </para>
  /// <list type="number">
  /// <item>Checks file existence</item>
  /// <item>Validates JSON is well-formed (parseable)</item>
  /// <item>Checks that deserialized object is not null</item>
  /// <item>Does NOT validate full schema compatibility (use InspectDeep for that)</item>
  /// </list>
  /// </remarks>
  public virtual async Task<ValidationResult> InspectShallow() {
    var result = new ValidationResult();

    try {
      // 1. Check file existence
      if (!File.Exists(_filePath)) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.NotFound,
          $"JSON file not found",
          $"Expected file at path: {_filePath}"));
        return result;
      }

      // 2. Validate JSON is parseable (check for syntax errors)
      await using var stream = File.OpenRead(_filePath);
      using var document = await JsonDocument.ParseAsync(stream);

      // 3. Quick check that it's not an empty document
      if (document.RootElement.ValueKind == JsonValueKind.Null) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.EmptyDataset,
          $"JSON file contains null",
          $"Expected valid JSON object in '{Key}'"));
        return result;
      }

      // Success - JSON is parseable and not null
      return ValidationResult.Success();
    } catch (JsonException ex) {
      result.AddError(new ValidationError(
        Key,
        ValidationErrorType.InvalidFormat,
        $"Invalid JSON format: {ex.Message}",
        $"File: {_filePath}"));
      return result;
    } catch (Exception ex) {
      return ValidationResult.FromException(Key, ex);
    }
  }

  /// <summary>
  /// Performs deep inspection of the JSON file.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Deep Validation:</strong> This method validates full schema compatibility
  /// by attempting to deserialize the entire object into type T.
  /// </para>
  /// </remarks>
  public virtual async Task<ValidationResult> InspectDeep() {
    var result = new ValidationResult();

    try {
      // 1. Perform shallow inspection first
      var shallowResult = await InspectShallow();
      if (shallowResult.HasErrors) {
        return shallowResult;
      }

      // 2. Attempt full deserialization to validate schema compatibility
      var data = await Load();

      // Success - object loaded and deserialized successfully
      return ValidationResult.Success();
    } catch (Exception ex) {
      return ValidationResult.FromException(Key, ex);
    }
  }
}
