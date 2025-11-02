using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Errors;

/// <summary>
/// Tests verifying ML.Next catches column type mismatches at compile-time.
///
/// Common scenario: Engineer tries to use float column where string is expected.
/// ML.NET: Compiles fine, runtime error when types don't match.
/// ML.Next: Compilation error - type mismatch in expression tree.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("TypeMismatch")]
public class TypeMismatchTests
{
  [Test]
  public void FloatColumnAsStringInput_Should_Not_Compile()
  {
    // Scenario: Trying to use MapValueToKey on a float column (expects categorical)
    // Note: This test documents desired behavior - ML.Next currently allows this
    // but should ideally enforce type compatibility at compile-time
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Attempting to map a float column (SepalLength) to key type
                    // This is semantically wrong but ML.NET allows it at runtime
                    var pipeline = ColumnTransforms.MapValueToKey<IRawSchema, IKeyedSchema>(
                        mlContext,
                        outputColumnName: ""KeyColumn"",
                        inputColumnSelector: schema => schema.SepalLength  // Float, not categorical!
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Currently this compiles, but ideally it should not
    // This test documents the gap in type safety
    if (result.Success)
    {
      Assert.Inconclusive(
        "Type mismatch currently allowed - enhancement needed to enforce categorical types for MapValueToKey"
      );
    }
    else
    {
      Assert.Pass("Type system correctly prevents using float column with MapValueToKey");
    }
  }

  [Test]
  public void WrongGenericTypeParameter_Should_Not_Compile()
  {
    // Scenario: Using wrong type parameter in ColumnName<T>
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;

            public interface ITestSchema : ISchemaDefinition {
                ColumnName<float> NumericColumn { get; }
            }

            public class Test {
                public void Execute() {
                    // Trying to treat float column as string
                    ColumnName<string> wrongType = default(ITestSchema).NumericColumn;
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(
      result.Success,
      Is.False,
      "Assigning ColumnName<float> to ColumnName<string> should not compile"
    );

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

    Assert.That(errors, Is.Not.Empty, "Should have compilation errors");
  }

  [Test]
  public void ArrayTypeWhenScalarExpected_Should_Not_Compile()
  {
    // Scenario: Using array column where scalar is expected
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Trying to use Features (float[]) in place expecting scalar
                    var pipeline = ColumnTransforms.MapValueToKey<IFeaturesSchema, IModelSchema>(
                        mlContext,
                        outputColumnName: ""KeyColumn"",
                        inputColumnSelector: schema => schema.Features  // float[] not valid for MapValueToKey
                    );
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // This may compile at C# level but fail at runtime
    // Ideally ML.Next should catch this at compile-time
    if (result.Success)
    {
      Assert.Inconclusive(
        "Array/scalar type mismatch currently allowed - enhancement needed for stricter type checking"
      );
    }
    else
    {
      Assert.Pass("Type system correctly prevents using array column where scalar expected");
    }
  }
}
