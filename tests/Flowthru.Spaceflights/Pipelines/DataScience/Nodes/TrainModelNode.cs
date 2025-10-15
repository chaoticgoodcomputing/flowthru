using Flowthru.Nodes;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Flowthru.Spaceflights.Pipelines.DataScience.Nodes;

/// <summary>
/// Trains a linear regression model using ML.NET.
/// Takes training features and targets, returns trained model.
/// 
/// Stateless node with implicit parameterless constructor,
/// compatible with type reference instantiation for distributed/parallel execution.
/// </summary>
public class TrainModelNode : Node<TrainTestSplit, ITransformer>
{
    protected override Task<IEnumerable<ITransformer>> TransformInternal(
        IEnumerable<TrainTestSplit> input)
    {
        var split = input.Single();

        var mlContext = new MLContext(seed: 0);

        // Convert training data to ML.NET format
        var trainingData = mlContext.Data.LoadFromEnumerable(split.XTrain);

        // Define ML pipeline
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
            .Append(mlContext.Regression.Trainers.Sdca(
                labelColumnName: "Label",
                featureColumnName: "Features"));

        // Train the model
        var model = pipeline.Fit(trainingData);

        // Return as singleton collection
        return Task.FromResult(new[] { model }.AsEnumerable());
    }
}
