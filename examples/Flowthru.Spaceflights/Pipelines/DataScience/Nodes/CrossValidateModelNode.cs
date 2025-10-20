using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Processed;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Logging;

namespace Flowthru.Spaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Performs k-fold cross-validation to generate R² distribution.
/// Helps understand model stability and variance across different train/test splits.
/// 
/// This node runs the entire train/evaluate cycle multiple times with different
/// random seeds to measure the natural variance in model performance.
/// </summary>
public class CrossValidateModelNode : NodeBase<ModelInputSchema, CrossValidationResults, CrossValidationOptions>
{
  protected override Task<IEnumerable<CrossValidationResults>> Transform(
      IEnumerable<ModelInputSchema> input)
  {
    var data = input.ToList();
    Logger?.LogInformation("Starting cross-validation with {Folds} folds", Parameters.NumFolds);

    // Convert to feature rows
    var featureRows = data.Select(row => new FeatureRow
    {
      Engines = (float)row.Engines,
      PassengerCapacity = (float)row.PassengerCapacity,
      Crew = (float)row.Crew,
      DCheckComplete = row.DCheckComplete,
      MoonClearanceComplete = row.MoonClearanceComplete,
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
            outputColumnName: "MoonClearanceCompleteEncoded",
            inputColumnName: nameof(FeatureRow.MoonClearanceComplete)))
        .Append(mlContext.Transforms.Categorical.OneHotEncoding(
            outputColumnName: "IataApprovedEncoded",
            inputColumnName: nameof(FeatureRow.IataApproved)))
        .Append(mlContext.Transforms.Concatenate(
            "Features",
            nameof(FeatureRow.Engines),
            nameof(FeatureRow.PassengerCapacity),
            nameof(FeatureRow.Crew),
            "DCheckCompleteEncoded",
            "MoonClearanceCompleteEncoded",
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
    var foldMetrics = cvResults.Select((result, index) => new FoldMetric
    {
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
    Logger?.LogInformation("  Kedro R²:   0.387");
    Logger?.LogInformation("  Difference: {Diff:F4} ({Pct:F1}%)",
        Math.Abs(meanR2 - 0.387),
        Math.Abs(meanR2 - 0.387) / 0.387 * 100);

    foreach (var fold in foldMetrics)
    {
      Logger?.LogInformation("  Fold {Fold}: R²={R2:F4}, MAE={MAE:F2}, RMSE={RMSE:F2}",
          fold.FoldNumber, fold.R2Score, fold.MeanAbsoluteError, fold.RootMeanSquaredError);
    }

    var results = new CrossValidationResults
    {
      FoldMetrics = foldMetrics,
      MeanR2Score = meanR2,
      StdDevR2Score = stdDevR2,
      MinR2Score = minR2,
      MaxR2Score = maxR2,
      NumFolds = Parameters.NumFolds,
      KedroR2Score = 0.387,
      DifferenceFromKedro = Math.Abs(meanR2 - 0.387)
    };

    return Task.FromResult(new[] { results }.AsEnumerable());
  }
}
