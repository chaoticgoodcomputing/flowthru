using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Flowthru.Data;

/// <summary>
/// CSV file-based catalog entry for reading and writing CSV data.
/// Uses CsvHelper for serialization.
/// </summary>
/// <typeparam name="T">The record type for CSV rows</typeparam>
public class CsvCatalogEntry<T> : ICatalogEntry<IEnumerable<T>>
{
  public string Key { get; }
  public string FilePath { get; }
  public CsvConfiguration Configuration { get; }

  public CsvCatalogEntry(string key, string filePath, CsvConfiguration? configuration = null)
  {
    Key = key;
    FilePath = filePath;
    Configuration = configuration ?? new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      HasHeaderRecord = true
    };
  }

  public async Task<IEnumerable<T>> Load()
  {
    if (!File.Exists(FilePath))
    {
      throw new FileNotFoundException($"CSV file not found for catalog entry '{Key}': {FilePath}");
    }

    using var reader = new StreamReader(FilePath);
    using var csv = new CsvReader(reader, Configuration);

    var records = csv.GetRecords<T>().ToList();
    return records;
  }

  public async Task Save(IEnumerable<T> data)
  {
    var directory = Path.GetDirectoryName(FilePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }

    using var writer = new StreamWriter(FilePath);
    using var csv = new CsvWriter(writer, Configuration);

    await csv.WriteRecordsAsync(data);
  }

  public Task<bool> Exists()
  {
    return Task.FromResult(File.Exists(FilePath));
  }
}
