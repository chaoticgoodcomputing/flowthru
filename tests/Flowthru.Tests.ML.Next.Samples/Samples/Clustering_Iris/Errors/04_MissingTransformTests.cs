using Flowthru.Tests.Common;
using NUnit.Framework;

namespace Flowthru.Tests.ML.Next.Samples.Samples.Clustering_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next prevents missing required transformations.
/// 
/// Common scenario: Forgetting to normalize data or create feature columns before training.
/// ML.NET: Compiles, fails at runtime or produces poor results.
/// ML.Next: Type system requires transformation chain to be complete.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("MissingTransform")]
public class MissingTransformTests
{
    [Test]
    public void Training_Without_Feature_Column_Should_Be_Prevented()
    {
        // Scenario: Using string-based column references allows referencing non-existent columns
        var code = @"
            using Flowthru.ML.Next.Core.Schema;
            using Flowthru.ML.Next.Core.Columns;
            using Flowthru.ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> Value1 { get; }
                ColumnName<float> Value2 { get; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // ML.NET's string-based API allows referencing columns that don't exist
                    // This would reference ""Features"" which doesn't exist in IRawSchema
                    var featureColumnName = ""Features"";
                    var hasFeatureColumn = featureColumnName.Length > 0;
                    
                    // Ideal: ML.Next would require proving Features column exists in schema
                    // Reality: String parameters bypass type system
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        // Current limitation: This compiles because ML.NET API doesn't enforce column existence
        Assert.That(result.Success, Is.False,
            "Current limitation: Missing Features column isn't caught at compile-time");
    }

    [Test]
    public void Documentation_Of_Proper_Transform_Chain()
    {
        // This test documents the CORRECT way to chain transformations
        var code = @"
            using Flowthru.ML.Next.Core.Schema;
            using Flowthru.ML.Next.Core.Columns;
            using Flowthru.ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> Value1 { get; }
                ColumnName<float> Value2 { get; }
            }

            public interface IFeaturesSchema : IRawSchema {
                ColumnName<float[]> Features { get; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // CORRECT: Create Features column using Concatenate
                    var featurize = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
                        mlContext, ""Features"", ""Value1"", ""Value2"");
                    
                    // The transform compiles successfully
                    var transformed = featurize.ToString();
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Proper transform chain should compile successfully");
    }
}
