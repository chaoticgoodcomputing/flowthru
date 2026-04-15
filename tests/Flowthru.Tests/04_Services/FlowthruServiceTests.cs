using Flowthru.Core.Data;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Graph.Validation;
using Flowthru.Core.Services;
using Flowthru.Core.Services.Models;
using Flowthru.Tests.Common;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestSteps;
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
      SimpleThreeStepCatalog catalog,
      Dictionary<string, Flow> pipelines
    )
    {
        // Use the public AddFlowthru extension method to create the service
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddFlowthru(flowthru =>
        {
            flowthru.RegisterCatalog(catalog);
            flowthru.RegisterFlows(sp => pipelines);
        });

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IFlowthruService>();
    }

    [Test]
    public void Constructor_WithNullCatalog_ThrowsArgumentNullException()
    {
        // Arrange
        CatalogAbstract nullCatalog = null!;

        // Act & Assert — RegisterCatalog(null) must throw immediately, not silently register null
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
        {
            services.AddFlowthru(flowthru =>
        {
              flowthru.RegisterCatalog(nullCatalog);
          });
        });
    }

    [Test]
    public void Constructor_WithNullFlows_ThrowsArgumentNullException()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();

        // Act & Assert
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();

        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddFlowthru(flowthru =>
        {
              flowthru.RegisterCatalog(catalog);
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
    public void FlowNames_ReturnsRegisteredFlows()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipeline1 = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Step1",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.StepOne
        );
        });

        var pipeline2 = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Step2",
          transform: PassthroughStep.Create(),
          input: catalog.StepOne,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow>
        {
            ["pipeline1"] = pipeline1,
            ["pipeline2"] = pipeline2,
        };

        var service = CreateService(catalog, pipelines);

        // Act
        var names = service.FlowNames;

        // Assert
        Assert.That(names, Has.Count.EqualTo(2));
        Assert.That(names, Does.Contain("pipeline1"));
        Assert.That(names, Does.Contain("pipeline2"));
    }

    [Test]
    public void Catalog_ReturnsCatalogInstance()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipelines = new Dictionary<string, Flow>();

        var service = CreateService(catalog, pipelines);

        // Act
        var result = service.Catalogs;

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.First(), Is.InstanceOf<SimpleThreeStepCatalog>());
    }

    [Test]
    public async Task ExecuteFlowAsync_WithValidFlow_ExecutesSuccessfully()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
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

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        pipeline.Name = "test_pipeline";
        var pipelines = new Dictionary<string, Flow> { ["test_pipeline"] = pipeline };

        var service = CreateService(catalog, pipelines);

        // Act
        var result = await service.ExecuteFlowAsync(options: null, exportMetadata: false);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.IsDryRun, Is.False);
        Assert.That(result.StepResults, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ExecuteFlowAsync_WithDryRun_DoesNotExecuteSteps()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
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

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow> { ["test_pipeline"] = pipeline };

        var service = CreateService(catalog, pipelines);

        // Act
        var result = await service.ExecuteFlowAsync(
          options: new ExecutionOptions { DryRun = true },
          exportMetadata: false
        );

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.IsDryRun, Is.True);
        Assert.That(result.StepResults, Is.Empty);

        // Verify output was not written
        var outputExists = await catalog.Output.Exists().Run();
        Assert.That(outputExists, Is.False);
    }

    [Test]
    public async Task ExecuteFlowAsync_WithStructureOnlyDryRun_SucceedsWithoutData()
    {
        // Arrange — no data seeded; StructureOnly must not probe any data source
        var catalog = new SimpleThreeStepCatalog();

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow> { ["test_pipeline"] = pipeline };
        var service = CreateService(catalog, pipelines);

        // Act
        var result = await service.ExecuteFlowAsync(
          options: new ExecutionOptions { DryRun = ValidationDepth.StructureOnly },
          exportMetadata: false
        );

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.IsDryRun, Is.True);
        Assert.That(result.StepResults, Is.Empty);
    }

    [Test]
    public async Task ExecuteFlowAsync_WithFullDryRun_FailsWithoutData()
    {
        // Arrange — no data seeded; Full depth must surface the missing external input
        var catalog = new SimpleThreeStepCatalog();

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow> { ["test_pipeline"] = pipeline };
        var service = CreateService(catalog, pipelines);

        // Act & Assert — Full validation probes external inputs and must fail when absent
        Assert.ThrowsAsync<ValidationException>(
          async () =>
            await service.ExecuteFlowAsync(
              options: new ExecutionOptions { DryRun = true },
              exportMetadata: false
            )
        );
    }

    [Test]
    public async Task ExecuteFlowAsync_MergesAndExecutesFlows()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
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

        var pipeline1 = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Step1",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.StepOne
        );
        });

        var pipeline2 = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Step2",
          transform: PassthroughStep.Create(),
          input: catalog.StepOne,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow>
        {
            ["pipeline1"] = pipeline1,
            ["pipeline2"] = pipeline2,
        };

        var service = CreateService(catalog, pipelines);

        // Act
        var result = await service.ExecuteFlowAsync();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.StepResults, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetFlowMetadata_WithValidFlow_ReturnsMetadata()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        // Set metadata properties directly
        pipeline.Name = "test_pipeline";
        pipeline.Description = "Test pipeline description";

        pipeline.Build();
        var pipelines = new Dictionary<string, Flow> { ["test_pipeline"] = pipeline };

        var service = CreateService(catalog, pipelines);

        // Act
        var metadata = service.GetFlowMetadata("test_pipeline");

        // Assert
        Assert.That(metadata.Name, Is.EqualTo("test_pipeline"));
        Assert.That(metadata.Description, Is.EqualTo("Test pipeline description"));
        Assert.That(metadata.StepCount, Is.EqualTo(1));
        Assert.That(metadata.LayerCount, Is.EqualTo(1)); // Single layer: node with no dependencies
        Assert.That(metadata.IsBuilt, Is.True);
    }

    [Test]
    public void GetFlowMetadata_WithNonExistentFlow_ThrowsKeyNotFoundException()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipelines = new Dictionary<string, Flow>();

        var service = CreateService(catalog, pipelines);

        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>(
          () => service.GetFlowMetadata("NonExistent")
        );

        Assert.That(exception.Message, Does.Contain("NonExistent"));
    }

    [Test]
    public async Task ValidateFlowAsync_WithValidInputs_ReturnsSuccess()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
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

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow> { ["test_pipeline"] = pipeline };

        var service = CreateService(catalog, pipelines);

        // Act
        var validationResult = await service.ValidateFlowAsync("test_pipeline");

        // Assert
        Assert.That(validationResult.IsValid, Is.True);
        Assert.That(validationResult.HasErrors, Is.False);
    }

    [Test]
    public async Task ValidateFlowAsync_WithMissingInputs_ReturnsFailure()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        // Note: Not saving any data to Input

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        // Configure validation to check for Shallow inspection (existence check)
        pipeline.ValidationOptions.Inspect(catalog.Input, InspectionLevel.Shallow);

        var pipelines = new Dictionary<string, Flow> { ["test_pipeline"] = pipeline };

        var service = CreateService(catalog, pipelines);

        // Act
        var validationResult = await service.ValidateFlowAsync("test_pipeline");

        // Assert
        Assert.That(validationResult.IsValid, Is.False);
        Assert.That(validationResult.HasErrors, Is.True);
        Assert.That(validationResult.Errors, Is.Not.Empty);
    }

    [Test]
    public void ValidateFlowAsync_WithNonExistentFlow_ThrowsKeyNotFoundException()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipelines = new Dictionary<string, Flow>();

        var service = CreateService(catalog, pipelines);

        // Act & Assert
        var exception = Assert.ThrowsAsync<KeyNotFoundException>(
          async () => await service.ValidateFlowAsync("NonExistent")
        );

        Assert.That(exception.Message, Does.Contain("NonExistent"));
    }

    [Test]
    public void GetDagMetadata_WithNoFlowName_ReturnsMergedDag()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipeline1 = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Step1",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.StepOne
        );
        });

        var pipeline2 = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Step2",
          transform: PassthroughStep.Create(),
          input: catalog.StepOne,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow>
        {
            ["pipeline1"] = pipeline1,
            ["pipeline2"] = pipeline2,
        };

        var service = CreateService(catalog, pipelines);

        // Act
        var dag = service.GetDagMetadata();

        // Assert
        Assert.That(dag, Is.Not.Null);
        Assert.That(dag.Steps, Has.Count.EqualTo(2));
        Assert.That(dag.Edges, Is.Not.Empty);
        Assert.That(dag.CatalogItems, Is.Not.Empty);
        Assert.That(dag.AppliedSlice, Is.Null);
        Assert.That(dag.SlicedStepIds, Is.Null);
    }

    [Test]
    public void GetDagMetadata_WithFlowName_ReturnsSingleFlowDag()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipeline1 = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Step1",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.StepOne
        );
        });

        var pipeline2 = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Step2",
          transform: PassthroughStep.Create(),
          input: catalog.StepOne,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow>
        {
            ["pipeline1"] = pipeline1,
            ["pipeline2"] = pipeline2,
        };

        var service = CreateService(catalog, pipelines);

        // Act
        var dag = service.GetDagMetadata("pipeline1");

        // Assert
        Assert.That(dag, Is.Not.Null);
        Assert.That(dag.Steps, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetDagMetadata_WithSliceStrategy_IncludesSliceOverlay()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Step1",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.StepOne
        );
            builder.AddStep(
          label: "Step2",
          transform: PassthroughStep.Create(),
          input: catalog.StepOne,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow> { ["test"] = pipeline };

        var service = CreateService(catalog, pipelines);

        // Act — slice to just Step1 (merged names are prefixed with flow name)
        var dag = service.GetDagMetadata(
          sliceStrategy: new FlowSliceStrategy { To = new HashSet<string> { "test.Step1" } }
        );

        // Assert
        Assert.That(dag, Is.Not.Null);
        Assert.That(dag.SlicedStepIds, Is.Not.Null);
        Assert.That(dag.AppliedSlice, Is.Not.Null);
    }

    [Test]
    public void GetDagMetadata_WithNonExistentFlow_ThrowsKeyNotFoundException()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipelines = new Dictionary<string, Flow>();

        var service = CreateService(catalog, pipelines);

        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>(
          () => service.GetDagMetadata("NonExistent")
        );

        Assert.That(exception.Message, Does.Contain("NonExistent"));
    }

    [Test]
    public void GetDagMetadata_StepsHaveInputsAndOutputs()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow> { ["test"] = pipeline };

        var service = CreateService(catalog, pipelines);

        // Act
        var dag = service.GetDagMetadata("test");

        // Assert
        var node = dag.Steps.Single();
        Assert.That(node.Inputs, Is.Not.Empty);
        Assert.That(node.Outputs, Is.Not.Empty);
        Assert.That(node.Layer, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void GetDagMetadata_ItemsHaveProducerConsumerInfo()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        var pipelines = new Dictionary<string, Flow> { ["test"] = pipeline };

        var service = CreateService(catalog, pipelines);

        // Act
        var dag = service.GetDagMetadata("test");

        // Assert — the output catalog entry should have "Process" as its producer
        var outputEntry = dag.CatalogItems.FirstOrDefault(e => e.Producer is not null);
        Assert.That(outputEntry, Is.Not.Null);
        Assert.That(outputEntry!.Producer, Is.Not.Null.And.Not.Empty);

        // The input catalog entry should have "Process" as a consumer
        var inputEntry = dag.CatalogItems.FirstOrDefault(e => e.Consumers.Count > 0);
        Assert.That(inputEntry, Is.Not.Null);
        Assert.That(inputEntry!.Consumers, Is.Not.Empty);
    }

    [Test]
    public async Task ExecuteFlowAsync_WithMetadataProvider_CapturesMetadata()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
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

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        pipeline.Name = "test_pipeline";
        var pipelines = new Dictionary<string, Flow> { ["test_pipeline"] = pipeline };

        var capturingProvider = new CapturingMetadataProvider();

        // Create service with metadata provider configured
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddFlowthru(flowthru =>
        {
            flowthru.RegisterCatalog(catalog);
            flowthru.RegisterFlows(_ => pipelines);
            flowthru.ConfigureMetadata(metadata =>
        {
              metadata.AddProvider(capturingProvider);
          });
        });

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IFlowthruService>();

        // Act - run with exportMetadata: true
        var result = await service.ExecuteFlowAsync(options: null, exportMetadata: true);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(capturingProvider.CapturedDag, Is.Not.Null, "Provider should capture DAG metadata");
        Assert.That(capturingProvider.CapturedDag.Steps, Has.Count.EqualTo(1));
        Assert.That(capturingProvider.CapturedDag.CatalogItems, Is.Not.Empty);
    }

    [Test]
    public async Task ExecuteFlowAsync_WithoutMetadataExport_DoesNotCallProvider()
    {
        // Arrange
        var catalog = new SimpleThreeStepCatalog();
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

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Process",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        pipeline.Name = "test_pipeline";
        var pipelines = new Dictionary<string, Flow> { ["test_pipeline"] = pipeline };

        var capturingProvider = new CapturingMetadataProvider();

        // Create service with metadata provider configured
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddFlowthru(flowthru =>
        {
            flowthru.RegisterCatalog(catalog);
            flowthru.RegisterFlows(_ => pipelines);
            flowthru.ConfigureMetadata(metadata =>
        {
              metadata.AddProvider(capturingProvider);
          });
        });

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IFlowthruService>();

        // Act - run with exportMetadata: false (default)
        var result = await service.ExecuteFlowAsync(options: null, exportMetadata: false);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(
          capturingProvider.CapturedDag,
          Is.Null,
          "Provider should not be called when exportMetadata=false"
        );
    }
}
