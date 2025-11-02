using Flowthru.Nodes;
using FlowthruIris.Data.Schemas;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FlowthruIris.Pipelines.Analysis.Nodes;

/// <summary>
/// Trains a simple multiclass classification model using SDCA Maximum Entropy algorithm.
///
/// <para><strong>Algorithm: SDCA (Stochastic Dual Coordinate Ascent)</strong></para>
/// <para>
/// - Linear model (logistic regression for multiclass)
/// - Fast training (&lt;1 second on Iris dataset)
/// - Deterministic results (same training data → same model)
/// - Interpretable (feature weights can be inspected)
/// - Good baseline performance (~95% accuracy on Iris)
/// </para>
///
/// <para><strong>ML.NET Pipeline Construction</strong></para>
/// <para>
/// This node demonstrates ML.NET best practices:
/// 1. Load data into IDataView
/// 2. Build feature engineering pipeline (concatenation, label mapping)
/// 3. Add caching for efficient training
/// 4. Train model with SDCA
/// 5. Evaluate on same data (descriptive analysis pattern)
/// </para>
///
/// <para><strong>Descriptive Analysis (No Train/Test Split)</strong></para>
/// <para>
/// Since this is a descriptive analysis (understanding the data),
/// we train and evaluate on the full dataset. For predictive modeling,
/// you would split into training and test sets.
/// </para>
///
/// <para><strong>Reference:</strong></para>
/// <para>
/// Training strategy adapted from ML.NET official samples:
/// https://github.com/dotnet/machinelearning-samples
/// Specifically: samples/csharp/getting-started/MulticlassClassification_Iris
/// </para>
/// </summary>
public class TrainSdcaModelNode
  : NodeBase<IEnumerable<IrisSchema>, (ITransformer, MulticlassClassificationMetrics)>
{
  protected override Task<(ITransformer, MulticlassClassificationMetrics)> Transform(
    IEnumerable<IrisSchema> input
  )
  {
    Logger?.LogInformation("Training SDCA Maximum Entropy model");

    var mlContext = new MLContext(seed: 42); // Fixed seed for reproducibility

    // Convert to ML.NET IDataView
    var dataView = mlContext.Data.LoadFromEnumerable(input);

    // Build the ML.NET pipeline
    var pipeline = mlContext
      .Transforms
      // Concatenate all numeric features into a single "Features" column
      .Concatenate(
        "Features",
        nameof(IrisSchema.SepalLength),
        nameof(IrisSchema.SepalWidth),
        nameof(IrisSchema.PetalLength),
        nameof(IrisSchema.PetalWidth),
        nameof(IrisSchema.PetalRatio),
        nameof(IrisSchema.SepalRatio)
      )
      // Convert text species labels to numeric keys (required by ML.NET)
      .Append(mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(IrisSchema.Species)))
      // Cache data in memory for efficient random access during training
      .AppendCacheCheckpoint(mlContext)
      // Train SDCA Maximum Entropy multiclass classifier
      .Append(
        mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
          labelColumnName: "Label",
          featureColumnName: "Features"
        )
      )
      // Convert predicted label keys back to species names
      .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

    // Train the model
    Logger?.LogInformation("Fitting SDCA model to {Count} samples", input.Count());
    var model = pipeline.Fit(dataView);
    Logger?.LogInformation("SDCA model training complete");

    // Evaluate the model on the same data (descriptive analysis)
    var predictions = model.Transform(dataView);
    var metrics = mlContext.MulticlassClassification.Evaluate(
      predictions,
      labelColumnName: "Label"
    );

    Logger?.LogInformation(
      "SDCA Model Performance - MicroAccuracy: {Accuracy:P2}, MacroAccuracy: {MacroAccuracy:P2}, LogLoss: {LogLoss:F4}",
      metrics.MicroAccuracy,
      metrics.MacroAccuracy,
      metrics.LogLoss
    );

    return Task.FromResult(((ITransformer)model, metrics));
  }
}
