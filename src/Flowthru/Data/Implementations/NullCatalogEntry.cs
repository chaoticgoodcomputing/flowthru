using LanguageExt;
using static LanguageExt.Prelude;

namespace Flowthru.Data.Implementations;

/// <summary>
/// A null implementation of ICatalogEntry that does nothing.
/// </summary>
/// <typeparam name="T">The type this entry nominally contains (typically NoData)</typeparam>
/// <remarks>
/// <para>
/// This catalog entry is used for nodes that:
/// <list type="bullet">
/// <item><description>Have no inputs (e.g., data generators)</description></item>
/// <item><description>Produce no outputs (e.g., side-effect-only nodes like loggers)</description></item>
/// <item><description>Need to satisfy type requirements without meaningful data flow</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage:</strong> Typically used with the <see cref="Nodes.NoData"/> type
/// for nodes that don't consume or produce meaningful data. It satisfies the ICatalogEntry
/// interface contract but performs no actual I/O operations.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// // No-input node (data generator)
/// .AddNode("generator", () => NoData.Input, catalog.GeneratedData)
///
/// // No-output node (side-effect only)
/// .AddNode("logger", catalog.LogData, () => NoData.Output)
/// </code>
/// </para>
/// </remarks>
public class NullCatalogEntry<T> : CatalogEntryBase<T> {
  /// <summary>
  /// Initializes a new instance of NullCatalogEntry with the specified key.
  /// </summary>
  /// <param name="key">Unique identifier for this entry (e.g., "_nodata_input_1")</param>
  public NullCatalogEntry(string key) : base(key) { }

  /// <summary>
  /// Always returns false - null entries don't persist data.
  /// </summary>
  public override IO<bool> Exists() => IO.pure(false);

  /// <summary>
  /// Returns an empty result immediately without performing any I/O.
  /// </summary>
  /// <returns>Effect that returns default(T) immediately</returns>
  public override IO<T> Load() {
    // For NullCatalogEntry, return default value
    if (typeof(T) == typeof(Nodes.NoData)) {
      return IO.pure((T)(object)Nodes.NoData.Value);
    }

    // For collection types, return empty Seq
    var type = typeof(T);
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Seq<>)) {
      var elementType = type.GetGenericArguments()[0];
      var emptyMethod = typeof(Seq)
        .GetMethod("empty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
        .MakeGenericMethod(elementType);
      var emptySeq = emptyMethod.Invoke(null, null);
      return IO.pure((T)emptySeq!);
    }

    // Default: return default(T)
    return IO.pure(default(T)!);
  }

  /// <summary>
  /// Does nothing - discards the data immediately.
  /// </summary>
  /// <param name="data">Data to discard</param>
  /// <returns>Effect that completes immediately</returns>
  public override IO<Unit> Save(T data) {
    // NullCatalogEntry discards all data
    return IO.pure(unit);
  }
}
