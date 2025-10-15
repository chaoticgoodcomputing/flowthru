using Parquet;
using Parquet.Serialization;

namespace Flowthru.Data;

/// <summary>
/// Parquet file-based catalog entry for reading and writing columnar data.
/// Uses Parquet.NET for serialization.
/// </summary>
/// <typeparam name="T">The record type for parquet rows</typeparam>
public class ParquetCatalogEntry<T> : ICatalogEntry<IEnumerable<T>> where T : new()
{
  public string Key { get; }
  public string FilePath { get; }

  public ParquetCatalogEntry(string key, string filePath)
  {
    Key = key;
    FilePath = filePath;
  }

  public async Task<IEnumerable<T>> Load()
  {
    if (!File.Exists(FilePath))
    {
      throw new FileNotFoundException($"Parquet file not found for catalog entry '{Key}': {FilePath}");
    }

    var records = await ParquetSerializer.DeserializeAsync<T>(FilePath);
    return records;
  }

  public async Task Save(IEnumerable<T> data)
  {
    var directory = Path.GetDirectoryName(FilePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }

    await ParquetSerializer.SerializeAsync(data, FilePath);
  }

  public Task<bool> Exists()
  {
    return Task.FromResult(File.Exists(FilePath));
  }
}
