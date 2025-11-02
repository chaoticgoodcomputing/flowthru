using LanguageExt;
using Microsoft.ML;

namespace ML.Next.Core.Schema;

/// <summary>
/// A strongly-typed wrapper around ML.NET's IDataView that tracks schema at compile-time.
/// </summary>
/// <typeparam name="TSchema">Phantom type representing the compile-time schema</typeparam>
/// <remarks>
/// This type provides compile-time guarantees about the structure of the underlying data.
/// The TSchema type parameter is a phantom type - it exists only at compile-time and carries
/// no runtime representation. It enables the type system to track schema changes through
/// transformation pipelines and validate operations at compile-time.
/// </remarks>
public readonly record struct DataView<TSchema>
  where TSchema : ISchemaDefinition
{
  /// <summary>
  /// The underlying ML.NET IDataView.
  /// </summary>
  public IDataView Underlying { get; init; }

  /// <summary>
  /// Creates a typed DataView from an ML.NET IDataView.
  /// </summary>
  /// <param name="view">The underlying ML.NET data view</param>
  /// <returns>A strongly-typed DataView with schema tracking</returns>
  /// <remarks>
  /// This method should typically be called after schema validation to ensure
  /// the underlying IDataView actually matches the TSchema definition.
  /// </remarks>
  public static DataView<TSchema> From(IDataView view) => new() { Underlying = view };

  /// <summary>
  /// Gets the runtime schema from the underlying IDataView.
  /// </summary>
  public DataViewSchema Schema => Underlying.Schema;

  /// <summary>
  /// Converts to an Option, None if underlying is null.
  /// </summary>
  public Option<DataView<TSchema>> ToOption() =>
    Underlying == null ? Option<DataView<TSchema>>.None : Option<DataView<TSchema>>.Some(this);
}
