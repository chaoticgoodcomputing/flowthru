using Flowthru.Core.Flows;
using Flowthru.Core.Services;
using Flowthru.Extensions.Spark.Runtime;
using Flowthru.Extensions.Spark.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Spark.Tests;

[TestFixture]
[Category("DependencyInjection")]
public class SparkDiRegistrationTests
{
    private IServiceProvider BuildProvider(Action<SparkRuntimeOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowthru(flowthru =>
        {
            // A no-op flow is required — AddFlowthru rejects a registration with zero flows.
            flowthru.RegisterFlows(_ => new Dictionary<string, Flow>());

            if (configure is null)
                flowthru.UseSpark();
            else
                flowthru.UseSpark(configure);
        });
        return services.BuildServiceProvider();
    }

    // ===================================================================
    //  SparkFrameProvider
    // ===================================================================

    [Test]
    public void UseSpark_RegistersSparkFrameProvider()
    {
        var provider = BuildProvider();

        var frameProvider = provider.GetService<SparkFrameProvider>();

        Assert.That(frameProvider, Is.Not.Null);
    }

    [Test]
    public void UseSpark_SparkFrameProvider_IsSingleton()
    {
        var provider = BuildProvider();

        var first = provider.GetRequiredService<SparkFrameProvider>();
        var second = provider.GetRequiredService<SparkFrameProvider>();

        Assert.That(first, Is.SameAs(second));
    }

    // ===================================================================
    //  SparkRuntime
    // ===================================================================

    [Test]
    public void UseSpark_RegistersSparkRuntime()
    {
        var provider = BuildProvider();

        var runtime = provider.GetService<SparkRuntime>();

        Assert.That(runtime, Is.Not.Null);
    }

    [Test]
    public void UseSpark_SparkRuntime_IsSingleton()
    {
        var provider = BuildProvider();

        var first = provider.GetRequiredService<SparkRuntime>();
        var second = provider.GetRequiredService<SparkRuntime>();

        Assert.That(first, Is.SameAs(second));
    }

    // ===================================================================
    //  SparkRuntimeOptions
    // ===================================================================

    [Test]
    public void UseSpark_WithConfiguration_RegistersOptions()
    {
        var provider = BuildProvider(opts => opts.Master = "spark://test:7077");

        var options = provider.GetRequiredService<SparkRuntimeOptions>();

        Assert.That(options.Master, Is.EqualTo("spark://test:7077"));
    }

    [Test]
    public void UseSpark_DefaultConfiguration_UsesLocalMaster()
    {
        var provider = BuildProvider();

        var options = provider.GetRequiredService<SparkRuntimeOptions>();

        Assert.That(options.Master, Does.StartWith("local"));
    }
}
