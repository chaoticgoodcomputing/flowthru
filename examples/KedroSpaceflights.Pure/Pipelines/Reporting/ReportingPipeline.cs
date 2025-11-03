using Flowthru.Pipelines;
using KedroSpaceflights.Pure.Data;
using KedroSpaceflights.Pure.Pipelines.Reporting.Nodes;

namespace KedroSpaceflights.Pure.Pipelines.Reporting;

/// <summary>
/// Creates the reporting pipeline that generates capacity analysis reports.
/// </summary>
public static class ReportingPipeline
{
  /// <summary>
  /// Creates the reporting pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <returns>A configured pipeline that produces shuttle capacity reports.</returns>
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        name: "ComparePassengerCapacity",
        transform: ComparePassengerCapacityNode.Create(),
        input: catalog.PreprocessedShuttles,
        output: catalog.ShuttleCapacityReport
      );
    });
  }
}
