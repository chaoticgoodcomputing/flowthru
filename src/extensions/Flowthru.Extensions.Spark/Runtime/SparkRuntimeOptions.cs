namespace Flowthru.Extensions.Spark.Runtime;

/// <summary>
/// Configuration options for the Spark runtime.
/// </summary>
/// <remarks>
/// <para>
/// Bind from the <c>Flowthru:Spark</c> configuration section, or override
/// via the <c>UseSpark(Action&lt;SparkRuntimeOptions&gt;)</c> code-first overload.
/// </para>
/// <para>
/// Properties left <c>null</c> are filled by platform auto-detection during
/// service registration (Homebrew paths, assembly-adjacent JAR). Properties
/// set explicitly — either via configuration or the code-first callback — take
/// precedence over auto-detection.
/// </para>
/// </remarks>
public sealed class SparkRuntimeOptions
{
  internal const string JarFileName = "flowthru-spark-4-1_2.13-2.3.1.jar";

  /// <summary>
  /// Path to the Spark installation directory (the value of <c>SPARK_HOME</c>).
  /// When <c>null</c>, auto-detected from common Homebrew install paths.
  /// </summary>
  public string? SparkHome { get; set; }

  /// <summary>
  /// Path to the <c>flowthru-spark-*.jar</c> bridge artifact.
  /// When <c>null</c>, resolved from the assembly output directory.
  /// </summary>
  public string? JarPath { get; set; }

  /// <summary>
  /// Spark master URL passed to <c>spark-submit</c>.
  /// Defaults to <c>local[*]</c> for in-process execution on all available cores.
  /// </summary>
  public string Master { get; set; } = "local[*]";

  /// <summary>
  /// Maximum seconds to wait for the JVM backend to accept connections after launch.
  /// Defaults to 60s for production use. Set lower (e.g. 10s) in test fixtures to
  /// fail fast when the backend is unavailable.
  /// </summary>
  public int BackendStartupTimeoutSeconds { get; set; } = 60;
}
