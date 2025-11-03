using Flowthru.Pipelines;
using KedroSpaceflights.Pure.Data;
using KedroSpaceflights.Pure.Pipelines.Reporting.Nodes;

namespace KedroSpaceflights.Pure.Pipelines.Reporting;

public static class ReportingPipeline
{
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
