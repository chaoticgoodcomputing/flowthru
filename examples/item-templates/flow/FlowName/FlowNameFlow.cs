using Flowthru.Core.Flows;
using ProjectName.Data;
using ProjectName.Flows.FlowName.Steps;

namespace ProjectName.Flows.FlowName;

/// <summary>
/// Flow for FlowName operations.
/// </summary>
public static class FlowNameFlow
{
  /// <summary>
  /// Creates the FlowName pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <returns>
  /// A configured pipeline for FlowName processing.
  /// </returns>
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Start with a dummy 0-input/0-output node — replace with your actual
      // step that wires real catalog inputs/outputs.
      pipeline.AddStep(
        label: "FlowNameDummy",
        description: "Placeholder",
        transform: FlowNameDummyStep.Create()
      );
    });
  }
}
