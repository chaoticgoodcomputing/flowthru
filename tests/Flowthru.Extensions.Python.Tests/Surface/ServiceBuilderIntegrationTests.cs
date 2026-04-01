using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Services;
using Flowthru.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Python.Tests.Surface;

/// <summary>
/// Tests for FlowthruServiceBuilder.UsePython() integration.
/// Validates the Surface API — the happy path for DI registration.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Surface")]
public class ServiceBuilderIntegrationTests
{
  [Test]
  public void UsePython_WithDefaultConfiguration_RegistersServices()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(new TestCatalog());
      flowthru.RegisterPipelines(_ => new Dictionary<string, Pipelines.Pipeline>());
      flowthru.UsePython();
    });

    var provider = services.BuildServiceProvider();

    // Assert
    var executor = provider.GetService<IPythonExecutor>();
    Assert.That(executor, Is.Not.Null);
    Assert.That(executor, Is.InstanceOf<SubprocessPythonExecutor>());
  }

  [Test]
  public void UsePython_WithCustomConfiguration_AppliesOptions()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Act
    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(new TestCatalog());
      flowthru.RegisterPipelines(_ => new Dictionary<string, Pipelines.Pipeline>());
      flowthru.UsePython(python =>
      {
        python.ModuleSearchPaths.Add("/custom/path");
      });
    });

    var provider = services.BuildServiceProvider();

    // Assert
    var options = provider.GetService<Runtime.PythonRuntimeOptions>();
    Assert.That(options, Is.Not.Null);
    Assert.That(options!.ModuleSearchPaths, Does.Contain("/custom/path"));
  }

  // Minimal test catalog for integration tests
  private class TestCatalog : Data.DataCatalogBase { }
}
