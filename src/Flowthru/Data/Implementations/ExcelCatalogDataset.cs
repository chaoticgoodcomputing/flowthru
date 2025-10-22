using System.Data;
using ExcelDataReader;

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
    where T : new() {
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
}
