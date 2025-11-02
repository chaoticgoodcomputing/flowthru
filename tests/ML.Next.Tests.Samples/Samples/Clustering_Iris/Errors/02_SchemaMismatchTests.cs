using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace ML.Next.Tests.Samples.Samples.Clustering_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next prevents schema mismatches in pipeline composition.
/// 
/// Common scenario: Engineer chains transformations where output of one stage
/// doesn't match input of next stage.
/// ML.NET: Compiles, fails at runtime during Fit() or Transform().
/// ML.Next: Compilation error - type mismatch in Append() call.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("SchemaMismatch")]
public class SchemaMismatchTests
{
    [Test]
    public void IncompatibleSchemaAppend_Should_Not_Compile()
    {
        // Scenario: Trying to append transformers with incompatible schemas
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface ISchemaA : ISchemaDefinition {
                ColumnName<float> ColA { get; }
            }

            public interface ISchemaB : ISchemaDefinition {
                ColumnName<float> ColB { get; }
            }

            public interface ISchemaC : ISchemaDefinition {
                ColumnName<float> ColC { get; }
            }

            public interface ISchemaD : ISchemaDefinition {
                ColumnName<float> ColD { get; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // First transformer: A -> B
                    var transform1 = ColumnTransforms.Concatenate<ISchemaA, ISchemaB>(
                        mlContext, ""ColB"", ""ColA"");
                    
                    // Second transformer: C -> D (incompatible with A -> B)
                    var transform2 = ColumnTransforms.Concatenate<ISchemaC, ISchemaD>(
                        mlContext, ""ColD"", ""ColC"");
                    
                    // This should not compile - B != C
                    var combined = transform1.Append(transform2);
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Appending transformers with incompatible schemas should not compile");

        var errors = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.That(errors, Is.Not.Empty, "Should have compilation errors");
    }

    [Test]
    public void SkippingRequiredTransformation_Should_Not_Compile()
    {
        // Scenario: Trying to use a column before creating it
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;
            using ML.Next.Transform;
            using Microsoft.ML;
            using Microsoft.ML.Data;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> Value { get; }
            }

            public interface IFeaturesSchema : IRawSchema {
                ColumnName<float[]> Features { get; }
            }

            public interface IClusteredSchema : IFeaturesSchema {
                ColumnName<uint> PredictedLabel { get; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Try to cluster without creating Features column first
                    var trainer = mlContext.Clustering.Trainers.KMeans(""Features"", numberOfClusters: 3);
                    var estimator = Estimator<IRawSchema, IClusteredSchema>.From(trainer);
                    
                    // This should fail - IRawSchema doesn't have Features column
                    // but IClusteredSchema requires it
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        // Note: This actually compiles because ML.NET doesn't validate at compile-time
        // This test documents current limitation - schema inheritance doesn't enforce requirements
        Assert.That(result.Success, Is.False,
            "Current limitation: Schema inheritance doesn't enforce column presence at compile-time");
    }
}
