using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Marshalling;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Tests.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using PythonEngineRuntime = Python.Runtime.Runtime;

namespace Flowthru.Extensions.Python.Tests.Surface;

/// <summary>
/// Unit tests for ScalarMarshaller - bidirectional C# ↔ Python conversion.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Surface")]
public class ScalarMarshallerTests
{
  private IServiceProvider _serviceProvider = null!;

#pragma warning disable NUnit1032 // The field type is not disposed. Suppressed because _runtime is a reference to the shared singleton.
  private PythonRuntime _runtime = null!;
#pragma warning restore NUnit1032

  [SetUp]
  public void SetUp()
  {
    var services = new ServiceCollection();
    services.AddLogging();

    var options = PythonTestHelper.CreateDefaultOptions();
    services.AddSingleton(options);

    // Use shared PythonRuntime singleton from fixture
    services.AddSingleton(PythonTestFixture.SharedRuntime);

    _serviceProvider = services.BuildServiceProvider();
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
  public void ToPython_Int_ConvertsCorrectly()
  {
    using (_runtime.AcquireGil())
    {
      var result = ScalarMarshaller.ToPython(42);
      Assert.That(result.As<int>(), Is.EqualTo(42));
    }
  }

  [Test]
  public void ToPython_Double_ConvertsCorrectly()
  {
    using (_runtime.AcquireGil())
    {
      var result = ScalarMarshaller.ToPython(3.14);
      Assert.That(result.As<double>(), Is.EqualTo(3.14).Within(0.0001));
    }
  }

  [Test]
  public void ToPython_String_ConvertsCorrectly()
  {
    using (_runtime.AcquireGil())
    {
      var result = ScalarMarshaller.ToPython("Hello, Python!");
      Assert.That(result.As<string>(), Is.EqualTo("Hello, Python!"));
    }
  }

  [Test]
  public void ToPython_Bool_ConvertsCorrectly()
  {
    using (_runtime.AcquireGil())
    {
      var resultTrue = ScalarMarshaller.ToPython(true);
      var resultFalse = ScalarMarshaller.ToPython(false);

      Assert.That(resultTrue.As<bool>(), Is.True);
      Assert.That(resultFalse.As<bool>(), Is.False);
    }
  }

  [Test]
  public void ToPython_Null_ReturnsNone()
  {
    using (_runtime.AcquireGil())
    {
      var result = ScalarMarshaller.ToPython(null);
      Assert.That(result.IsNone(), Is.True);
    }
  }

  [Test]
  public void ToPython_Record_ConvertsToDictionary()
  {
    using (_runtime.AcquireGil())
    {
      var config = new ModelConfigSchema
      {
        LearningRate = 0.01,
        Iterations = 100,
        ModelName = "Test",
      };

      var result = ScalarMarshaller.ToPython(config);
      var dict = new PyDict(result);

      // Verify it's a dict with expected keys and values
      Assert.That(dict.HasKey("LearningRate".ToPython()), Is.True);
      Assert.That(dict.HasKey("Iterations".ToPython()), Is.True);
      Assert.That(dict.HasKey("ModelName".ToPython()), Is.True);

      Assert.That(
        dict.GetItem("LearningRate".ToPython()).As<double>(),
        Is.EqualTo(0.01).Within(0.0001)
      );
      Assert.That(dict.GetItem("Iterations".ToPython()).As<int>(), Is.EqualTo(100));
      Assert.That(dict.GetItem("ModelName".ToPython()).As<string>(), Is.EqualTo("Test"));
    }
  }

  [Test]
  public void FromPython_Int_ConvertsCorrectly()
  {
    using (_runtime.AcquireGil())
    {
      var pyValue = 42.ToPython();
      var result = ScalarMarshaller.FromPython<int>(pyValue);
      Assert.That(result, Is.EqualTo(42));
    }
  }

  [Test]
  public void FromPython_Double_ConvertsCorrectly()
  {
    using (_runtime.AcquireGil())
    {
      var pyValue = 3.14.ToPython();
      var result = ScalarMarshaller.FromPython<double>(pyValue);
      Assert.That(result, Is.EqualTo(3.14).Within(0.0001));
    }
  }

  [Test]
  public void FromPython_String_ConvertsCorrectly()
  {
    using (_runtime.AcquireGil())
    {
      var pyValue = "Hello, C#!".ToPython();
      var result = ScalarMarshaller.FromPython<string>(pyValue);
      Assert.That(result, Is.EqualTo("Hello, C#!"));
    }
  }

  [Test]
  public void FromPython_Bool_ConvertsCorrectly()
  {
    using (_runtime.AcquireGil())
    {
      var pyTrue = true.ToPython();
      var pyFalse = false.ToPython();

      Assert.That(ScalarMarshaller.FromPython<bool>(pyTrue), Is.True);
      Assert.That(ScalarMarshaller.FromPython<bool>(pyFalse), Is.False);
    }
  }

  [Test]
  public void FromPython_None_ReturnsNull()
  {
    using (_runtime.AcquireGil())
    {
      var result = ScalarMarshaller.FromPython<string?>(PythonEngineRuntime.None);
      Assert.That(result, Is.Null);
    }
  }

  [Test]
  public void FromPython_Dictionary_ConvertsToRecord()
  {
    using (_runtime.AcquireGil())
    {
      // Create Python dict
      var dict = new PyDict();
      dict.SetItem("LearningRate".ToPython(), 0.05.ToPython());
      dict.SetItem("Iterations".ToPython(), 50.ToPython());
      dict.SetItem("ModelName".ToPython(), "FromPython".ToPython());

      var result = ScalarMarshaller.FromPython<ModelConfigSchema>(dict);

      Assert.That(result.LearningRate, Is.EqualTo(0.05).Within(0.0001));
      Assert.That(result.Iterations, Is.EqualTo(50));
      Assert.That(result.ModelName, Is.EqualTo("FromPython"));
    }
  }

  [Test]
  public void RoundTrip_Record_PreservesData()
  {
    using (_runtime.AcquireGil())
    {
      var original = new ModelConfigSchema
      {
        LearningRate = 0.02,
        Iterations = 200,
        ModelName = "RoundTrip",
      };

      var pyValue = ScalarMarshaller.ToPython(original);
      var result = ScalarMarshaller.FromPython<ModelConfigSchema>(pyValue);

      Assert.That(result.LearningRate, Is.EqualTo(original.LearningRate).Within(0.0001));
      Assert.That(result.Iterations, Is.EqualTo(original.Iterations));
      Assert.That(result.ModelName, Is.EqualTo(original.ModelName));
    }
  }

  // Array marshalling tests (Phase 5)

  [Test]
  public void ToPython_IntArray_ConvertsToList()
  {
    using (_runtime.AcquireGil())
    {
      var array = new[] { 1, 2, 3, 4, 5 };
      var result = ScalarMarshaller.ToPython(array);
      var pyList = new PyList(result);

      Assert.That(pyList.Length(), Is.EqualTo(5));
      Assert.That(pyList.GetItem(0).As<int>(), Is.EqualTo(1));
      Assert.That(pyList.GetItem(4).As<int>(), Is.EqualTo(5));
    }
  }

  [Test]
  public void ToPython_DoubleArray_ConvertsToList()
  {
    using (_runtime.AcquireGil())
    {
      var array = new[] { 1.1, 2.2, 3.3 };
      var result = ScalarMarshaller.ToPython(array);
      var pyList = new PyList(result);

      Assert.That(pyList.Length(), Is.EqualTo(3));
      Assert.That(pyList.GetItem(0).As<double>(), Is.EqualTo(1.1).Within(0.0001));
      Assert.That(pyList.GetItem(2).As<double>(), Is.EqualTo(3.3).Within(0.0001));
    }
  }

  [Test]
  public void ToPython_StringArray_ConvertsToList()
  {
    using (_runtime.AcquireGil())
    {
      var array = new[] { "alpha", "beta", "gamma" };
      var result = ScalarMarshaller.ToPython(array);
      var pyList = new PyList(result);

      Assert.That(pyList.Length(), Is.EqualTo(3));
      Assert.That(pyList.GetItem(0).As<string>(), Is.EqualTo("alpha"));
      Assert.That(pyList.GetItem(2).As<string>(), Is.EqualTo("gamma"));
    }
  }

  [Test]
  public void FromPython_List_ConvertsToIntArray()
  {
    using (_runtime.AcquireGil())
    {
      var pyList = new PyList();
      pyList.Append(10.ToPython());
      pyList.Append(20.ToPython());
      pyList.Append(30.ToPython());

      var result = ScalarMarshaller.FromPython<int[]>(pyList);

      Assert.That(result, Is.EqualTo(new[] { 10, 20, 30 }));
    }
  }

  [Test]
  public void FromPython_List_ConvertsToDoubleArray()
  {
    using (_runtime.AcquireGil())
    {
      var pyList = new PyList();
      pyList.Append(1.5.ToPython());
      pyList.Append(2.5.ToPython());

      var result = ScalarMarshaller.FromPython<double[]>(pyList);

      Assert.That(result.Length, Is.EqualTo(2));
      Assert.That(result[0], Is.EqualTo(1.5).Within(0.0001));
      Assert.That(result[1], Is.EqualTo(2.5).Within(0.0001));
    }
  }

  [Test]
  public void FromPython_List_ConvertsToStringArray()
  {
    using (_runtime.AcquireGil())
    {
      var pyList = new PyList();
      pyList.Append("first".ToPython());
      pyList.Append("second".ToPython());

      var result = ScalarMarshaller.FromPython<string[]>(pyList);

      Assert.That(result, Is.EqualTo(new[] { "first", "second" }));
    }
  }

  [Test]
  public void RoundTrip_IntArray_PreservesData()
  {
    using (_runtime.AcquireGil())
    {
      var original = new[] { 100, 200, 300 };
      var pyValue = ScalarMarshaller.ToPython(original);
      var result = ScalarMarshaller.FromPython<int[]>(pyValue);

      Assert.That(result, Is.EqualTo(original));
    }
  }

  [Test]
  public void RoundTrip_RecordWithArrays_PreservesData()
  {
    using (_runtime.AcquireGil())
    {
      var original = new ModelWithArraysSchema
      {
        Coefficients = new[] { 1.1, 2.2, 3.3 },
        Intercept = 42.0,
        FeatureNames = new[] { "feature1", "feature2", "feature3" },
      };

      var pyValue = ScalarMarshaller.ToPython(original);
      var result = ScalarMarshaller.FromPython<ModelWithArraysSchema>(pyValue);

      Assert.That(result.Coefficients, Is.EqualTo(original.Coefficients));
      Assert.That(result.Intercept, Is.EqualTo(original.Intercept).Within(0.0001));
      Assert.That(result.FeatureNames, Is.EqualTo(original.FeatureNames));
    }
  }

  [Test]
  public void FromPython_DictWithArrays_ConvertsToRecord()
  {
    using (_runtime.AcquireGil())
    {
      // Simulate Python returning a dict with list values (like scikit-learn model)
      var dict = new PyDict();

      var coeffList = new PyList();
      coeffList.Append(0.5.ToPython());
      coeffList.Append(1.5.ToPython());
      dict.SetItem("Coefficients".ToPython(), coeffList);

      dict.SetItem("Intercept".ToPython(), 10.0.ToPython());

      var featureList = new PyList();
      featureList.Append("x".ToPython());
      featureList.Append("y".ToPython());
      dict.SetItem("FeatureNames".ToPython(), featureList);

      var result = ScalarMarshaller.FromPython<ModelWithArraysSchema>(dict);

      Assert.That(result.Coefficients, Is.EqualTo(new[] { 0.5, 1.5 }));
      Assert.That(result.Intercept, Is.EqualTo(10.0).Within(0.0001));
      Assert.That(result.FeatureNames, Is.EqualTo(new[] { "x", "y" }));
    }
  }

  // SerializedLabel tests

  [Test]
  public void ToPython_RecordWithSerializedLabel_UsesExternalFieldNames()
  {
    using (_runtime.AcquireGil())
    {
      var metrics = new MetricsReportSchema
      {
        Accuracy = 0.95,
        CorrectPredictions = 19,
        TotalSamples = 20,
      };

      var result = ScalarMarshaller.ToPython(metrics);
      var dict = new PyDict(result);

      // Assert - dictionary should have snake_case keys from SerializedLabel, not PascalCase property names
      Assert.That(dict.HasKey("accuracy".ToPython()), Is.True, "Expected 'accuracy' key");
      Assert.That(
        dict.HasKey("correct_predictions".ToPython()),
        Is.True,
        "Expected 'correct_predictions' key"
      );
      Assert.That(dict.HasKey("total_samples".ToPython()), Is.True, "Expected 'total_samples' key");

      // Should NOT have PascalCase keys
      Assert.That(dict.HasKey("Accuracy".ToPython()), Is.False);
      Assert.That(dict.HasKey("CorrectPredictions".ToPython()), Is.False);
      Assert.That(dict.HasKey("TotalSamples".ToPython()), Is.False);

      // Verify values
      Assert.That(
        dict.GetItem("accuracy".ToPython()).As<double>(),
        Is.EqualTo(0.95).Within(0.0001)
      );
      Assert.That(dict.GetItem("correct_predictions".ToPython()).As<int>(), Is.EqualTo(19));
      Assert.That(dict.GetItem("total_samples".ToPython()).As<int>(), Is.EqualTo(20));
    }
  }

  [Test]
  public void FromPython_DictWithExternalFieldNames_MapsToProperties()
  {
    using (_runtime.AcquireGil())
    {
      // Create Python dict with snake_case keys (as Python would return)
      var dict = new PyDict();
      dict.SetItem("accuracy".ToPython(), 1.0.ToPython());
      dict.SetItem("correct_predictions".ToPython(), 30.ToPython());
      dict.SetItem("total_samples".ToPython(), 30.ToPython());

      var result = ScalarMarshaller.FromPython<MetricsReportSchema>(dict);

      // Assert - values should be correctly mapped to PascalCase properties
      Assert.That(result.Accuracy, Is.EqualTo(1.0).Within(0.0001));
      Assert.That(result.CorrectPredictions, Is.EqualTo(30));
      Assert.That(result.TotalSamples, Is.EqualTo(30));
    }
  }

  [Test]
  public void FromPython_DictWithMixedCasing_MapsCorrectly()
  {
    using (_runtime.AcquireGil())
    {
      // Test case-insensitive mapping (Python dict with various casings)
      var dict = new PyDict();
      dict.SetItem("ACCURACY".ToPython(), 0.75.ToPython()); // all uppercase
      dict.SetItem("Correct_Predictions".ToPython(), 15.ToPython()); // mixed case
      dict.SetItem("total_samples".ToPython(), 20.ToPython()); // lowercase

      var result = ScalarMarshaller.FromPython<MetricsReportSchema>(dict);

      // All should map correctly thanks to case-insensitive lookup
      Assert.That(result.Accuracy, Is.EqualTo(0.75).Within(0.0001));
      Assert.That(result.CorrectPredictions, Is.EqualTo(15));
      Assert.That(result.TotalSamples, Is.EqualTo(20));
    }
  }

  [Test]
  public void RoundTrip_RecordWithSerializedLabel_PreservesData()
  {
    using (_runtime.AcquireGil())
    {
      var original = new MetricsReportSchema
      {
        Accuracy = 0.88,
        CorrectPredictions = 88,
        TotalSamples = 100,
      };

      // C# → Python (should use snake_case keys)
      var pyValue = ScalarMarshaller.ToPython(original);

      // Python → C# (should map snake_case keys back to properties)
      var result = ScalarMarshaller.FromPython<MetricsReportSchema>(pyValue);

      Assert.That(result.Accuracy, Is.EqualTo(original.Accuracy).Within(0.0001));
      Assert.That(result.CorrectPredictions, Is.EqualTo(original.CorrectPredictions));
      Assert.That(result.TotalSamples, Is.EqualTo(original.TotalSamples));
    }
  }
}
