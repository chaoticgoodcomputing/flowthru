using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace ML.Next.Tests.Samples.Samples.Clustering_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next catches column name typos in Iris clustering at compile-time.
///
/// Common scenario: Engineer copies clustering code and misspells "SepalLength" as "SepelLength".
/// ML.NET: Compiles fine, fails at runtime when pipeline executes.
/// ML.Next: Compilation error - column doesn't exist in schema.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("ColumnNames")]
public class ColumnNameTests
{
  [Test]
  public void TypoInColumnName_Should_Not_Compile()
  {
    // Scenario: Engineer types "SepelLength" instead of "SepalLength"
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> SepalLength { get; }
                ColumnName<float> SepalWidth { get; }
            }

            public interface IFeaturesSchema : IRawSchema {
                ColumnName<float[]> Features { get; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Typo: 'SepelLength' instead of 'SepalLength'
                    var pipeline = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
                        mlContext,
                        ""Features"",
                        ""SepelLength"",  // TYPO - runtime error in ML.NET, caught here via nameof()
                        ""SepalWidth""
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Should compile (typos in strings aren't caught at compile-time)
    // This test documents ML.NET behavior - typos compile but fail at runtime
    Assert.That(
      result.Success,
      Is.False,
      "String-based column names allow typos - this is the ML.NET limitation ML.Next aims to fix"
    );
  }

  [Test]
  public void NonExistentColumn_Should_Not_Compile()
  {
    // Scenario: Referencing a column that doesn't exist in the schema
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;
            using ML.Next.Transform;
            using Microsoft.ML;

            public interface IRawSchema : ISchemaDefinition {
                ColumnName<float> SepalLength { get; }
                ColumnName<float> SepalWidth { get; }
            }

            public interface IFeaturesSchema : IRawSchema {
                ColumnName<float[]> Features { get; }
            }

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Using nameof with non-existent type
                    var pipeline = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
                        mlContext,
                        ""Features"",
                        nameof(NonExistentType.NonExistentColumn),  // Should not compile
                        ""SepalWidth""
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Referencing non-existent type should not compile");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

    Assert.That(errors, Is.Not.Empty, "Should have compilation errors");
  }
}
