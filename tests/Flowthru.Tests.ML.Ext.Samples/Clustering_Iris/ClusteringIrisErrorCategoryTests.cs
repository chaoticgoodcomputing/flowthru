using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace Flowthru.Tests.ML.Ext.Samples.Clustering_Iris;

/// <summary>
/// Comprehensive error category tests for Clustering_Iris sample.
/// Documents how each error category from the ML.NET/ML.Ext comparison manifests in clustering workflows.
/// Tests are organized by error category with current ML.Ext status noted.
/// </summary>
[TestFixture]
[Category("Compilation")]
[Category("ErrorCategories")]
public class ClusteringIrisErrorCategoryTests {

  // ============================================================================
  // ERROR CATEGORY 1: Column Name Typos
  // ML.NET: ❌ Runtime error
  // ML.Ext Current: ⚠️ nameof() helps but not enforced
  // ML.Ext Future: ✅ Expression trees
  // Priority: HIGH
  // ============================================================================

  [Test]
  [Explicit("Documentation test - demonstrates ML.NET behavior, not compilable without full ML.NET setup")]
  public void ColumnNameTypo_WithStringLiteral_CompilesButFailsAtRuntime() {
    // Current ML.NET behavior: String literals allow typos that fail at runtime
    var code = @"
      using Microsoft.ML;
      using Microsoft.ML.Data;
      
      public class IrisData {
        public float SepalLength { get; set; }
        public float SepalWidth { get; set; }
        public float PetalLength { get; set; }
        public float PetalWidth { get; set; }
      }
      
      public class Test {
        public void Execute() {
          var mlContext = new MLContext();
          var data = mlContext.Data.LoadFromEnumerable(new IrisData[0]);
          
          // Typo: 'SepalLenght' instead of 'SepalLength' - compiles but fails at runtime!
          var pipeline = mlContext.Transforms.Concatenate(
            ""Features"",
            ""SepalLenght"",  // TYPO HERE
            ""SepalWidth"",
            ""PetalLength"",
            ""PetalWidth""
          );
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // This DOES compile (unfortunately) - string literals aren't type-checked
    Assert.That(result.Success, Is.True,
        "String literal column names compile even with typos - runtime error only");
  }

  [Test]
  public void ColumnNameTypo_WithNameof_DoesNotCompile() {
    // ML.Ext improvement: Using nameof() catches typos at compile-time
    var code = @"
      using Flowthru.ML.Ext.Core.Columns;
      
      public class IrisData {
        public float SepalLength { get; set; }
        public float SepalWidth { get; set; }
        public float PetalLength { get; set; }
        public float PetalWidth { get; set; }
      }
      
      public class Test {
        public void Execute() {
          // Using nameof with typo - should NOT compile
          var columnName = ColumnName<float>.From(nameof(IrisData.SepalLenght)); // Typo
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "nameof() catches typos at compile-time");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasNameofError = errors.Any(e => e.Id == "CS0117"); // Member not found
    Assert.That(hasNameofError, Is.True, "Should have 'member not found' error");
  }

  [Test]
  [Explicit("Future enhancement - expression tree-based column references")]
  public void ColumnNameTypo_WithExpressionTree_WouldNotCompile() {
    // Future ML.Ext: Expression trees for fully type-safe column references
    var code = @"
      using Flowthru.ML.Ext.Transform;
      
      public class IrisData {
        public float SepalLength { get; set; }
        public float SepalWidth { get; set; }
      }
      
      public class Test {
        public void Execute() {
          // Future API: Concatenate(x => x.SepalLenght, ...) would not compile
          // Current ML.Ext doesn't have this yet
        }
      }
    ";

    // Mark as expected to fail when implemented
    Assert.Inconclusive("Expression tree-based column API not yet implemented");
  }

  // ============================================================================
  // ERROR CATEGORY 2: Schema Mismatches
  // ML.NET: ❌ Runtime error
  // ML.Ext Current: ✅ Compile-time (WORKING!)
  // ML.Ext Future: ✅ Compile-time
  // Priority: CRITICAL
  // ============================================================================

  [Test]
  public void SchemaMismatch_InPipelineComposition_DoesNotCompile() {
    // ML.Ext CURRENT SUCCESS: Schema tracking prevents mismatches
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      
      public interface ISchema1 : ISchemaDefinition { }
      public interface ISchema2 : ISchemaDefinition { }
      public interface ISchema3 : ISchemaDefinition { }
      
      public class Test {
        public void Execute() {
          var transform1 = new Transformer<ISchema1, ISchema2>(null!);
          var transform2 = new Transformer<ISchema3, ISchema1>(null!); // Wrong input schema
          
          // This should NOT compile: ISchema2 != ISchema3
          var pipeline = transform1.Append(transform2);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Schema mismatch prevented at compile-time");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    Assert.That(errors, Is.Not.Empty, "Should have type inference or mismatch errors");
  }

  [Test]
  public void SchemaMismatch_BetweenDataAndTransformer_DoesNotCompile() {
    // ML.Ext prevents applying transformer to wrong schema
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      
      public interface IRawSchema : ISchemaDefinition { }
      public interface IProcessedSchema : ISchemaDefinition { }
      public interface IWrongSchema : ISchemaDefinition { }
      
      public class Test {
        public void Execute() {
          var data = new DataView<IRawSchema>(null!);
          var transformer = new Transformer<IWrongSchema, IProcessedSchema>(null!);
          
          // Should NOT compile: IRawSchema != IWrongSchema
          var result = transformer.Transform(data);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "Cannot apply transformer to wrong schema");
  }

  // ============================================================================
  // ERROR CATEGORY 3: Type Mismatches
  // ML.NET: ❌ Runtime error
  // ML.Ext Current: ❌ Runtime (not yet caught)
  // ML.Ext Future: ✅ Type-level encoding
  // Priority: HIGH
  // ============================================================================

  [Test]
  [Explicit("Future enhancement - type-level column type encoding")]
  public void TypeMismatch_FloatColumnAsString_CurrentlyCompiles() {
    // Current limitation: Column data types not tracked at type level
    var code = @"
      using Flowthru.ML.Ext.Core.Columns;
      
      public class IrisData {
        public float SepalLength { get; set; }  // Actually a float
      }
      
      public class Test {
        public void Execute() {
          // Declaring as string when it's actually float - currently not caught
          var columnName = ColumnName<string>.From(nameof(IrisData.SepalLength));
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Currently DOES compile (type info not encoded in schema yet)
    Assert.That(result.Success, Is.True,
        "Type mismatches not yet caught - future enhancement needed");
  }

  [Test]
  [Explicit("Future enhancement - type-level column type encoding")]
  public void TypeMismatch_ConcatenatingIncompatibleTypes_WouldNotCompile() {
    // Future: Schema would encode column types, preventing incompatible concatenation
    var code = @"
      using Flowthru.ML.Ext.Transform;
      
      public interface ISchema : ISchemaDefinition {
        // Future: Schema would declare column types
        // ColumnSpec<float> SepalLength { get; }
        // ColumnSpec<string> Label { get; }
      }
      
      public class Test {
        public void Execute() {
          // Future: Trying to concatenate float and string columns would not compile
          // var concat = Concatenate(schema => schema.SepalLength, schema => schema.Label);
        }
      }
    ";

    Assert.Inconclusive("Type-level column type encoding not yet implemented");
  }

  // ============================================================================
  // ERROR CATEGORY 4: Missing Transforms
  // ML.NET: ❌ Runtime error
  // ML.Ext Current: ❌ Runtime (not enforced)
  // ML.Ext Future: ✅ Required transform chains
  // Priority: MEDIUM
  // ============================================================================

  [Test]
  [Explicit("Future enhancement - required transform chains")]
  public void MissingTransform_FeaturizationSkipped_CurrentlyCompiles() {
    // K-Means requires numeric features, but this isn't enforced
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      using Microsoft.ML;
      
      public interface IRawSchema : ISchemaDefinition { }
      public interface IClusteredSchema : ISchemaDefinition { }
      
      public class Test {
        public void Execute(MLContext mlContext) {
          var data = new DataView<IRawSchema>(null!);
          
          // Missing: Feature concatenation/normalization before clustering
          // Currently compiles but would fail at runtime
          var trainer = mlContext.Clustering.Trainers.KMeans(""Features"", numberOfClusters: 3);
          var estimator = Estimator<IRawSchema, IClusteredSchema>.From(trainer);
          
          var model = estimator.Fit(data);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Currently compiles - transform requirements not encoded
    Assert.That(result.Success, Is.True,
        "Missing transform requirements not yet enforced");
  }

  [Test]
  [Explicit("Future enhancement - schema type constraints")]
  public void MissingTransform_RequiredColumnMissing_WouldNotCompile() {
    // Future: Schema constraints would require specific columns
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      
      // Future: Schema with required columns
      public interface IRawSchema : ISchemaDefinition { }
      
      public interface IFeaturizedSchema : ISchemaDefinition {
        // Future: Require 'Features' column of type float[]
        // ColumnSpec<float[]> Features { get; }
      }
      
      public class Test {
        public void Execute() {
          // Future: Cannot create IFeaturizedSchema without Features column
          var data = new DataView<IFeaturizedSchema>(null!);
        }
      }
    ";

    Assert.Inconclusive("Schema column requirements not yet implemented");
  }

  // ============================================================================
  // ERROR CATEGORY 5: Feature Column Errors
  // ML.NET: ❌ Runtime error
  // ML.Ext Current: ⚠️ Partial (schema tracking helps)
  // ML.Ext Future: ✅ Schema-aware with column specifications
  // Priority: MEDIUM
  // ============================================================================

  [Test]
  [Explicit("Current partial protection - schema helps but not complete")]
  public void FeatureColumnError_WrongColumnForClustering_PartiallyDetected() {
    // ML.Ext schema tracking provides some protection
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      using Microsoft.ML;
      
      public interface ISchemaWithFeatures : ISchemaDefinition { }
      public interface ISchemaWithWrongColumn : ISchemaDefinition { }
      
      public class Test {
        public void Execute(MLContext mlContext) {
          var data = new DataView<ISchemaWithWrongColumn>(null!);
          
          // K-Means expects 'Features' but schema doesn't guarantee it exists
          var trainer = mlContext.Clustering.Trainers.KMeans(""Features"", numberOfClusters: 3);
          var estimator = Estimator<ISchemaWithWrongColumn, ISchemaWithFeatures>.From(trainer);
          
          // Compiles but schema transition is undocumented
          var model = estimator.Fit(data);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Currently compiles - column existence not enforced
    Assert.That(result.Success, Is.True,
        "Feature column existence partially protected by schema tracking");
  }

  [Test]
  [Explicit("Future enhancement - schema column specifications")]
  public void FeatureColumnError_WithSchemaSpec_WouldNotCompile() {
    // Future: Schema specifications would declare exact columns
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      
      // Future: Schema with column declarations
      public interface ISchemaWithFeatures : ISchemaDefinition {
        // ColumnSpec<float[]> Features { get; }
      }
      
      public interface ISchemaWithoutFeatures : ISchemaDefinition {
        // ColumnSpec<float> SepalLength { get; }
        // (no Features column)
      }
      
      public class Test {
        public void Execute() {
          // Future: Cannot use KMeans with schema that lacks Features column
          // var trainer = KMeans<ISchemaWithoutFeatures>(numberOfClusters: 3);
        }
      }
    ";

    Assert.Inconclusive("Schema column specifications not yet implemented");
  }

  // ============================================================================
  // ERROR CATEGORY 6: Model Versioning
  // ML.NET: ❌ Runtime error when loading mismatched models
  // ML.Ext Current: ⚠️ Documented but not enforced
  // ML.Ext Future: ✅ Schema validation on model load
  // Priority: MEDIUM
  // ============================================================================

  [Test]
  [Explicit("Future enhancement - model schema validation")]
  public void ModelVersioning_SchemaMismatchOnLoad_CurrentlyNotEnforced() {
    // Current: No compile-time or load-time schema validation
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Load;
      using Microsoft.ML;
      
      public interface IOriginalSchema : ISchemaDefinition { }
      public interface IDifferentSchema : ISchemaDefinition { }
      
      public class Test {
        public void Execute(MLContext mlContext) {
          // Model was trained with IOriginalSchema
          var modelPath = ""model.zip"";
          
          // Trying to load with different schema - currently not caught
          var result = ModelPersistence.LoadModel<IDifferentSchema>(mlContext, modelPath);
          
          // Would fail at prediction time, not load time
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    // Currently compiles - schema validation not implemented
    Assert.That(result.Success, Is.True,
        "Model schema versioning not yet enforced");
  }

  [Test]
  [Explicit("Future enhancement - schema hash verification")]
  public void ModelVersioning_WithSchemaHash_WouldValidateAtLoadTime() {
    // Future: Models would store schema hash, validated on load
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Load;
      
      // Future: Schema with hash for versioning
      [SchemaVersion(""clustering-v1"", Hash = ""abc123"")]
      public interface IClusteringSchemaV1 : ISchemaDefinition { }
      
      [SchemaVersion(""clustering-v2"", Hash = ""def456"")]
      public interface IClusteringSchemaV2 : ISchemaDefinition { }
      
      public class Test {
        public void Execute() {
          // Future: LoadModel would verify schema hash matches
          // var result = ModelPersistence.LoadModel<IClusteringSchemaV2>(""model-v1.zip"");
          // Would return Fin.Fail with schema mismatch error
        }
      }
    ";

    Assert.Inconclusive("Schema versioning/hashing not yet implemented");
  }

  // ============================================================================
  // POSITIVE CONTROLS - Valid Patterns That Should Compile
  // ============================================================================

  [Test]
  [Explicit("Positive control - demonstrates valid usage")]
  public void ValidClustering_WithProperSchemaTracking_Compiles() {
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      using Flowthru.ML.Ext.Extract;
      using LanguageExt;
      
      public interface IRawSchema : ISchemaDefinition { }
      public interface IFeaturesSchema : ISchemaDefinition { }
      public interface IClusteredSchema : ISchemaDefinition { }
      
      public class Test {
        public void Execute() {
          var data = new DataView<IRawSchema>(null!);
          
          var concatenate = new Estimator<IRawSchema, IFeaturesSchema>(null!);
          var cluster = new Estimator<IFeaturesSchema, IClusteredSchema>(null!);
          
          // Valid pipeline - schemas match correctly
          var pipeline = concatenate.Append(cluster);
          var model = pipeline.Fit(data);
          var predictions = model.Transform(data);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.True,
        "Valid pipeline with proper schema tracking should compile");
  }
}
