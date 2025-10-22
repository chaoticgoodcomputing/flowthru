using Parquet;
using Parquet.Serialization;

namespace Flowthru.Data.Implementations;

/// <summary>
/// Parquet file-based catalog entry using Parquet.NET.
/// </summary>
/// <typeparam name="T">The type of data (must have parameterless constructor)</typeparam>
/// <remarks>
/// <para>
/// <strong>Use Cases:</strong>
/// - Intermediate processed data (02_Intermediate layer)
/// - Model input tables (03_Primary layer)
/// - High-performance columnar storage
/// - Large datasets that benefit from compression
/// </para>
/// <para>
/// <strong>Requirements:</strong>
/// Type T must:
/// - Have a parameterless constructor
/// - Have public properties for data members
/// - Be compatible with Parquet schema mapping
/// </para>
/// <para>
/// <strong>Performance:</strong>
/// Parquet provides:
/// - Columnar storage format (better compression)
/// - Efficient querying of specific columns
/// - Good for analytics workloads
/// </para>
/// <para>
/// <strong>Dependencies:</strong> Requires Parquet.Net NuGet package.
/// </para>
/// </remarks>
public class ParquetCatalogDataset<T> : CatalogDatasetBase<T>
    where T : new() {
  private readonly string _filePath;

  /// <summary>
  /// Creates a new Parquet catalog entry.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog entry</param>
  /// <param name="filePath">Path to the Parquet file (absolute or relative to working directory)</param>
  public ParquetCatalogDataset(string key, string filePath)
      : base(key) {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
  }

  /// <summary>
  /// Gets the file path for this Parquet catalog entry.
  /// </summary>
  public string FilePath => _filePath;

  /// <inheritdoc/>
  public override async Task<IEnumerable<T>> Load() {
    if (!File.Exists(_filePath)) {
      throw new FileNotFoundException(
          $"Parquet file not found for catalog entry '{Key}'", _filePath);
    }

    // ParquetSerializer.DeserializeAsync in v5.x accepts file path directly
    var records = await ParquetSerializer.DeserializeAsync<T>(_filePath);

    return records;
  }

  /// <inheritdoc/>
  public override async Task Save(IEnumerable<T> data) {
    // Ensure directory exists
    var directory = Path.GetDirectoryName(_filePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
      Directory.CreateDirectory(directory);
    }

    // ParquetSerializer.SerializeAsync in v5.x accepts file path directly
    await ParquetSerializer.SerializeAsync(data, _filePath);
  }

  /// <inheritdoc/>
  public override Task<bool> Exists() {
    return Task.FromResult(File.Exists(_filePath));
  }
}
