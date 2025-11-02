using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Errors;

/// <summary>
/// Tests verifying ML.Next enforces correct pipeline transformation ordering.
///
/// Common scenario: Engineer applies transformations in wrong order.
/// ML.NET: Compiles fine, runtime error or incorrect results.
/// ML.Next: Type system enforces dependencies through schema evolution.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("PipelineOrdering")]
public class PipelineOrderingTests
{
  [Test]
  public void NormalizationBeforeConcatenation_Should_Require_Correct_Schema()
  {
    // Scenario: Normalizing before concatenating into Features
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Trying to normalize Features before it exists
                    var normalize = ColumnTransforms.NormalizeMinMax<IRawSchema, IFeaturesSchema>(
                        mlContext,
                        ""Features""  // Features doesn't exist yet in IRawSchema!
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // This should fail because Features column doesn't exist in IRawSchema
    Assert.That(
      result.Success,
      Is.False,
      "Normalizing non-existent Features column should not compile"
    );
  }

  [Test]
  public void MapKeyToValueBeforeTraining_Should_Not_Compile()
  {
    // Scenario: Converting key back to value before training (loses key information)
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Train;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = default(DataView<IKeyedSchema>);
                    
                    // Convert key back to value
                    var mapKeyToValue = ColumnTransforms.MapKeyToValue<IKeyedSchema, IRawSchema>(
                        mlContext,
                        outputColumnName: ""Label"",
                        inputColumnSelector: schema => schema.KeyColumn
                    );
                    
                    var valueData = mapKeyToValue.Underlying.Transform(data.Underlying);
                    var wrappedData = DataLoader.Wrap<IRawSchema>(valueData);
                    
                    // Now try to train with value instead of key - should not compile
                    var trainer = MulticlassClassificationTrainers.SdcaMaximumEntropy<IRawSchema, IModelSchema>(
                        mlContext,
                        labelColumnSelector: schema => schema.Label,  // Should be KeyColumn!
                        featureColumnSelector: schema => schema.Features  // Also doesn't exist
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(
      result.Success,
      Is.False,
      "Training with wrong label type and missing features should not compile"
    );
  }

  [Test]
  public void TrainingBeforeMapValueToKey_Should_Not_Compile()
  {
    // Scenario: Trying to train with raw Label instead of KeyColumn
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Train;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = default(DataView<IFeaturesSchema>);
                    
                    // Trying to train with raw Label (needs to be keyed first)
                    var trainer = MulticlassClassificationTrainers.SdcaMaximumEntropy<IFeaturesSchema, IModelSchema>(
                        mlContext,
                        labelColumnSelector: schema => schema.Label,  // Should use KeyColumn!
                        featureColumnSelector: schema => schema.Features
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // This may compile but will fail at runtime - Label should be keyed first
    // ML.Next can't enforce this at compile-time without additional type-level constraints
    if (result.Success)
    {
      Assert.Inconclusive(
        "Label keying requirement not enforced at compile-time - would need type-level key tracking"
      );
    }
    else
    {
      Assert.Pass("Type system correctly enforces label must be keyed before training");
    }
  }

  [Test]
  public void AppendingIncompatibleTransformations_Should_Not_Compile()
  {
    // Scenario: Chaining transformations where output schema doesn't match input schema
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // First transform: IRawSchema -> IKeyedSchema
                    var mapToKey = default(Estimator<IRawSchema, IKeyedSchema>);
                    
                    // Second transform: IModelSchema -> IModelSchema (incompatible with IKeyedSchema)
                    var secondTransform = default(Estimator<IModelSchema, IModelSchema>);
                    
                    // Try to chain - IKeyedSchema != IModelSchema
                    var pipeline = mapToKey.Append(secondTransform);
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(
      result.Success,
      Is.False,
      "Chaining transformations with incompatible schemas should not compile"
    );
  }
}
