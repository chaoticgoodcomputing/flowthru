using Flowthru.Core.Flows;
using Minimal.Data;
using Minimal.Flows.Greetings.Steps;

namespace Minimal.Flows.Greetings;

/// <summary>
/// A simple greeting transformation pipeline demonstrating Flowthru's core concepts.
/// </summary>
public static class GreetingsFlow
{
    /// <summary>
    /// Creates the greetings pipeline.
    /// </summary>
    /// <param name="catalog">The data catalog containing input and output entries.</param>
    /// <returns>
    /// A configured pipeline that transforms names into various greeting formats.
    /// </returns>
    public static Flow Create(Catalog catalog)
    {
        return FlowBuilder.CreateFlow(pipeline =>
        {
            // Step 1: Transform names to "Hello, {name}!"
            pipeline.AddStep(
          label: "CreateHello",
          description: "Transform names into 'Hello' greetings.",
          transform: CreateHelloStep.Create(),
          input: catalog.Names,
          output: catalog.HelloGreetings
        );

            // Step 2: Transform "Hello" greetings into "Goodbye" and "So long" variants
            pipeline.AddStep(
          label: "TransformGreetings",
          description: "Convert 'Hello' greetings into 'Goodbye' and 'So long' variants.",
          transform: TransformGreetingsStep.Create(),
          input: catalog.HelloGreetings,
          output: (catalog.Goodbyes, catalog.SoLongs)
        );
        });
    }
}
