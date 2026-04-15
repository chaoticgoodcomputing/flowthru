using Flowthru.Extensions.Spark.Runtime;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Flowthru.Extensions.Spark.Tests;

/// <summary>
/// Assembly-level setup fixture that starts a single Spark JVM backend for all tests
/// that require one. Individual test fixtures call <see cref="Assume"/> against
/// <see cref="IsAvailable"/> rather than managing their own <see cref="SparkRuntime"/>
/// instances, preventing the static JvmBridge from being connected to stale ports.
/// </summary>
[SetUpFixture]
public static class SparkAssemblySetup
{
  private static SparkRuntime? _runtime;

  /// <summary>
  /// The shared Spark JVM backend runtime for this test assembly.
  /// Null if SPARK_HOME was not set or the backend failed to start.
  /// </summary>
  public static SparkRuntime? Runtime => _runtime;

  /// <summary>
  /// Whether the Spark JVM backend started successfully and is available for tests.
  /// </summary>
  public static bool IsAvailable { get; private set; }

  /// <summary>
  /// Message describing why the backend is unavailable, if applicable.
  /// </summary>
  public static string? UnavailableReason { get; private set; }

  [OneTimeSetUp]
  public static void StartSparkBackend()
  {
    var sparkHome = Environment.GetEnvironmentVariable("SPARK_HOME");
    if (string.IsNullOrEmpty(sparkHome))
    {
      UnavailableReason = "SPARK_HOME is not set — skipping JVM-backed tests.";
      return;
    }

    try
    {
      var options = new SparkRuntimeOptions { BackendStartupTimeoutSeconds = 30 };
      _runtime = new SparkRuntime(
        options,
        NullLogger<SparkRuntime>.Instance,
        NullLoggerFactory.Instance
      );
      _runtime.Initialize();
      IsAvailable = true;
    }
    catch (Exception ex)
    {
      UnavailableReason = $"Spark JVM backend failed to start: {ex.Message}";
      _runtime?.Dispose();
      _runtime = null;
    }
  }

  [OneTimeTearDown]
  public static void StopSparkBackend()
  {
    _runtime?.Dispose();
    _runtime = null;
    IsAvailable = false;
  }
}
