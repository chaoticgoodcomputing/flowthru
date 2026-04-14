using System;
using Flowthru.Core.Services;
using Flowthru.Extensions.Spark.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Spark.Services;

/// <summary>
/// Extension methods for integrating Spark support with <see cref="FlowthruServiceBuilder"/>.
/// </summary>
public static class FlowthruServiceBuilderExtensions
{
    /// <summary>
    /// Registers the Spark runtime with default configuration.
    /// </summary>
    /// <param name="builder">The Flowthru service builder.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Uses auto-detection for all configuration:
    /// <list type="bullet">
    /// <item>JAR: <c>FLOWTHRU_SPARK_JAR</c> → <c>flowthru-spark-4-1_2.13-2.3.1.jar</c> alongside assembly</item>
    /// <item>Spark home: <c>SPARK_HOME</c> → Homebrew default on macOS</item>
    /// <item>Master: <c>local[*]</c></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example:</strong>
    /// <code>
    /// services.AddFlowthru(flowthru =>
    /// {
    ///     flowthru
    ///         .RegisterCatalog&lt;MyCatalog&gt;()
    ///         .UseSpark();
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    public static FlowthruServiceBuilder UseSpark(this FlowthruServiceBuilder builder) =>
        builder.UseSpark(_ => { });

    /// <summary>
    /// Registers the Spark runtime with custom configuration.
    /// </summary>
    /// <param name="builder">The Flowthru service builder.</param>
    /// <param name="configure">Action to configure Spark runtime options.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Example (explicit master for staging cluster):</strong>
    /// <code>
    /// services.AddFlowthru(flowthru =>
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
    public static FlowthruServiceBuilder UseSpark(
        this FlowthruServiceBuilder builder,
        Action<SparkRuntimeOptions> configure
    )
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new SparkRuntimeOptions();
        configure(options);

        return builder.ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<SparkRuntime>();
            services.AddSingleton<SparkFrameProvider>();
        });
    }
}

