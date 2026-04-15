using Flowthru.Core.Abstractions;

namespace Minimal.Data._02_Intermediate.Schemas;

/// <summary>
/// Schema for greetings with "Hello" prefix.
/// </summary>
[FlowthruSchema]
public partial record GreetingSchema
{
    /// <summary>
    /// A greeting in the format "Hello, {name}!"
    /// </summary>
    [SerializedLabel("greeting")]
    public required string Greeting { get; init; }
}
