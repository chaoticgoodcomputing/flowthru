using Flowthru.Core.Data;
using Flowthru.Extensions.Python.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Python.Tests.Surface;

/// <summary>
/// Tests for <see cref="Directory{T}"/> marshalling through the in-process Python executor.
/// Each test sends a <see cref="Directory{T}"/> in, lets the Python side perform some
/// dict-shaped transformation, and asserts the result round-trips back into a
/// <see cref="Directory{T}"/> with the expected entries.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Surface")]
[NonParallelizable]
public class PythonExecutorDirectoryTests
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
    services.AddSingleton(PythonTestFixture.SharedRuntime);
    services.AddSingleton<IPythonExecutor, PythonNetExecutor>();

    _serviceProvider = services.BuildServiceProvider();
    _executor = _serviceProvider.GetRequiredService<IPythonExecutor>();
  }

  [TearDown]
  public void TearDown()
  {
    if (_serviceProvider is IDisposable disposable)
      disposable.Dispose();
  }

  /// <summary>
  /// <c>Directory&lt;byte[]&gt;</c> with two PNG-shaped blobs round-trips through the
  /// <c>echo_dir</c> Python function unchanged. This is the icicle case shape.
  /// </summary>
  [Test]
  public void EchoDir_Bytes_RoundTrips()
  {
    var input = new Directory<byte[]>(new Dictionary<string, byte[]>
    {
      ["alpha.bin"] = new byte[] { 0x01, 0x02, 0x03 },
      ["beta.bin"] = new byte[] { 0x10, 0x20, 0x30, 0x40 },
    });

    var result = _executor.Invoke<Directory<byte[]>, Directory<byte[]>>(
      "directory_steps",
      "echo_dir",
      input
    );

    Assert.That(result.Count, Is.EqualTo(2));
    Assert.That(result["alpha.bin"], Is.EqualTo(new byte[] { 0x01, 0x02, 0x03 }));
    Assert.That(result["beta.bin"], Is.EqualTo(new byte[] { 0x10, 0x20, 0x30, 0x40 }));
  }

  /// <summary>
  /// <c>Directory&lt;int&gt;</c> with scalar inner values round-trips through
  /// <c>add_one_to_values</c>; the Python side produces <c>dict[str, int]</c> and the
  /// marshaller decodes each value via <c>ScalarMarshaller.FromPython&lt;int&gt;</c>.
  /// </summary>
  [Test]
  public void AddOne_ScalarValues_RoundTrips()
  {
    var input = new Directory<int>(new Dictionary<string, int>
    {
      ["a"] = 1,
      ["b"] = 41,
    });

    var result = _executor.Invoke<Directory<int>, Directory<int>>(
      "directory_steps",
      "add_one_to_values",
      input
    );

    Assert.That(result["a"], Is.EqualTo(2));
    Assert.That(result["b"], Is.EqualTo(42));
  }

  /// <summary>
  /// <c>upper_keys</c> demonstrates that keys are real strings on the Python side and
  /// the unmarshaller picks them back up correctly.
  /// </summary>
  [Test]
  public void UpperKeys_TransformsKeysOnPythonSide()
  {
    var input = new Directory<int>(new Dictionary<string, int>
    {
      ["alpha"] = 1,
      ["beta"] = 2,
    });

    var result = _executor.Invoke<Directory<int>, Directory<int>>(
      "directory_steps",
      "upper_keys",
      input
    );

    Assert.That(result.Keys, Is.EquivalentTo(new[] { "ALPHA", "BETA" }));
    Assert.That(result["ALPHA"], Is.EqualTo(1));
    Assert.That(result["BETA"], Is.EqualTo(2));
  }
}
