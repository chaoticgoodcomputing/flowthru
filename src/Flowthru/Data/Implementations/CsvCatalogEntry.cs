using System.Collections;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Flowthru.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Flowthru.Data.Implementations;

/// <summary>
/// CSV file-based catalog entry using CsvHelper.
/// Supports collections (typical) or singletons (single-row CSV).
/// </summary>
/// <typeparam name="T">
/// The data type to store.
/// For collections: Use Seq&lt;TRow&gt; or IEnumerable&lt;TRow&gt; where TRow : IFlatSerializable
/// For singletons: Use TRow directly where TRow : IFlatSerializable
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Unified Design:</strong> This single implementation handles both collection and singleton storage.
/// CSV files naturally represent collections, but can also represent a single record.
/// </para>
/// <para>
/// <strong>Typical Usage (Collections):</strong>
/// <code>
/// var companies = new CsvCatalogEntry&lt;Seq&lt;CompanySchema&gt;&gt;("companies", "data/companies.csv");
/// </code>
/// </para>
/// <para>
/// <strong>Singleton Usage (Rare):</strong>
/// <code>
/// var config = new CsvCatalogEntry&lt;ConfigSchema&gt;("config", "data/config.csv");
/// // Expects single-row CSV
/// </code>
/// </para>
/// <para>
/// <strong>Requirements:</strong>
/// - Row type must implement <see cref="IFlatSerializable"/> (no nested structures)
/// - Row type must have parameterless constructor
/// - Properties should be primitive types or strings
/// </para>
/// </remarks>
public class CsvCatalogEntry<T> : CatalogEntryBase<T> {
  private readonly string _filePath;
  private readonly CsvConfiguration _configuration;

  /// <summary>
  /// Creates a new CSV catalog entry with default configuration.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog entry</param>
  /// <param name="filePath">Path to the CSV file (absolute or relative to working directory)</param>
  public CsvCatalogEntry(string key, string filePath)
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
  public CsvCatalogEntry(string key, string filePath, CsvConfiguration configuration)
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
  public override Aff<T> Load() {
    return Aff(async () => {
      if (!File.Exists(_filePath)) {
        throw new FileNotFoundException(
            $"CSV file not found for catalog entry '{Key}'", _filePath);
      }

      var type = typeof(T);

      // Determine if T is a collection type
      if (IsCollectionType(type)) {
        // T is IEnumerable<TRow> or Seq<TRow>
        var elementType = GetCollectionElementType(type);

        await using var stream = new FileStream(
          _filePath,
          FileMode.Open,
          FileAccess.Read,
          FileShare.Read,
          bufferSize: 4096,
          useAsync: true);

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, _configuration);

        // Read all records
        var records = new List<object?>();
        await foreach (var record in csv.GetRecordsAsync(elementType)) {
          records.Add(record);
        }

        // Convert to T (Seq<TRow> or IEnumerable<TRow>)
        return ConvertToCollectionType(records, type);
      } else {
        // T is TRow (singleton) - read first record only
        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _configuration);

        await foreach (var record in csv.GetRecordsAsync<T>()) {
          return record; // Return first record
        }

        throw new InvalidOperationException(
          $"CSV file '{_filePath}' for catalog entry '{Key}' contains no data");
      }
    });
  }

  /// <inheritdoc/>
  public override Aff<Unit> Save(T data) {
    return Aff(async () => {
      // Ensure directory exists
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
        Directory.CreateDirectory(directory);
      }

      using var writer = new StreamWriter(_filePath);
      using var csv = new CsvWriter(writer, _configuration);

      var type = typeof(T);

      if (IsCollectionType(type)) {
        // T is IEnumerable<TRow> or Seq<TRow>
        if (data is IEnumerable enumerable) {
          csv.WriteRecords(enumerable);
        } else {
          throw new InvalidOperationException(
            $"Expected IEnumerable for catalog entry '{Key}', got {data?.GetType().Name ?? "null"}");
        }
      } else {
        // T is TRow (singleton) - write single record
        csv.WriteRecords(new[] { data });
      }

      return unit;
    });
  }

  /// <inheritdoc/>
  public override Aff<bool> Exists() {
    return Aff(async () => File.Exists(_filePath));
  }

  private static bool IsCollectionType(Type type) {
    if (type == typeof(string))
      return false;
    return typeof(IEnumerable).IsAssignableFrom(type);
  }

  private static Type GetCollectionElementType(Type collectionType) {
    if (collectionType.IsGenericType) {
      var genericArgs = collectionType.GetGenericArguments();
      if (genericArgs.Length > 0) {
        return genericArgs[0];
      }
    }

    // Fall back to object if we can't determine element type
    return typeof(object);
  }

  private static T ConvertToCollectionType(List<object?> records, Type targetType) {
    // If T is Seq<TElement>
    if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Seq<>)) {
      var elementType = targetType.GetGenericArguments()[0];
      var typedList = typeof(Enumerable)
        .GetMethod(nameof(Enumerable.Cast))!
        .MakeGenericMethod(elementType)
        .Invoke(null, new object[] { records });

      // Use toSeq from Prelude
      var toSeqMethod = typeof(Prelude).GetMethods()
        .First(m => m.Name == "toSeq" && m.GetParameters().Length == 1)
        .MakeGenericMethod(elementType);

      var seqResult = toSeqMethod.Invoke(null, new[] { typedList });
      return (T)seqResult!;
    }

    // If T is IEnumerable<TElement> or other collection interface
    if (targetType.IsInterface || targetType.IsAbstract) {
      // Return as IEnumerable
      var elementType = GetCollectionElementType(targetType);
      var typedEnumerable = typeof(Enumerable)
        .GetMethod(nameof(Enumerable.Cast))!
        .MakeGenericMethod(elementType)
        .Invoke(null, new object[] { records });

      return (T)typedEnumerable!;
    }

    // For concrete collection types, try to instantiate
    throw new NotSupportedException(
      $"CSV catalog entry does not support collection type '{targetType.Name}'. Use Seq<T> or IEnumerable<T>.");
  }
}
