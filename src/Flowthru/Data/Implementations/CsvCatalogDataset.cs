using System.Globalization;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Flowthru.Abstractions;
using Flowthru.Data.Validation;

namespace Flowthru.Data.Implementations;

/// <summary>
/// CSV file-based catalog dataset using CsvHelper.
/// </summary>
/// <typeparam name="T">The type of individual rows in the CSV (NOT IEnumerable&lt;T&gt;)</typeparam>
/// <remarks>
/// <para>
/// <strong>Breaking Change (v0.2.0):</strong> This class now extends CatalogDatasetBase&lt;T&gt; instead of CatalogEntryBase&lt;IEnumerable&lt;T&gt;&gt;.
/// Previously: <c>CsvCatalogEntry&lt;IEnumerable&lt;Company&gt;&gt;</c>
/// Now: <c>CsvCatalogEntry&lt;Company&gt;</c>
/// </para>
/// <para>
/// <strong>Breaking Change (v0.4.0):</strong> Type parameter T now requires <see cref="IFlatSerializable"/> constraint.
/// CSV format cannot represent nested structures (collections, nested objects). Schemas with nested data
/// must use <see cref="JsonCatalogDataset{T}"/> or <see cref="ParquetCatalogDataset{T}"/> instead.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - Raw input data from external sources (01_Raw layer)
/// - Simple tabular data export
/// - Data interchange with external systems
/// </para>
/// <para>
/// <strong>Requirements:</strong>
/// Type T must:
/// - Implement <see cref="IFlatSerializable"/> (all properties are primitives, no collections or nested objects)
/// - Have public properties matching CSV column names
/// - Have a parameterless constructor
/// - Properties should be primitive types or strings
/// </para>
/// <para>
/// <strong>Dependencies:</strong> Requires CsvHelper NuGet package.
/// </para>
/// <para>
/// <strong>Default Configuration:</strong>
/// - HasHeaderRecord = true
/// - CultureInfo = InvariantCulture
/// - Custom configuration can be provided via constructor
/// </para>
/// </remarks>
public class CsvCatalogDataset<T> : CatalogDatasetBase<T>
    where T : IFlatSerializable, new() {
  private readonly string _filePath;
  private readonly CsvConfiguration _configuration;

  /// <summary>
  /// Creates a new CSV catalog entry with default configuration.
  /// Uses attribute-based mapping from the type T (e.g., [Name("column_name")] attributes).
  /// </summary>
  /// <param name="key">Unique identifier for this catalog entry</param>
  /// <param name="filePath">Path to the CSV file (absolute or relative to working directory)</param>
  public CsvCatalogDataset(string key, string filePath)
      : this(key, filePath, new CsvConfiguration(CultureInfo.InvariantCulture, typeof(T)) {
        HasHeaderRecord = true
      }) {
  }

  /// <summary>
  /// Creates a new CSV catalog entry with custom configuration.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog entry</param>
  /// <param name="filePath">Path to the CSV file</param>
  /// <param name="configuration">CsvHelper configuration</param>
  public CsvCatalogDataset(string key, string filePath, CsvConfiguration configuration)
      : base(key) {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <summary>
  /// Gets the file path for this CSV catalog entry.
  /// </summary>
  public string FilePath => _filePath;

  /// <summary>
  /// Gets the CsvHelper configuration for this catalog entry.
  /// </summary>
  public CsvConfiguration Configuration => _configuration;

  /// <inheritdoc/>
  public override async Task<IEnumerable<T>> Load() {
    if (!File.Exists(_filePath)) {
      throw new FileNotFoundException(
          $"CSV file not found for catalog entry '{Key}'", _filePath);
    }

    // Use an async FileStream to avoid blocking thread pool on large files
    await using var stream = new FileStream(
      _filePath,
      FileMode.Open,
      FileAccess.Read,
      FileShare.Read,
      bufferSize: 4096,
      useAsync: true);

    using var reader = new StreamReader(stream);
    using var csv = new CsvReader(reader, _configuration);

    var records = new List<T>();
    await foreach (var record in csv.GetRecordsAsync<T>()) {
      records.Add(record);
    }

    return records;
  }

  /// <inheritdoc/>
  public override Task Save(IEnumerable<T> data) {
    // Ensure directory exists
    var directory = Path.GetDirectoryName(_filePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
      Directory.CreateDirectory(directory);
    }

    using var writer = new StreamWriter(_filePath);
    using var csv = new CsvWriter(writer, _configuration);

    csv.WriteRecords(data);

    return Task.CompletedTask;
  }

  /// <inheritdoc/>
  public override Task<bool> Exists() {
    return Task.FromResult(File.Exists(_filePath));
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// <strong>CSV-Specific Validation:</strong> This override provides efficient validation
  /// without loading the entire file:
  /// </para>
  /// <list type="number">
  /// <item>Checks file existence</item>
  /// <item>Validates CSV is parseable (not corrupted)</item>
  /// <item>Checks CSV headers match [Name] attributes on type T</item>
  /// <item>Deserializes first <paramref name="sampleSize"/> rows</item>
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
          $"CSV file not found",
          $"Expected file at path: {_filePath}"));
        return result;
      }

      // 2. Open file and read headers
      await using var stream = new FileStream(
        _filePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 4096,
        useAsync: true);

      using var reader = new StreamReader(stream);
      using var csv = new CsvReader(reader, _configuration);

      // 3. Read header record
      await csv.ReadAsync();
      csv.ReadHeader();
      var headerRecord = csv.HeaderRecord;

      if (headerRecord == null || headerRecord.Length == 0) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.InvalidFormat,
          $"CSV file has no header record",
          $"File: {_filePath}"));
        return result;
      }

      // 4. Validate headers match [Name] attributes
      var expectedColumns = GetExpectedColumnNames();
      var missingColumns = expectedColumns.Except(headerRecord, StringComparer.OrdinalIgnoreCase).ToList();

      if (missingColumns.Any()) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.SchemaMismatch,
          $"CSV file is missing required columns",
          $"Missing columns: {string.Join(", ", missingColumns)}\n" +
          $"Found columns: {string.Join(", ", headerRecord)}\n" +
          $"File: {_filePath}"));
        return result;
      }

      // 5. Deserialize sample rows
      var sampleRecords = new List<T>();
      var rowNumber = 1; // Header is row 0

      await foreach (var record in csv.GetRecordsAsync<T>()) {
        sampleRecords.Add(record);
        rowNumber++;

        if (sampleRecords.Count >= sampleSize) {
          break;
        }
      }

      // 6. Check if empty
      if (sampleRecords.Count == 0) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.EmptyDataset,
          $"CSV file is empty",
          $"Expected at least one data row in '{Key}'\nFile: {_filePath}"));
        return result;
      }

      // Success - file exists, headers match, and sample rows deserialized successfully
      return ValidationResult.Success();
    } catch (CsvHelper.HeaderValidationException ex) {
      result.AddError(new ValidationError(
        Key,
        ValidationErrorType.SchemaMismatch,
        $"CSV headers do not match expected schema",
        $"File: {_filePath}\nError: {ex.Message}"));
      return result;
    } catch (CsvHelper.ReaderException ex) {
      result.AddError(new ValidationError(
        Key,
        ValidationErrorType.InvalidFormat,
        $"CSV file is malformed or corrupted",
        $"File: {_filePath}\nRow: {ex.Context?.Parser?.Row ?? -1}\nError: {ex.Message}"));
      return result;
    } catch (Exception ex) {
      return ValidationResult.FromException(Key, ex);
    }
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// <strong>CSV-Specific Deep Validation:</strong> Validates ALL rows in the CSV file.
  /// </para>
  /// <para>
  /// This method performs shallow validation first, then loads and validates every row
  /// to ensure the entire file is correctly formatted and deserializable.
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

      // 2. Load ALL rows to validate entire file
      await using var stream = new FileStream(
        _filePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 4096,
        useAsync: true);

      using var reader = new StreamReader(stream);
      using var csv = new CsvReader(reader, _configuration);

      var rowCount = 0;

      await foreach (var record in csv.GetRecordsAsync<T>()) {
        rowCount++;
        // Record loaded successfully - validation happens implicitly during deserialization
      }

      if (rowCount == 0) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.EmptyDataset,
          $"CSV file is empty",
          $"File: {_filePath}"));
        return result;
      }

      // Success - all rows loaded and deserialized successfully
      return ValidationResult.Success();
    } catch (CsvHelper.ReaderException ex) {
      result.AddError(new ValidationError(
        Key,
        ValidationErrorType.DeserializationError,
        $"CSV row failed to deserialize during deep inspection",
        $"File: {_filePath}\nRow: {ex.Context?.Parser?.Row ?? -1}\nError: {ex.Message}"));
      return result;
    } catch (Exception ex) {
      return ValidationResult.FromException(Key, ex);
    }
  }

  /// <summary>
  /// Extracts expected column names from [Name] attributes on type T properties.
  /// </summary>
  private static List<string> GetExpectedColumnNames() {
    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var columnNames = new List<string>();

    foreach (var property in properties) {
      var nameAttribute = property.GetCustomAttribute<NameAttribute>();
      if (nameAttribute != null && nameAttribute.Names.Length > 0) {
        // Use first name from [Name("column_name")] attribute
        columnNames.Add(nameAttribute.Names[0]);
      } else {
        // Fallback to property name if no [Name] attribute
        columnNames.Add(property.Name);
      }
    }

    return columnNames;
  }
}
