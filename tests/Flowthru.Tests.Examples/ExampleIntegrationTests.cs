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
  /// Tests the service layer directly by invoking ConfigureServices and IFlowthruService.
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

    // Show diagnostic message if present
    if (!string.IsNullOrEmpty(result.DiagnosticMessage))
    {
      TestContext.WriteLine($"Diagnostic: {result.DiagnosticMessage}");
    }

    // Assert success
    Assert.That(
      result.Success,
      Is.True,
      $"Example {example.Name} failed. Category: {result.Category}, Exit Code: {result.ExitCode}"
    );
  }
}
