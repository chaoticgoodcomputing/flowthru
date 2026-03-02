using Flowthru.Pipelines;
using Minimal.Data;
using Minimal.Pipelines.Greetings.Nodes;

namespace Minimal.Pipelines.Greetings;

/// <summary>
/// A simple greeting transformation pipeline demonstrating Flowthru's core concepts.
/// </summary>
public static class GreetingsPipeline
{
  /// <summary>
  /// Creates the greetings pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <returns>
  /// A configured pipeline that transforms names into various greeting formats.
  /// </returns>
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Node 1: Transform names to "Hello, {name}!"
      pipeline.AddNode(
        label: "CreateHello",
        description: "Transform names into 'Hello' greetings.",
        transform: CreateHelloNode.Create(),
        input: catalog.Names,
        output: catalog.HelloGreetings
      );

      // Node 2: Transform "Hello" greetings into "Goodbye" and "So long" variants
      pipeline.AddNode(
        label: "TransformGreetings",
        description: "Convert 'Hello' greetings into 'Goodbye' and 'So long' variants.",
        transform: TransformGreetingsNode.Create(),
        input: catalog.HelloGreetings,
        output: (catalog.Goodbyes, catalog.SoLongs)
      );
    });
  }
}
