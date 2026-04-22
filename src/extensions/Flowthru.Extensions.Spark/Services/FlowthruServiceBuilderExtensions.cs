using System;
using System.IO;
using System.Reflection;
using Flowthru.Core.Services;
using Flowthru.Extensions.Spark.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Spark.Services;

/// <summary>
/// Extension methods for integrating Spark support with <see cref="IFlowthruBuilder"/>.
/// </summary>
public static class FlowthruServiceBuilderExtensions
{
  /// <summary>
  /// Registers the Spark runtime with configuration bound from <c>Flowthru:Spark</c>.
  /// </summary>
  /// <param name="builder">The Flowthru service builder.</param>
  /// <returns>The builder for method chaining.</returns>
  /// <remarks>
  /// <para>
  /// Platform defaults are applied after configuration binding:
  /// <list type="bullet">
  /// <item>JAR: <c>flowthru-spark-4-1_2.13-2.3.1.jar</c> alongside the executing assembly</item>
  /// <item>Spark home: common Homebrew paths on macOS</item>
  /// <item>Master: <c>local[*]</c></item>
  /// </list>
  /// </para>
  /// <para>
  /// <strong>Example:</strong>
  /// <code>
  /// services.AddFlowthru(configuration, flowthru =>
  /// {
  ///     flowthru
  ///         .RegisterCatalog&lt;MyCatalog&gt;()
  ///         .UseSpark();
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  public static IFlowthruBuilder UseSpark(this IFlowthruBuilder builder)
  {
    builder
      .Services.AddOptions<SparkRuntimeOptions>()
      .Configure<IConfiguration>((opts, cfg) => cfg.GetSection("Flowthru:Spark").Bind(opts))
      .PostConfigure(opts =>
      {
        // Fill SparkHome from SPARK_HOME env var, then Homebrew detection.
        // SPARK_HOME is an Apache Spark convention that predates Flowthru's config
        // namespace, so we read it as a platform default rather than via IConfiguration.
        if (string.IsNullOrWhiteSpace(opts.SparkHome))
        {
          var envSparkHome = Environment.GetEnvironmentVariable("SPARK_HOME");
          if (!string.IsNullOrWhiteSpace(envSparkHome) && Directory.Exists(envSparkHome))
          {
            opts.SparkHome = envSparkHome;
          }
          else
          {
            foreach (
              var candidate in new[]
              {
                "/opt/homebrew/opt/apache-spark/libexec",
                "/usr/local/opt/apache-spark/libexec",
              }
            )
            {
              if (Directory.Exists(candidate))
              {
                opts.SparkHome = candidate;
                break;
              }
            }
          }
        }

        // Fill JarPath from FLOWTHRU_SPARK_JAR env var, then assembly-adjacent detection.
        if (string.IsNullOrWhiteSpace(opts.JarPath))
        {
          var envJar = Environment.GetEnvironmentVariable("FLOWTHRU_SPARK_JAR");
          if (!string.IsNullOrWhiteSpace(envJar))
          {
            opts.JarPath = envJar;
          }
          else
          {
            var assemblyDir =
              Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
              ?? AppContext.BaseDirectory;
            var candidate = Path.Combine(assemblyDir, SparkRuntimeOptions.JarFileName);
            if (File.Exists(candidate))
            {
              opts.JarPath = candidate;
            }
          }
        }
      })
      .ValidateOnStart();

    builder.Services.AddSingleton<SparkRuntime>();
    builder.Services.AddSingleton<SparkFrameProvider>();

    return builder;
  }

  /// <summary>
  /// Registers the Spark runtime with code-first configuration overrides.
  /// </summary>
  /// <param name="builder">The Flowthru service builder.</param>
  /// <param name="configure">Action to override Spark options after config-file binding.</param>
  /// <returns>The builder for method chaining.</returns>
  /// <remarks>
  /// <para>
  /// The <paramref name="configure"/> callback runs after <c>Flowthru:Spark</c> section
  /// binding and platform auto-detection, so it can selectively override specific values.
  /// </para>
  /// <para>
  /// <strong>Example (explicit master for staging cluster):</strong>
  /// <code>
  /// services.AddFlowthru(configuration, flowthru =>
  /// {
  ///     flowthru
  ///         .RegisterCatalog&lt;MyCatalog&gt;()
  ///         .UseSpark(spark =>
  ///         {
  ///             spark.Master = "spark://staging-cluster:7077";
  ///         });
  /// });
  /// </code>
  /// </para>
  /// </remarks>
  public static IFlowthruBuilder UseSpark(
    this IFlowthruBuilder builder,
    Action<SparkRuntimeOptions> configure
  )
  {
    builder.UseSpark();
    builder.Services.PostConfigure(configure);
    return builder;
  }
}
