using Flowthru.Flow;
using Minimal.Data;
using Minimal.Data._01_Raw.Schemas;
using Minimal.Data._02_Intermediate.Schemas;
using Minimal.Data._03_Primary.Schemas;
using Minimal.Flows.Greetings.Steps;

namespace Minimal.Flows.Greetings;

/// <summary>
/// A simple greeting transformation pipeline demonstrating Flowthru's core concepts.
/// </summary>
public static class GreetingsFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("Greetings", pipeline =>
    {
      pipeline.AddStep<IEnumerable<NameSchema>, IEnumerable<GreetingSchema>>(
        label: "CreateHello",
        transform: CreateHelloStep.Create(),
        input1: catalog.Names,
        output1: catalog.HelloGreetings
      );

      pipeline.AddStep<
        IEnumerable<GreetingSchema>,
        IEnumerable<GoodbyeSchema>,
        IEnumerable<SoLongSchema>
      >(
        label: "TransformGreetings",
        transform: TransformGreetingsStep.Create(),
        input1: catalog.HelloGreetings,
        output1: catalog.Goodbyes,
        output2: catalog.SoLongs
      );
    });
  }
}
