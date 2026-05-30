using Flowthru.Data.Schema;

namespace GoogleSheets.Data._03_Primary.Schemas;

/// <summary>
/// One row of the daily-totals table Flowthru writes back to the spreadsheet —
/// the sum of every sale recorded on a given day. This is the "Raw Data" surface
/// a human-readable formula tab can reference: Flowthru owns it and replaces it
/// wholesale each run, leaving sibling tabs untouched.
/// </summary>
[FlowthruSchema]
public partial record DailyTotalSchema
{
  /// <summary>The day these sales were recorded.</summary>
  [SerializedLabel("Day")]
  public required DateOnly Day { get; init; }

  /// <summary>The total dollar amount sold that day.</summary>
  [SerializedLabel("Total")]
  public required double Total { get; init; }
}
