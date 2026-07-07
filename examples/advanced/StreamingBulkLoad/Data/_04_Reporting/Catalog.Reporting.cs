using Flowthru.Data.Catalog;

namespace StreamingBulkLoad.Data;

public partial class Catalog
{
  /// <summary>
  /// The headline artefact: a Markdown report contrasting eager and streaming
  /// peak memory, the ratio, and a one-line verdict. Rendered from the template
  /// and the comparison summary by <c>RenderMemoryReportStep</c>.
  /// </summary>
  public IItem<byte[]> MemoryReport =>
    CreateItem(() =>
      Item.Of<byte[]>("MemoryReport")
        .Binary()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/memory_report.md")
        .Build());
}
