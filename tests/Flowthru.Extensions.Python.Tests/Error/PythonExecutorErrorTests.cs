using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Python.Runtime;

namespace Flowthru.Extensions.Python.Tests.Error;

/// <summary>
/// Tests for Python executor error conditions.
/// Validates the Error surface — how failures propagate and are reported.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Error")]
public class PythonExecutorErrorTests
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
  public void Invoke_WithNonexistentModule_ThrowsInvalidOperationException()
  {
    // Act & Assert
    var ex = Assert.Throws<InvalidOperationException>(() =>
    {
      _executor.Invoke<int, int>("nonexistent_module", "some_function", 0);
    });

    Assert.That(ex!.Message, Does.Contain("nonexistent_module"));
  }

  [Test]
  public void Invoke_WithNonexistentFunction_ThrowsInvalidOperationException()
  {
    // Act & Assert
    var ex = Assert.Throws<InvalidOperationException>(() =>
    {
      _executor.Invoke<int, int>("test_module", "nonexistent_function", 0);
    });

    Assert.That(ex!.Message, Does.Contain("nonexistent_function"));
  }

  [Test]
  public void Invoke_WithNullModuleName_ThrowsArgumentException()
  {
    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
    {
      _executor.Invoke<int, int>(null!, "some_function", 0);
    });

    Assert.That(ex!.ParamName, Is.EqualTo("moduleName"));
  }

  [Test]
  public void Invoke_WithEmptyModuleName_ThrowsArgumentException()
  {
    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
    {
      _executor.Invoke<int, int>("", "some_function", 0);
    });

    Assert.That(ex!.ParamName, Is.EqualTo("moduleName"));
  }

  [Test]
  public void Invoke_WithNullFunctionName_ThrowsArgumentException()
  {
    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
    {
      _executor.Invoke<int, int>("test_module", null!, 0);
    });

    Assert.That(ex!.ParamName, Is.EqualTo("functionName"));
  }

  [Test]
  public void Invoke_WithEmptyFunctionName_ThrowsArgumentException()
  {
    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
    {
      _executor.Invoke<int, int>("test_module", "", 0);
    });

    Assert.That(ex!.ParamName, Is.EqualTo("functionName"));
  }

  [Test]
  public void Invoke_ThatRaisesException_ThrowsPythonException()
  {
    // Act & Assert
    var ex = Assert.Throws<PythonException>(() =>
    {
      _executor.Invoke<int, int>("test_module", "raise_exception", 0);
    });

    Assert.That(ex!.Message, Does.Contain("Intentional test exception"));
  }
}
