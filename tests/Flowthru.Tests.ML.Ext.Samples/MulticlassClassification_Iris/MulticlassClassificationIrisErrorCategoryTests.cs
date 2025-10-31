using Flowthru.Tests.Common;
using Microsoft.CodeAnalysis;

namespace Flowthru.Tests.ML.Ext.Samples.MulticlassClassification_Iris;

/// <summary>
/// Comprehensive error category tests for MulticlassClassification_Iris sample.
/// Documents how each error category manifests in multi-stage classification pipelines.
/// Tests establish the standard for what ML.Ext should prevent, including future enhancements.
/// </summary>
[TestFixture]
[Category("Compilation")]
[Category("ErrorCategories")]
public class MulticlassClassificationIrisErrorCategoryTests {

  // ============================================================================
  // ERROR CATEGORY 1: Column Name Typos
  // ML.NET: ❌ Runtime error
  // ML.Ext Current: ⚠️ nameof() helps but not enforced
  // ML.Ext Future: ✅ Expression trees
  // Priority: HIGH
  // ============================================================================

  [Test]
  [Explicit("Documentation test - demonstrates ML.NET behavior, not compilable without full ML.NET setup")]
  public void ColumnNameTypo_InLabelColumn_CompilesWithStringLiteral() {
    // ML.NET string literals allow typos in critical columns like labels
    var code = @"
      using Microsoft.ML;
      using Microsoft.ML.Data;
      
      public class IrisData {
        public string Label { get; set; }
        public float SepalLength { get; set; }
      }
      
      public class Test {
        public void Execute() {
          var mlContext = new MLContext();
          var data = mlContext.Data.LoadFromEnumerable(new IrisData[0]);
          
          // Typo: 'Lable' instead of 'Label' - compiles but fails at runtime
          var pipeline = mlContext.Transforms.Conversion.MapValueToKey(""Label"", ""Lable"");
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.True,
        "String literal typos in label columns compile - runtime error only");
  }

  [Test]
  [Explicit("Documentation test - demonstrates ML.NET behavior, not compilable without full ML.NET setup")]
  public void ColumnNameTypo_InFeatureConcatenation_CompilesWithStringLiteral() {
    // Multiple column names means more opportunities for typos
    var code = @"
      using Microsoft.ML;
      
      public class Test {
        public void Execute() {
          var mlContext = new MLContext();
          var data = mlContext.Data.LoadFromEnumerable(new object[0]);
          
          // Typo in one of four columns - hard to spot, fails at runtime
          var pipeline = mlContext.Transforms.Concatenate(
            ""Features"",
            ""SepalLength"",
            ""SepalWidth"",
            ""PedalLength"",  // TYPO: 'Pedal' instead of 'Petal'
            ""PetalWidth""
          );
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.True,
        "Typo in multi-column concatenation compiles");
  }

  [Test]
  public void ColumnNameTypo_WithNameof_DoesNotCompile() {
    // ML.Ext improvement: nameof() catches typos
    var code = @"
      using Flowthru.ML.Ext.Core.Columns;
      
      public class IrisData {
        public string Label { get; set; }
        public float PetalLength { get; set; }
      }
      
      public class Test {
        public void Execute() {
          var column = ColumnName<float>.From(nameof(IrisData.PedalLength)); // Typo
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False, "nameof() catches typos at compile-time");

    var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    var hasNameofError = errors.Any(e => e.Id == "CS0117");
    Assert.That(hasNameofError, Is.True);
  }

  [Test]
  [Explicit("Future enhancement - expression tree-based column references")]
  public void ColumnNameTypo_WithExpressionTree_WouldNotCompile() {
    // Future: Concatenate(x => new[] { x.SepalLength, x.PedalLength, ... })
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
  public void SchemaMismatch_BetweenLabelKeyAndFeaturize_DoesNotCompile() {
    // Multi-stage classification: Raw -> LabelKey -> Features -> Classified
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      
      public interface IRawSchema : ISchemaDefinition { }
      public interface ILabelKeySchema : ISchemaDefinition { }
      public interface IWrongSchema : ISchemaDefinition { }
      public interface IFeaturesSchema : ISchemaDefinition { }
      
      public class Test {
        public void Execute() {
          var labelKey = new Estimator<IRawSchema, ILabelKeySchema>(null!);
          var featurize = new Estimator<IWrongSchema, IFeaturesSchema>(null!); // Wrong!
          
          // Should NOT compile: ILabelKeySchema != IWrongSchema
          var pipeline = labelKey.Append(featurize);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False,
        "Schema mismatch in multi-stage pipeline prevented at compile-time");
  }

  [Test]
  public void SchemaMismatch_ThreeStageBreak_DoesNotCompile() {
    // Breaking the chain at any point should be caught
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      
      public interface IS1 : ISchemaDefinition { }
      public interface IS2 : ISchemaDefinition { }
      public interface IS3 : ISchemaDefinition { }
      public interface IS4 : ISchemaDefinition { }
      
      public class Test {
        public void Execute() {
          var step1 = new Estimator<IS1, IS2>(null!);
          var step2 = new Estimator<IS2, IS3>(null!);
          var step3 = new Estimator<IS1, IS4>(null!); // Wrong input!
          
          // step1.Append(step2) works, but .Append(step3) should fail
          var pipeline = step1.Append(step2).Append(step3); // IS3 != IS1
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.False,
        "Break in three-stage pipeline caught at compile-time");
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
  public void TypeMismatch_LabelAsFloatInsteadOfKey_CurrentlyNotCaught() {
    // Multiclass classification requires label as key, not raw string/float
    var code = @"
      using Flowthru.ML.Ext.Core.Columns;
      
      public class IrisData {
        public string Label { get; set; }  // Should be converted to key type
      }
      
      public class Test {
        public void Execute() {
          // Declaring label as float when MapValueToKey produces key type
          // This type mismatch not currently caught
          var labelColumn = ColumnName<float>.From(nameof(IrisData.Label));
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.True,
        "Column type mismatches not yet caught - future enhancement");
  }

  [Test]
  [Explicit("Future enhancement - type-level column type encoding")]
  public void TypeMismatch_FeatureVectorAsScalar_WouldNotCompile() {
    // Future: Concatenate produces float[], not float
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      
      // Future: Schema with typed columns
      public interface IFeaturesSchema : ISchemaDefinition {
        // ColumnSpec<float[]> Features { get; }  // Vector type
      }
      
      public class Test {
        public void Execute() {
          // Future: Cannot treat vector column as scalar
          // var scalarColumn = GetColumn<float>(schema.Features); // Type error!
        }
      }
    ";

    Assert.Inconclusive("Type-level column type encoding not yet implemented");
  }

  [Test]
  [Explicit("Future enhancement - key type tracking")]
  public void TypeMismatch_KeyTypeNotTracked_WouldBeEnforced() {
    // Future: MapValueToKey produces KeyDataViewType, should be tracked
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      
      // Future: Key type in schema
      public interface ILabelKeySchema : ISchemaDefinition {
        // ColumnSpec<KeyType<uint, 3>> Label { get; }  // Key with cardinality 3
      }
      
      public interface IRawSchema : ISchemaDefinition {
        // ColumnSpec<string> Label { get; }  // Raw string
      }
      
      public class Test {
        public void Execute() {
          // Future: Cannot use string label where key is required
          // var classifier = SdcaMaximumEntropy<IRawSchema>(); // Type error!
        }
      }
    ";

    Assert.Inconclusive("Key type tracking not yet implemented");
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
  public void MissingTransform_LabelKeySkipped_CurrentlyCompiles() {
    // SDCA requires label as key type, but this isn't enforced
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      using Microsoft.ML;
      
      public interface IRawSchema : ISchemaDefinition { }
      public interface IClassifiedSchema : ISchemaDefinition { }
      
      public class Test {
        public void Execute(MLContext mlContext) {
          var data = new DataView<IRawSchema>(null!);
          
          // Missing: MapValueToKey on label column
          // SDCA expects key type but we're passing raw schema
          var trainer = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
            labelColumnName: ""Label"",
            featureColumnName: ""Features""
          );
          
          var estimator = Estimator<IRawSchema, IClassifiedSchema>.From(trainer);
          var model = estimator.Fit(data); // Would fail at runtime
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.True,
        "Missing required transforms not yet enforced");
  }

  [Test]
  [Explicit("Future enhancement - transform requirements")]
  public void MissingTransform_FeaturizationSkipped_WouldNotCompile() {
    // Future: Classifiers require Features column
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      
      // Future: Schema requirements
      public interface IRawSchema : ISchemaDefinition {
        // Has: SepalLength, SepalWidth, PetalLength, PetalWidth
        // Missing: Features vector
      }
      
      public interface IRequiresFeaturesSchema : ISchemaDefinition {
        // ColumnSpec<float[]> Features { get; }  // REQUIRED
      }
      
      public class Test {
        public void Execute() {
          // Future: Cannot use classifier without Features column
          // var classifier = SdcaMaximumEntropy<IRawSchema>(); // Compile error!
          // Must use: SdcaMaximumEntropy<IRequiresFeaturesSchema>()
        }
      }
    ";

    Assert.Inconclusive("Transform requirements not yet implemented");
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
  public void FeatureColumnError_WrongFeatureColumnName_PartiallyDetected() {
    // Schema tracking provides some guidance but doesn't enforce column names
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      using Microsoft.ML;
      
      public interface IFeaturesSchema : ISchemaDefinition { }
      public interface IClassifiedSchema : ISchemaDefinition { }
      
      public class Test {
        public void Execute(MLContext mlContext) {
          var data = new DataView<IFeaturesSchema>(null!);
          
          // Classifier expects 'Features' but might be called something else
          var trainer = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
            labelColumnName: ""Label"",
            featureColumnName: ""WrongColumnName""  // Not validated
          );
          
          var estimator = Estimator<IFeaturesSchema, IClassifiedSchema>.From(trainer);
          var model = estimator.Fit(data);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.True,
        "Feature column name partially protected by schema tracking");
  }

  [Test]
  [Explicit("Future enhancement - schema column specifications")]
  public void FeatureColumnError_WithSchemaSpec_WouldEnforceColumnExistence() {
    // Future: Schema explicitly declares required columns
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      
      // Future: Schema with column declarations
      public interface IFeaturesSchema : ISchemaDefinition {
        // ColumnSpec<float[]> Features { get; }
        // ColumnSpec<KeyType<uint, 3>> Label { get; }
      }
      
      public interface IMissingFeaturesSchema : ISchemaDefinition {
        // ColumnSpec<KeyType<uint, 3>> Label { get; }
        // (No Features column declared)
      }
      
      public class Test {
        public void Execute() {
          // Future: Cannot use classifier with schema missing Features
          // var classifier = SdcaMaximumEntropy<IMissingFeaturesSchema>();
          // Compile error: IMissingFeaturesSchema does not contain 'Features' column
        }
      }
    ";

    Assert.Inconclusive("Schema column specifications not yet implemented");
  }

  [Test]
  [Explicit("Future enhancement - feature dimension tracking")]
  public void FeatureColumnError_WrongFeatureDimensions_WouldBeValidated() {
    // Future: Track feature vector dimensions in schema
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      
      // Future: Feature dimensions in schema
      public interface IFeatures4D : ISchemaDefinition {
        // ColumnSpec<float[], Dim<4>> Features { get; }  // 4 dimensions
      }
      
      public interface IFeatures3D : ISchemaDefinition {
        // ColumnSpec<float[], Dim<3>> Features { get; }  // 3 dimensions
      }
      
      public class Test {
        public void Execute() {
          // Model trained on 4D features
          var modelFor4D = LoadModel<IFeatures4D>(""model.zip"");
          
          // Future: Cannot use 3D data with 4D model
          var data3D = new DataView<IFeatures3D>(null!);
          // var predictions = modelFor4D.Transform(data3D); // Compile error!
        }
      }
    ";

    Assert.Inconclusive("Feature dimension tracking not yet implemented");
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
  public void ModelVersioning_DifferentLabelEncoding_CurrentlyNotValidated() {
    // Label encoding changed between model versions - not detected
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Load;
      using Microsoft.ML;
      
      public interface IClassificationV1Schema : ISchemaDefinition {
        // V1: Label encoded as 0,1,2 (alphabetical)
      }
      
      public interface IClassificationV2Schema : ISchemaDefinition {
        // V2: Label encoded as 2,1,0 (reverse alphabetical)
      }
      
      public class Test {
        public void Execute(MLContext mlContext) {
          // Model trained with V1 encoding
          var modelPath = ""model-v1.zip"";
          
          // Loading with V2 schema - encoding mismatch not detected
          var model = ModelPersistence.LoadModel<IClassificationV2Schema>(mlContext, modelPath);
          
          // Predictions would use wrong label mapping!
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.True,
        "Model schema versioning not yet enforced");
  }

  [Test]
  [Explicit("Future enhancement - schema evolution tracking")]
  public void ModelVersioning_SchemaEvolution_WouldValidateCompatibility() {
    // Future: Track schema changes and validate compatibility
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      
      // Future: Schema versioning attributes
      [SchemaVersion(""iris-classification-v1"", Hash = ""abc123"")]
      public interface IIrisV1 : ISchemaDefinition {
        // 4 features: Sepal/Petal Length/Width
      }
      
      [SchemaVersion(""iris-classification-v2"", Hash = ""def456"")]
      [CompatibleWith(typeof(IIrisV1))]  // Declares backward compatibility
      public interface IIrisV2 : ISchemaDefinition {
        // 4 features + 1 new derived feature
      }
      
      [SchemaVersion(""iris-classification-v3"", Hash = ""ghi789"")]
      public interface IIrisV3 : ISchemaDefinition {
        // Breaking change: removed PetalWidth, added new features
      }
      
      public class Test {
        public void Execute() {
          // V1 model can load V2 data (compatible)
          var modelV1 = LoadModel<IIrisV1>(""model-v1.zip"");
          var dataV2 = new DataView<IIrisV2>(null!);
          // var predictions = modelV1.Transform(dataV2); // OK
          
          // V1 model CANNOT load V3 data (incompatible)
          var dataV3 = new DataView<IIrisV3>(null!);
          // var predictions2 = modelV1.Transform(dataV3); // Compile error!
        }
      }
    ";

    Assert.Inconclusive("Schema evolution tracking not yet implemented");
  }

  [Test]
  [Explicit("Future enhancement - prediction schema validation")]
  public void ModelVersioning_PredictionSchemaChanged_WouldBeValidated() {
    // Future: Validate prediction output schema matches expectations
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      
      // Future: Output schema specifications
      public interface IThreeClassOutput : ISchemaDefinition {
        // ColumnSpec<string> PredictedLabel { get; }
        // ColumnSpec<float[], Dim<3>> Score { get; }  // 3 classes
      }
      
      public interface IFourClassOutput : ISchemaDefinition {
        // ColumnSpec<string> PredictedLabel { get; }
        // ColumnSpec<float[], Dim<4>> Score { get; }  // 4 classes!
      }
      
      public class Test {
        public void Execute() {
          // Model trained for 3 classes
          var model = LoadModel<IFeaturesSchema, IThreeClassOutput>(""model-3class.zip"");
          
          // Future: Cannot treat as 4-class output
          // Transformer<IFeaturesSchema, IFourClassOutput> wrongModel = model; // Error!
        }
      }
    ";

    Assert.Inconclusive("Prediction schema validation not yet implemented");
  }

  // ============================================================================
  // POSITIVE CONTROLS - Valid Patterns That Should Compile
  // ============================================================================

  [Test]
  [Explicit("Positive control - demonstrates valid multi-stage classification")]
  public void ValidClassification_WithProperSchemaChaining_Compiles() {
    var code = @"
      using Flowthru.ML.Ext.Core.Schema;
      using Flowthru.ML.Ext.Transform;
      
      public interface IRawSchema : ISchemaDefinition { }
      public interface ILabelKeySchema : ISchemaDefinition { }
      public interface IFeaturesSchema : ISchemaDefinition { }
      public interface IClassifiedSchema : ISchemaDefinition { }
      
      public class Test {
        public void Execute() {
          var data = new DataView<IRawSchema>(null!);
          
          var labelKey = new Estimator<IRawSchema, ILabelKeySchema>(null!);
          var featurize = new Estimator<ILabelKeySchema, IFeaturesSchema>(null!);
          var classify = new Estimator<IFeaturesSchema, IClassifiedSchema>(null!);
          
          // Valid three-stage pipeline - all schemas match
          var pipeline = labelKey
            .Append(featurize)
            .Append(classify);
          
          var model = pipeline.Fit(data);
          var predictions = model.Transform(data);
        }
      }
    ";

    var result = CompilationTestHelper.CompileWithMLExt(code);

    Assert.That(result.Success, Is.True,
        "Valid multi-stage classification pipeline should compile");
  }
}
