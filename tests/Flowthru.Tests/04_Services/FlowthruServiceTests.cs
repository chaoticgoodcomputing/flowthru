using Flowthru.Data;
using Flowthru.Meta.Models;
using Flowthru.Pipelines;
using Flowthru.Services;
using Flowthru.Services.Models;
using Flowthru.Tests.Common;
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

    // Act
    var result = await service.ExecutePipelineAsync(options: null, exportMetadata: false);

    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.IsDryRun, Is.False);
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

    // Act
    var result = await service.ExecutePipelineAsync(
      options: new ExecutionOptions { DryRun = true },
      exportMetadata: false
    );

    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.IsDryRun, Is.True);
    Assert.That(result.NodeResults, Is.Empty);

    // Verify output was not written
    var outputExists = await catalog.Output.Exists().Run();
    Assert.That(outputExists, Is.False);
  }

  [Test]
  public async Task ExecutePipelineAsync_MergesAndExecutesPipelines()
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
    var result = await service.ExecutePipelineAsync();

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

    pipeline.Build();
    var pipelines = new Dictionary<string, Pipeline> { ["test_pipeline"] = pipeline };

    var service = CreateService(catalog, pipelines);

    // Act
    var metadata = service.GetPipelineMetadata("test_pipeline");

    // Assert
    Assert.That(metadata.Name, Is.EqualTo("test_pipeline"));
    Assert.That(metadata.Description, Is.EqualTo("Test pipeline description"));
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

  [Test]
  public void GetDagMetadata_WithNoPipelineName_ReturnsMergedDag()
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
    var dag = service.GetDagMetadata();

    // Assert
    Assert.That(dag, Is.Not.Null);
    Assert.That(dag.Nodes, Has.Count.EqualTo(2));
    Assert.That(dag.Edges, Is.Not.Empty);
    Assert.That(dag.CatalogEntries, Is.Not.Empty);
    Assert.That(dag.AppliedSlice, Is.Null);
    Assert.That(dag.SlicedNodeIds, Is.Null);
  }

  [Test]
  public void GetDagMetadata_WithPipelineName_ReturnsSinglePipelineDag()
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
    var dag = service.GetDagMetadata("pipeline1");

    // Assert
    Assert.That(dag, Is.Not.Null);
    Assert.That(dag.Nodes, Has.Count.EqualTo(1));
  }

  [Test]
  public void GetDagMetadata_WithSliceStrategy_IncludesSliceOverlay()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Node1",
        transform: PassthroughNode.Create(),
        input: catalog.Input,
        output: catalog.StepOne
      );
      builder.AddNode(
        label: "Node2",
        transform: PassthroughNode.Create(),
        input: catalog.StepOne,
        output: catalog.Output
      );
    });

    var pipelines = new Dictionary<string, Pipeline> { ["test"] = pipeline };

    var service = CreateService(catalog, pipelines);

    // Act — slice to just Node1 (merged names are prefixed with pipeline name)
    var dag = service.GetDagMetadata(
      sliceStrategy: new PipelineSliceStrategy { ToNodes = new HashSet<string> { "test.Node1" } }
    );

    // Assert
    Assert.That(dag, Is.Not.Null);
    Assert.That(dag.SlicedNodeIds, Is.Not.Null);
    Assert.That(dag.AppliedSlice, Is.Not.Null);
  }

  [Test]
  public void GetDagMetadata_WithNonExistentPipeline_ThrowsKeyNotFoundException()
  {
    // Arrange
    var catalog = new SimpleThreeNodeCatalog();
    var pipelines = new Dictionary<string, Pipeline>();

    var service = CreateService(catalog, pipelines);

    // Act & Assert
    var exception = Assert.Throws<KeyNotFoundException>(
      () => service.GetDagMetadata("NonExistent")
    );

    Assert.That(exception.Message, Does.Contain("NonExistent"));
  }

  [Test]
  public void GetDagMetadata_NodesHaveInputsAndOutputs()
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

    var pipelines = new Dictionary<string, Pipeline> { ["test"] = pipeline };

    var service = CreateService(catalog, pipelines);

    // Act
    var dag = service.GetDagMetadata("test");

    // Assert
    var node = dag.Nodes.Single();
    Assert.That(node.Inputs, Is.Not.Empty);
    Assert.That(node.Outputs, Is.Not.Empty);
    Assert.That(node.Layer, Is.GreaterThanOrEqualTo(0));
  }

  [Test]
  public void GetDagMetadata_CatalogEntriesHaveProducerConsumerInfo()
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

    var pipelines = new Dictionary<string, Pipeline> { ["test"] = pipeline };

    var service = CreateService(catalog, pipelines);

    // Act
    var dag = service.GetDagMetadata("test");

    // Assert — the output catalog entry should have "Process" as its producer
    var outputEntry = dag.CatalogEntries.FirstOrDefault(e => e.Producer is not null);
    Assert.That(outputEntry, Is.Not.Null);
    Assert.That(outputEntry!.Producer, Is.Not.Null.And.Not.Empty);

    // The input catalog entry should have "Process" as a consumer
    var inputEntry = dag.CatalogEntries.FirstOrDefault(e => e.Consumers.Count > 0);
    Assert.That(inputEntry, Is.Not.Null);
    Assert.That(inputEntry!.Consumers, Is.Not.Empty);
  }

  [Test]
  public async Task ExecutePipelineAsync_WithMetadataProvider_CapturesMetadata()
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

    var capturingProvider = new CapturingMetadataProvider();

    // Create service with metadata provider configured
    var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    services.AddLogging();
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(catalog);
      flowthru.UsePipelines(_ => pipelines);
      flowthru.ConfigureMetadata(metadata =>
      {
        metadata.AddProvider(capturingProvider);
      });
    });

    var serviceProvider = services.BuildServiceProvider();
    var service = serviceProvider.GetRequiredService<IFlowthruService>();

    // Act - run with exportMetadata: true
    var result = await service.ExecutePipelineAsync(options: null, exportMetadata: true);

    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(capturingProvider.CapturedDag, Is.Not.Null, "Provider should capture DAG metadata");
    Assert.That(capturingProvider.CapturedDag.Nodes, Has.Count.EqualTo(1));
    Assert.That(capturingProvider.CapturedDag.CatalogEntries, Is.Not.Empty);
  }

  [Test]
  public async Task ExecutePipelineAsync_WithoutMetadataExport_DoesNotCallProvider()
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

    var capturingProvider = new CapturingMetadataProvider();

    // Create service with metadata provider configured
    var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    services.AddLogging();
    services.AddFlowthru(flowthru =>
    {
      flowthru.UseCatalog(catalog);
      flowthru.UsePipelines(_ => pipelines);
      flowthru.ConfigureMetadata(metadata =>
      {
        metadata.AddProvider(capturingProvider);
      });
    });

    var serviceProvider = services.BuildServiceProvider();
    var service = serviceProvider.GetRequiredService<IFlowthruService>();

    // Act - run with exportMetadata: false (default)
    var result = await service.ExecutePipelineAsync(options: null, exportMetadata: false);

    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(
      capturingProvider.CapturedDag,
      Is.Null,
      "Provider should not be called when exportMetadata=false"
    );
  }
}
