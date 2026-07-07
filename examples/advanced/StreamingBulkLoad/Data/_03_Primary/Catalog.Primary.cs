using Flowthru.Data.Catalog;
using StreamingBulkLoad.Data._03_Primary.Schemas;

namespace StreamingBulkLoad.Data;

public partial class Catalog
{
  /// <summary>
  /// The computed eager-vs-streaming verdict (a one-row collection), held in
  /// memory between the two Reporting Steps that produce and consume it.
  /// </summary>
  public IItem<IEnumerable<MemoryComparison>> MemoryComparisonSummary =>
    CreateItem(() =>
      Item.Of<IEnumerable<MemoryComparison>>("MemoryComparisonSummary").Memory().Build());
}
