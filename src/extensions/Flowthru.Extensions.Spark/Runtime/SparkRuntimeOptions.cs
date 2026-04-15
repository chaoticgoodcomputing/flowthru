using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Flowthru.Extensions.Spark.Runtime;

/// <summary>
/// Configuration options for the Spark runtime.
/// </summary>
/// <remarks>
/// <para>
/// All paths are auto-detected from the executing assembly's output directory and environment
/// variables. Override properties explicitly only when auto-detection is insufficient
/// (e.g., containers with non-standard SPARK_HOME, CI with pre-built JARs elsewhere).
/// </para>
/// <para>
/// <strong>Auto-detection hierarchy for <see cref="GetResolvedSparkHome"/>:</strong>
/// <list type="number">
/// <item>Explicit <see cref="SparkHome"/> property</item>
/// <item><c>SPARK_HOME</c> environment variable</item>
/// <item>Common Homebrew path on macOS (<c>/opt/homebrew/opt/apache-spark/libexec</c>)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Auto-detection hierarchy for <see cref="GetResolvedJarPath"/>:</strong>
/// <list type="number">
/// <item>Explicit <see cref="JarPath"/> property</item>
/// <item><c>FLOWTHRU_SPARK_JAR</c> environment variable</item>
/// <item><c>flowthru-spark-4-1_2.13-2.3.1.jar</c> alongside the executing assembly</item>
/// </list>
/// </para>
/// </remarks>
public sealed class SparkRuntimeOptions
{
    internal const string JarFileName = "flowthru-spark-4-1_2.13-2.3.1.jar";

    /// <summary>
    /// Path to the Spark installation directory (the value of <c>SPARK_HOME</c>).
    /// If null, resolved via auto-detection.
    /// </summary>
    public string? SparkHome { get; set; }

    /// <summary>
    /// Path to the <c>flowthru-spark-*.jar</c> bridge artifact.
    /// If null, resolved from the executing assembly's output directory.
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

    /// <summary>
    /// Resolves the Spark home directory using the auto-detection hierarchy.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no Spark installation can be located.
    /// </exception>
    public string GetResolvedSparkHome()
    {
        if (!string.IsNullOrWhiteSpace(SparkHome))
            return SparkHome!;

        var envSparkHome = Environment.GetEnvironmentVariable("SPARK_HOME");
        if (!string.IsNullOrWhiteSpace(envSparkHome) && Directory.Exists(envSparkHome))
            return envSparkHome!;

        // Common Homebrew path on Apple Silicon and Intel macOS
        var homebrewPaths = new[]
        {
            "/opt/homebrew/opt/apache-spark/libexec",
            "/usr/local/opt/apache-spark/libexec",
        };

        foreach (var candidate in homebrewPaths)
        {
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new InvalidOperationException(
            "Spark installation not found. Set the SPARK_HOME environment variable to your "
                + "Spark installation directory, or install via 'brew install apache-spark'."
        );
    }

    /// <summary>
    /// Resolves the JVM bridge JAR path using the auto-detection hierarchy.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the JAR cannot be located.
    /// </exception>
    public string GetResolvedJarPath()
    {
        if (!string.IsNullOrWhiteSpace(JarPath))
        {
            if (!File.Exists(JarPath))
                throw new InvalidOperationException($"Spark bridge JAR not found at explicitly configured path: {JarPath}");
            return JarPath!;
        }

        var envJar = Environment.GetEnvironmentVariable("FLOWTHRU_SPARK_JAR");
        if (!string.IsNullOrWhiteSpace(envJar))
        {
            if (!File.Exists(envJar))
                throw new InvalidOperationException($"Spark bridge JAR not found at FLOWTHRU_SPARK_JAR path: {envJar}");
            return envJar!;
        }

        // JAR is shipped as contentFiles alongside the executing assembly
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory;
        var jarPath = Path.Combine(assemblyDir, JarFileName);

        if (!File.Exists(jarPath))
            throw new InvalidOperationException(
                $"Spark bridge JAR '{JarFileName}' not found in assembly output directory '{assemblyDir}'. "
                    + "Ensure Flowthru.Extensions.Spark was built with its NX 'build' target so the JAR is staged, "
                    + "or set the FLOWTHRU_SPARK_JAR environment variable explicitly."
            );

        return jarPath;
    }
}
