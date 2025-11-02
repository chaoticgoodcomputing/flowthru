using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Errors;

/// <summary>
/// Tests verifying ML.Next catches schema drift/evolution issues at compile-time.
/// 
/// Common scenario: Data schema changes in production (new columns, removed columns, type changes).
/// ML.NET: Model continues to run, potentially with incorrect results or runtime errors.
/// ML.Next: Type system tracks schema versions through phantom types.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("SchemaDrift")]
public class SchemaDriftTests {
  [Test]
  public void ModelTrainedOnV1_AppliedToV2_Should_Not_Compile() {
    // Scenario: Schema evolves but model trained on old schema
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Extract;
            using Microsoft.ML;

            public interface ISchemaV1 : ISchemaDefinition {
                ColumnName<float> FeatureA { get; }
                ColumnName<float> FeatureB { get; }
            }

            public interface ISchemaV2 : ISchemaDefinition {
                ColumnName<float> FeatureA { get; }
                ColumnName<float> FeatureB { get; }
                ColumnName<float> FeatureC { get; }  // New column added
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Model trained on V1
                    var modelV1 = default(Transformer<ISchemaV1, ISchemaV1>);
                    
                    // Data with V2 schema
                    var dataV2 = default(DataView<ISchemaV2>);
                    
                    // Try to apply V1 model to V2 data - should not compile!
                    var result = modelV1.Transform(dataV2);
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False,
      "Applying model trained on old schema to new schema should not compile");
  }

  [Test]
  public void RemovedColumn_Should_Not_Compile() {
    // Scenario: Schema evolution removes a column that model depends on
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Extract;
            using Microsoft.ML;

            public interface ISchemaV1 : ISchemaDefinition {
                ColumnName<float> FeatureA { get; }
                ColumnName<float> FeatureB { get; }
                ColumnName<float> FeatureC { get; }
            }

            public interface ISchemaV2 : ISchemaDefinition {
                ColumnName<float> FeatureA { get; }
                ColumnName<float> FeatureB { get; }
                // FeatureC removed!
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = default(DataView<ISchemaV2>);
                    
                    // Try to use removed column FeatureC
                    var pipeline = ColumnTransforms.Concatenate<ISchemaV2, ISchemaV2>(
                        mlContext,
                        ""Features"",
                        schema => schema.FeatureC  // This column was removed in V2!
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False,
      "Using removed column should not compile");
  }

  [Test]
  public void ColumnTypeChanged_Should_Require_New_Schema_Version() {
    // Scenario: Column type changes from float to string
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface ISchemaV1 : ISchemaDefinition {
                ColumnName<float> NumericId { get; }
            }

            public interface ISchemaV2 : ISchemaDefinition {
                ColumnName<string> NumericId { get; }  // Changed to string
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Pipeline expects float
                    var pipelineV1 = default(Transformer<ISchemaV1, ISchemaV1>);
                    
                    // Data now has string
                    var dataV2 = default(DataView<ISchemaV2>);
                    
                    // Try to apply - schemas are incompatible by name alone
                    var result = pipelineV1.Transform(dataV2);
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False,
      "Applying pipeline to data with changed column types should not compile");
  }
}
