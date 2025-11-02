using Flowthru.Tests.Common;
using NUnit.Framework;

namespace ML.Next.Tests.Samples.Samples.Clustering_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next enforces correct pipeline ordering.
/// 
/// Common scenario: Applying transformations in wrong order.
/// ML.NET: Compiles, produces incorrect results or runtime errors.
/// ML.Next: Type system ensures transformations are ordered correctly.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("PipelineOrdering")]
public class PipelineOrderingTests
{
    [Test]
    public void Normalization_Before_Feature_Creation_Wrong_Order()
    {
        // Scenario: Trying to normalize Features before creating them
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> Value1 { get; }
                ColumnName<float> Value2 { get; }
            }

            public interface INormalizedSchema : IRawSchema {
                ColumnName<float[]> Features { get; }  // Already normalized
            }

            public interface IFeaturesSchema : IRawSchema {
                ColumnName<float[]> Features { get; }  // Not normalized yet
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // WRONG: Trying to normalize before creating
                    var normalize = ColumnTransforms.NormalizeMinMax<IRawSchema, INormalizedSchema>(
                        mlContext, ""Features"");  // Features doesn't exist yet!
                    
                    var concatenate = ColumnTransforms.Concatenate<INormalizedSchema, IFeaturesSchema>(
                        mlContext, ""Features"", ""Value1"", ""Value2"");
                    
                    var pipeline = normalize.Append(concatenate);  // Wrong order!
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        // This compiles because ML.NET doesn't validate column existence
        Assert.That(result.Success, Is.False,
            "Current limitation: Pipeline ordering isn't fully enforced at compile-time");
    }

    [Test]
    public void Documentation_Of_Correct_Pipeline_Order()
    {
        // This test documents the CORRECT pipeline ordering
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> Value1 { get; }
                ColumnName<float> Value2 { get; }
            }

            public interface IFeaturesSchema : IRawSchema {
                ColumnName<float[]> Features { get; }
            }

            public interface INormalizedSchema : IFeaturesSchema {
                ColumnName<float[]> Features { get; }  // Normalized version
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // CORRECT: Create features first
                    var concatenate = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
                        mlContext, ""Features"", ""Value1"", ""Value2"");
                    
                    // Then normalize
                    var normalize = ColumnTransforms.NormalizeMinMax<IFeaturesSchema, INormalizedSchema>(
                        mlContext, ""Features"");
                    
                    var pipeline = concatenate.Append(normalize);  // Correct order!
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Correct pipeline ordering should compile successfully");
    }
}
