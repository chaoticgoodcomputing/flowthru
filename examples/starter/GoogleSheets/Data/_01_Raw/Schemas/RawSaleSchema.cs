using Flowthru.Data.Schema;

namespace GoogleSheets.Data._01_Raw.Schemas;

/// <summary>
/// One row of the raw sales table — a flat, typed record matching one column per
/// property. A Sheets row is tabular, so the schema is flat
/// (<c>[FlowthruSchema]</c> generates <see cref="IFlatSchema"/> for a non-nested
/// record). Each property maps to one Sheets column, typed by its CLR type:
/// <see cref="Product"/> → a text column, <see cref="SoldOn"/> → a date column
/// (a <see cref="DateOnly"/> maps to a calendar-date column, distinct from a
/// date-and-time one), <see cref="Amount"/> → a number column.
/// </summary>
[FlowthruSchema]
public partial record RawSaleSchema
{
  /// <summary>The product that was sold.</summary>
  [SerializedLabel("Product")]
  public required string Product { get; init; }

  /// <summary>The day the sale was recorded.</summary>
  [SerializedLabel("SoldOn")]
  public required DateOnly SoldOn { get; init; }

  /// <summary>The sale amount in dollars.</summary>
  [SerializedLabel("Amount")]
  public required double Amount { get; init; }
}
