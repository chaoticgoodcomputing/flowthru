using System.Data;
using ExcelDataReader;
using Flowthru.Abstractions;
using Flowthru.Data.Validation;

namespace Flowthru.Data.Implementations;

/// <summary>
/// Excel file-based read-only catalog dataset using ExcelDataReader.
/// </summary>
/// <typeparam name="T">The type of data (must have parameterless constructor)</typeparam>
/// <remarks>
/// <para>
/// <strong>IMPORTANT: Read-Only Implementation</strong>
/// This catalog dataset is READ-ONLY and implements <see cref="IReadableCatalogDataset{T}"/>.
/// It cannot be used as a pipeline output. Attempting to use this as an output mapping
/// will result in a compile-time error.
/// </para>
/// <para>
/// <strong>Breaking Change (v0.4.0):</strong> Type parameter T now requires <see cref="IFlatSerializable"/> constraint.
/// Excel format cannot reliably represent nested structures. Schemas with nested data
/// should be loaded from JSON or other hierarchical formats instead.
/// </para>
/// <para>
/// <strong>Compile-Time Safety:</strong> By inheriting from <see cref="ReadOnlyCatalogDatasetBase{T}"/>,
/// this class provides no Save() method, making it impossible to accidentally write to Excel files.
/// Use CsvCatalogDataset or ParquetCatalogDataset for output datasets.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// - Reading raw data from Excel files (01_Raw layer)
/// - Data provided by business users in Excel format
/// - Legacy data sources
/// </para>
/// <para>
/// <strong>Requirements:</strong>
/// Type T must:
/// - Implement <see cref="IFlatSerializable"/> (all properties are primitives, no collections or nested objects)
/// - Have a parameterless constructor
/// - Have public properties matching Excel column names
/// - Properties should be primitive types or strings
/// </para>
/// <para>
/// <strong>Dependencies:</strong> Requires ExcelDataReader and ExcelDataReader.DataSet NuGet packages.
/// </para>
/// <para>
/// <strong>Initialization:</strong> 
/// ExcelDataReader requires one-time registration of encoding provider:
/// <code>
/// System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
/// </code>
/// </para>
/// </remarks>
public class ExcelCatalogDataset<T> : ReadOnlyCatalogDatasetBase<T>
    where T : IFlatSerializable, new() {
  private readonly string _filePath;
  private readonly string _sheetName;
  private static bool _encodingRegistered;
  private static readonly object _encodingLock = new();

  /// <summary>
  /// Creates a new Excel catalog entry.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog entry</param>
  /// <param name="filePath">Path to the Excel file (absolute or relative to working directory)</param>
  /// <param name="sheetName">Name of the worksheet to read (defaults to "Sheet1")</param>
  public ExcelCatalogDataset(string key, string filePath, string sheetName = "Sheet1")
      : base(key) {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    _sheetName = sheetName ?? throw new ArgumentNullException(nameof(sheetName));

    EnsureEncodingProviderRegistered();
  }

  /// <summary>
  /// Gets the file path for this Excel catalog entry.
  /// </summary>
  public string FilePath => _filePath;

  /// <summary>
  /// Gets the worksheet name for this Excel catalog entry.
  /// </summary>
  public string SheetName => _sheetName;

  /// <inheritdoc/>
  public override async Task<IEnumerable<T>> Load() {
    if (!File.Exists(_filePath)) {
      throw new FileNotFoundException(
          $"Excel file not found for catalog entry '{Key}'", _filePath);
    }

    using var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read);
    using var reader = ExcelReaderFactory.CreateReader(stream);

    var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration {
      ConfigureDataTable = _ => new ExcelDataTableConfiguration {
        UseHeaderRow = true
      }
    });

    var table = dataSet.Tables[_sheetName];
    if (table == null) {
      throw new InvalidOperationException(
          $"Worksheet '{_sheetName}' not found in Excel file '{_filePath}' " +
          $"for catalog entry '{Key}'");
    }

    var records = ConvertDataTableToRecords(table);

    return await Task.FromResult(records);
  }

  /// <inheritdoc/>
  public override Task<bool> Exists() {
    return Task.FromResult(File.Exists(_filePath));
  }

  private List<T> ConvertDataTableToRecords(DataTable table) {
    var records = new List<T>();
    var properties = typeof(T).GetProperties()
        .Where(p => p.CanWrite)
        .ToList();

    // Build a case-insensitive column name mapping (also handles snake_case → PascalCase)
    var columnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (DataColumn column in table.Columns) {
      columnMap[column.ColumnName] = column.ColumnName;

      // Also map snake_case to PascalCase (e.g., "company_id" → "CompanyId")
      var pascalCase = ConvertSnakeCaseToPascalCase(column.ColumnName);
      if (!columnMap.ContainsKey(pascalCase)) {
        columnMap[pascalCase] = column.ColumnName;
      }
    }

    foreach (DataRow row in table.Rows) {
      var record = new T();

      foreach (var property in properties) {
        // Try to find column by property name (case-insensitive, with snake_case support)
        if (columnMap.TryGetValue(property.Name, out var columnName)) {
          var value = row[columnName];

          if (value != DBNull.Value) {
            // Handle type conversion
            var convertedValue = Convert.ChangeType(value, property.PropertyType);
            property.SetValue(record, convertedValue);
          }
        }
      }

      records.Add(record);
    }

    return records;
  }

  /// <summary>
  /// Converts snake_case column names to PascalCase property names.
  /// Example: "company_id" → "CompanyId", "review_scores_rating" → "ReviewScoresRating"
  /// </summary>
  private static string ConvertSnakeCaseToPascalCase(string snakeCase) {
    if (string.IsNullOrWhiteSpace(snakeCase)) {
      return snakeCase;
    }

    var parts = snakeCase.Split('_', StringSplitOptions.RemoveEmptyEntries);
    return string.Concat(parts.Select(part =>
      char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant()));
  }

  private static void EnsureEncodingProviderRegistered() {
    if (!_encodingRegistered) {
      lock (_encodingLock) {
        if (!_encodingRegistered) {
          System.Text.Encoding.RegisterProvider(
              System.Text.CodePagesEncodingProvider.Instance);
          _encodingRegistered = true;
        }
      }
    }
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// <strong>Excel-Specific Validation:</strong> This override provides efficient validation
  /// without loading the entire file:
  /// </para>
  /// <list type="number">
  /// <item>Checks file existence</item>
  /// <item>Validates Excel file is readable (not corrupted)</item>
  /// <item>Checks specified worksheet exists</item>
  /// <item>Validates column names can be mapped to type T properties</item>
  /// <item>Deserializes first <paramref name="sampleSize"/> rows</item>
  /// </list>
  /// </remarks>
  public override Task<ValidationResult> InspectShallow(int sampleSize = 100) {
    var result = new ValidationResult();

    try {
      // 1. Check file existence
      if (!File.Exists(_filePath)) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.NotFound,
          $"Excel file not found",
          $"Expected file at path: {_filePath}"));
        return Task.FromResult(result);
      }

      // 2. Open Excel file and read worksheet
      using var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read);
      using var reader = ExcelReaderFactory.CreateReader(stream);

      var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration {
        ConfigureDataTable = _ => new ExcelDataTableConfiguration {
          UseHeaderRow = true
        }
      });

      // 3. Check if worksheet exists
      var table = dataSet.Tables[_sheetName];
      if (table == null) {
        var availableSheets = dataSet.Tables.Cast<DataTable>()
          .Select(t => t.TableName)
          .ToList();

        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.NotFound,
          $"Worksheet '{_sheetName}' not found in Excel file",
          $"Available worksheets: {string.Join(", ", availableSheets)}\n" +
          $"File: {_filePath}"));
        return Task.FromResult(result);
      }

      // 4. Check if worksheet is empty
      if (table.Rows.Count == 0) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.EmptyDataset,
          $"Worksheet '{_sheetName}' is empty",
          $"Expected at least one data row in '{Key}'\nFile: {_filePath}"));
        return Task.FromResult(result);
      }

      // 5. Validate column mappings
      var properties = typeof(T).GetProperties()
        .Where(p => p.CanWrite)
        .ToList();

      var columnNames = table.Columns.Cast<DataColumn>()
        .Select(c => c.ColumnName)
        .ToList();

      // Build column map (same logic as ConvertDataTableToRecords)
      var columnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (DataColumn column in table.Columns) {
        columnMap[column.ColumnName] = column.ColumnName;

        var pascalCase = ConvertSnakeCaseToPascalCase(column.ColumnName);
        if (!columnMap.ContainsKey(pascalCase)) {
          columnMap[pascalCase] = column.ColumnName;
        }
      }

      // Check for unmapped required properties
      var unmappedProperties = properties
        .Where(p => !columnMap.ContainsKey(p.Name))
        .Select(p => p.Name)
        .ToList();

      if (unmappedProperties.Any()) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.SchemaMismatch,
          $"Excel worksheet is missing columns for some properties",
          $"Unmapped properties: {string.Join(", ", unmappedProperties)}\n" +
          $"Available columns: {string.Join(", ", columnNames)}\n" +
          $"File: {_filePath}, Sheet: {_sheetName}"));
        // Note: This is a warning, not a hard failure - properties may be nullable/optional
      }

      // 6. Deserialize sample rows
      var sampleRecords = new List<T>();
      var rowsToSample = Math.Min(sampleSize, table.Rows.Count);

      for (int i = 0; i < rowsToSample; i++) {
        var row = table.Rows[i];
        var record = new T();

        foreach (var property in properties) {
          if (columnMap.TryGetValue(property.Name, out var columnName)) {
            var value = row[columnName];

            if (value != DBNull.Value) {
              try {
                var convertedValue = Convert.ChangeType(value, property.PropertyType);
                property.SetValue(record, convertedValue);
              } catch (Exception ex) {
                result.AddError(new ValidationError(
                  Key,
                  ValidationErrorType.TypeMismatch,
                  $"Type conversion failed for property '{property.Name}' at row {i + 1}",
                  $"Column: {columnName}\n" +
                  $"Value: {value}\n" +
                  $"Expected type: {property.PropertyType.Name}\n" +
                  $"Error: {ex.Message}\n" +
                  $"File: {_filePath}, Sheet: {_sheetName}"));
                return Task.FromResult(result);
              }
            }
          }
        }

        sampleRecords.Add(record);
      }

      // Success - file exists, worksheet exists, and sample rows deserialized successfully
      return Task.FromResult(ValidationResult.Success());
    } catch (IOException ex) {
      result.AddError(new ValidationError(
        Key,
        ValidationErrorType.InspectionFailure,
        $"Cannot access Excel file (may be open in another program)",
        $"File: {_filePath}\nError: {ex.Message}"));
      return Task.FromResult(result);
    } catch (Exception ex) {
      return Task.FromResult(ValidationResult.FromException(Key, ex));
    }
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// <strong>Excel-Specific Deep Validation:</strong> Validates ALL rows in the worksheet.
  /// </para>
  /// <para>
  /// This method performs shallow validation first, then loads and validates every row
  /// to ensure the entire worksheet is correctly formatted and deserializable.
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

      // 2. Load and validate ALL rows
      using var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read);
      using var reader = ExcelReaderFactory.CreateReader(stream);

      var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration {
        ConfigureDataTable = _ => new ExcelDataTableConfiguration {
          UseHeaderRow = true
        }
      });

      var table = dataSet.Tables[_sheetName];
      if (table == null) {
        // Should have been caught by shallow inspection
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.NotFound,
          $"Worksheet '{_sheetName}' not found",
          $"File: {_filePath}"));
        return result;
      }

      // Convert all rows
      var records = ConvertDataTableToRecords(table);

      if (records.Count == 0) {
        result.AddError(new ValidationError(
          Key,
          ValidationErrorType.EmptyDataset,
          $"Worksheet '{_sheetName}' is empty",
          $"File: {_filePath}"));
        return result;
      }

      // Success - all rows loaded and deserialized successfully
      return ValidationResult.Success();
    } catch (Exception ex) {
      return ValidationResult.FromException(Key, ex);
    }
  }
}
