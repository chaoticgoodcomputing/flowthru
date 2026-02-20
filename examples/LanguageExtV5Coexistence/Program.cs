using Flowthru.Effects;
using LanguageExt;
using static LanguageExt.Prelude;

namespace LanguageExtV5Coexistence;

/// <summary>
/// Demonstrates that a project can use both LanguageExt v5 and Flowthru simultaneously.
/// This proves that Flowthru's abstraction layer successfully eliminates the v4/v5 conflict.
/// </summary>
public class Program
{
  public static async Task<int> Main(string[] args)
  {
    Console.WriteLine("=== LanguageExt v5 + Flowthru Coexistence Test ===\n");

    // Test 1: Use LanguageExt v5's IO<T> in our own code
    Console.WriteLine("Test 1: Using LanguageExt v5 IO<T>");
    var ioResult = await UseLanguageExtV5();
    Console.WriteLine($"  Result: {ioResult}");
    Console.WriteLine($"  Status: ✓ IO<T> works\n");

    // Test 2: Use Flowthru's FlowIO<T>
    Console.WriteLine("Test 2: Using Flowthru FlowIO<T>");
    var flowResult = await UseFlowthruFlowIO();
    Console.WriteLine($"  Result: {flowResult}");
    Console.WriteLine($"  Status: ✓ FlowIO<T> works\n");

    // Test 3: Both types coexist without conflicts
    Console.WriteLine("Test 3: Both types coexist");
    Console.WriteLine($"  IO<T> type: {typeof(IO<int>).FullName}");
    Console.WriteLine($"  FlowIO<T> type: {typeof(FlowIO<int>).FullName}");
    Console.WriteLine($"  Status: ✓ No type conflicts\n");

    Console.WriteLine("=== All Tests Passed ===");
    Console.WriteLine("Flowthru can be used alongside LanguageExt v5 without conflicts!");

    return 0;
  }

  /// <summary>
  /// Demonstrates using LanguageExt v5's IO<T> effect type.
  /// </summary>
  private static async Task<string> UseLanguageExtV5()
  {
    // Use v5's IO<T> for async effects
    IO<string> effect = IO.liftAsync(async () =>
    {
      await Task.Delay(10);
      return "LanguageExt v5 IO<T> works!";
    });

    return await effect.RunAsync();
  }

  /// <summary>
  /// Demonstrates using Flowthru's FlowIO<T> effect type.
  /// </summary>
  private static async Task<string> UseFlowthruFlowIO()
  {
    // Use Flowthru's FlowIO<T>
    var effect = FlowIO.LiftAsync(async () =>
    {
      await Task.Delay(10);
      return "Flowthru FlowIO<T> works!";
    });

    return await effect.Run();
  }
}
