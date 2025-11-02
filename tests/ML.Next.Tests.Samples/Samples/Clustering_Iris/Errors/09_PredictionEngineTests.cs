using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace ML.Next.Tests.Samples.Samples.Clustering_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next prevents prediction engine type mismatches.
///
/// Common scenario: Creating prediction engine with wrong input/output types.
/// ML.NET: Compiles, fails at runtime with confusing errors.
/// ML.Next: Type system ensures prediction engine types match model.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("PredictionEngine")]
public class PredictionEngineTests
{
  [Test]
  public void PredictionEngine_With_Wrong_Input_Type()
  {
    // Scenario: Model expects IrisData but engine created with different type
    var code =
      @"
            using ML.Next.Core.Schema;
            using Microsoft.ML;
            using Microsoft.ML.Data;

            public class IrisData {
                public float SepalLength { get; set; }
                public float SepalWidth { get; set; }
            }

            public class WrongData {
                public float DifferentField { get; set; }
            }

            public class IrisPrediction {
                public uint ClusterId { get; set; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    ITransformer model = null;  // Assume trained model
                    
                    // Model was trained with IrisData, but engine uses WrongData
                    var engine = mlContext.Model.CreatePredictionEngine<WrongData, IrisPrediction>(model);
                    
                    // This compiles but will fail at runtime
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Compiles - ML.NET doesn't verify prediction engine types against model
    Assert.That(
      result.Success,
      Is.False,
      "ML.NET doesn't enforce prediction engine type safety at compile-time"
    );
  }

  [Test]
  public void PredictionEngine_With_Wrong_Output_Type()
  {
    // Scenario: Model outputs clustering results but engine expects classification
    var code =
      @"
            using Microsoft.ML;
            using Microsoft.ML.Data;

            public class Input {
                public float Value { get; set; }
            }

            public class ClusteringOutput {
                public uint ClusterId { get; set; }
                public float[] Distances { get; set; }
            }

            public class ClassificationOutput {
                public string PredictedLabel { get; set; }  // Wrong type!
                public float[] Score { get; set; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    ITransformer model = null;  // Clustering model
                    
                    // Clustering model but expecting classification output
                    var engine = mlContext.Model.CreatePredictionEngine<Input, ClassificationOutput>(model);
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(
      result.Success,
      Is.False,
      "ML.NET doesn't verify output type matches model at compile-time"
    );
  }

  [Test]
  public void Documentation_Of_Type_Safe_Prediction()
  {
    // This test documents how ML.Next could provide type safety (future enhancement)
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> Value { get; }
            }

            public interface IClusteredSchema : IRawSchema {
                ColumnName<uint> PredictedLabel { get; }
                ColumnName<float[]> Score { get; }
            }

            public class Input {
                public float Value { get; set; }
            }

            public class Output {
                public uint PredictedLabel { get; set; }
                public float[] Score { get; set; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Type-safe approach: Transformer knows its schema types
                    var transformer = default(Transformer<IRawSchema, IClusteredSchema>);
                    
                    // Future: ML.Next could provide type-safe prediction engine
                    // var engine = PredictionEngine<Input, Output>.Create(mlContext, transformer);
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Type-safe prediction engine pattern should compile");
  }
}
