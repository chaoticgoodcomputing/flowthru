using Flowthru.Abstractions;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Container;
using Flowthru.Data.Storage.Format;
using Flowthru.Data.Storage.Medium;
using LanguageExt;

namespace Flowthru.Data;

public static partial class CatalogEntries
{
  /// <summary>
  /// Factory methods for LanguageExt Seq&lt;T&gt; container types.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Seq&lt;T&gt; is LanguageExt's functional, immutable sequence type.
  /// </para>
  /// <para>
  /// <strong>Characteristics:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item><strong>Lazy + Memoized:</strong> Deferred evaluation, results cached after first enumeration</item>
  /// <item><strong>Immutable:</strong> Cannot modify after creation</item>
  /// <item><strong>Safe re-enumeration:</strong> Cached, so safe to iterate multiple times</item>
  /// <item><strong>Null-safe:</strong> null becomes empty sequence</item>
  /// <item><strong>Functional API:</strong> Built-in Map, Filter, Fold, Bind, etc.</item>
  /// </list>
  /// <para>
  /// <strong>Use Cases:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>Functional programming style pipelines</item>
  /// <item>Data that will be enumerated multiple times (prevents side effects)</item>
  /// <item>When immutability guarantees are important</item>
  /// <item>Working with LanguageExt effects (IO&lt;Seq&lt;T&gt;&gt;)</item>
  /// </list>
  /// </remarks>
  public static class Seq
  {
    /// <summary>
    /// Creates a LanguageExt Seq catalog entry from a CSV file.
    /// </summary>
    /// <typeparam name="TRow">Row schema type (must be flat and text-serializable)</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="filePath">Path to CSV file</param>
    /// <returns>Catalog entry with file + CSV + Seq composition</returns>
    /// <remarks>
    /// <para>
    /// <strong>Benefits of Seq:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>Lazy evaluation with memoization</item>
    /// <item>Immutable - safe to pass around</item>
    /// <item>Functional operations (Map, Filter, Fold)</item>
    /// <item>Safe re-enumeration (no multiple DB/file hits)</item>
    /// </list>
    /// <para>
    /// <strong>Schema Support:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>Traditional schemas with parameterless constructors</item>
    /// <item>Modern schemas with required properties (C# 11+)</item>
    /// <item>Positional records with primary constructors</item>
    /// </list>
    /// </remarks>
    public static ICatalogEntry<LanguageExt.Seq<TRow>> Csv<TRow>(string label, string filePath)
      where TRow : notnull, IFlatSchema, ITextSerializable
    {
      var medium = new FileStorageMedium(filePath);
      var format = new CsvFormatSerializer<TRow>();
      var container = new SeqContainerAdapter<TRow>();
      var storage = new ComposedStorageAdapter<LanguageExt.Seq<TRow>, TRow>(
        medium,
        format,
        container
      );

      return new CatalogEntry<LanguageExt.Seq<TRow>>(label, storage);
    }

    /// <summary>
    /// Creates a LanguageExt Seq catalog entry from a JSON file.
    /// </summary>
    /// <typeparam name="TRow">Row schema type (must be structured-serializable)</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="filePath">Path to JSON file</param>
    /// <returns>Catalog entry with file + JSON + Seq composition</returns>
    /// <remarks>
    /// <para>
    /// <strong>Schema Support:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>Traditional schemas with parameterless constructors</item>
    /// <item>Modern schemas with required properties (C# 11+)</item>
    /// <item>Positional records with primary constructors</item>
    /// </list>
    /// </remarks>
    public static ICatalogEntry<LanguageExt.Seq<TRow>> Json<TRow>(string label, string filePath)
      where TRow : notnull, IStructuredSerializable
    {
      var medium = new FileStorageMedium(filePath);
      var format = new JsonFormatSerializer<TRow>();
      var container = new SeqContainerAdapter<TRow>();
      var storage = new ComposedStorageAdapter<LanguageExt.Seq<TRow>, TRow>(
        medium,
        format,
        container
      );

      return new CatalogEntry<LanguageExt.Seq<TRow>>(label, storage);
    }

    /// <summary>
    /// Creates a LanguageExt Seq catalog entry from a Parquet file.
    /// </summary>
    /// <typeparam name="TRow">Row schema type (must be flat and binary-serializable)</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="filePath">Path to Parquet file</param>
    /// <returns>Catalog entry with file + Parquet + Seq composition</returns>
    /// <remarks>
    /// <para>
    /// <strong>Schema Support:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>Traditional schemas with parameterless constructors</item>
    /// <item>Modern schemas with required properties (C# 11+)</item>
    /// <item>Positional records with primary constructors</item>
    /// </list>
    /// </remarks>
    public static ICatalogEntry<LanguageExt.Seq<TRow>> Parquet<TRow>(string label, string filePath)
      where TRow : notnull, IFlatSchema, IBinarySerializable
    {
      var medium = new FileStorageMedium(filePath);
      var format = new ParquetFormatSerializer<TRow>();
      var container = new SeqContainerAdapter<TRow>();
      var storage = new ComposedStorageAdapter<LanguageExt.Seq<TRow>, TRow>(
        medium,
        format,
        container
      );

      return new CatalogEntry<LanguageExt.Seq<TRow>>(label, storage);
    }

    /// <summary>
    /// Creates an in-memory Seq catalog entry.
    /// </summary>
    /// <typeparam name="TRow">Row schema type</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <returns>Catalog entry with memory storage</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> Intermediate pipeline data with immutability guarantees
    /// </para>
    /// </remarks>
    public static ICatalogEntry<LanguageExt.Seq<TRow>> Memory<TRow>(string label)
    {
      var storage = new MemoryStorageAdapter<LanguageExt.Seq<TRow>>();
      return new CatalogEntry<LanguageExt.Seq<TRow>>(label, storage);
    }
  }
}
