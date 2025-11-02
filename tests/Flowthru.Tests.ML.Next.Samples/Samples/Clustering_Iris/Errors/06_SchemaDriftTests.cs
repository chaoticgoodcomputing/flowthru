using Flowthru.Tests.Common;
using NUnit.Framework;

namespace Flowthru.Tests.ML.Next.Samples.Samples.Clustering_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next helps prevent schema drift issues.
/// 
/// Common scenario: Model trained on v1 schema, but production data uses v2 schema.
/// ML.NET: Silently fails or produces wrong results.
/// ML.Next: Type system tracks schema versions through phantom types.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("SchemaDrift")]
public class SchemaDriftTests
{
    [Test]
    public void Model_Trained_On_V1_Used_With_V2_Data()
    {
        // Scenario: Schema changes between training and production
        var code = @"
            using Flowthru.ML.Next.Core.Schema;
            using Flowthru.ML.Next.Core.Columns;
            using Flowthru.ML.Next.Transform;
            using Microsoft.ML;

            public interface ISchemaV1 : ISchemaDefinition {
                ColumnName<float> Feature1 { get; }
                ColumnName<float> Feature2 { get; }
            }

            public interface ISchemaV2 : ISchemaDefinition {
                ColumnName<float> Feature1 { get; }
                ColumnName<float> Feature2 { get; }
                ColumnName<float> Feature3 { get; }  // New column added
            }

            public interface IFeaturesV1 : ISchemaV1 {
                ColumnName<float[]> Features { get; }
            }

            public interface IFeaturesV2 : ISchemaV2 {
                ColumnName<float[]> Features { get; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Model trained on V1
                    var transformV1 = ColumnTransforms.Concatenate<ISchemaV1, IFeaturesV1>(
                        mlContext, ""Features"", ""Feature1"", ""Feature2"");
                    
                    // Trying to use V1 transformer with V2 data - should not compile
                    var dataV2 = DataView<ISchemaV2>.From(null);
                    var result = transformV1.Fit(dataV2);  // Type mismatch!
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Using transformer trained on V1 schema with V2 data should not compile");
    }

    [Test]
    public void Documentation_Of_Schema_Versioning()
    {
        // This test documents the CORRECT way to handle schema versions
        var code = @"
            using Flowthru.ML.Next.Core.Schema;
            using Flowthru.ML.Next.Core.Columns;
            using Flowthru.ML.Next.Transform;
            using Microsoft.ML;

            public interface ISchemaV1 : ISchemaDefinition {
                ColumnName<float> Feature1 { get; }
                ColumnName<float> Feature2 { get; }
            }

            public interface ISchemaV2 : ISchemaDefinition {
                ColumnName<float> Feature1 { get; }
                ColumnName<float> Feature2 { get; }
                ColumnName<float> Feature3 { get; }
            }

            public interface IFeaturesV2 : ISchemaV2 {
                ColumnName<float[]> Features { get; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // CORRECT: Create new transformer for V2
                    var transformV2 = ColumnTransforms.Concatenate<ISchemaV2, IFeaturesV2>(
                        mlContext, ""Features"", ""Feature1"", ""Feature2"", ""Feature3"");
                    
                    var dataV2 = DataView<ISchemaV2>.From(null);
                    var result = transformV2.Fit(dataV2);  // Types match!
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Matching schema versions should compile successfully");
    }
}
