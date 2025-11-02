using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next catches column name typos in Iris multiclass classification at compile-time.
///
/// Common scenario: Engineer copies classification code and misspells "SepalLength" as "SepelLength".
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
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Typo: 'SepelLength' instead of 'SepalLength'
                    var pipeline = ColumnTransforms.Concatenate<IKeyedSchema, IFeaturesSchema>(
                        mlContext,
                        ""Features"",
                        schema => schema.SepelLength,  // TYPO - should not compile!
                        schema => schema.SepalWidth,
                        schema => schema.PetalLength,
                        schema => schema.PetalWidth
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Should fail to compile
    Assert.That(result.Success, Is.False, "Code with column name typo should not compile");

    // Verify we get the expected error
    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

    Assert.That(errors, Is.Not.Empty, "Should have compilation errors");

    // Should have "member not found" error (CS0117)
    var hasMemberNotFound = errors.Any(e =>
      e.Id == "CS0117" || e.GetMessage().Contains("does not contain a definition")
    );

    Assert.That(
      hasMemberNotFound,
      Is.True,
      $"Should have 'member not found' error. Got: {string.Join(", ", errors.Select(e => e.Id))}"
    );
  }

  [Test]
  public void ColumnFromWrongSchema_Should_Not_Compile()
  {
    // Scenario: Engineer references a column that exists in a different schema
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Trying to use 'Features' column before it exists
                    var pipeline = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
                        mlContext,
                        ""AllFeatures"",
                        schema => schema.Features,  // Doesn't exist in IRawSchema!
                        schema => schema.SepalLength
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(
      result.Success,
      Is.False,
      "Referencing column from wrong schema should not compile"
    );
  }

  [Test]
  public void WrongColumnInMapValueToKey_Should_Not_Compile()
  {
    // Scenario: Engineer tries to map a non-existent column to key
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Typo in column name
                    var pipeline = ColumnTransforms.MapValueToKey<IRawSchema, IKeyedSchema>(
                        mlContext,
                        outputColumnName: ""KeyColumn"",
                        inputColumnSelector: schema => schema.Lable  // Typo: 'Lable' instead of 'Label'
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Typo in MapValueToKey input column should not compile");
  }
}
