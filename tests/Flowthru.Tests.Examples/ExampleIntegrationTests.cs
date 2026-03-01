using Flowthru.Tests.Examples.Infrastructure;

namespace Flowthru.Tests.Examples;

/// <summary>
/// Integration tests that execute all example projects to verify they run successfully.
/// Provides code coverage for the Flowthru framework through real-world usage patterns.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ParallelizableAttribute"/> with <see cref="ParallelScope.Children"/> allows
/// individual examples to run concurrently. This is safe because the runner never mutates
/// global state (no <c>Directory.SetCurrentDirectory</c>) — each example receives its
/// project path as a <c>basePath</c> argument.
/// </para>
/// </remarks>
[TestFixture]
[Category("Examples")]
[Category("Integration")]
[Parallelizable(ParallelScope.Children)]
public class ExampleIntegrationTests
{
  private static IEnumerable<ExampleProject> DiscoveredExamples() =>
    ExampleDiscovery.DiscoverExamples();

  /// <summary>
  /// Smoke test: at least one example must be discoverable.
  /// If this fails, the build likely didn't compile the example projects
  /// or the <c>&lt;ProjectReference&gt;</c> glob in the test csproj is broken.
  /// </summary>
  [Test]
  public void Discovery_FindsAtLeastOneExample()
  {
    var examples = ExampleDiscovery.DiscoverExamples().ToList();

    TestContext.Out.WriteLine($"Discovered {examples.Count} example(s):");
    foreach (var example in examples)
    {
      TestContext.Out.WriteLine($"  - {example.Name} ({example.ProjectPath})");
    }

    Assert.That(examples, Is.Not.Empty, "No example projects were discovered.");
  }

  /// <summary>
  /// Executes each example project's full pipeline graph and asserts success.
  /// Times out after <see cref="ExampleTestRunner.DefaultTimeout"/> to prevent
  /// infinite hangs from blocking the test run.
  /// </summary>
  [TestCaseSource(nameof(DiscoveredExamples))]
  public async Task Example_ExecutesSuccessfully(ExampleProject example)
  {
    TestContext.Out.WriteLine($"Running example: {example.Name}");
    TestContext.Out.WriteLine($"  Project path: {example.ProjectPath}");

    await new ExampleTestRunner(example).RunAsync();
  }
}
