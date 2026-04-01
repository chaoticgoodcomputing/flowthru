using Flowthru.Data;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
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
      flowthru.UsePipelines(sp => new Dictionary<string, Pipeline>());
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
      flowthru.UsePipelines(sp => new Dictionary<string, Pipeline>());
    });

    var serviceProvider = services.BuildServiceProvider();

    // Assert — catalogs are registered by concrete type, not DataCatalogBase
    var resolvedCatalog = serviceProvider.GetService<SimpleThreeNodeCatalog>();
    Assert.That(resolvedCatalog, Is.Not.Null);
    Assert.That(resolvedCatalog, Is.SameAs(catalog));
  }

  [Test]
  public void AddFlowthru_WithCatalogType_ResolvesCatalog()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog<SimpleThreeNodeCatalog>();
      flowthru.UsePipelines(sp => new Dictionary<string, Pipeline>());
    });

    var serviceProvider = services.BuildServiceProvider();

    // Assert — concrete type is resolvable directly
    var catalog = serviceProvider.GetService<SimpleThreeNodeCatalog>();
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
      flowthru.UseCatalog<SimpleThreeNodeCatalog>(sp => new SimpleThreeNodeCatalog());
      flowthru.UsePipelines(sp => new Dictionary<string, Pipeline>());
    });

    var serviceProvider = services.BuildServiceProvider();

    // Assert — concrete type is resolvable directly
    var catalog = serviceProvider.GetService<SimpleThreeNodeCatalog>();
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
      var catalog = new SimpleThreeNodeCatalog();
      flowthru.UseCatalog(catalog);
      flowthru.RegisterPipeline(
        "test",
        (SimpleThreeNodeCatalog cat) =>
          PipelineBuilder.CreatePipeline(builder =>
          {
            builder.AddNode(
              label: "Node",
              transform: PassthroughNode.Create(),
              input: cat.Input,
              output: cat.Output
            );
          })
      );
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
      flowthru.UsePipelines(sp => new Dictionary<string, Pipeline>());
      flowthru.ConfigureMetadata(meta =>
      {
        meta.AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json =>
          json.WithOutputDirectory("test-metadata")
        );
      });
    });

    var serviceProvider = services.BuildServiceProvider();
    var metadataBuilder = serviceProvider.GetService<Meta.FlowthruMetadataBuilder>();

    // Assert
    Assert.That(metadataBuilder, Is.Not.Null);
    Assert.That(metadataBuilder.Providers.Count, Is.EqualTo(1));
    Assert.That(metadataBuilder.Providers[0].Name, Is.EqualTo("JSON"));
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
      flowthru.UsePipelines(sp => new Dictionary<string, Pipeline>());
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var service1 = serviceProvider.GetRequiredService<IFlowthruService>();
    var service2 = serviceProvider.GetRequiredService<IFlowthruService>();

    // Assert
    Assert.That(service1, Is.SameAs(service2));
  }

  [Test]
  public void AddFlowthru_RegisterPipelineAndUsePipelines_MergesAll()
  {
    // Arrange — one inline registration and one factory-based registration
    var services = new ServiceCollection();
    services.AddLogging();

    var catalog = new SimpleThreeNodeCatalog();

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(catalog);

      flowthru.RegisterPipeline(
        "inline",
        (SimpleThreeNodeCatalog cat) =>
          PipelineBuilder.CreatePipeline(builder =>
          {
            builder.AddNode(
              label: "Node",
              transform: PassthroughNode.Create(),
              input: cat.Input,
              output: cat.Output
            );
          })
      );

      flowthru.UsePipelines(_ => new Dictionary<string, Pipeline>
      {
        ["dynamic"] = PipelineBuilder.CreatePipeline(builder =>
        {
          builder.AddNode(
            label: "Node",
            transform: PassthroughNode.Create(),
            input: catalog.Input,
            output: catalog.Output
          );
        }),
      });
    });

    var serviceProvider = services.BuildServiceProvider();
    var service = serviceProvider.GetRequiredService<IFlowthruService>();

    // Assert — both pipelines are present
    Assert.That(service.PipelineNames, Has.Count.EqualTo(2));
    Assert.That(service.PipelineNames, Does.Contain("inline"));
    Assert.That(service.PipelineNames, Does.Contain("dynamic"));
  }
}
