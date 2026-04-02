using Flowthru.Data.Validation;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Steps;
using Flowthru.Extensions.Python.Tests.Schemas;
using Flowthru.Extensions.Python.Validation;
using Flowthru.Flows;
using Flowthru.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Python.Tests.Validation;

/// <summary>
/// Tests for Phase 4 Python node validation.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Validation")]
public class PythonNodeValidationTests
{
  private IServiceProvider _serviceProvider = null!;
  private IPythonExecutor _executor = null!;

#pragma warning disable NUnit1032 // The field type is not disposed. Suppressed because _runtime is a reference to the shared singleton.
  private PythonRuntime _runtime = null!;
#pragma warning restore NUnit1032

  private PythonStepValidator _validator = null!;

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

    _validator = new PythonStepValidator(_executor, _runtime);
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

  // ──────────────────────────────────────────────────────────────────────
  // Registration-time validation tests
  // ──────────────────────────────────────────────────────────────────────

  [Test]
  public void ValidateRegistration_MissingModule_ThrowsException()
  {
    // Arrange & Act & Assert
    var ex = Assert.Throws<InvalidOperationException>(
      () =>
        new PythonNodeWrapper<ModelConfigSchema, ModelResultSchema>(
          _executor,
          "_Fixtures.nonexistent_module",
          "some_function"
        )
    );

    Assert.That(ex.Message, Does.Contain("Module '_Fixtures.nonexistent_module' not found"));
  }

  [Test]
  public void ValidateRegistration_MissingFunction_ThrowsException()
  {
    // Arrange & Act & Assert
    var ex = Assert.Throws<InvalidOperationException>(
      () =>
        new PythonNodeWrapper<ModelConfigSchema, ModelResultSchema>(
          _executor,
          "_Fixtures.validation_test_nodes",
          "nonexistent_function"
        )
    );

    Assert.That(ex.Message, Does.Contain("Function 'nonexistent_function' not found in module"));
  }

  [Test]
  public void ValidateRegistration_MissingDecorator_ThrowsException()
  {
    // Arrange & Act & Assert
    var ex = Assert.Throws<InvalidOperationException>(
      () =>
        new PythonNodeWrapper<ModelConfigSchema, ModelConfigSchema>(
          _executor,
          "_Fixtures.validation_test_nodes",
          "missing_decorator_node"
        )
    );

    Assert.That(ex.Message, Does.Contain("missing required @node decorator"));
  }

  [Test]
  public void ValidateRegistration_ValidNode_Succeeds()
  {
    // Arrange & Act
    var wrapper = new PythonNodeWrapper<ModelConfigSchema, ModelResultSchema>(
      _executor,
      "_Fixtures.validation_test_nodes",
      "valid_node"
    );

    // Assert
    Assert.That(wrapper, Is.Not.Null);
    Assert.That(wrapper.GetTransform(), Is.Not.Null);
  }

  // ──────────────────────────────────────────────────────────────────────
  // Pre-flight validation tests
  // ──────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ValidateAsync_WrongInputSchema_ReportsError()
  {
    // Arrange
    var pipeline = CreateTestPipeline("_Fixtures.validation_test_nodes", "wrong_input_schema");

    // Act
    var result = await _validator.ValidateAsync(pipeline, CancellationToken.None);

    // Assert
    Assert.That(result.HasErrors, Is.True);
    Assert.That(result.Errors.Count, Is.EqualTo(1));
    Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(
      result.Errors[0].Message,
      Does.Contain("Input schema mismatch")
        .And.Contain("ModelConfigSchema")
        .And.Contain("WrongInputSchema")
    );
  }

  [Test]
  public async Task ValidateAsync_WrongOutputSchema_ReportsError()
  {
    // Arrange
    var pipeline = CreateTestPipeline("_Fixtures.validation_test_nodes", "wrong_output_schema");

    // Act
    var result = await _validator.ValidateAsync(pipeline, CancellationToken.None);

    // Assert
    Assert.That(result.HasErrors, Is.True);
    Assert.That(result.Errors.Count, Is.EqualTo(1));
    Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(
      result.Errors[0].Message,
      Does.Contain("Output schema mismatch")
        .And.Contain("ModelResultSchema")
        .And.Contain("WrongOutputSchema")
    );
  }

