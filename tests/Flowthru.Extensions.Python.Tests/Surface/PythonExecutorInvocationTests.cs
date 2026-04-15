using Flowthru.Extensions.Python.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Python.Tests.Surface;

/// <summary>
/// Tests for successful Python function invocation via IPythonExecutor.
/// Validates the Surface API — the happy path for executor usage.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Surface")]
public class PythonExecutorInvocationTests
{
    private IServiceProvider _serviceProvider = null!;
    private IPythonExecutor _executor = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var options = PythonTestHelper.CreateDefaultOptions();

        services.AddSingleton(options);

        // Use shared PythonRuntime singleton from fixture
        services.AddSingleton(PythonTestFixture.SharedRuntime);
        services.AddSingleton<IPythonExecutor, PythonNetExecutor>();

        _serviceProvider = services.BuildServiceProvider();
        _executor = _serviceProvider.GetRequiredService<IPythonExecutor>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Test]
    public void Invoke_Add_ReturnsCorrectResult()
    {
        // Act
        var result = _executor.Invoke<(int, int), int>("test_module", "add", (2, 3));

        // Assert
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Invoke_Multiply_ReturnsCorrectResult()
    {
        // Act
        var result = _executor.Invoke<(int, int), int>("test_module", "multiply", (7, 6));

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Invoke_ConcatStrings_ReturnsCorrectResult()
    {
        // Act
        var result = _executor.Invoke<(string, string), string>(
          "test_module",
          "concat_strings",
          ("Hello, ", "World!")
        );

        // Assert
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void Invoke_CalledMultipleTimes_UsesModuleCache()
    {
        // Act — invoke same module multiple times
        var result1 = _executor.Invoke<(int, int), int>("test_module", "add", (1, 1));
        var result2 = _executor.Invoke<(int, int), int>("test_module", "add", (2, 2));
        var result3 = _executor.Invoke<(int, int), int>("test_module", "multiply", (3, 3));

        // Assert
        Assert.That(result1, Is.EqualTo(2));
        Assert.That(result2, Is.EqualTo(4));
        Assert.That(result3, Is.EqualTo(9));
    }
}
