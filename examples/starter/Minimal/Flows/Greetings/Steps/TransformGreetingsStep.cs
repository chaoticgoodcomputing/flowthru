using Flowthru.Step;
using Minimal.Data._02_Intermediate.Schemas;
using Minimal.Data._03_Primary.Schemas;

namespace Minimal.Flows.Greetings.Steps;

/// <summary>
/// Transforms "Hello" greetings into two outputs: "Goodbye" and "So long" greetings.
/// This demonstrates a 1→2 node transformation.
/// </summary>
#region docs:step-shape
[FlowthruStep]
public static class TransformGreetingsStep
{
  /// <summary>
  /// Creates a transformation function that converts "Hello" greetings into
  /// both "Goodbye" and "So long" variants.
  /// </summary>
  /// <returns>
  /// A function that takes hello greetings and returns a tuple of
  /// (goodbye greetings, so long greetings).
  /// </returns>
  public static Func<
    IEnumerable<GreetingSchema>,
    (IEnumerable<GoodbyeSchema>, IEnumerable<SoLongSchema>)
  > Create()
  {
    return (helloGreetings) =>
    {
      // Convert to list to avoid multiple enumerations
      var greetings = helloGreetings.ToList();

      var goodbyeGreetings = greetings.Select(hello => new GoodbyeSchema
      {
        Greeting = hello.Greeting.Replace("Hello", "Goodbye"),
      });

      var soLongGreetings = greetings.Select(hello => new SoLongSchema
      {
        Greeting = hello.Greeting.Replace("Hello", "So long"),
      });

      return (goodbyeGreetings, soLongGreetings);
    };
  }
}
#endregion
