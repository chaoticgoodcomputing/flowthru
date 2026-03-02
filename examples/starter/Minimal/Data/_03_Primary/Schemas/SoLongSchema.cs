using Flowthru.Abstractions;

namespace Minimal.Data._03_Primary.Schemas;

/// <summary>
/// Schema for greetings with "So long" prefix.
/// </summary>
[FlowthruSchema]
public partial record SoLongSchema
{
  /// <summary>
  /// A farewell greeting in the format "So long, {name}!"
  /// </summary>
  [SerializedLabel("greeting")]
  public required string Greeting { get; init; }
}
