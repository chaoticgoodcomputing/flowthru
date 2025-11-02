using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Errors;

/// <summary>
/// Tests verifying ML.Next catches schema type mismatches at compile-time.
///
/// Common scenario: Engineer chains transformations with incompatible schemas.
/// ML.NET: Compiles fine, runtime error when schemas don't match expected columns.
/// ML.Next: Compilation error - schema types must align.
/// </summary>
[TestFixture]
[Category("CompilationSafety")]
[Category("SchemaMismatch")]
public class SchemaMismatchTests
{
  [Test]
  public void MismatchedSchemaTypes_Should_Not_Compile()
  {
    // Scenario: Trying to chain transformations with incompatible schema types
    var code =
      @"
            using ML.Next.Core.Schema;
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
                    
                    // Create transformer A->B
                    var transformer1 = default(Transformer<ISchemaA, ISchemaB>);
                    
                    // Create transformer C->D (incompatible with previous output)
                    var transformer2 = default(Transformer<ISchemaC, ISchemaD>);
                    
                    // Try to chain them - should not compile!
                    // ISchemaB != ISchemaC
                    var combined = transformer1.Append(transformer2);
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(
      result.Success,
      Is.False,
      "Chaining transformers with mismatched schemas should not compile"
    );
  }

  [Test]
  public void TransformerWithWrongInputSchema_Should_Not_Compile()
  {
    // Scenario: Applying transformer to data with wrong schema type
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Extract;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Create data with IRawSchema
                    var data = default(DataView<IRawSchema>);
                    
                    // Create transformer that expects IFeaturesSchema input
                    var transformer = default(Transformer<IFeaturesSchema, IModelSchema>);
                    
                    // Try to apply transformer to incompatible data - should not compile!
                    var result = transformer.Transform(data);
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(
      result.Success,
      Is.False,
      "Applying transformer to data with wrong schema should not compile"
    );
  }

  [Test]
  public void EstimatorFitWithWrongSchema_Should_Not_Compile()
  {
    // Scenario: Fitting estimator with wrong schema type
    var code =
      @"
            using ML.Next.Core.Schema;
            using ML.Next.Transform;
            using ML.Next.Extract;
            using Microsoft.ML;
            using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

            public class Test {
                public void Execute() {
                    var mlContext = new MLContext();
                    
                    // Create data with IRawSchema
                    var data = default(DataView<IRawSchema>);
                    
                    // Create estimator that expects IFeaturesSchema input
                    var estimator = default(Estimator<IFeaturesSchema, IModelSchema>);
                    
                    // Try to fit with incompatible data - should not compile!
                    var result = estimator.Fit(data);
                }
            }
        ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(
      result.Success,
      Is.False,
      "Fitting estimator with data of wrong schema should not compile"
    );
  }
}
