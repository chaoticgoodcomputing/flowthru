using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Python.Tests.Surface;

/// <summary>
/// Tests for successful Python runtime initialization.
/// Validates the Surface API — the happy path.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Surface")]
public class PythonRuntimeInitializationTests
{
  [Test]
  public void Initialize_WithDefaultOptions_Succeeds()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    var options = PythonTestHelper.CreateDefaultOptions();
    services.AddSingleton(options);

    // Use shared PythonRuntime singleton from fixture
    services.AddSingleton(PythonTestFixture.SharedRuntime);

    var provider = services.BuildServiceProvider();
    var runtime = provider.GetRequiredService<PythonRuntime>();

    // Act
    runtime.Initialize();

    // Assert
    // If we reach here without exception, initialization succeeded
    Assert.Pass("Python runtime initialized successfully");
  }

  [Test]
  public void Initialize_CalledMultipleTimes_IsIdempotent()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    var options = PythonTestHelper.CreateDefaultOptions();
    services.AddSingleton(options);

    // Use shared PythonRuntime singleton from fixture
    services.AddSingleton(PythonTestFixture.SharedRuntime);

    var provider = services.BuildServiceProvider();
    var runtime = provider.GetRequiredService<PythonRuntime>();

    // Act
    runtime.Initialize();
    runtime.Initialize();
    runtime.Initialize();

    // Assert
    Assert.Pass("Multiple initialization calls succeeded (idempotent)");
  }

  [Test]
  public void AcquireGil_AfterInitialize_Succeeds()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    var options = PythonTestHelper.CreateDefaultOptions();
    services.AddSingleton(options);

    // Use shared PythonRuntime singleton from fixture
    services.AddSingleton(PythonTestFixture.SharedRuntime);

    var provider = services.BuildServiceProvider();
    var runtime = provider.GetRequiredService<PythonRuntime>();
    runtime.Initialize();

    // Act
    using (var gil = runtime.AcquireGil())
    {
      // GIL acquired successfully
      Assert.That(gil, Is.Not.Null);
    }

    // Assert
    Assert.Pass("GIL acquisition succeeded");
  }

  [Test]
  [Ignore(
    "Cannot test PythonRuntime.Dispose() with shared runtime — Python.NET's PythonEngine is process-global and disposing any runtime shuts down the engine for all tests"
  )]
  public void Dispose_AfterInitialize_ShutdownSucceeds()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    var options = PythonTestHelper.CreateDefaultOptions();
    services.AddSingleton(options);

    // Create a NEW instance (not the shared one) to test disposal
    services.AddSingleton<PythonRuntime>();

    var provider = services.BuildServiceProvider();
    var runtime = provider.GetRequiredService<PythonRuntime>();
    runtime.Initialize();

    // Act
    runtime.Dispose();

    // Assert
    Assert.Pass("Python runtime disposed successfully");
  }
}
