using System.Data;
using ExcelDataReader;
using Flowthru.Abstractions;
using Flowthru.Data.Validation;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Flowthru.Data.Implementations;

/// <summary>
/// Excel file-based read-only catalog entry using ExcelDataReader.
/// </summary>
/// <typeparam name="T">The item type (must implement IFlatSerializable and have parameterless constructor)</typeparam>
/// <remarks>
/// <para>
/// <strong>IMPORTANT: Read-Only Implementation</strong>
/// This catalog entry is READ-ONLY. The Save() method will throw <see cref="NotSupportedException"/>.
/// It cannot be used as a pipeline output.
/// </para>
/// <para>
/// <strong>Type Parameter:</strong> Use <c>IEnumerable&lt;T&gt;</c> for collection entries.
/// Example: <c>ExcelCatalogEntry&lt;IEnumerable&lt;ShuttleRawSchema&gt;&gt;</c>
/// </para>
/// <para>
/// <strong>Compile-Time Safety:</strong> By implementing <see cref="IReadableCatalogEntry{T}"/>,
/// this class makes it impossible to accidentally write to Excel files in pipeline mappings.
/// Use CsvCatalogEntry or ParquetCatalogEntry for output datasets.
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
public class ExcelCatalogEntry<T> : CatalogEntryBase<IEnumerable<T>>,
    IReadableCatalogEntry<IEnumerable<T>>
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
  public ExcelCatalogEntry(string key, string filePath, string sheetName = "Sheet1")
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
  public override IO<IEnumerable<T>> Load() {
    return IO.liftAsync(async () => {
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

      return await Task.FromResult<IEnumerable<T>>(records);
    });
  }

  /// <inheritdoc/>
  /// <exception cref="NotSupportedException">Always thrown - Excel files are read-only</exception>
  public override IO<Unit> Save(IEnumerable<T> data) {
    return IO.lift(() => throw new NotSupportedException(
        $"Cannot save to Excel catalog entry '{Key}'. " +
        "Excel entries are read-only. Use CsvCatalogEntry or ParquetCatalogEntry for writable datasets."));
  }

  /// <inheritdoc/>
  public override IO<bool> Exists() {
    return IO.liftAsync(async () => File.Exists(_filePath));
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

  /// <summary>
  /// Performs shallow inspection of the Excel file (validates structure and sample rows).
  /// </summary>
  public override IO<ValidationResult> InspectShallow(int sampleSize = 100) {
    return IO.liftAsync(async () => {
      try {
        // 1. Check file existence
        if (!File.Exists(_filePath)) {
          return new ValidationResult(new[] {
            new ValidationError(Key, ValidationErrorType.NotFound, $"Excel file not found: {_filePath}")
          });
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

          return new ValidationResult(new[] {
            new ValidationError(Key, ValidationErrorType.SchemaMismatch,
              $"Worksheet '{_sheetName}' not found in Excel file",
              $"Available worksheets: {string.Join(", ", availableSheets)}")
          });
        }

        // 4. Check if worksheet is empty
        if (table.Rows.Count == 0) {
          return new ValidationResult(new[] {
            new ValidationError(Key, ValidationErrorType.EmptyDataset, $"Worksheet '{_sheetName}' is empty")
          });
        }

        // 5. Validate sample rows
        var rowsToSample = Math.Min(sampleSize, table.Rows.Count);

        // Build column map for validation
        var properties = typeof(T).GetProperties().Where(p => p.CanWrite).ToList();
        var columnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataColumn column in table.Columns) {
          columnMap[column.ColumnName] = column.ColumnName;
          var pascalCase = ConvertSnakeCaseToPascalCase(column.ColumnName);
          if (!columnMap.ContainsKey(pascalCase)) {
            columnMap[pascalCase] = column.ColumnName;
          }
        }

        // Try to convert sample rows
        for (int i = 0; i < rowsToSample; i++) {
          var row = table.Rows[i];
          foreach (var property in properties) {
            if (columnMap.TryGetValue(property.Name, out var columnName)) {
              var value = row[columnName];
              if (value != DBNull.Value) {
                try {
                  Convert.ChangeType(value, property.PropertyType);
                } catch (Exception ex) {
                  return new ValidationResult(new[] {
                    new ValidationError(Key, ValidationErrorType.TypeMismatch,
                      $"Type conversion failed for property '{property.Name}' at row {i + 1}",
                      ex.Message)
                  });
                }
              }
            }
          }
        }

        return new ValidationResult(); // Success - no errors
      } catch (IOException ex) {
        return new ValidationResult(new[] {
          new ValidationError(Key, ValidationErrorType.InspectionFailure,
            "Cannot access Excel file (may be open in another program)", ex.Message)
        });
      } catch (Exception ex) {
        return new ValidationResult(new[] {
          new ValidationError(Key, ValidationErrorType.InspectionFailure, "Failed to inspect Excel file", ex.Message)
        });
      }
    });
  }

  /// <summary>
  /// Performs deep inspection of the Excel file (validates all rows).
  /// </summary>
  public override IO<ValidationResult> InspectDeep() {
    return IO.liftAsync(async () => {
      try {
        // Load and validate ALL rows
        using var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration {
          ConfigureDataTable = _ => new ExcelDataTableConfiguration {
            UseHeaderRow = true
          }
        });

        var table = dataSet.Tables[_sheetName];
        if (table == null) {
          return new ValidationResult(new[] {
            new ValidationError(Key, ValidationErrorType.SchemaMismatch, $"Worksheet '{_sheetName}' not found")
          });
        }

        // Convert all rows
        var records = ConvertDataTableToRecords(table);

        if (records.Count == 0) {
          return new ValidationResult(new[] {
            new ValidationError(Key, ValidationErrorType.EmptyDataset, $"Worksheet '{_sheetName}' is empty")
          });
        }

        return new ValidationResult(); // Success - no errors
      } catch (Exception ex) {
        return new ValidationResult(new[] {
          new ValidationError(Key, ValidationErrorType.InspectionFailure, "Failed to load all rows", ex.Message)
        });
      }
    });
  }
}
