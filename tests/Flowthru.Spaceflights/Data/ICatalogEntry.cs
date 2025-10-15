namespace Flowthru.Data;

/// <summary>
/// Represents a strongly-typed catalog entry that stores data of type T.
/// 
/// <para><strong>Compile-Time Type Safety:</strong></para>
/// <para>
/// ICatalogEntry&lt;T&gt; replaces string-based catalog keys with typed references,
/// enabling the compiler to validate that pipeline nodes receive correct input types
/// and produce correct output types. Misconfigured pipelines will not compile.
/// </para>
/// 
/// <para><strong>Benefits:</strong></para>
/// <list type="bullet">
/// <item>IntelliSense: IDE shows available catalog entries with their types</item>
/// <item>Refactoring: Rename catalog entry → all usages update automatically</item>
/// <item>Type Safety: Cannot pass wrong type to a node (compile error)</item>
/// <item>Self-Documenting: Entry type visible in tooltips and signatures</item>
/// </list>
/// </summary>
/// <typeparam name="T">The data type stored in this catalog entry</typeparam>
public interface ICatalogEntry<T>
{
  /// <summary>
  /// Unique identifier for this catalog entry.
  /// Used for logging, debugging, and serialization.
  /// </summary>
  string Key { get; }

  /// <summary>
  /// The type of data stored in this catalog entry.
  /// Guaranteed to be typeof(T) at compile-time.
  /// </summary>
  Type DataType => typeof(T);

  /// <summary>
  /// Loads data from the underlying storage.
  /// </summary>
  Task<T> Load();

  /// <summary>
  /// Saves data to the underlying storage.
  /// </summary>
  Task Save(T data);

  /// <summary>
  /// Indicates whether this entry exists in storage.
  /// </summary>
  Task<bool> Exists();
}
