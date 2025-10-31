using LanguageExt.Traits.Domain;

namespace Flowthru.ML.Ext.Core.Columns;

/// <summary>
/// Strongly-typed column name that carries type information at compile-time.
/// </summary>
/// <typeparam name="TType">The type of values in the column</typeparam>
/// <remarks>
/// This type uses LanguageExt v5's Identifier trait to provide semantic meaning
/// and equality semantics for column names. The TType parameter enables
/// compile-time checking of column types in transformation operations.
/// </remarks>
public readonly record struct ColumnName<TType> : Identifier<ColumnName<TType>> {
  /// <summary>
  /// The string name of the column.
  /// </summary>
  public string Value { get; init; }

  /// <summary>
  /// Creates a new column name from a string.
  /// </summary>
  /// <param name="name">The column name</param>
  /// <returns>A strongly-typed column reference</returns>
  public static ColumnName<TType> From(string name) =>
      new() { Value = name };

  /// <summary>
  /// Implicit conversion from string for convenience.
  /// </summary>
  public static implicit operator ColumnName<TType>(string name) =>
      From(name);

  /// <summary>
  /// Implicit conversion to string for ML.NET interop.
  /// </summary>
  public static implicit operator string(ColumnName<TType> columnName) =>
      columnName.Value;

  /// <summary>
  /// String representation of the column name.
  /// </summary>
  public override string ToString() => Value;

  /// <summary>
  /// Changes the type parameter while keeping the same name.
  /// Use with caution - this is for type-level schema transformations.
  /// </summary>
  public ColumnName<TNewType> As<TNewType>() =>
      ColumnName<TNewType>.From(Value);
}
