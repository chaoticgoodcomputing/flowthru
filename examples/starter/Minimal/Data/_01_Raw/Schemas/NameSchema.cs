using Flowthru.Data.Schema;

namespace Minimal.Data._01_Raw.Schemas;

/// <summary>
/// Schema for raw name data from the input CSV file.
/// </summary>
[FlowthruSchema]
public partial record NameSchema
{
  /// <summary>
  /// A person's name.
  /// </summary>
  [SerializedLabel("name")]
  public required string Name { get; init; }
}
