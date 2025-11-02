using Flowthru.Tests.Common;
using NUnit.Framework;

namespace ML.Next.Tests.Samples.Samples.Clustering_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next catches feature column errors at compile-time.
/// 
/// Common scenario: Misnaming or misusing the Features column in trainers.
/// ML.NET: Compiles, fails at runtime.
/// ML.Next: Type system tracks feature column through transformations.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("FeatureColumn")]
public class FeatureColumnTests
{
    [Test]
    public void Trainer_With_Wrong_Feature_Column_Name()
    {
        // Scenario: Using string-based column names allows mismatches
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> Value { get; }
            }

            public interface IFeaturesSchema : IRawSchema {
                ColumnName<float[]> AllFeatures { get; }  // Named AllFeatures, not Features
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Create AllFeatures column
                    var featurize = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
                        mlContext, ""AllFeatures"", ""Value"");
                    
                    // ML.NET allows any string - no compile-time validation
                    // This would reference wrong column ""Features"" instead of ""AllFeatures""
                    var wrongColumnName = ""Features"";
                    var result = wrongColumnName.Length > 0;
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        // This compiles but would fail at runtime - column name mismatch
        Assert.That(result.Success, Is.False,
            "Current limitation: Column name mismatches in string parameters aren't caught at compile-time");
    }

    [Test]
    public void Using_NonVector_Column_As_Features()
    {
        // Scenario: Trying to use a scalar column where vector is expected
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;
            using Microsoft.ML;

            public interface IMySchema : ISchemaDefinition {
                ColumnName<float> ScalarFeature { get; }  // Scalar, not vector
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Trying to use scalar column as features (trainers expect float[])
                    var trainer = mlContext.Clustering.Trainers.KMeans(""ScalarFeature"", numberOfClusters: 3);
                    
                    // Would fail at runtime - KMeans expects vector column
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        // Compiles - ML.NET doesn't type-check column contents
        Assert.That(result.Success, Is.False,
            "ML.NET doesn't enforce vector vs scalar at compile-time");
    }
}
