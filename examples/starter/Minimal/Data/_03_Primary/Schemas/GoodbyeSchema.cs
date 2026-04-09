using Flowthru.Core.Abstractions;

namespace Minimal.Data._03_Primary.Schemas;

/// <summary>
/// Schema for greetings with "Goodbye" prefix.
/// </summary>
[FlowthruSchema]
public partial record GoodbyeSchema
{
  /// <summary>
  /// A farewell greeting in the format "Goodbye, {name}!"
  /// </summary>
  [SerializedLabel("greeting")]
  public required string Greeting { get; init; }
}
