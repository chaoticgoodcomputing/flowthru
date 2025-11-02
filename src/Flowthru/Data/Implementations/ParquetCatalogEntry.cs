using LanguageExt;
using Parquet;
using Parquet.Serialization;
using static LanguageExt.Prelude;

namespace Flowthru.Data.Implementations;

/// <summary>
/// Parquet file-based catalog entry.
/// Parquet is inherently columnar, so T must be a collection type.
/// </summary>
/// <typeparam name="T">Collection type (e.g., Seq&lt;TRow&gt; or IEnumerable&lt;TRow&gt;)</typeparam>
public class ParquetCatalogEntry<T> : CatalogEntryBase<T>
  where T : System.Collections.IEnumerable
{ // Parquet requires collections
  private readonly string _filePath;

  public ParquetCatalogEntry(string key, string filePath)
    : base(key)
  {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
  }

  public string FilePath => _filePath;

  public override IO<T> Load() =>
    IO.liftAsync(async () =>
    {
      if (!File.Exists(_filePath))
      {
        throw new FileNotFoundException(
          $"Parquet file not found for catalog entry '{Key}'",
          _filePath
        );
      }

      // Get element type from T
      var elementType = typeof(T).GetGenericArguments()[0];

      // Use reflection to call ParquetSerializer.DeserializeAsync<TElement>
      var deserializeMethod = typeof(ParquetSerializer)
        .GetMethods()
        .FirstOrDefault(m =>
          m.Name == nameof(ParquetSerializer.DeserializeAsync)
          && m.IsGenericMethodDefinition
          && m.GetGenericArguments().Length == 1
          && m.GetParameters().Length == 4
        )
        ?.MakeGenericMethod(elementType);

      if (deserializeMethod == null)
      {
        throw new InvalidOperationException(
          "Could not find ParquetSerializer.DeserializeAsync method"
        );
      }

      await using var stream = File.OpenRead(_filePath);
      var task = (Task?)
        deserializeMethod.Invoke(null, new object?[] { stream, 0, null, CancellationToken.None });
      if (task == null)
      {
        throw new InvalidOperationException("ParquetSerializer.DeserializeAsync returned null");
      }

      await task;
      var resultProperty = task.GetType().GetProperty("Result");
      var records = resultProperty?.GetValue(task) as System.Collections.IEnumerable;

      if (records == null)
      {
        throw new InvalidOperationException("Failed to deserialize Parquet file");
      }

      // Convert to T (Seq<TElement> or IEnumerable<TElement>)
      if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Seq<>))
      {
        // Use toSeq from Prelude
        var toSeqMethod = typeof(Prelude)
          .GetMethods()
          .First(m => m.Name == "toSeq" && m.GetParameters().Length == 1)
          .MakeGenericMethod(elementType);
        var seqResult = toSeqMethod.Invoke(null, new object[] { records });
        return (T)seqResult!;
      }

      return (T)records;
    });

  public override IO<Unit> Save(T data) =>
    IO.liftAsync(async () =>
    {
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      // Get element type
      var elementType = typeof(T).GetGenericArguments()[0];

      // Use reflection to call ParquetSerializer.SerializeAsync<TElement>
      var serializeMethod = typeof(ParquetSerializer)
        .GetMethods()
        .FirstOrDefault(m =>
          m.Name == nameof(ParquetSerializer.SerializeAsync)
          && m.IsGenericMethodDefinition
          && m.GetGenericArguments().Length == 1
          && m.GetParameters().Length == 4
        )
        ?.MakeGenericMethod(elementType);

      if (serializeMethod == null)
      {
        throw new InvalidOperationException(
          "Could not find ParquetSerializer.SerializeAsync method"
        );
      }

      await using var stream = File.Create(_filePath);
      await (Task)
        serializeMethod.Invoke(null, new object?[] { data, stream, null, CancellationToken.None })!;
      return unit;
    });

  public override IO<bool> Exists()
  {
    return IO.pure(File.Exists(_filePath));
  }
}
