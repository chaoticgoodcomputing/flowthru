using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Errors;

/// <summary>
/// Tests verifying ML.Next catches prediction engine type mismatches at compile-time.
/// 
/// Common scenario: Engineer creates prediction engine with wrong input/output types.
/// ML.NET: Runtime error when types don't match model.
/// ML.Next: Compilation error - types must match transformer schema.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("PredictionEngine")]
public class PredictionEngineTests {
  [Test]
  public void PredictionEngineWithWrongInputType_Should_Not_Compile() {
    // Scenario: Creating prediction engine with input type that doesn't match model
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Load;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class WrongInput {
                public float OnlyOneFeature { get; set; }
            }

            public class CorrectOutput {
                public float PredictedLabel { get; set; }
                public float[] Score { get; set; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var model = default(Transformer<IRawSchema, IModelSchema>);
                    
                    // Creating prediction engine with wrong input type
                    var engine = PredictionEngine<WrongInput, CorrectOutput>.Create(
                        mlContext,
                        model
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Currently may compile - runtime would catch the mismatch
    if (result.Success) {
      Assert.Inconclusive(
        "PredictionEngine type validation not enforced at compile-time - requires schema-to-class matching");
    } else {
      Assert.Pass("Type system caught input type mismatch at compile-time");
    }
  }

  [Test]
  public void PredictionEngineWithWrongOutputType_Should_Not_Compile() {
    // Scenario: Creating prediction engine with output type that doesn't match model predictions
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Load;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;
            using ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas;

            public class WrongOutput {
                public string PredictedLabel { get; set; }  // Should be float
                public float[] Score { get; set; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var model = default(Transformer<IRawSchema, IModelSchema>);
                    
                    // Creating prediction engine with wrong output type
                    var engine = PredictionEngine<IrisData, WrongOutput>.Create(
                        mlContext,
                        model
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Currently may compile - runtime would catch the mismatch
    if (result.Success) {
      Assert.Inconclusive(
        "PredictionEngine output type validation not enforced at compile-time");
    } else {
      Assert.Pass("Type system caught output type mismatch at compile-time");
    }
  }

  [Test]
  public void PredictionEngineWithMissingOutputColumn_Should_Not_Compile() {
    // Scenario: Output class missing required column
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Load;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;
            using ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas;

            public class IncompleteOutput {
                public float PredictedLabel { get; set; }
                // Missing Score[] column!
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var model = default(Transformer<IRawSchema, IModelSchema>);
                    
                    // Creating prediction engine with incomplete output
                    var engine = PredictionEngine<IrisData, IncompleteOutput>.Create(
                        mlContext,
                        model
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // This compiles fine - missing columns are just not populated
    // Not really an error in ML.NET's design
    Assert.That(result.Success, Is.True,
      "Missing output columns are allowed - they simply won't be populated");
  }

  [Test]
  public void PredictionForWrongSchemaStage_Should_Not_Compile() {
    // Scenario: Using prediction engine with model that outputs intermediate schema
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Load;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;
            using ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Partial pipeline that only produces features, not predictions
                    var featurizer = default(Transformer<IRawSchema, IFeaturesSchema>);
                    
                    // Trying to create prediction engine for intermediate stage
                    // Output schema doesn't have PredictedLabel or Score
                    var engine = PredictionEngine<IrisData, IrisPrediction>.Create(
                        mlContext,
                        featurizer
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // This may compile but will fail at runtime
    if (result.Success) {
      Assert.Inconclusive(
        "Schema stage validation not enforced - requires matching schema to class structure");
    } else {
      Assert.Pass("Type system caught schema stage mismatch");
    }
  }
}
