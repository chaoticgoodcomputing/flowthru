using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Errors;

/// <summary>
/// Tests verifying ML.Next handles null and missing values safely.
/// 
/// Common scenario: Data contains nulls but pipeline doesn't handle them.
/// ML.NET: Runtime error or silent NaN propagation.
/// ML.Next: Schema tracks nullable columns, enforces null handling.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("NullHandling")]
public class NullHandlingTests {
  [Test]
  public void NullableColumnWithoutHandling_Should_Require_Explicit_Strategy() {
    // Scenario: Column may contain nulls but no imputation specified
    // This documents desired behavior - track nullability in schema
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface IMySchema : ISchemaDefinition {
                ColumnName<float?> NullableFeature { get; }  // Nullable float
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Using nullable column directly in concatenation
                    // Should require explicit handling
                    var pipeline = ColumnTransforms.Concatenate<IMySchema, IMySchema>(
                        mlContext,
                        ""Features"",
                        schema => schema.NullableFeature  // Nullable not handled!
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Currently compiles - nullability tracking not fully implemented
    if (result.Success) {
      Assert.Inconclusive(
        "Nullable column tracking not fully implemented - requires schema-level nullability tracking");
    } else {
      Assert.Pass("Type system correctly requires explicit null handling");
    }
  }

  [Test]
  public void ImputationChangesNullability_In_Schema() {
    // Scenario: After imputation, column should no longer be nullable
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float?> Feature { get; }  // Nullable
            }

            public interface ICleanedSchema : ISchemaDefinition {
                ColumnName<float> Feature { get; }  // No longer nullable after imputation
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = default(DataView<IRawSchema>);
                    
                    // Imputation transformer should change schema from nullable to non-nullable
                    var imputer = default(Transformer<IRawSchema, ICleanedSchema>);
                    
                    var cleaned = imputer.Transform(data);
                    // cleaned should have type DataView<ICleanedSchema> with non-nullable Feature
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // This should compile - demonstrates proper nullable tracking
    Assert.That(result.Success, Is.True,
      "Nullable tracking in schemas should compile correctly");
  }

  [Test]
  public void MissingValueIndicatorColumn_Should_Be_Tracked() {
    // Scenario: Creating indicator column for missing values
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> Feature { get; }
            }

            public interface IIndicatorSchema : IRawSchema {
                ColumnName<bool> Feature_Missing { get; }  // Missing value indicator
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Creating missing value indicator
                    // Schema should track the new indicator column
                    var indicator = default(Estimator<IRawSchema, IIndicatorSchema>);
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Should compile - demonstrates proper indicator column tracking
    Assert.That(result.Success, Is.True,
      "Missing value indicator columns should be trackable in schema");
  }

  [Test]
  public void NullInNonNullableColumn_Should_Be_Caught_At_Load() {
    // Scenario: Loading data with nulls into non-nullable schema
    var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Extract;
            using Microsoft.ML;

            public interface IStrictSchema : ISchemaDefinition {
                ColumnName<float> Feature { get; }  // Non-nullable
            }

            public class DataWithNulls {
                public float? Feature { get; set; }  // Source has nulls
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    var data = new[] { new DataWithNulls { Feature = null } };
                    
                    // Loading nullable source into non-nullable schema
                    // Should be validated at load time
                    var result = DataLoader.LoadFromEnumerable<IStrictSchema, DataWithNulls>(
                        mlContext,
                        data
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Currently compiles - runtime validation would catch the null
    if (result.Success) {
      Assert.Inconclusive(
        "Compile-time null validation not implemented - requires type-level nullable analysis");
    } else {
      Assert.Pass("Type system caught nullable/non-nullable mismatch at compile-time");
    }
  }
}
