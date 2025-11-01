using Microsoft.ML.Data;

namespace Flowthru.ML.Next.Core.Columns;

/// <summary>
/// Represents a column definition with type and optional annotations.
/// </summary>
/// <typeparam name="TType">The .NET type of values in the column</typeparam>
public readonly record struct ColumnDefinition<TType>
{
  /// <summary>
  /// The column name with type information.
  /// </summary>
  public ColumnName<TType> Name { get; init; }

  /// <summary>
  /// The ML.NET DataViewType for this column.
  /// </summary>
  public DataViewType Type { get; init; }

  /// <summary>
  /// Whether this column is required (default: true).
  /// </summary>
  public bool IsRequired { get; init; }

  /// <summary>
  /// Creates a column definition.
  /// </summary>
  public static ColumnDefinition<TType> Create(
      ColumnName<TType> name,
      DataViewType type,
      bool isRequired = true) =>
      new()
      {
        Name = name,
        Type = type,
        IsRequired = isRequired
      };
}
