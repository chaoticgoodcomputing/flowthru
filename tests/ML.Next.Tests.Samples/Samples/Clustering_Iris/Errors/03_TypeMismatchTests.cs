using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace ML.Next.Tests.Samples.Samples.Clustering_Iris.Errors;

/// <summary>
/// Tests verifying that ML.Next catches type mismatches at compile-time.
/// 
/// Common scenario: Using wrong column types in transformations.
/// ML.NET: Compiles, fails at runtime with cryptic type errors.
/// ML.Next: Compilation error - type system catches mismatches.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("TypeMismatch")]
public class TypeMismatchTests
{
    [Test]
    public void WrongColumnType_In_Schema_Definition()
    {
        // Scenario: Schema declares float but code expects string
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;
            using Microsoft.ML;

            public interface IMySchema : ISchemaDefinition {
                ColumnName<float> NumericColumn { get; }
            }

            public class Test {
                public void Execute() {
                    // Trying to use ColumnName<string> where ColumnName<float> is expected
                    ColumnName<string> wrongType = ""NumericColumn"";
                    ColumnName<float> correctType = wrongType;  // Should not compile
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Assigning ColumnName<string> to ColumnName<float> should not compile");

        var errors = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.That(errors, Is.Not.Empty, "Should have type mismatch errors");
    }

    [Test]
    public void VectorColumn_Used_As_Scalar()
    {
        // Scenario: Trying to use a vector column (float[]) as if it were scalar (float)
        var code = @"
            using ML.Next.Core.Schema;
            using ML.Next.Core.Columns;
            using Microsoft.ML;

            public interface IMySchema : ISchemaDefinition {
                ColumnName<float[]> VectorColumn { get; }
                ColumnName<float> ScalarColumn { get; }
            }

            public class Test {
                public void Execute() {
                    // Trying to assign vector type to scalar type
                    ColumnName<float> scalar = default(ColumnName<float[]>);  // Should not compile
                }
            }
        ";

        var result = CompilationTestHelper.CompileWithMLExt(code);

        Assert.That(result.Success, Is.False,
            "Assigning ColumnName<float[]> to ColumnName<float> should not compile");
    }
}
