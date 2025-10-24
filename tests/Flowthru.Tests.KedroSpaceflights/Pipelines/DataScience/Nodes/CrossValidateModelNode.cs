using Flowthru.Nodes;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Parameters for cross-validation analysis
/// </summary>
public record CrossValidationParams {
  /// <summary>
  /// Number of folds for k-fold cross-validation
  /// </summary>
  public int NumFolds { get; init; }

  /// <summary>
  /// Base random seed for reproducibility
  /// </summary>
  public int BaseSeed { get; init; }

  /// <summary>
  /// Kedro's reference R² score for comparison.
  /// 
  /// As an additional clarification, this metric is NOT meant to determine if our pipeline "beats"
  /// Kedro's implementation. Rather, it serves as a benchmark to ensure our implementation is
  /// comfortably close to Kedro's original spaceflights example, indicating that our data processing
  /// and modeling steps are correctly aligned.
  /// </summary>
  public float KedroReferenceR2Score { get; init; }
}

/// <summary>
/// Performs k-fold cross-validation to generate R² distribution.
/// Helps understand model stability and variance across different train/test splits.
/// 
/// This node runs the entire train/evaluate cycle multiple times with different
/// random seeds to measure the natural variance in model performance.
/// </summary>
public class CrossValidateModelNode : NodeBase<ModelInputSchema, CrossValidationResults, CrossValidationParams> {
  protected override Task<IEnumerable<CrossValidationResults>> Transform(
      IEnumerable<ModelInputSchema> input) {
    var data = input.ToList();
    Logger?.LogInformation("Starting cross-validation with {Folds} folds", Parameters.NumFolds);

    // Convert to feature rows
    var featureRows = data.Select(row => new FeatureRow {
      Engines = (float)row.Engines,
      PassengerCapacity = (float)row.PassengerCapacity,
      Crew = (float)row.Crew,
      DCheckComplete = row.DCheckComplete,
      IataApproved = row.IataApproved,
      CompanyRating = (float)row.CompanyRating,
      ReviewScoresRating = (float)row.ReviewScoresRating,
      Price = (float)row.Price
    }).ToList();

    var mlContext = new MLContext(seed: Parameters.BaseSeed);
    var allData = mlContext.Data.LoadFromEnumerable(featureRows);

    // Define the ML pipeline (same as TrainModelNode)
    var pipeline = mlContext.Transforms.CopyColumns(
            outputColumnName: "Label",
            inputColumnName: nameof(FeatureRow.Price))
        .Append(mlContext.Transforms.Categorical.OneHotEncoding(
            outputColumnName: "DCheckCompleteEncoded",
            inputColumnName: nameof(FeatureRow.DCheckComplete)))
        .Append(mlContext.Transforms.Categorical.OneHotEncoding(
            outputColumnName: "IataApprovedEncoded",
            inputColumnName: nameof(FeatureRow.IataApproved)))
        .Append(mlContext.Transforms.Concatenate(
            "Features",
            nameof(FeatureRow.Engines),
            nameof(FeatureRow.PassengerCapacity),
            nameof(FeatureRow.Crew),
            "DCheckCompleteEncoded",
            "IataApprovedEncoded",
            nameof(FeatureRow.CompanyRating),
            nameof(FeatureRow.ReviewScoresRating)))
        .Append(mlContext.Transforms.NormalizeMinMax("Features"))
        .Append(mlContext.Regression.Trainers.OnlineGradientDescent(
            labelColumnName: "Label",
            featureColumnName: "Features",
            numberOfIterations: 1000));

    // Perform cross-validation
    var cvResults = mlContext.Regression.CrossValidate(
        allData,
        pipeline,
        numberOfFolds: Parameters.NumFolds,
        labelColumnName: "Label");

    // Extract metrics from each fold
    var foldMetrics = cvResults.Select((result, index) => new FoldMetric {
      FoldNumber = index + 1,
      R2Score = result.Metrics.RSquared,
      MeanAbsoluteError = result.Metrics.MeanAbsoluteError,
      RootMeanSquaredError = result.Metrics.RootMeanSquaredError,
      LossFunctionValue = result.Metrics.LossFunction
    }).ToList();

    // Calculate statistics
    var r2Scores = foldMetrics.Select(f => f.R2Score).ToList();
    var meanR2 = r2Scores.Average();
    var stdDevR2 = Math.Sqrt(r2Scores.Select(x => Math.Pow(x - meanR2, 2)).Average());
    var minR2 = r2Scores.Min();
    var maxR2 = r2Scores.Max();

    Logger?.LogInformation("Cross-validation complete:");
    Logger?.LogInformation("  Mean R²:    {MeanR2:F4} ± {StdDev:F4}", meanR2, stdDevR2);
    Logger?.LogInformation("  Range:      [{Min:F4}, {Max:F4}]", minR2, maxR2);
    Logger?.LogInformation("  Kedro R²:   {KedroR2:F4}", Parameters.KedroReferenceR2Score);
    Logger?.LogInformation("  Difference: {Diff:F4} ({Pct:F1}%)",
        Math.Abs(meanR2 - Parameters.KedroReferenceR2Score),
        Math.Abs(meanR2 - Parameters.KedroReferenceR2Score) / Parameters.KedroReferenceR2Score * 100);

    foreach (var fold in foldMetrics) {
      Logger?.LogInformation("  Fold {Fold}: R²={R2:F4}, MAE={MAE:F2}, RMSE={RMSE:F2}",
          fold.FoldNumber, fold.R2Score, fold.MeanAbsoluteError, fold.RootMeanSquaredError);
    }

    var results = new CrossValidationResults {
      FoldMetrics = foldMetrics,
      MeanR2Score = meanR2,
      StdDevR2Score = stdDevR2,
      MinR2Score = minR2,
      MaxR2Score = maxR2,
      NumFolds = Parameters.NumFolds,
      KedroR2Score = Parameters.KedroReferenceR2Score,
      DifferenceFromKedro = Math.Abs(meanR2 - Parameters.KedroReferenceR2Score)
    };

    return Task.FromResult(new[] { results }.AsEnumerable());
  }
}
