using Flowthru.Data.Schema;

namespace KedroSpaceflightsPython.Data._05_ModelInput.Schemas;

/// <summary>
/// Represents preprocessed company data with strongly-typed fields.
/// Produced by parsing and validating raw company data.
/// </summary>
/// <remarks>
/// Uses required members to enforce that all critical fields must be set
/// during construction, preventing accidental omission in pipeline nodes.
/// </remarks>
[FlowthruSchema]
public partial record YValues
{
  [SerializedLabel("price")]
  public double Label { get; init; } // Price
}
