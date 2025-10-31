using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace Flowthru.Tests.ML.Ext.Samples.MulticlassClassification_Iris;

/// <summary>
/// Compilation tests for multiclass classification scenarios, demonstrating ML.Ext's
/// compile-time safety for multi-stage pipelines with schema transformations.
/// </summary>
[TestFixture]
[Category("Compilation")]
[Category("TypeSafety")]
public class MulticlassClassificationIrisCompilationTests {
  [Test]
  public void Three_Step_Pipeline_With_Schema_Break_Should_Not_Compile() {
    // In multiclass classification, we have: Original -> LabelKey -> Features -> Classified
    // This test verifies that breaking the schema chain fails at compile-time
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;

      public interface IOriginal : ISchemaDefinition { }
      public interface ILabelKey : ISchemaDefinition { }
      public interface IFeatures : ISchemaDefinition { }
      public interface IClassified : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var labelKeyEstimator = new Estimator<IOriginal, ILabelKey>(null!);
          var featurizeEstimator = new Estimator<ILabelKey, IFeatures>(null!);

          // Intentionally use wrong schema - skipping IFeatures
          var classifierEstimator = new Estimator<IOriginal, IClassified>(null!);

          // This should NOT compile: IFeatures != IOriginal
          var pipeline = labelKeyEstimator
            .Append(featurizeEstimator)
            .Append(classifierEstimator);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Multi-stage pipeline with schema break should not compile");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasTypeMismatch = errors.Any(e =>
        e.Id == "CS1503" || e.Id == "CS0311" ||
        e.ToString().Contains("type argument") ||
        e.ToString().Contains("cannot convert"));
    Assert.That(hasTypeMismatch, Is.True,
        $"Should have type mismatch error. Errors: {string.Join(", ", errors.Select(e => $"{e.Id}: {e.GetMessage()}"))}");
  }

  [Test]
  public void Label_Key_And_Featurize_Schema_Mismatch_Should_Not_Compile() {
    // Tests that the second step in a pipeline must accept the first step's output
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;

      public interface IOriginal : ISchemaDefinition { }
      public interface ILabelKey : ISchemaDefinition { }
      public interface IWrongSchema : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var labelKeyEstimator = new Estimator<IOriginal, ILabelKey>(null!);
          
          // This expects IWrongSchema but labelKeyEstimator produces ILabelKey
          var featurizeEstimator = new Estimator<IWrongSchema, IOriginal>(null!);

          // This should NOT compile
          var pipeline = labelKeyEstimator.Append(featurizeEstimator);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Schema mismatch between pipeline steps should not compile");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    Assert.That(errors, Is.Not.Empty, "Should have compilation errors");
  }

  [Test]
  public void Featurize_And_Classifier_Schema_Mismatch_Should_Not_Compile() {
    // Tests schema mismatch in the final composition step
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;

      public interface ILabelKey : ISchemaDefinition { }
      public interface IFeatures : ISchemaDefinition { }
      public interface IWrongSchema : ISchemaDefinition { }
      public interface IClassified : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var featurizeEstimator = new Estimator<ILabelKey, IFeatures>(null!);
          
          // This expects IWrongSchema but featurizeEstimator produces IFeatures
          var classifierEstimator = new Estimator<IWrongSchema, IClassified>(null!);

          // This should NOT compile
          var pipeline = featurizeEstimator.Append(classifierEstimator);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Schema mismatch in classifier step should not compile");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    Assert.That(errors, Is.Not.Empty, "Should have compilation errors");
  }

  [Test]
  public void Prediction_Engine_Type_Mismatch_Should_Not_Compile() {
    // Verifies that PredictionEngine enforces correct input/output types at call site
    var code = @"
      using Flowthru.ML.Ext.Load;
      using Microsoft.ML;

      public class IrisData {
        public float SepalLength { get; set; }
        public float SepalWidth { get; set; }
        public float PetalLength { get; set; }
        public float PetalWidth { get; set; }
        public string Label { get; set; }
      }

      public class WrongInput {
        public double Value { get; set; }
      }

      public class IrisPrediction {
        public string PredictedLabel { get; set; }
        public float[] Score { get; set; }
      }

      public class Test {
        public void Execute() {
          // Create prediction engine with specific types
          var predictor = new PredictionEngine<IrisData, IrisPrediction>(null!);

          // Try to pass wrong input type through variable with wrong type
          WrongInput wrongInput = new WrongInput();
          IrisData typedInput = wrongInput; // This should NOT compile

          var prediction = predictor.Predict(typedInput);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Cannot assign WrongInput to IrisData");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasConversionError = errors.Any(e =>
        e.Id == "CS0029" || e.Id == "CS0266" || e.Id == "CS1503" ||
        e.ToString().Contains("cannot convert"));
    Assert.That(hasConversionError, Is.True,
        $"Should have type mismatch error. Errors: {string.Join(", ", errors.Select(e => $"{e.Id}: {e.GetMessage()}"))}");
  }

  [Test]
  public void Transformer_Fit_Returns_Correct_Schema_Types() {
    // Verifies that Estimator.Fit() returns Transformer with matching schema types
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;

      public interface ISchema1 : ISchemaDefinition { }
      public interface ISchema2 : ISchemaDefinition { }
      public interface ISchema3 : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var estimator = new Estimator<ISchema1, ISchema2>(null!);
          var data = new DataView<ISchema1>(null!);

          Transformer<ISchema1, ISchema2> transformer = estimator.Fit(data);

          // Try to assign to wrong transformer type - should NOT compile
          Transformer<ISchema3, ISchema2> wrongTransformer = transformer;

          var wrongData = new DataView<ISchema3>(null!);
          var result = wrongTransformer.Transform(wrongData);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Cannot assign Transformer with incompatible input schema");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasTypeMismatch = errors.Any(e =>
        e.Id == "CS0266" || e.Id == "CS1503" ||
        e.ToString().Contains("cannot convert"));
    Assert.That(hasTypeMismatch, Is.True,
        $"Should have type mismatch error. Errors: {string.Join(", ", errors.Select(e => $"{e.Id}: {e.GetMessage()}"))}");
  }

  [Test]
  public void Multiple_Append_Calls_Verify_Schema_Propagation() {
    // Tests that schema types propagate correctly through multiple Append calls
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;

      public interface IS1 : ISchemaDefinition { }
      public interface IS2 : ISchemaDefinition { }
      public interface IS3 : ISchemaDefinition { }
      public interface IS4 : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var e1 = new Estimator<IS1, IS2>(null!);
          var e2 = new Estimator<IS2, IS3>(null!);
          var e3 = new Estimator<IS3, IS4>(null!);

          // These should all compile - schemas match correctly
          var pipeline = e1.Append(e2).Append(e3);

          // Verify the final type: should be Estimator<IS1, IS4>
          // Trying to append something that expects IS1 should NOT compile
          var e4 = new Estimator<IS1, IS2>(null!);
          var invalid = pipeline.Append(e4); // IS4 != IS1
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Pipeline with final schema mismatch should not compile");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasTypeMismatch = errors.Any(e =>
        e.Id == "CS1503" || e.Id == "CS0311" ||
        e.ToString().Contains("type argument"));
    Assert.That(hasTypeMismatch, Is.True, "Should have type mismatch in final Append");
  }

  [Test]
  public void Fin_Chaining_Without_Match_Should_Not_Compile() {
    // Verifies that Fin<T> results must be unwrapped before use
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Extract;
      using Flowthru.ML.Ext.Transform;
      using LanguageExt;

      public interface ISchema1 : ISchemaDefinition { }
      public interface ISchema2 : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var finData = DataLoader.LoadFromEnumerable<ISchema1>(new object[0]);
          var transformer = new Transformer<ISchema1, ISchema2>(null!);

          // Try to transform Fin<DataView<T>> directly - should NOT compile
          var result = transformer.Transform(finData);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Cannot use Fin<DataView<T>> without unwrapping");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasConversionError = errors.Any(e =>
        e.Id == "CS1503" || e.Id == "CS1929" ||
        e.ToString().Contains("cannot convert") ||
        e.ToString().Contains("Fin"));
    Assert.That(hasConversionError, Is.True, "Should have conversion error for Fin type");
  }

  [Test]
  [Explicit("Positive test - demonstrates valid multi-stage pipeline")]
  public void Valid_Three_Stage_Pipeline_Should_Compile() {
    // Positive control: shows correct multi-stage pipeline composition
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      using Flowthru.ML.Ext.Extract;
      using LanguageExt;

      public interface IOriginal : ISchemaDefinition { }
      public interface ILabelKey : ISchemaDefinition { }
      public interface IFeatures : ISchemaDefinition { }
      public interface IClassified : ISchemaDefinition { }

      public class Test {
        public void Execute() {
          var loader = DataLoader.LoadFromEnumerable<IOriginal>(new object[0]);
          var data = loader.Match(
            Succ: d => d,
            Fail: e => throw new System.Exception()
          );

          var labelKeyEstimator = new Estimator<IOriginal, ILabelKey>(null!);
          var featurizeEstimator = new Estimator<ILabelKey, IFeatures>(null!);
          var classifierEstimator = new Estimator<IFeatures, IClassified>(null!);

          // This SHOULD compile: all schemas match correctly
          var pipeline = labelKeyEstimator
            .Append(featurizeEstimator)
            .Append(classifierEstimator);

          var model = pipeline.Fit(data);
          var predictions = model.Transform(data);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.True,
        $"Valid three-stage pipeline should compile. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(e => $"{e.Id}: {e.GetMessage()}"))}");
  }
}
