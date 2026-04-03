using Minimal.Data._01_Raw.Schemas;
using Minimal.Data._02_Intermediate.Schemas;

namespace Minimal.Flows.Greetings.Steps;

/// <summary>
/// Transforms names into greetings with "Hello" prefix.
/// </summary>
public static class CreateHelloStep
{
  /// <summary>
  /// Creates a transformation function that converts names to "Hello, {name}!" greetings.
  /// </summary>
  /// <returns>
  /// A function that takes a collection of names and returns a collection of hello greetings.
  /// </returns>
  public static Func<IEnumerable<NameSchema>, IEnumerable<GreetingSchema>> Create()
  {
    return (names) =>
    {
      return names.Select(name => new GreetingSchema { Greeting = $"Hello, {name.Name}!" });
    };
  }
}
