using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Steps;
using Flowthru.Extensions.Python.Tests.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Python.Tests.Integration;

/// <summary>
/// Integration tests for tabular (DataFrame) Python node execution via Arrow marshalling.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Integration")]
public class TabularNodeTests
{
  private IServiceProvider _serviceProvider = null!;

#pragma warning disable NUnit1032 // The field type is not disposed. Suppressed because _runtime is a reference to the shared singleton.
  private PythonRuntime _runtime = null!;
#pragma warning restore NUnit1032

  private IPythonExecutor _executor = null!;

  [SetUp]
  public void SetUp()
  {
    var services = new ServiceCollection();
    services.AddLogging();

    var options = PythonTestHelper.CreateDefaultOptions();

    services.AddSingleton(options);

    // Use shared PythonRuntime singleton from fixture
    services.AddSingleton(PythonTestFixture.SharedRuntime);
    services.AddSingleton<IPythonExecutor, PythonNetExecutor>();

    _serviceProvider = services.BuildServiceProvider();
    _runtime = _serviceProvider.GetRequiredService<PythonRuntime>();
    _executor = _serviceProvider.GetRequiredService<IPythonExecutor>();
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
  public void PythonNode_Passthrough_RoundTripsDataIntact()
  {
    // Arrange
    var inputData = new[]
    {
      new SimpleRowSchema
      {
        Id = 1,
        Name = "Alice",
        Value = 10.5,
      },
      new SimpleRowSchema
      {
        Id = 2,
        Name = "Bob",
        Value = 20.3,
      },
      new SimpleRowSchema
      {
        Id = 3,
        Name = "Charlie",
        Value = 30.7,
      },
    };

    var wrapper = new PythonNodeWrapper<IEnumerable<SimpleRowSchema>, IEnumerable<SimpleRowSchema>>(
      _executor,
      "_Fixtures.tabular_nodes",
      "passthrough"
    );

    var transform = wrapper.GetTransform();

    // Act
    var result = transform(inputData).ToList();

    // Assert
    Assert.That(result.Count, Is.EqualTo(3));

    Assert.That(result[0].Id, Is.EqualTo(1));
    Assert.That(result[0].Name, Is.EqualTo("Alice"));
    Assert.That(result[0].Value, Is.EqualTo(10.5).Within(0.0001));

    Assert.That(result[1].Id, Is.EqualTo(2));
    Assert.That(result[1].Name, Is.EqualTo("Bob"));
    Assert.That(result[1].Value, Is.EqualTo(20.3).Within(0.0001));

    Assert.That(result[2].Id, Is.EqualTo(3));
    Assert.That(result[2].Name, Is.EqualTo("Charlie"));
    Assert.That(result[2].Value, Is.EqualTo(30.7).Within(0.0001));
  }

  [Test]
  public void PythonNode_FilterRows_AppliesTransformation()
  {
    // Arrange
    var inputData = new[]
    {
      new SimpleRowSchema
      {
        Id = 1,
        Name = "Alice",
        Value = 25.0,
      },
      new SimpleRowSchema
      {
        Id = 2,
        Name = "Bob",
        Value = 75.0,
      },
      new SimpleRowSchema
      {
        Id = 3,
        Name = "Charlie",
        Value = 100.0,
      },
      new SimpleRowSchema
      {
        Id = 4,
        Name = "Dave",
        Value = 40.0,
      },
    };

    var wrapper = new PythonNodeWrapper<IEnumerable<SimpleRowSchema>, IEnumerable<SimpleRowSchema>>(
      _executor,
      "_Fixtures.tabular_nodes",
      "filter_rows"
    );

    var transform = wrapper.GetTransform();

    // Act
    var result = transform(inputData).ToList();

    // Assert - only rows where value > 50
    Assert.That(result.Count, Is.EqualTo(2));
    Assert.That(result[0].Name, Is.EqualTo("Bob"));
    Assert.That(result[0].Value, Is.EqualTo(75.0).Within(0.0001));
    Assert.That(result[1].Name, Is.EqualTo("Charlie"));
    Assert.That(result[1].Value, Is.EqualTo(100.0).Within(0.0001));
  }

  [Test]
  public void PythonNode_EmptyInput_ReturnsEmptyOutput()
  {
    // Arrange
    var inputData = Array.Empty<SimpleRowSchema>();

    var wrapper = new PythonNodeWrapper<IEnumerable<SimpleRowSchema>, IEnumerable<SimpleRowSchema>>(
      _executor,
      "_Fixtures.tabular_nodes",
      "passthrough"
    );

    var transform = wrapper.GetTransform();

    // Act
    var result = transform(inputData).ToList();

    // Assert
    Assert.That(result, Is.Empty);
  }

  [Test]
  public void PythonNode_ExtendedTypes_PreservesAllTypes()
  {
    // Arrange
    var testGuid = Guid.NewGuid();
    var testDateTime = new DateTime(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);
    var testOffset = new DateTimeOffset(2026, 3, 6, 15, 30, 0, TimeSpan.FromHours(3));
    var testDuration = TimeSpan.FromMinutes(45);

    var inputData = new[]
    {
      new ExtendedTypesSchema
      {
        Id = testGuid,
        CreatedAt = testDateTime,
        ModifiedAt = testOffset,
        Duration = testDuration,
        Name = "Test Item",
      },
    };

    var wrapper = new PythonNodeWrapper<
      IEnumerable<ExtendedTypesSchema>,
      IEnumerable<ExtendedTypesSchema>
    >(_executor, "_Fixtures.tabular_nodes", "passthrough");

    var transform = wrapper.GetTransform();

    // Act
    var result = transform(inputData).ToList();

    // Assert
    Assert.That(result.Count, Is.EqualTo(1));
    Assert.That(result[0].Id, Is.EqualTo(testGuid));
    Assert.That(result[0].CreatedAt, Is.EqualTo(testDateTime));
    Assert.That(result[0].ModifiedAt?.UtcDateTime, Is.EqualTo(testOffset.UtcDateTime));
    Assert.That(result[0].Duration, Is.EqualTo(testDuration));
    Assert.That(result[0].Name, Is.EqualTo("Test Item"));
  }

  [Test]
  public void PythonNode_NullableFields_PreservesNulls()
  {
    // Arrange
    var inputData = new[]
    {
      new ExtendedTypesSchema
      {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        ModifiedAt = null, // nullable
        Duration = null, // nullable
        Name = null, // nullable
      },
    };

    var wrapper = new PythonNodeWrapper<
      IEnumerable<ExtendedTypesSchema>,
      IEnumerable<ExtendedTypesSchema>
    >(_executor, "_Fixtures.tabular_nodes", "passthrough");

    var transform = wrapper.GetTransform();

    // Act
    var result = transform(inputData).ToList();

    // Assert
    Assert.That(result[0].ModifiedAt, Is.Null);
    Assert.That(result[0].Duration, Is.Null);
    Assert.That(result[0].Name, Is.Null);
  }

  [Test]
  public void PythonNode_LargeDataset_HandlesEfficiently()
  {
    // Arrange - 1000 rows
    var inputData = Enumerable
      .Range(1, 1000)
      .Select(i => new SimpleRowSchema
      {
        Id = i,
        Name = $"Row{i}",
        Value = i * 1.5,
      })
      .ToArray();

    var wrapper = new PythonNodeWrapper<IEnumerable<SimpleRowSchema>, IEnumerable<SimpleRowSchema>>(
      _executor,
      "_Fixtures.tabular_nodes",
      "passthrough"
    );

    var transform = wrapper.GetTransform();

    // Act
    var result = transform(inputData).ToList();

    // Assert
    Assert.That(result.Count, Is.EqualTo(1000));
    Assert.That(result[0].Id, Is.EqualTo(1));
    Assert.That(result[999].Id, Is.EqualTo(1000));
  }

  [Test]
  public void PythonNode_SerializedLabelRespected_FieldNamesCorrect()
  {
    // Arrange - SimpleRowSchema uses SerializedLabel attributes
    var inputData = new[]
    {
      new SimpleRowSchema
      {
        Id = 1,
        Name = "Test",
        Value = 42.0,
      },
    };

    var wrapper = new PythonNodeWrapper<IEnumerable<SimpleRowSchema>, IEnumerable<SimpleRowSchema>>(
      _executor,
      "_Fixtures.tabular_nodes",
      "passthrough"
    );

    var transform = wrapper.GetTransform();

    // Act - Python sees field names as "id", "name", "value" (not "Id", "Name", "Value")
    var result = transform(inputData).ToList();

    // Assert - data integrity maintained through field name mapping
    Assert.That(result[0].Id, Is.EqualTo(1));
    Assert.That(result[0].Name, Is.EqualTo("Test"));
    Assert.That(result[0].Value, Is.EqualTo(42.0).Within(0.0001));
  }
}
