using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

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
/// <strong>Use Cases:</strong>
/// - Raw input data from external sources (01_Raw layer)
/// - Simple tabular data export
/// - Data interchange with external systems
/// </para>
/// <para>
/// <strong>Requirements:</strong>
/// Type T should have:
/// - Public properties matching CSV column names
/// - Parameterless constructor
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
public class CsvCatalogDataset<T> : CatalogDatasetBase<T> {
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
}
