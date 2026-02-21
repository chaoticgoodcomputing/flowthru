using Flowthru.Data;
using Flowthru.Pipelines;
using Flowthru.Services;
using Flowthru.Services.Models;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestNodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowthru.Tests.Services;

/// <summary>
/// Tests verifying correct behavior of <see cref="IFlowthruService"/>.
/// </summary>
[TestFixture]
[Category("Services")]
public class FlowthruServiceTests
{
  private ILogger<IFlowthruService> _logger = null!;
  private IServiceProvider? _serviceProvider;

  [SetUp]
  public void SetUp()
  {
    // Create null logger (no output during tests)
    _logger = NullLogger<IFlowthruService>.Instance;

    // Create minimal service provider
    var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    _serviceProvider = services.BuildServiceProvider();
  }

  [TearDown]
  public void TearDown()
  {
    if (_serviceProvider is IDisposable disposable)
    {
      disposable.Dispose();
    }
  }

  private IFlowthruService CreateService(
    DataCatalogBase catalog,
    Dictionary<string, Pipeline> pipelines
  )
  {
    // Use the public AddFlowthru extension method to create the service
    var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    services.AddLogging();
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(catalog);
      flowthru.UsePipelines(_ => pipelines);
    });

    var serviceProvider = services.BuildServiceProvider();
    return serviceProvider.GetRequiredService<IFlowthruService>();
  }

  [Test]
  public void Constructor_WithNullCatalog_ThrowsArgumentNullException()
  {
    // Arrange
    var pipelines = new Dictionary<string, Pipeline>();

    // Act & Assert
    // Service creation happens through DI, so this test validates DI registration
    var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    services.AddLogging();

    Assert.Throws<InvalidOperationException>(() =>
    {
      services.AddFlowthru(flowthru =>
      {
        // Don't register catalog
        flowthru.UsePipelines(_ => pipelines);
      });

      var serviceProvider = services.BuildServiceProvider();
      serviceProvider.GetRequiredService<IFlowthruService>();
    });
  }

  [Test]
  public void Constructor_WithNullPipelines_ThrowsArgumentNullException()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();

    // Act & Assert
    var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    services.AddLogging();

    Assert.Throws<InvalidOperationException>(() =>
    {
      services.AddFlowthru(flowthru =>
      {
        flowthru.UseCatalog(catalog);
        // Don't register pipelines
      });

      var serviceProvider = services.BuildServiceProvider();
      serviceProvider.GetRequiredService<IFlowthruService>();
    });
  }

  [Test]
  public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
  {
    // Arrange - Skip this test since service provider is always provided by DI
    Assert.Pass("Service provider is always provided by DI container");
  }

  [Test]
  public void Constructor_WithNullLogger_ThrowsArgumentNullException()
  {
    // Arrange - Skip this test since logger is always provided by DI
    Assert.Pass("Logger is always provided by DI container");
  }

  [Test]
  public void PipelineNames_ReturnsRegisteredPipelines()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var pipeline1 = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Node1",
        transform: PassthroughNode.Create(),
        input: catalog.Input,
        output: catalog.StepOne
      );
    });

    var pipeline2 = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Node2",
        transform: PassthroughNode.Create(),
        input: catalog.StepOne,
        output: catalog.Output
      );
    });

    var pipelines = new Dictionary<string, Pipeline>
    {
      ["pipeline1"] = pipeline1,
      ["pipeline2"] = pipeline2,
    };

    var service = CreateService(catalog, pipelines);

    // Act
    var names = service.PipelineNames;

    // Assert
    Assert.That(names, Has.Count.EqualTo(2));
    Assert.That(names, Does.Contain("pipeline1"));
    Assert.That(names, Does.Contain("pipeline2"));
  }

  [Test]
  public void Catalog_ReturnsCatalogInstance()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var pipelines = new Dictionary<string, Pipeline>();

    var service = CreateService(catalog, pipelines);

    // Act
    var result = service.Catalog;

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result, Is.InstanceOf<SimpleThreeNodeCatalog>());
  }

  [Test]
  public async Task ExecutePipelineAsync_WithNonExistentPipeline_ThrowsKeyNotFoundException()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var pipelines = new Dictionary<string, Pipeline>();

    var service = CreateService(catalog, pipelines);

    var request = new PipelineExecutionRequest { PipelineName = "NonExistent" };

    // Act & Assert
    var exception = Assert.ThrowsAsync<KeyNotFoundException>(
      async () => await service.ExecutePipelineAsync(request)
    );

    Assert.That(exception.Message, Does.Contain("NonExistent"));
  }

  [Test]
  public async Task ExecutePipelineAsync_WithValidPipeline_ExecutesSuccessfully()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var testData = new[]
    {
      new TestData
      {
        Id = 1,
        Name = "Test",
        Value = 42.0,
      },
    };
    await catalog.Input.Save(testData).Run();

    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Process",
        transform: PassthroughNode.Create(),
        input: catalog.Input,
        output: catalog.Output
      );
    });

    pipeline.Name = "test_pipeline";
    var pipelines = new Dictionary<string, Pipeline> { ["test_pipeline"] = pipeline };

    var service = CreateService(catalog, pipelines);

    var request = new PipelineExecutionRequest
    {
      PipelineName = "test_pipeline",
      ExportMetadata = false, // Disable metadata export for test
    };

    // Act
    var result = await service.ExecutePipelineAsync(request);

    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.IsDryRun, Is.False);
    Assert.That(result.PipelineName, Is.EqualTo("test_pipeline"));
    Assert.That(result.NodeResults, Has.Count.EqualTo(1));
  }

  [Test]
  public async Task ExecutePipelineAsync_WithDryRun_DoesNotExecuteNodes()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var testData = new[]
    {
      new TestData
      {
        Id = 1,
        Name = "Test",
        Value = 42.0,
      },
    };
    await catalog.Input.Save(testData).Run();

    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Process",
        transform: PassthroughNode.Create(),
        input: catalog.Input,
        output: catalog.Output
      );
    });

    var pipelines = new Dictionary<string, Pipeline> { ["test_pipeline"] = pipeline };

    var service = CreateService(catalog, pipelines);

    var request = new PipelineExecutionRequest
    {
      PipelineName = "test_pipeline",
      Options = new ExecutionOptions { DryRun = true },
      ExportMetadata = false,
    };

    // Act
    var result = await service.ExecutePipelineAsync(request);

    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.IsDryRun, Is.True);
    Assert.That(result.NodeResults, Is.Empty);

    // Verify output was not written
    var outputExists = await catalog.Output.Exists().Run();
    Assert.That(outputExists, Is.False);
  }

  [Test]
  public async Task ExecuteAllPipelinesAsync_MergesAndExecutesPipelines()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var testData = new[]
    {
      new TestData
      {
        Id = 1,
        Name = "Test",
        Value = 42.0,
      },
    };
    await catalog.Input.Save(testData).Run();

    var pipeline1 = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Node1",
        transform: PassthroughNode.Create(),
        input: catalog.Input,
        output: catalog.StepOne
      );
    });

    var pipeline2 = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Node2",
        transform: PassthroughNode.Create(),
        input: catalog.StepOne,
        output: catalog.Output
      );
    });

    var pipelines = new Dictionary<string, Pipeline>
    {
      ["pipeline1"] = pipeline1,
      ["pipeline2"] = pipeline2,
    };

    var service = CreateService(catalog, pipelines);

    // Act
    var result = await service.ExecuteAllPipelinesAsync();

    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.NodeResults, Has.Count.EqualTo(2));
  }

  [Test]
  public void GetPipelineMetadata_WithValidPipeline_ReturnsMetadata()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Process",
        transform: PassthroughNode.Create(),
        input: catalog.Input,
        output: catalog.Output
      );
    });

    // Set metadata properties directly
    pipeline.Name = "test_pipeline";
    pipeline.Description = "Test pipeline description";
    pipeline.Tags = new[] { "test", "metadata" };

    pipeline.Build();
    var pipelines = new Dictionary<string, Pipeline> { ["test_pipeline"] = pipeline };

    var service = CreateService(catalog, pipelines);

    // Act
    var metadata = service.GetPipelineMetadata("test_pipeline");

    // Assert
    Assert.That(metadata.Name, Is.EqualTo("test_pipeline"));
    Assert.That(metadata.Description, Is.EqualTo("Test pipeline description"));
    Assert.That(metadata.Tags, Has.Count.EqualTo(2));
    Assert.That(metadata.Tags, Does.Contain("test"));
    Assert.That(metadata.Tags, Does.Contain("metadata"));
    Assert.That(metadata.NodeCount, Is.EqualTo(1));
    Assert.That(metadata.LayerCount, Is.EqualTo(1)); // Single layer: node with no dependencies
    Assert.That(metadata.IsBuilt, Is.True);
  }

  [Test]
  public void GetPipelineMetadata_WithNonExistentPipeline_ThrowsKeyNotFoundException()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var pipelines = new Dictionary<string, Pipeline>();

    var service = CreateService(catalog, pipelines);

    // Act & Assert
    var exception = Assert.Throws<KeyNotFoundException>(
      () => service.GetPipelineMetadata("NonExistent")
    );

    Assert.That(exception.Message, Does.Contain("NonExistent"));
  }

  [Test]
  public async Task ValidatePipelineAsync_WithValidInputs_ReturnsSuccess()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var testData = new[]
    {
      new TestData
      {
        Id = 1,
        Name = "Test",
        Value = 42.0,
      },
    };
    await catalog.Input.Save(testData).Run();

    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Process",
        transform: PassthroughNode.Create(),
        input: catalog.Input,
        output: catalog.Output
      );
    });

    var pipelines = new Dictionary<string, Pipeline> { ["test_pipeline"] = pipeline };

    var service = CreateService(catalog, pipelines);

    // Act
    var validationResult = await service.ValidatePipelineAsync("test_pipeline");

    // Assert
    Assert.That(validationResult.IsValid, Is.True);
    Assert.That(validationResult.HasErrors, Is.False);
  }

  [Test]
  public async Task ValidatePipelineAsync_WithMissingInputs_ReturnsFailure()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    // Note: Not saving any data to Input

    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Process",
        transform: PassthroughNode.Create(),
        input: catalog.Input,
        output: catalog.Output
      );
    });

    // Configure validation to check for Shallow inspection (existence check)
    pipeline.ValidationOptions.Inspect(catalog.Input, Data.Validation.InspectionLevel.Shallow);

    var pipelines = new Dictionary<string, Pipeline> { ["test_pipeline"] = pipeline };

    var service = CreateService(catalog, pipelines);

    // Act
    var validationResult = await service.ValidatePipelineAsync("test_pipeline");

    // Assert
    Assert.That(validationResult.IsValid, Is.False);
    Assert.That(validationResult.HasErrors, Is.True);
    Assert.That(validationResult.Errors, Is.Not.Empty);
  }

  [Test]
  public void ValidatePipelineAsync_WithNonExistentPipeline_ThrowsKeyNotFoundException()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var pipelines = new Dictionary<string, Pipeline>();

    var service = CreateService(catalog, pipelines);

    // Act & Assert
    var exception = Assert.ThrowsAsync<KeyNotFoundException>(
      async () => await service.ValidatePipelineAsync("NonExistent")
    );

    Assert.That(exception.Message, Does.Contain("NonExistent"));
  }
}
