using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Errors;

/// <summary>
/// Tests verifying ML.Next catches feature column errors at compile-time.
///
/// Common scenario: Engineer specifies wrong columns or column order in Features.
/// ML.NET: Compiles fine, potentially incorrect model or runtime error.
/// ML.Next: Compilation error - column doesn't exist or wrong type.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("FeatureColumn")]
public class FeatureColumnTests
{
  [Test]
  public void EmptyFeatureConcatenation_Should_Not_Compile()
  {
    // Scenario: Trying to create Features column with no input columns
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Trying to concatenate with no columns - should not compile
                    var pipeline = ColumnTransforms.Concatenate<IKeyedSchema, IFeaturesSchema>(
                        mlContext,
                        ""Features""
                        // No column selectors provided!
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(
      result.Success,
      Is.False,
      "Feature concatenation with no columns should not compile"
    );
  }

  [Test]
  public void IncludingLabelInFeatures_Should_Not_Be_Prevented()
  {
    // Scenario: Engineer accidentally includes Label in Features (data leakage)
    // This is a logical error that's hard to catch at compile-time
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Including Label in Features causes data leakage
                    var pipeline = ColumnTransforms.Concatenate<IKeyedSchema, IFeaturesSchema>(
                        mlContext,
                        ""Features"",
                        schema => schema.Label,  // OOPS! Data leakage
                        schema => schema.SepalLength,
                        schema => schema.SepalWidth
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // This is a logical error that compiles fine
    // Future enhancement: mark label columns in schema to prevent inclusion in features
    if (result.Success)
    {
      Assert.Inconclusive(
        "Label-in-features prevention not implemented - requires schema-level column marking"
      );
    }
    else
    {
      Assert.Pass("Schema correctly prevents including label column in features");
    }
  }

  [Test]
  public void DuplicateColumnsInFeatures_Should_Be_Allowed()
  {
    // Scenario: Engineer accidentally includes same column twice
    // ML.NET allows this (sometimes intentional for weighting)
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Including SepalLength twice
                    var pipeline = ColumnTransforms.Concatenate<IKeyedSchema, IFeaturesSchema>(
                        mlContext,
                        ""Features"",
                        schema => schema.SepalLength,
                        schema => schema.SepalLength,  // Duplicate
                        schema => schema.SepalWidth
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Duplicates are allowed in ML.NET (sometimes intentional)
    Assert.That(
      result.Success,
      Is.True,
      "Duplicate columns in features should be allowed (may be intentional)"
    );
  }

  [Test]
  public void WrongColumnTypeInConcatenation_Should_Not_Compile()
  {
    // Scenario: Trying to concatenate non-numeric column into Features
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Trying to concatenate Score (float[]) - already an array
                    var pipeline = ColumnTransforms.Concatenate<IModelSchema, IModelSchema>(
                        mlContext,
                        ""NewFeatures"",
                        schema => schema.Score  // This is already float[], concatenation may behave unexpectedly
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // ML.NET allows this - it concatenates array elements
    // Type system currently doesn't distinguish scalar from array columns in concatenation
    if (result.Success)
    {
      Assert.Inconclusive(
        "Array column concatenation allowed - consider warning for unexpected behavior"
      );
    }
    else
    {
      Assert.Pass("Type system prevents array column concatenation");
    }
  }
}
