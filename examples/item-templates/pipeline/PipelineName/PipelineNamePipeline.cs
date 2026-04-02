using Flowthru.Flows;
using ProjectName.Data;
using ProjectName.Pipelines.FlowName.Nodes;

namespace ProjectName.Pipelines.FlowName;

/// <summary>
/// Pipeline for FlowName operations.
/// </summary>
public static class FlowNamePipeline
{
  /// <summary>
  /// Creates the FlowName pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <returns>
  /// A configured pipeline for FlowName processing.
  /// </returns>
  public static Pipeline Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Start with a dummy node - replace with your actu
      pipeline.AddNode(
        label: "FlowNameDummy",
        description: "Placeholder",
        transform: FlowNameDummyNode.Create(),
        input: catalog.NoData,
        output: catalog.NoData
      );
    });
  }
}
