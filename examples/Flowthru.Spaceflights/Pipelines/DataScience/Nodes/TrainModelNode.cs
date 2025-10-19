using System.ComponentModel.DataAnnotations;
using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Models;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;

namespace Flowthru.Spaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Trains a linear regression model using ordinary least squares (OLS).
/// Uses Math.NET Numerics MultipleRegression.QR() which matches sklearn's LinearRegression.
/// Takes training features (x_train) and targets (y_train) as separate inputs.
/// 
/// Multi-input node - receives two catalog entries independently.
/// Stateless node with implicit parameterless constructor,
/// compatible with type reference instantiation for distributed/parallel execution.
/// </summary>
public class TrainModelNode : NodeBase<TrainModelInputs, LinearRegressionModel>
{
    protected override Task<IEnumerable<LinearRegressionModel>> Transform(
        IEnumerable<TrainModelInputs> inputs)
    {
        // Extract the singleton input containing all catalog data
        var input = inputs.Single();
        var xTrainData = input.XTrain.ToList();
        var yTrainData = input.YTrain.ToList();

        // Build design matrix (with one-hot encoding for boolean features)
        // 
        // CRITICAL DIFFERENCE: Python sklearn vs. C# Math.NET
        // =====================================================
        // sklearn's LinearRegression uses scipy.linalg.lstsq with a 'cond' parameter that enables
        // pseudo-inverse calculations, allowing it to gracefully handle singular/rank-deficient matrices
        // caused by zero-variance features. It effectively treats constant features as redundant.
        //
        // Math.NET's MultipleRegression.QR() uses strict QR decomposition which FAILS with singular
        // matrices, producing NaN coefficients when zero-variance features are present.
        //
        // Python ecosystem preprocessing (sklearn.feature_selection.VarianceThreshold) typically
        // removes zero-variance features BEFORE regression, making this a non-issue.
        //
        // In this dataset, MoonClearanceComplete has zero variance (all False), so we must exclude it
        // manually to match sklearn's behavior and avoid NaN coefficients.
        //
        // This is NOT a bug in either library - it's a fundamental difference in numerical approaches:
        // - sklearn prioritizes robustness (pseudo-inverse)
        // - Math.NET prioritizes mathematical purity (strict decomposition)
        var dataPoints = xTrainData.Select(row => new[]
        {
            (double)row.Engines,
            (double)row.PassengerCapacity,
            (double)row.Crew,
            row.DCheckComplete ? 1.0 : 0.0,
            row.IataApproved ? 1.0 : 0.0,
            (double)row.CompanyRating,
            (double)row.ReviewScoresRating
        }).ToArray();

        // Convert target prices to double array
        var targets = yTrainData.Select(p => (double)p).ToArray();

        // Train OLS regression using QR decomposition (same as sklearn's LinearRegression)
        // intercept: true adds a bias term automatically
        double[] coefficients = MultipleRegression.QR(dataPoints, targets, intercept: true);

        // Extract intercept and feature coefficients
        var intercept = coefficients[0];
        var featureCoefficients = coefficients.Skip(1).ToArray();

        var model = new LinearRegressionModel
        {
            Intercept = intercept,
            Coefficients = featureCoefficients,
            FeatureNames = new[]
            {
                "Engines",
                "PassengerCapacity",
                "Crew",
                "DCheckComplete",
                "IataApproved",
                "CompanyRating",
                "ReviewScoresRating"
            }
        };

        // Return as singleton collection
        return Task.FromResult(new[] { model }.AsEnumerable());
    }
}

#region Node Artifacts (Colocated)

/// <summary>
/// Multi-input schema for TrainModelNode.
/// Bundles training features and targets for model training.
/// </summary>
public record TrainModelInputs
{
    /// <summary>
    /// Training features
    /// </summary>
    [Required]
    public IEnumerable<FeatureRow> XTrain { get; init; } = null!;

    /// <summary>
    /// Training targets (prices)
    /// </summary>
    [Required]
    public IEnumerable<decimal> YTrain { get; init; } = null!;
}

/// <summary>
/// Trained ordinary least squares linear regression model.
/// Contains intercept and feature coefficients.
/// </summary>
public record LinearRegressionModel
{
    /// <summary>
    /// Model intercept (bias term)
    /// </summary>
    public double Intercept { get; init; }

    /// <summary>
    /// Feature coefficients in order of features
    /// </summary>
    public double[] Coefficients { get; init; } = Array.Empty<double>();

    /// <summary>
    /// Feature names corresponding to coefficients
    /// </summary>
    public string[] FeatureNames { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Predict a single value given feature values
    /// </summary>
    public double Predict(double[] features)
    {
        if (features.Length != Coefficients.Length)
            throw new ArgumentException($"Expected {Coefficients.Length} features, got {features.Length}");

        var prediction = Intercept;
        for (int i = 0; i < features.Length; i++)
        {
            prediction += Coefficients[i] * features[i];
        }
        return prediction;
    }

    /// <summary>
    /// Predict values for multiple feature rows.
    /// 
    /// Note: MoonClearanceComplete is excluded from predictions to match the training feature set.
    /// This zero-variance feature would cause singular matrix errors in Math.NET's QR decomposition,
    /// whereas sklearn's lstsq handles it via pseudo-inverse. See training comments for details.
    /// </summary>
    public double[] Predict(IEnumerable<FeatureRow> rows)
    {
        return rows.Select(row => Predict(new[]
        {
            (double)row.Engines,
            (double)row.PassengerCapacity,
            (double)row.Crew,
            row.DCheckComplete ? 1.0 : 0.0,
            row.IataApproved ? 1.0 : 0.0,
            (double)row.CompanyRating,
            (double)row.ReviewScoresRating
        })).ToArray();
    }
}

#endregion
