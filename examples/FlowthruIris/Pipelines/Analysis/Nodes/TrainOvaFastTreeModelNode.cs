using Flowthru.Nodes;
using FlowthruIris.Data.Schemas;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FlowthruIris.Pipelines.Analysis.Nodes;

/// <summary>
/// Trains an advanced multiclass classifier using OneVersusAll with Averaged Perceptron.
///
/// <para><strong>Algorithm:</strong></para>
/// <para>
/// OneVersusAll (OVA) meta-algorithm trains N binary classifiers (one per class)
/// and combines their predictions. Each binary classifier uses Averaged Perceptron,
/// a linear online learning algorithm that can handle non-linearly separable data
/// better than basic linear models.
/// </para>
///
/// <para><strong>When to Use:</strong></para>
/// <list type="bullet">
/// <item>Multi-class problems where classes may not be linearly separable</item>
/// <item>Moderate to large datasets (efficient online learning)</item>
/// <item>When you want more flexible decision boundaries than SDCA</item>
/// <item>When you need a more sophisticated model as comparison to simple linear</item>
/// </list>
///
/// <para><strong>Model Output:</strong></para>
/// <para>
/// Returns a tuple of (ITransformer, MulticlassClassificationMetrics) containing
/// the trained ensemble model and its evaluation metrics on the training set.
/// </para>
///
/// <para><strong>Reference:</strong></para>
/// <para>
/// Training strategy adapted from ML.NET official samples:
/// https://github.com/dotnet/machinelearning-samples
/// Specifically: samples/csharp/getting-started/BinaryClassification_SpamDetection
/// and samples/csharp/end-to-end-apps/MulticlassClassification-GitHubLabeler
/// </para>
/// </summary>
public class TrainOvaPerceptronModelNode
  : NodeBase<IEnumerable<IrisSchema>, (ITransformer, MulticlassClassificationMetrics)>
{
  protected override Task<(ITransformer, MulticlassClassificationMetrics)> Transform(
    IEnumerable<IrisSchema> input
  )
  {
    Logger?.LogInformation("Training OneVersusAll + Averaged Perceptron model");

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
      // Convert text species labels to numeric keys
      .Append(mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(IrisSchema.Species)))
      // Cache data in memory for efficient training
      .AppendCacheCheckpoint(mlContext)
      // Train OneVersusAll with Averaged Perceptron as the binary classifier
      .Append(
        mlContext.MulticlassClassification.Trainers.OneVersusAll(
          binaryEstimator: mlContext.BinaryClassification.Trainers.AveragedPerceptron(
            labelColumnName: "Label",
            featureColumnName: "Features",
            numberOfIterations: 10 // Moderate iterations for small dataset
          ),
          labelColumnName: "Label"
        )
      )
      // Convert predicted label keys back to species names
      .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

    // Train the model
    Logger?.LogInformation(
      "Fitting OVA + Averaged Perceptron model to {Count} samples",
      input.Count()
    );
    var model = pipeline.Fit(dataView);
    Logger?.LogInformation("OVA + Averaged Perceptron model training complete");

    // Evaluate the model on the same data (descriptive analysis)
    var predictions = model.Transform(dataView);
    var metrics = mlContext.MulticlassClassification.Evaluate(
      predictions,
      labelColumnName: "Label"
    );

    Logger?.LogInformation(
      "OVA + FastForest Performance - MicroAccuracy: {Accuracy:P2}, MacroAccuracy: {MacroAccuracy:P2}, LogLoss: {LogLoss:F4}",
      metrics.MicroAccuracy,
      metrics.MacroAccuracy,
      metrics.LogLoss
    );

    return Task.FromResult(((ITransformer)model, metrics));
  }
}
