using Flowthru.Data;
using Flowthru.Pipelines;
using Flowthru.Services;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestNodes;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Tests.Services;

/// <summary>
/// Tests verifying correct behavior of Flowthru service DI registration.
/// </summary>
[TestFixture]
[Category("Services")]
[Category("DependencyInjection")]
public class ServiceCollectionExtensionsTests
{
  [Test]
  public void AddFlowthru_RegistersService()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(new SimpleThreeNodeCatalog());
      flowthru.UsePipelines(catalog => new Dictionary<string, Pipeline>());
    });

    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var service = serviceProvider.GetService<IFlowthruService>();
    Assert.That(service, Is.Not.Null);
  }

  [Test]
  public void AddFlowthru_RegistersCatalog()
  {
    // Arrange
    var services = new ServiceCollection();
    var catalog = new SimpleThreeNodeCatalog();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(catalog);
      flowthru.UsePipelines(c => new Dictionary<string, Pipeline>());
    });

    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var resolvedCatalog = serviceProvider.GetService<DataCatalogBase>();
    Assert.That(resolvedCatalog, Is.Not.Null);
    Assert.That(resolvedCatalog, Is.SameAs(catalog));
  }

  [Test]
  public void AddFlowthru_WithCatalogType_ResolvesCatalog()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddSingleton<SimpleThreeNodeCatalog>();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog<SimpleThreeNodeCatalog>();
      flowthru.UsePipelines(c => new Dictionary<string, Pipeline>());
    });

    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var catalog = serviceProvider.GetService<DataCatalogBase>();
    Assert.That(catalog, Is.Not.Null);
    Assert.That(catalog, Is.InstanceOf<SimpleThreeNodeCatalog>());
  }

  [Test]
  public void AddFlowthru_WithCatalogFactory_CreatesCatalog()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(sp => new SimpleThreeNodeCatalog());
      flowthru.UsePipelines(c => new Dictionary<string, Pipeline>());
    });

    var serviceProvider = services.BuildServiceProvider();

    // Assert
    var catalog = serviceProvider.GetService<DataCatalogBase>();
    Assert.That(catalog, Is.Not.Null);
    Assert.That(catalog, Is.InstanceOf<SimpleThreeNodeCatalog>());
  }

  [Test]
  public void AddFlowthru_RegistersPipelines()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(new SimpleThreeNodeCatalog());
      flowthru.UsePipelines(catalog =>
      {
        var typedCatalog = (SimpleThreeNodeCatalog)catalog;
        var pipeline = PipelineBuilder.CreatePipeline(builder =>
        {
          builder.AddNode(
            label: "Node",
            transform: PassthroughNode.Create(),
            input: typedCatalog.Input,
            output: typedCatalog.Output
          );
        });

        return new Dictionary<string, Pipeline> { ["test"] = pipeline };
      });
    });

    var serviceProvider = services.BuildServiceProvider();
    var service = serviceProvider.GetRequiredService<IFlowthruService>();

    // Assert
    Assert.That(service.PipelineNames, Has.Count.EqualTo(1));
    Assert.That(service.PipelineNames, Does.Contain("test"));
  }

  [Test]
  public void AddFlowthru_WithNullServices_ThrowsArgumentNullException()
  {
    // Arrange
    ServiceCollection services = null!;

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => services.AddFlowthru(flowthru => { }));
  }

  [Test]
  public void AddFlowthru_WithNullConfigure_ThrowsArgumentNullException()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => services.AddFlowthru(null!));
  }

  [Test]
  public void AddFlowthru_WithMetadata_ConfiguresMetadataBuilder()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(new SimpleThreeNodeCatalog());
      flowthru.UsePipelines(c => new Dictionary<string, Pipeline>());
      flowthru.ConfigureMetadata(meta =>
      {
        meta.WithOutputDirectory("test-metadata").AddJson();
      });
    });

    var serviceProvider = services.BuildServiceProvider();
    var metadataBuilder = serviceProvider.GetService<Meta.FlowthruMetadataBuilder>();

    // Assert
    Assert.That(metadataBuilder, Is.Not.Null);
    Assert.That(metadataBuilder.OutputDirectory, Is.EqualTo("test-metadata"));
  }

  [Test]
  public void AddFlowthru_ServiceIsSingleton()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(new SimpleThreeNodeCatalog());
      flowthru.UsePipelines(c => new Dictionary<string, Pipeline>());
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var service1 = serviceProvider.GetRequiredService<IFlowthruService>();
    var service2 = serviceProvider.GetRequiredService<IFlowthruService>();

    // Assert
    Assert.That(service1, Is.SameAs(service2));
  }
}
