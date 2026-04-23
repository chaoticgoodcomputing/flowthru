using Flowthru.Extensions.Python.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: NUnit.Framework.LevelOfParallelism(1)]
[assembly: NUnit.Framework.Parallelizable(NUnit.Framework.ParallelScope.None)]

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Base fixture for Python tests that creates a shared PythonRuntime singleton for all tests.
/// </summary>
/// <remarks>
/// <para>
/// Python.NET's <c>PythonEngine</c> is a process-global singleton that cannot be safely
/// re-initialized. This fixture creates a single <see cref="PythonRuntime"/> instance
/// that is shared across all test classes, matching the lifecycle of the underlying engine.
/// </para>
/// <para>
/// Individual test classes should register <see cref="SharedRuntime"/> in their DI container
/// instead of creating new instances:
/// <code>
/// services.AddSingleton(PythonTestFixture.SharedRuntime);
/// </code>
/// </para>
/// </remarks>
[SetUpFixture]
public class PythonTestFixture
{
  /// <summary>
  /// Shared PythonRuntime singleton used by all test classes.
  /// </summary>
  /// <remarks>
  /// Initialized once in <see cref="SetUpPythonEnvironment"/> and reused across all tests.
  /// Do not dispose — the runtime's lifecycle matches the test process.
  /// </remarks>
#pragma warning disable NUnit1032 // The type is not disposed. Suppressed because SharedRuntime is a process-global singleton.
  public static PythonRuntime SharedRuntime { get; private set; } = null!;
#pragma warning restore NUnit1032

  [OneTimeSetUp]
  public void SetUpPythonEnvironment()
  {
    var options = PythonTestHelper.CreateDefaultOptions();

    try
    {
      var pythonDll = PythonEnvironmentResolver.ResolvePythonDll(options);
      Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
      Console.WriteLine($"[PythonTestFixture] Set PYTHONNET_PYDLL={pythonDll}");

      // Create shared singleton runtime directly (not via DI container)
      // to avoid disposal when ServiceProvider is GC'd
      SharedRuntime = new PythonRuntime(
        Microsoft.Extensions.Options.Options.Create(options),
        Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<PythonRuntime>()
      );
      SharedRuntime.Initialize();

      Console.WriteLine($"[PythonTestFixture] Shared PythonRuntime initialized");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[PythonTestFixture] Failed to initialize shared runtime: {ex.Message}");
      throw;
    }
  }

  [OneTimeTearDown]
  public void TearDownPythonEnvironment()
  {
    // Clean up environment variable after all tests complete
    Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", null);
    // Do NOT dispose SharedRuntime — Python.NET's engine is process-global and
    // cannot be safely shut down. The OS will reclaim resources on process exit.
  }
}
