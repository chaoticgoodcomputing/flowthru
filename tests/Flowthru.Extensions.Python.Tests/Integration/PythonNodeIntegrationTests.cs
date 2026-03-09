using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Nodes;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Tests.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Python.Tests.Integration;

/// <summary>
/// Integration tests for Python node wrappers.
/// Tests the complete Phase 2 implementation: scalar marshalling and PythonNodeWrapper.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Integration")]
public class PythonNodeWrapperIntegrationTests
{
  private IServiceProvider _serviceProvider = null!;
  private IPythonExecutor _executor = null!;

#pragma warning disable NUnit1032 // The field type is not disposed. Suppressed because _runtime is a reference to the shared singleton.
  private PythonRuntime _runtime = null!;
#pragma warning restore NUnit1032

  [SetUp]
  public void SetUp()
  {
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

    var options = PythonTestHelper.CreateDefaultOptions();

    services.AddSingleton(options);

    // Use shared PythonRuntime singleton from fixture
    services.AddSingleton(PythonTestFixture.SharedRuntime);
    services.AddSingleton<IPythonExecutor, PythonNetExecutor>();

    _serviceProvider = services.BuildServiceProvider();
    _executor = _serviceProvider.GetRequiredService<IPythonExecutor>();
    _runtime = _serviceProvider.GetRequiredService<PythonRuntime>();
  }

  [TearDown]
  public void TearDown()
  {
    // Do NOT dispose _runtime — it's the shared singleton
    if (_serviceProvider is IDisposable disposable)
    {
      disposable.Dispose();
    }
  }

  [Test]
  public void PythonNodeWrapper_TrainModel_ExecutesSuccessfully()
  {
    // Arrange
    var wrapper = new PythonNodeWrapper<ModelConfigSchema, ModelResultSchema>(
      _executor,
      "scalar_nodes",
      "train_model"
    );

    var transform = wrapper.GetTransform();

    var inputConfig = new ModelConfigSchema
    {
      LearningRate = 0.01,
      Iterations = 100,
      ModelName = "TestModel",
    };

    // Act
    var result = transform(inputConfig);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(
      result.Accuracy,
      Is.EqualTo(0.01),
      "Accuracy should be 0.01 (0.01 * 100 / 100 = 0.01)"
    );
    Assert.That(result.Loss, Is.EqualTo(0.99).Within(0.001), "Loss should be 0.99");
    Assert.That(result.Success, Is.False, "Success should be false (accuracy < 0.5)");
    Assert.That(result.Message, Is.EqualTo("Training completed for TestModel"));
  }

  [Test]
  public void PythonNodeWrapper_Identity_PreservesData()
  {
    // Arrange
    var wrapper = new PythonNodeWrapper<ModelConfigSchema, ModelConfigSchema>(
      _executor,
      "scalar_nodes",
      "identity"
    );

    var transform = wrapper.GetTransform();

    var inputConfig = new ModelConfigSchema
    {
      LearningRate = 0.05,
      Iterations = 50,
      ModelName = "IdentityTest",
    };

    // Act
    var result = transform(inputConfig);

    // Assert
    Assert.That(result.LearningRate, Is.EqualTo(inputConfig.LearningRate));
    Assert.That(result.Iterations, Is.EqualTo(inputConfig.Iterations));
    Assert.That(result.ModelName, Is.EqualTo(inputConfig.ModelName));
  }

  [Test]
  public void PythonNodeWrapper_DoubleIterations_ModifiesCorrectly()
  {
    // Arrange
    var wrapper = new PythonNodeWrapper<ModelConfigSchema, ModelConfigSchema>(
      _executor,
      "scalar_nodes",
      "double_iterations"
    );

    var transform = wrapper.GetTransform();

    var inputConfig = new ModelConfigSchema
    {
      LearningRate = 0.02,
      Iterations = 25,
      ModelName = "DoubleTest",
    };

    // Act
    var result = transform(inputConfig);

    // Assert
    Assert.That(result.Iterations, Is.EqualTo(50), "Iterations should be doubled");
    Assert.That(result.LearningRate, Is.EqualTo(inputConfig.LearningRate));
    Assert.That(result.ModelName, Is.EqualTo(inputConfig.ModelName));
  }

  [Test]
  public void PythonNodeWrapper_CanBeChained_WorksAsFunc()
  {
    // Arrange: Create two wrappers and chain them
    var doubleWrapper = new PythonNodeWrapper<ModelConfigSchema, ModelConfigSchema>(
      _executor,
      "scalar_nodes",
      "double_iterations"
    );

    var trainWrapper = new PythonNodeWrapper<ModelConfigSchema, ModelResultSchema>(
      _executor,
      "scalar_nodes",
      "train_model"
    );

    var doubleTransform = doubleWrapper.GetTransform();
    var trainTransform = trainWrapper.GetTransform();

    var initialConfig = new ModelConfigSchema
    {
      LearningRate = 0.1,
      Iterations = 10,
      ModelName = "Chained",
    };

    // Act: Apply transformations in sequence
    var configAfterDouble = doubleTransform(initialConfig);
    var finalResult = trainTransform(configAfterDouble);

    // Assert
    Assert.That(configAfterDouble.Iterations, Is.EqualTo(20), "Iterations should be doubled");
    Assert.That(
      finalResult.Accuracy,
      Is.EqualTo(0.02),
      "Accuracy should reflect doubled iterations (0.1 * 20 / 100 = 0.02)"
    );
    Assert.That(finalResult.Success, Is.False, "Success should be false (accuracy < 0.5)");
    Assert.That(finalResult.Message, Contains.Substring("Chained"));
  }

  [Test]
  public void PythonNodeWrapper_DictWithSerializedLabel_MapsCorrectly()
  {
    // Arrange - Test dict → singleton with SerializedLabel attributes
    var wrapper = new PythonNodeWrapper<(int, int), MetricsReportSchema>(
      _executor,
      "scalar_nodes",
      "calculate_metrics"
    );

    var transform = wrapper.GetTransform();

    // Act - Python returns dict with snake_case keys (accuracy, correct_predictions, total_samples)
    var result = transform((30, 30));

    // Assert - Values should be correctly mapped to C# properties despite different casing
    Assert.That(result.Accuracy, Is.EqualTo(1.0).Within(0.0001), "Accuracy should be 1.0 (100%)");
    Assert.That(
      result.CorrectPredictions,
      Is.EqualTo(30),
      "CorrectPredictions should be 30 (from 'correct_predictions')"
    );
    Assert.That(
      result.TotalSamples,
      Is.EqualTo(30),
      "TotalSamples should be 30 (from 'total_samples')"
    );
  }

  [Test]
  public void PythonNodeWrapper_DictWithSerializedLabel_PartialMatch()
  {
    // Arrange - Test with non-100% accuracy
    var wrapper = new PythonNodeWrapper<(int, int), MetricsReportSchema>(
      _executor,
      "scalar_nodes",
      "calculate_metrics"
    );

    var transform = wrapper.GetTransform();

    // Act - 19 out of 20 correct
    var result = transform((19, 20));

    // Assert
    Assert.That(result.Accuracy, Is.EqualTo(0.95).Within(0.0001), "Accuracy should be 0.95");
    Assert.That(result.CorrectPredictions, Is.EqualTo(19));
    Assert.That(result.TotalSamples, Is.EqualTo(20));
  }
}
