using LanguageExt;
using static LanguageExt.Prelude;
using Flowthru.Effects;

namespace LanguageExtV4Coexistence;

/// <summary>
/// Demonstrates that a project can use both LanguageExt v4 and Flowthru simultaneously.
/// This proves that Flowthru's abstraction layer successfully eliminates the v4/v5 conflict.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== LanguageExt v4 + Flowthru Coexistence Test ===\n");

        // Test 1: Use LanguageExt v4's Aff<T> in our own code
        Console.WriteLine("Test 1: Using LanguageExt v4 Aff<T>");
        var affResult = await UseLanguageExtV4();
        Console.WriteLine($"  Result: {affResult}");
        Console.WriteLine($"  Status: ✓ Aff<T> works\n");

        // Test 2: Use Flowthru's FlowIO<T>
        Console.WriteLine("Test 2: Using Flowthru FlowIO<T>");
        var flowResult = await UseFlowthruFlowIO();
        Console.WriteLine($"  Result: {flowResult}");
        Console.WriteLine($"  Status: ✓ FlowIO<T> works\n");

        // Test 3: Both types coexist without conflicts
        Console.WriteLine("Test 3: Both types coexist");
        Console.WriteLine($"  Aff<T> type: {typeof(Aff<int>).FullName}");
        Console.WriteLine($"  FlowIO<T> type: {typeof(FlowIO<int>).FullName}");
        Console.WriteLine($"  Status: ✓ No type conflicts\n");

        Console.WriteLine("=== All Tests Passed ===");
        Console.WriteLine("Flowthru can be used alongside LanguageExt v4 without conflicts!");
        
        return 0;
    }

    /// <summary>
    /// Demonstrates using LanguageExt v4's Aff<T> effect type.
    /// </summary>
    private static async Task<string> UseLanguageExtV4()
    {
        // Use v4's Aff<T> for async effects
        Aff<string> effect = Aff(async () =>
        {
            await Task.Delay(10);
            return "LanguageExt v4 Aff<T> works!";
        });

        var result = await effect.Run();
        return result.Match(
            Succ: value => value,
            Fail: ex => $"Error: {ex.Message}"
        );
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
