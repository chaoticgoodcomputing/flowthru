using Flowthru.Tests.Examples.Infrastructure;

namespace Flowthru.Tests.Examples;

/// <summary>
/// Integration tests that execute all example projects to verify they run successfully.
/// These tests provide code coverage for the Flowthru framework through real-world usage.
/// </summary>
[TestFixture]
[Category("Examples")]
[Category("Integration")]
public class ExampleIntegrationTests
{
  /// <summary>
  /// Discovers all example projects from the examples directory.
  /// </summary>
  private static IEnumerable<ExampleProject> GetAllExamples()
  {
    return ExampleDiscovery.DiscoverExamples();
  }

  /// <summary>
  /// Verifies that all examples can be discovered successfully.
  /// This test ensures the discovery mechanism is working and all example projects
  /// are properly referenced in the .csproj file for coverage collection.
  /// </summary>
  [Test]
  public void Discovery_FindsAllExamples()
  {
    // Arrange & Act
    var examples = GetAllExamples().ToList();

    // Assert
    Assert.That(examples, Is.Not.Empty, "No examples were discovered");
    Assert.That(examples.Count, Is.GreaterThanOrEqualTo(3), "Expected at least 3 example projects");

    // Log discovered examples for debugging
    TestContext.WriteLine($"Discovered {examples.Count} example projects:");
    foreach (var example in examples)
    {
      TestContext.WriteLine($"  - {example.Name} at {example.ProjectPath}");
    }
  }

  /// <summary>
  /// Verifies that each discovered example has a valid entry point type.
  /// </summary>
  [TestCaseSource(nameof(GetAllExamples))]
  public void Example_HasValidEntryPoint(ExampleProject example)
  {
    // Assert
    Assert.That(example.EntryPointType, Is.Not.Null, $"{example.Name} has no entry point type");
    Assert.That(
      example.EntryPointType.Name,
      Is.EqualTo("Program"),
      $"{example.Name} entry point type should be named 'Program'"
    );
  }

  /// <summary>
  /// Executes each example project and verifies it completes successfully.
  /// This is the main integration test that provides code coverage through example execution.
  /// </summary>
    [TestCaseSource(nameof(GetAllExamples))]
  public async Task Example_ExecutesSuccessfully(ExampleProject example)
  {
    // Arrange
    var runner = new ExampleTestRunner(example);
    TestContext.WriteLine($"Running example: {example.Name}");

    // Act
    var result = await runner.RunAsync();
    TestContext.WriteLine($"Completed in {result.Duration.TotalSeconds:F2}s");

    // Assert - Always show captured output for debugging
    if (!string.IsNullOrEmpty(result.CapturedOutput))
    {
      TestContext.WriteLine("--- Captured Output ---");
      TestContext.WriteLine(result.CapturedOutput);
      TestContext.WriteLine("--- End Captured Output ---");
    }

    // Show exception details if present
    if (result.Exception != null)
    {
      TestContext.WriteLine("--- Exception Details ---");
      TestContext.WriteLine($"Type: {result.Exception.GetType().FullName}");
      TestContext.WriteLine($"Message: {result.Exception.Message}");
      TestContext.WriteLine($"StackTrace:\n{result.Exception.StackTrace}");
      if (result.Exception.InnerException != null)
      {
        TestContext.WriteLine($"Inner Exception: {result.Exception.InnerException.Message}");
      }
      TestContext.WriteLine("--- End Exception Details ---");
    }

    // Assert success
    Assert.That(result.ExitCode, Is.EqualTo(0), $"Example {example.Name} failed with exit code {result.ExitCode}");
  }
