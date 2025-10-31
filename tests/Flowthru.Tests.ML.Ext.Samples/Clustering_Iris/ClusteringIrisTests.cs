using Microsoft.ML;
using Microsoft.ML.Data;
using Flowthru.ML.Ext.Core.Schema;
using Flowthru.ML.Ext.Extract;
using Flowthru.Tests.ML.Ext.Samples.Clustering_Iris.DataStructures;
using Flowthru.Tests.ML.Ext.Samples.Shared;

namespace Flowthru.Tests.ML.Ext.Samples.Clustering_Iris;

/// <summary>
/// Demonstrates using Flowthru.ML.Ext for Iris flower clustering.
/// Based on ML.NET sample: Clustering_Iris
/// </summary>
[TestFixture]
public class ClusteringIrisTests {
  private MLContext _mlContext = null!;
  private string _dataPath = null!;

  // Schema definitions for type-safe pipeline
  public struct IrisRawSchema : ISchemaDefinition { }
  public struct IrisFeaturesSchema : ISchemaDefinition { }
  public struct IrisClusteredSchema : ISchemaDefinition { }

  [OneTimeSetUp]
  public void Setup() {
    _mlContext = new MLContext(seed: 1);
    _dataPath = TestHelpers.GetDataPath("Clustering_Iris/Data/iris-full.txt");

    // Verify data file was copied correctly
    TestHelpers.VerifyDataFileExists("Clustering_Iris/Data/iris-full.txt");
  }

  [Test]
  public void Clustering_Iris_Pipeline_Should_Train_Without_Errors() {
    // STEP 1: Load data using Flowthru.ML.Ext type-safe loader
    var options = new TextLoader.Options {
      Columns = new[]
        {
                new TextLoader.Column("Label", Microsoft.ML.Data.DataKind.Single, 0),
                new TextLoader.Column(nameof(IrisData.SepalLength), Microsoft.ML.Data.DataKind.Single, 1),
                new TextLoader.Column(nameof(IrisData.SepalWidth), Microsoft.ML.Data.DataKind.Single, 2),
                new TextLoader.Column(nameof(IrisData.PetalLength), Microsoft.ML.Data.DataKind.Single, 3),
                new TextLoader.Column(nameof(IrisData.PetalWidth), Microsoft.ML.Data.DataKind.Single, 4),
            },
      HasHeader = true,
      Separators = new[] { '\t' }
    };

    var dataResult = DataLoader.LoadFromTextFile<IrisRawSchema>(
        _mlContext,
        path: _dataPath,
        options: options);

    // Assert data loaded successfully
    Assert.That(dataResult.IsSucc, Is.True,
        dataResult.IsSucc ? "" : $"Failed to load data: {dataResult}");

    var fullData = dataResult.ThrowIfFail();

    // STEP 2: Split data into train/test (80/20)
    var trainTestData = _mlContext.Data.TrainTestSplit(fullData.Underlying, testFraction: 0.2);
    var trainingData = Flowthru.ML.Ext.Core.Schema.DataView<IrisRawSchema>.From(trainTestData.TrainSet);
    var testData = Flowthru.ML.Ext.Core.Schema.DataView<IrisRawSchema>.From(trainTestData.TestSet);

    // STEP 3: Build transformation pipeline - concatenate features
    var featurizeEstimator = _mlContext.Transforms.Concatenate(
        "Features",
        nameof(IrisData.SepalLength),
        nameof(IrisData.SepalWidth),
        nameof(IrisData.PetalLength),
        nameof(IrisData.PetalWidth));

    var featurizeWrapped = Flowthru.ML.Ext.Transform.Estimator<IrisRawSchema, IrisFeaturesSchema>
        .From(featurizeEstimator);

    // STEP 4: Add K-Means clustering trainer (3 clusters for 3 iris types)
    var clusteringTrainer = _mlContext.Clustering.Trainers.KMeans(
        featureColumnName: "Features",
        numberOfClusters: 3);

    var clusteringEstimator = Flowthru.ML.Ext.Transform.Estimator<IrisFeaturesSchema, IrisClusteredSchema>
        .From(clusteringTrainer);

    // STEP 5: Compose the full pipeline
    var fullPipeline = featurizeWrapped.Append(clusteringEstimator);

    // STEP 6: Train the model
    var modelResult = fullPipeline.Fit(trainingData);

    // Assert training succeeded
    Assert.That(modelResult.IsSucc, Is.True,
        modelResult.IsSucc ? "" : $"Failed to train model: {modelResult}");

    var trainedModel = modelResult.ThrowIfFail();

    // STEP 7: Transform test data to verify the pipeline works
    var predictionsResult = trainedModel.Transform(testData);

    // Assert transformation succeeded
    Assert.That(predictionsResult.IsSucc, Is.True,
        predictionsResult.IsSucc ? "" : $"Failed to transform test data: {predictionsResult}");

    var predictions = predictionsResult.ThrowIfFail();

    // STEP 8: Evaluate clustering quality
    var metrics = _mlContext.Clustering.Evaluate(
        predictions.Underlying,
        scoreColumnName: "Score",
        featureColumnName: "Features");

    // Assert metrics are reasonable (Davies-Bouldin Index lower is better, typically 0-2 range)
    Assert.That(metrics.DaviesBouldinIndex, Is.LessThan(5.0),
        "Davies-Bouldin Index should indicate reasonable clustering quality");

    // STEP 9: Test single prediction using type-safe prediction engine
    var predictionEngineResult = Flowthru.ML.Ext.Load.PredictionEngine<IrisData, IrisPrediction>
        .Create(_mlContext, trainedModel);

    Assert.That(predictionEngineResult.IsSucc, Is.True,
        predictionEngineResult.IsSucc ? "" : $"Failed to create prediction engine: {predictionEngineResult}");

    var predictionEngine = predictionEngineResult.ThrowIfFail();

    // Test sample prediction
    var sampleIris = new IrisData {
      SepalLength = 3.3f,
      SepalWidth = 1.6f,
      PetalLength = 0.2f,
      PetalWidth = 5.1f
    };

    var predictionResult = predictionEngine.Predict(sampleIris);

    Assert.That(predictionResult.IsSucc, Is.True,
        predictionResult.IsSucc ? "" : $"Failed to make prediction: {predictionResult}");

    var prediction = predictionResult.ThrowIfFail();

    // Assert cluster ID is valid (0, 1, or 2 for 3 clusters)
    Assert.That(prediction.SelectedClusterId, Is.InRange(0u, 2u),
        "Cluster ID should be in valid range");

    Console.WriteLine($"✓ Clustering pipeline completed successfully");
    Console.WriteLine($"  Davies-Bouldin Index: {metrics.DaviesBouldinIndex:F4}");
    Console.WriteLine($"  Average Distance: {metrics.AverageDistance:F4}");
    Console.WriteLine($"  Sample prediction - Cluster: {prediction.SelectedClusterId}");
  }
}
