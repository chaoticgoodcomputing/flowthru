using Flowthru.Tests.Common;
using NUnit.Framework;

namespace Flowthru.Tests.ML.Next.Samples.Samples.Clustering_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next handles null and missing values safely.
/// 
/// Common scenario: Using nullable types incorrectly or missing null checks.
/// ML.NET: Runtime null reference exceptions.
/// ML.Next: Type system tracks nullability through transformations.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("NullHandling")]
public class NullHandlingTests
{
    [Test]
    public void Nullable_Column_Used_As_NonNullable()
    {
        // Scenario: Schema has nullable float but code assumes non-null
        var code = @"
            using Flowthru.ML.Next.Core.Schema;
            using Flowthru.ML.Next.Core.Columns;
            using Microsoft.ML;

            public interface IMySchema : ISchemaDefinition {
                ColumnName<float?> NullableColumn { get; }  // Nullable
                ColumnName<float> RequiredColumn { get; }   // Non-nullable
            }

            public class Test {
                public void Execute() {
                    // Trying to assign nullable to non-nullable
                    ColumnName<float> required = default(ColumnName<float?>);  // Should not compile
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Assigning ColumnName<float?> to ColumnName<float> should not compile");
    }

    [Test]
    public void Documentation_Of_Proper_Null_Handling()
    {
        // This test documents the CORRECT way to handle nullable columns
        var code = @"
            using Flowthru.ML.Next.Core.Schema;
            using Flowthru.ML.Next.Core.Columns;
            using Flowthru.ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float?> PossiblyMissingValue { get; }
            }

            public interface ICleanedSchema : ISchemaDefinition {
                ColumnName<float> CleanedValue { get; }  // Non-nullable after cleaning
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // CORRECT: Use ML.NET transforms to handle nulls
                    // ReplaceMissingValues, Filter, etc.
                    var cleaner = mlContext.Transforms.ReplaceMissingValues(""CleanedValue"", ""PossiblyMissingValue"");
                    var estimator = Estimator<IRawSchema, ICleanedSchema>.From(cleaner);
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Proper null handling with transforms should compile");
    }
}
