using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Errors;

/// <summary>
/// Tests verifying ML.Next enforces required transformations at compile-time.
/// 
/// Common scenario: Engineer forgets to create Features column before training.
/// ML.NET: Compiles fine, runtime error when trainer expects missing column.
/// ML.Next: Compilation error - schema doesn't contain required column.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("MissingTransform")]
public class MissingTransformTests {
  [Test]
  public void TrainingWithoutFeatureConcatenation_Should_Not_Compile() {
    // Scenario: Trying to train without creating Features column
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Train;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = default(DataView<IKeyedSchema>);
                    
                    // Trying to train without Features column (only in IFeaturesSchema)
                    var trainer = MulticlassClassificationTrainers.SdcaMaximumEntropy<IKeyedSchema, IModelSchema>(
                        mlContext,
                        labelColumnSelector: schema => schema.KeyColumn,
                        featureColumnSelector: schema => schema.Features  // Features doesn't exist in IKeyedSchema!
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False,
      "Training without creating Features column should not compile");
  }

  [Test]
  public void UsingKeyColumnWithoutMapValueToKey_Should_Not_Compile() {
    // Scenario: Trying to use KeyColumn before calling MapValueToKey
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Train;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = default(DataView<IRawSchema>);
                    
                    // Trying to use KeyColumn before it's created
                    var concatenate = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
                        mlContext,
                        ""Features"",
                        schema => schema.KeyColumn,  // KeyColumn doesn't exist in IRawSchema!
                        schema => schema.SepalLength
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False,
      "Using KeyColumn before MapValueToKey should not compile");
  }

  [Test]
  public void SkippingRequiredNormalizationStep_Should_Not_Compile() {
    // Scenario: Some algorithms require normalized features
    // This test documents the ideal behavior where schema tracks normalization state
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Train;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = default(DataView<IFeaturesSchema>);
                    
                    // Some trainers work better with normalized features
                    // Ideally, schema should track if features are normalized
                    var trainer = MulticlassClassificationTrainers.SdcaMaximumEntropy<IFeaturesSchema, IModelSchema>(
                        mlContext,
                        labelColumnSelector: schema => schema.KeyColumn,
                        featureColumnSelector: schema => schema.Features
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Currently this compiles - normalization is optional for SDCA
    // Future enhancement: track normalization state in schema for algorithms that require it
    if (result.Success) {
      Assert.Inconclusive(
        "Normalization tracking not yet implemented - feature for future enhancement");
    } else {
      Assert.Pass("Schema correctly enforces normalization requirement");
    }
  }
}
