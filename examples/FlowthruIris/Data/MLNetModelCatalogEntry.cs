using Flowthru.Data;
using Flowthru.Data.Validation;
using FlowthruIris.Pipelines.Analysis.Nodes;
using LanguageExt;
using Microsoft.ML;
using Microsoft.ML.Data;
using static LanguageExt.Prelude;

namespace FlowthruIris.Data;

/// <summary>
/// Custom catalog entry for persisting ML.NET ITransformer models to disk.
/// 
/// <para><strong>ML.NET Model Serialization:</strong></para>
/// <para>
/// ML.NET provides native model serialization through MLContext.Model.Save() and Load().
/// Models are saved as .zip files containing the trained pipeline and metadata.
/// This catalog entry wraps that functionality to integrate with Flowthru's catalog system.
/// </para>
/// 
/// <para><strong>Schema Requirement:</strong></para>
/// <para>
/// ML.NET's Save() method requires the original training data schema. This entry
/// caches the schema during the first Save() call and reuses it for subsequent saves.
/// The schema is serialized alongside the model in the .zip file.
/// </para>
/// 
/// <para><strong>Multi-Output Pattern:</strong></para>
/// <para>
/// This entry stores ModelWithSchema which bundles the ITransformer with its DataViewSchema.
/// For ML.NET workflows that produce both a model and metrics, use a multi-output node:
/// </para>
/// <code>
/// // Training node returns (ModelWithSchema, Metrics)
/// var model = pipeline.Fit(dataView);
/// var metrics = mlContext.MulticlassClassification.Evaluate(predictions);
/// return (new ModelWithSchema(model, dataView.Schema), metrics);
/// 
/// // Catalog entries
/// public ICatalogEntry&lt;ModelWithSchema&gt; SdcaModel => ...MLNetModelCatalogEntry...
/// public ICatalogEntry&lt;MulticlassClassificationMetrics&gt; SdcaMetrics => ...JsonCatalogEntry...
/// 
/// // Pipeline wiring
/// pipeline.AddNode&lt;TrainSdcaModelNode&gt;(
///     input: processedData,
///     output: (catalog.SdcaModel, catalog.SdcaMetrics),
///     label: "TrainSdca"
/// );
/// </code>
/// 
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// public ICatalogEntry&lt;ModelWithSchema&gt; MyModel =>
///     GetOrCreateEntry(() =>
///         new MLNetModelCatalogEntry(
///             key: "my_model",
///             modelPath: "Models/my_model.zip"
///         )
///     );
/// </code>
/// 
/// <para><strong>Limitations:</strong></para>
/// <list type="bullet">
/// <item>Requires an MLContext instance for Load/Save operations</item>
/// <item>Schema must be available during first Save (captured from training data)</item>
/// <item>Not suitable for streaming scenarios (loads entire model into memory)</item>
/// </list>
/// 
/// <para><strong>Reference:</strong></para>
/// <para>
/// Model persistence pattern adapted from ML.NET official samples:
/// https://github.com/dotnet/machinelearning-samples
/// See: samples/csharp/getting-started/MulticlassClassification_Iris
/// </para>
/// </summary>
public class MLNetModelCatalogEntry : CatalogEntryBase<ModelWithSchema> {
  private readonly string _modelPath;
  private readonly MLContext _mlContext;
  private DataViewSchema? _cachedSchema;

  /// <summary>
  /// Initializes a new ML.NET model catalog entry.
  /// </summary>
  /// <param name="key">Unique identifier for this catalog entry</param>
  /// <param name="modelPath">Path to the .zip file for the model</param>
  /// <param name="mlContext">ML.NET context (optional, creates new if null)</param>
  public MLNetModelCatalogEntry(
      string key,
      string modelPath,
      MLContext? mlContext = null)
      : base(key) {
    _modelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
    _mlContext = mlContext ?? new MLContext();
  }

  /// <summary>
  /// Loads the ML.NET model from disk.
  /// </summary>
  public override Aff<ITransformer> Load() {
    return Aff(() => {
      if (!File.Exists(_modelPath)) {
        throw new FileNotFoundException(
            $"Model file not found for catalog entry '{Key}'", _modelPath);
      }

      try {
        // Load the ML.NET model
        var model = _mlContext.Model.Load(_modelPath, out var schema);
        _cachedSchema = schema; // Cache for future saves

        return model;
      } catch (Exception ex) when (ex is not FileNotFoundException) {
        throw new InvalidOperationException(
            $"Failed to load ML.NET model for catalog entry '{Key}'", ex);
      }
    });
  }

