using Flowthru.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Flowthru.Data.Storage.Strategies;

/// <summary>
/// CSV file-based storage strategy for local development.
/// </summary>
/// <remarks>
/// <para>
/// Uses CSV files for data storage, enabling:
/// </para>
/// <list type="bullet">
/// <item>Easy inspection with text editors or spreadsheet tools</item>
/// <item>Version control-friendly (human-readable diffs)</item>
/// <item>No external dependencies (no database required)</item>
/// </list>
/// <para>
/// <strong>Path Resolution:</strong>
/// </para>
/// <code>
/// // With explicit path
/// factory.CreateEnumerable&lt;Company&gt;("Companies",
///     StorageOptions.WithPath("_01_Raw/data.csv"))
/// // → {BasePath}/_01_Raw/data.csv
///
/// // With default path (label-based)
/// factory.CreateEnumerable&lt;Company&gt;("Companies")
/// // → {BasePath}/Companies.csv
/// </code>
/// </remarks>
public sealed class CsvStorageEntryFactory : IStorageEntryFactory
{
  private readonly string _basePath;

  /// <summary>
  /// Initializes a new CSV storage factory.
  /// </summary>
  /// <param name="configuration">Configuration containing optional DataPath setting</param>
  public CsvStorageEntryFactory(IConfiguration configuration)
  {
    _basePath = configuration["Flowthru:DataPath"] ?? "Data";
  }

  /// <summary>
  /// Initializes a new CSV storage factory with explicit base path.
  /// </summary>
  /// <param name="basePath">Base directory for all CSV files</param>
  public CsvStorageEntryFactory(string basePath)
  {
    _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
  }

  /// <inheritdoc />
  public ICatalogEntry<IEnumerable<T>> CreateEnumerable<T>(
    string label,
    StorageOptions? options = null
  )
    where T : notnull, IFlatSchema, ITextSerializable
  {
    var path = ResolvePath(label, options, ".csv");
    return CatalogEntries.Enumerable.Csv<T>(label, path);
  }

  /// <inheritdoc />
  public ICatalogEntry<T> CreateSingle<T>(string label, StorageOptions? options = null)
    where T : IStructuredSerializable
  {
    var path = ResolvePath(label, options, ".json");
    return CatalogEntries.Single.Json<T>(label, path);
  }

  private string ResolvePath(string label, StorageOptions? options, string defaultExtension)
  {
    if (options?.Path != null)
    {
      // Use explicit path (relative to base path)
      return Path.Combine(_basePath, options.Path);
    }

    // Generate default path from label
    return Path.Combine(_basePath, $"{label}{defaultExtension}");
  }
}
