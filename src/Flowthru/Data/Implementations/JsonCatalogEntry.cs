using System.Collections;
using System.Text.Json;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Flowthru.Data.Implementations;

/// <summary>
/// JSON file-based catalog entry using System.Text.Json.
/// Supports both singleton objects and collections.
/// </summary>
/// <typeparam name="T">
/// The data type to store.
/// For collections: Use Seq&lt;TItem&gt; or IEnumerable&lt;TItem&gt;
/// For singletons: Use TItem directly
/// </typeparam>
public class JsonCatalogEntry<T> : CatalogEntryBase<T> {
  private readonly string _filePath;
  private readonly JsonSerializerOptions _options;

  public JsonCatalogEntry(string key, string filePath, bool minified = false)
      : this(key, filePath, new JsonSerializerOptions {
        WriteIndented = !minified,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
      }) {
  }

  public JsonCatalogEntry(string key, string filePath, JsonSerializerOptions options)
      : base(key) {
    _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    _options = options ?? throw new ArgumentNullException(nameof(options));
  }

  public override Aff<T> Load() {
    return Aff(async () => {
      if (!File.Exists(_filePath)) {
        throw new FileNotFoundException(
            $"JSON file not found for catalog entry '{Key}'", _filePath);
      }

      await using var stream = File.OpenRead(_filePath);
      var result = await JsonSerializer.DeserializeAsync<T>(stream, _options);
      return result ?? throw new InvalidOperationException(
        $"Failed to deserialize JSON from '{_filePath}' for catalog entry '{Key}'");
    });
  }

  public override Aff<Unit> Save(T data) {
    return Aff(async () => {
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
        Directory.CreateDirectory(directory);
      }

      await using var stream = File.Create(_filePath);
      await JsonSerializer.SerializeAsync(stream, data, _options);
      return unit;
    });
  }

  public override Aff<bool> Exists() {
    return Aff(async () => File.Exists(_filePath));
  }
}