  /// <summary>
  /// Saves the ML.NET model to disk.
  /// 
  /// Note: The first save must include training data to extract the schema.
  /// The schema is extracted from the ITransformer if it implements ITransformerChainAccessor,
  /// or you must call SetSchema() before Save().
  /// </summary>
  public override Aff<Unit> Save(ITransformer model) {
    return Aff(() => {
      if (model == null) {
        throw new ArgumentNullException(nameof(model),
            $"Cannot save null model to catalog entry '{Key}'");
      }

      // Ensure directory exists
      var modelDir = Path.GetDirectoryName(_modelPath);
      if (!string.IsNullOrEmpty(modelDir) && !Directory.Exists(modelDir)) {
        Directory.CreateDirectory(modelDir);
      }

      // Try to get schema from the model if it's a transformer chain
      if (_cachedSchema == null) {
        // Attempt to extract schema from model
        // For ML.NET pipelines, we need the source data schema
        // This is a limitation - we require SetSchema() to be called first
        throw new InvalidOperationException(
            $"Cannot save model for '{Key}': schema not available. " +
            "The schema must be provided before first Save(). " +
            "Consider using SaveWithSchema() or call SetSchema() first.");
      }

      try {
        // Save the ML.NET model
        _mlContext.Model.Save(model, _cachedSchema, _modelPath);

        return unit;
      } catch (Exception ex) {
        throw new InvalidOperationException(
            $"Failed to save ML.NET model for catalog entry '{Key}'", ex);
      }
    });
  }

  /// <summary>
  /// Saves the ML.NET model with an explicit schema.
  /// This is the preferred method for initial saves.
  /// </summary>
  /// <param name="model">The trained model</param>
  /// <param name="schema">Schema from the training data</param>
  public Aff<Unit> SaveWithSchema(ITransformer model, DataViewSchema schema) {
    return Aff(() => {
      _cachedSchema = schema ?? throw new ArgumentNullException(nameof(schema));
      return unit;
    }).Bind(_ => Save(model));
  }

  /// <summary>
  /// Sets the schema for model serialization.
  /// Must be called before first Save() if model hasn't been loaded yet.
  /// </summary>
  /// <param name="schema">Data schema from training data (IDataView.Schema)</param>
  public void SetSchema(DataViewSchema schema) {
    _cachedSchema = schema ?? throw new ArgumentNullException(nameof(schema));
  }

  /// <summary>
  /// Sets the DataViewSchema for future Save operations.
  /// Must be called before the first Save() if the model hasn't been loaded yet.
  /// </summary>
  /// <param name="schema">Schema from the training data</param>
  public void SetSchema(DataViewSchema schema) {
    _cachedSchema = schema ?? throw new ArgumentNullException(nameof(schema));
  }

  /// <summary>
  /// Checks if the model file exists.
  /// </summary>
  public override Aff<bool> Exists() {
    return Aff(() => File.Exists(_modelPath));
  }

  /// <summary>
  /// Shallow inspection: validates model file exists and has correct extension.
  /// </summary>
  public override Aff<ValidationResult> InspectShallow(int sampleSize = 100) {
    return Aff(() => {
      var errors = new List<ValidationError>();

      // Check model file
      if (!File.Exists(_modelPath)) {
        errors.Add(new ValidationError(
            Key,
            ValidationErrorType.NotFound,
            $"Model file not found: {_modelPath}"));
      } else if (!_modelPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) {
        errors.Add(new ValidationError(
            Key,
            ValidationErrorType.TypeMismatch,
            $"Model file should have .zip extension: {_modelPath}",
            "ML.NET models are stored as .zip archives"));
      }

      return errors.Count > 0
          ? new ValidationResult(errors)
          : new ValidationResult(); // Success
    });
  }

  /// <summary>
  /// Deep inspection: attempts to load the model.
  /// </summary>
  public override Aff<ValidationResult> InspectDeep() {
    return Aff(async () => {
      try {
        // Attempt to load - this validates the file is readable and valid
        var result = await Load().Run();
        return result.Match(
            Succ: _ => new ValidationResult(), // Success
            Fail: ex => new ValidationResult(new[] {
                        new ValidationError(
                            Key,
                            ValidationErrorType.InspectionFailure,
                            "Failed to load ML.NET model",
                            ex.Message)
            }));
      } catch (Exception ex) {
        return new ValidationResult(new[] {
                    new ValidationError(
                        Key,
                        ValidationErrorType.InspectionFailure,
                        "Deep inspection failed",
                        ex.Message)
                });
      }
    });
  }
}
