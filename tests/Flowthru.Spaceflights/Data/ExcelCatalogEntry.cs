using ExcelDataReader;
using System.Data;

namespace Flowthru.Data;

/// <summary>
/// Excel file-based catalog entry for reading Excel workbooks.
/// Uses ExcelDataReader for deserialization.
/// 
/// <para><strong>Note:</strong></para>
/// <para>
/// Save operation is not currently supported for Excel files.
/// Consider using CSV or Parquet for output datasets.
/// </para>
/// </summary>
/// <typeparam name="T">The record type for Excel rows</typeparam>
public class ExcelCatalogEntry<T> : ICatalogEntry<IEnumerable<T>> where T : new()
{
  public string Key { get; }
  public string FilePath { get; }
  public string SheetName { get; }

  static ExcelCatalogEntry()
  {
    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
  }

  public ExcelCatalogEntry(string key, string filePath, string sheetName = "Sheet1")
  {
    Key = key;
    FilePath = filePath;
    SheetName = sheetName;
  }

  public async Task<IEnumerable<T>> Load()
  {
    if (!File.Exists(FilePath))
    {
      throw new FileNotFoundException($"Excel file not found for catalog entry '{Key}': {FilePath}");
    }

    using var stream = File.Open(FilePath, FileMode.Open, FileAccess.Read);
    using var reader = ExcelReaderFactory.CreateReader(stream);

    var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
    {
      ConfigureDataTable = _ => new ExcelDataTableConfiguration
      {
        UseHeaderRow = true
      }
    });

    var table = dataSet.Tables[SheetName]
      ?? throw new InvalidOperationException($"Sheet '{SheetName}' not found in Excel file '{FilePath}'");

    var records = new List<T>();
    var properties = typeof(T).GetProperties();

    foreach (DataRow row in table.Rows)
    {
      var record = new T();
      foreach (var prop in properties)
      {
        if (table.Columns.Contains(prop.Name))
        {
          var value = row[prop.Name];
          if (value != DBNull.Value)
          {
            prop.SetValue(record, Convert.ChangeType(value, prop.PropertyType));
          }
        }
      }
      records.Add(record);
    }

    return records;
  }

  public Task Save(IEnumerable<T> data)
  {
    throw new NotSupportedException("Excel write operations are not currently supported. Use CsvCatalogEntry or ParquetCatalogEntry for outputs.");
  }

  public Task<bool> Exists()
  {
    return Task.FromResult(File.Exists(FilePath));
  }
}