  [Test]
  public async Task ValidateAsync_TooManyInputs_ReportsError()
  {
    // Arrange
    var pipeline = CreateTestPipeline("_Fixtures.validation_test_nodes", "too_many_inputs");

    // Act
    var result = await _validator.ValidateAsync(pipeline, CancellationToken.None);

    // Assert
    Assert.That(result.HasErrors, Is.True);
    Assert.That(result.Errors.Count, Is.EqualTo(1));
    Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(
      result.Errors[0].Message,
      Does.Contain("Input schema count mismatch")
        .And.Contain("expects 1 input")
        .And.Contain("declares 2")
    );
  }

  [Test]
  public async Task ValidateAsync_TooManyOutputs_ReportsError()
  {
    // Arrange
    var pipeline = CreateTestPipeline("_Fixtures.validation_test_nodes", "too_many_outputs");

    // Act
    var result = await _validator.ValidateAsync(pipeline, CancellationToken.None);

    // Assert
    Assert.That(result.HasErrors, Is.True);
    Assert.That(result.Errors.Count, Is.EqualTo(1));
    Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(
      result.Errors[0].Message,
      Does.Contain("Output schema count mismatch")
        .And.Contain("expects 1 output")
        .And.Contain("declares 2")
    );
  }

  [Test]
  public async Task ValidateAsync_ZeroInputs_ReportsError()
  {
    // Arrange
    var pipeline = CreateTestPipeline("_Fixtures.validation_test_nodes", "zero_inputs");

    // Act
    var result = await _validator.ValidateAsync(pipeline, CancellationToken.None);

    // Assert
    Assert.That(result.HasErrors, Is.True);
    Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(
      result.Errors[0].Message,
      Does.Contain("Input schema count mismatch").And.Contain("declares 0")
    );
  }

  [Test]
  public async Task ValidateAsync_ZeroOutputs_ReportsError()
  {
    // Arrange
    var pipeline = CreateTestPipeline("_Fixtures.validation_test_nodes", "zero_outputs");

    // Act
    var result = await _validator.ValidateAsync(pipeline, CancellationToken.None);

    // Assert
    Assert.That(result.HasErrors, Is.True);
    Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(
      result.Errors[0].Message,
      Does.Contain("Output schema count mismatch").And.Contain("declares 0")
    );
  }

  [Test]
  public async Task ValidateAsync_ValidNode_NoErrors()
  {
    // Arrange
    var pipeline = CreateTestPipeline("_Fixtures.validation_test_nodes", "valid_node");

    // Act
    var result = await _validator.ValidateAsync(pipeline, CancellationToken.None);

    // Assert
    Assert.That(result.IsValid, Is.True);
    Assert.That(result.Errors.Count, Is.EqualTo(0));
  }

  [Test]
  public async Task ValidateAsync_NonPythonNode_Skipped()
  {
    // Arrange - empty pipeline with no Python nodes
    var pipeline = new Flow();

    // Act
    var result = await _validator.ValidateAsync(pipeline, CancellationToken.None);

    // Assert - should pass because there are no Python nodes to validate
    Assert.That(result.IsValid, Is.True);
  }

  // ──────────────────────────────────────────────────────────────────────
  // Helper methods
  // ──────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Creates a minimal test pipeline with a single Python node.
  /// </summary>
  private Flow CreateTestPipeline(string moduleName, string functionName)
  {
    var pipeline = new Flow();

    // Create Python node wrapper
    var wrapper = new PythonNodeWrapper<ModelConfigSchema, ModelResultSchema>(
      _executor,
      moduleName,
      functionName
    );

    // Create a PipelineNode using the public constructor
    var pipelineNode = new Flowthru.Flows.FlowStep(
      label: "python_node",
      description: $"Test Python node: {moduleName}.{functionName}",
      step: wrapper.GetTransform(),
      inputs: Array.Empty<Data.IItem>(),
      outputs: Array.Empty<Data.IItem>()
    );

    // Use reflection to access internal AddNode method for testing
    var addNodeMethod = typeof(Flow).GetMethod(
      "AddNode",
      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
    );

    if (addNodeMethod == null)
    {
      throw new InvalidOperationException("Could not find Pipeline.AddNode method via reflection");
    }

    addNodeMethod.Invoke(pipeline, new object[] { pipelineNode });

    return pipeline;
  }
}
