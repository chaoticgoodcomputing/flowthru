using Flowthru.Abstractions;

namespace SpaceflightsPythonEFCore.Data._05_ModelInput.Schemas;

/// <summary>
/// Target label for model training/testing. Produced by Python split_data node.
/// </summary>
[FlowthruSchema]
public partial record YValues
{
  [SerializedLabel("price")]
  public double Label { get; init; } // Price
}
