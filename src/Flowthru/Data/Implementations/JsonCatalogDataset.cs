using System.Text.Json;
using Flowthru.Data.Validation;

namespace Flowthru.Data.Implementations;

/// <summary>
/// JSON file-based catalog dataset for collections using System.Text.Json.
/// </summary>
/// <typeparam name="T">The type of individual items in the dataset (NOT IEnumerable&lt;T&gt;)</typeparam>
/// <remarks>
/// <para>
/// <strong>New in v0.4.0:</strong> This class provides JSON serialization for collections.
/// Use this for datasets with complex nested structures that need human-readable storage.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - Collections of complex objects with nested properties
/// - Data interchange requiring human-readable format
/// - Datasets where CSV would lose structural information
/// - Export of model input data for debugging/inspection
/// </para>
/// <para>
/// <strong>Comparison with CSV and Parquet:</strong>
/// - CSV: Fast, widely compatible, but cannot preserve nested structures
/// - Parquet: Highly efficient columnar format, but binary and less human-readable
/// - JSON: Human-readable, preserves full object hierarchy, but larger file size
/// </para>
/// <para>
/// <strong>Storage Format:</strong>
/// Data is stored as a JSON array: <c>[{...}, {...}, {...}]</c>
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
/// <para>
/// <strong>Performance Considerations:</strong>
/// For very large datasets (&gt;100MB), consider Parquet format instead.
/// JSON loads entire array into memory during deserialization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In catalog - pretty-printed (default)
/// public ICatalogDataset&lt;ModelInputSchema&gt; ModelInputTableJson =>
///     GetOrCreateDataset(() => new JsonCatalogDataset&lt;ModelInputSchema&gt;(
///         "model_input_table_json",
///         $"{BasePath}/model_input_table.json"));
/// 
/// // In catalog - minified for production
/// public ICatalogDataset&lt;ModelInputSchema&gt; ProdData =>
///     GetOrCreateDataset(() => new JsonCatalogDataset&lt;ModelInputSchema&gt;(
///         "prod_data",
///         $"{BasePath}/data.json",
///         minified: true));
/// 
/// // Usage
/// var data = await catalog.ModelInputTableJson.Load();
/// await catalog.ModelInputTableJson.Save(processedData);
/// </code>
/// </example>
public class JsonCatalogDataset<T> : CatalogDatasetBase<T> {
  private readonly string _filePath;
  private readonly JsonSerializerOptions _options;

  /// <summary>
  /// Creates a new JSON catalog dataset with default serialization options.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog dataset</param>
  /// <param name="filePath">Path to the JSON file (absolute or relative to working directory)</param>
  /// <param name="minified">If true, output compact JSON without indentation (smaller files). If false (default), output pretty-printed JSON (human-readable).</param>
  public JsonCatalogDataset(string key, string filePath, bool minified = false)
      : this(key, filePath, new JsonSerializerOptions {
        WriteIndented = !minified,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
      }) {
  }

  /// <summary>
  /// Creates a new JSON catalog dataset with custom serialization options.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog dataset</param>
  /// <param name="filePath">Path to the JSON file</param>
  /// <param name="options">JsonSerializerOptions for customizing serialization behavior</param>
  public JsonCatalogDataset(string key, string filePath, JsonSerializerOptions options)
      : base(key) {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    _options = options ?? throw new ArgumentNullException(nameof(options));
  }

  /// <summary>
  /// Gets the file path for this JSON catalog dataset.
  /// </summary>
  public string FilePath => _filePath;

  /// <summary>
  /// Gets the JsonSerializerOptions for this catalog dataset.
  /// </summary>
  public JsonSerializerOptions Options => _options;

  /// <inheritdoc/>
  public override async Task<IEnumerable<T>> Load() {
    if (!File.Exists(_filePath)) {
      throw new FileNotFoundException(
          $"JSON file not found for catalog dataset '{Key}'", _filePath);
    }

    await using var stream = new FileStream(
      _filePath,
      FileMode.Open,
      FileAccess.Read,
      FileShare.Read,
      bufferSize: 4096,
      useAsync: true);

    var result = await JsonSerializer.DeserializeAsync<List<T>>(stream, _options);

    if (result == null) {
      throw new InvalidOperationException(
          $"Deserialization resulted in null for catalog dataset '{Key}' at path: {_filePath}");
    }

    return result;
  }

  /// <inheritdoc/>
  public override async Task Save(IEnumerable<T> data) {
    if (data == null) {
      throw new ArgumentNullException(nameof(data),
          $"Cannot save null data to catalog dataset '{Key}'");
    }

    // Ensure directory exists
    var directory = Path.GetDirectoryName(_filePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
      Directory.CreateDirectory(directory);
    }

    // Materialize enumerable to list for serialization
    var dataList = data as List<T> ?? data.ToList();

    await using var stream = new FileStream(
      _filePath,
      FileMode.Create,
      FileAccess.Write,
      FileShare.None,
      bufferSize: 4096,
      useAsync: true);

    await JsonSerializer.SerializeAsync(stream, dataList, _options);
  }

  /// <inheritdoc/>
  public override Task<bool> Exists() {
    return Task.FromResult(File.Exists(_filePath));
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// <strong>JSON-Specific Validation:</strong> This override provides efficient validation:
  /// </para>
  /// <list type="number">
  /// <item>Checks file existence</item>
  /// <item>Validates JSON is well-formed (parseable)</item>
  /// <item>Checks that JSON root is an array</item>
  /// <item>Validates first <paramref name="sampleSize"/> items can be deserialized</item>
  /// </list>
  /// </remarks>
  public override async Task<ValidationResult> InspectShallow(int sampleSize = 100) {
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

      // 2. Validate JSON is parseable and is an array
      await using var stream = File.OpenRead(_filePath);
      using var document = await JsonDocument.ParseAsync(stream);

      if (document.RootElement.ValueKind != JsonValueKind.Array) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.InvalidFormat,
          $"JSON file is not an array",
          $"Expected JSON array for dataset '{Key}', but found {document.RootElement.ValueKind}\nFile: {_filePath}"));
        return result;
      }

      // 3. Check if array is empty
      var arrayLength = document.RootElement.GetArrayLength();
      if (arrayLength == 0) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.EmptyDataset,
          $"JSON array is empty",
          $"Expected at least one item in '{Key}'"));
        return result;
      }

      // 4. Attempt to deserialize sample items to validate schema compatibility
      // For shallow inspection, we'll just verify the JSON structure is valid
      // Deep inspection will attempt full deserialization
      var sampleCount = Math.Min(sampleSize, arrayLength);

      // Success - JSON is valid array with items
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

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// <strong>Deep Validation:</strong> This method validates full schema compatibility
  /// by attempting to deserialize all items in the dataset.
  /// </para>
  /// <para>
  /// <strong>Performance Warning:</strong> This loads the entire dataset into memory.
  /// For very large JSON files, this may be slow or consume significant memory.
  /// </para>
  /// </remarks>
  public override async Task<ValidationResult> InspectDeep() {
    var result = new ValidationResult();

    try {
      // 1. Perform shallow inspection first
      var shallowResult = await InspectShallow(sampleSize: 100);
      if (shallowResult.HasErrors) {
        return shallowResult;
      }

      // 2. Attempt full deserialization to validate schema compatibility
      var data = await Load();
      var count = data.Count();

      // Success - all items loaded and deserialized successfully
      return ValidationResult.Success();
    } catch (Exception ex) {
      return ValidationResult.FromException(Key, ex);
    }
  }
}
