using Flowthru.Extensions.Python.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Python.Tests.Error;

/// <summary>
/// Tests for Python runtime initialization error conditions.
/// Validates the Error surface — how initialization failures propagate.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Error")]
public class PythonRuntimeErrorTests
{
    [Test]
    public void AcquireGil_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var options = PythonTestHelper.CreateDefaultOptions();
        services.AddSingleton(options);
        services.AddSingleton<PythonRuntime>();

        var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<PythonRuntime>();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            runtime.AcquireGil();
        });

        Assert.That(ex!.Message, Does.Contain("not initialized"));
    }

    [Test]
    public void AcquireGil_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var options = PythonTestHelper.CreateDefaultOptions();
        services.AddSingleton(options);
        services.AddSingleton<PythonRuntime>();

        var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<PythonRuntime>();
        runtime.Initialize();
        runtime.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
        {
            runtime.AcquireGil();
        });
    }

    [Test]
    public void Initialize_WithInvalidPythonDll_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var options = new PythonRuntimeOptions { PythonDll = "/nonexistent/path/to/libpython.so" };
        services.AddSingleton(options);
        services.AddSingleton<PythonRuntime>();

        var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<PythonRuntime>();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            runtime.Initialize();
        });

        Assert.That(ex!.Message, Does.Contain("Failed to initialize Python runtime"));
    }
}
