using Microsoft.ML;
using Microsoft.ML.Data;
using Flowthru.ML.Ext.Core.Schema;
using Flowthru.ML.Ext.Extract;
using Flowthru.Tests.ML.Ext.Samples.MulticlassClassification_Iris.DataStructures;
using Flowthru.Tests.ML.Ext.Samples.Shared;

namespace Flowthru.Tests.ML.Ext.Samples.MulticlassClassification_Iris;

/// <summary>
/// Demonstrates using Flowthru.ML.Ext for Iris flower multiclass classification.
/// Based on ML.NET sample: MulticlassClassification_Iris
/// </summary>
[TestFixture]
public class MulticlassClassificationIrisTests {
  private MLContext _mlContext = null!;
  private string _trainDataPath = null!;
  private string _testDataPath = null!;

  // Schema definitions for type-safe pipeline
  public struct IrisRawSchema : ISchemaDefinition { }
  public struct IrisFeaturesSchema : ISchemaDefinition { }
  public struct IrisClassifiedSchema : ISchemaDefinition { }

  [OneTimeSetUp]
  public void Setup() {
    _mlContext = new MLContext(seed: 1);
    _trainDataPath = TestHelpers.GetDataPath("MulticlassClassification_Iris/Data/iris-train.txt");
    _testDataPath = TestHelpers.GetDataPath("MulticlassClassification_Iris/Data/iris-test.txt");

    // Verify data files were copied correctly
    TestHelpers.VerifyDataFileExists("MulticlassClassification_Iris/Data/iris-train.txt");
    TestHelpers.VerifyDataFileExists("MulticlassClassification_Iris/Data/iris-test.txt");
  }

  [Test]
  public void MulticlassClassification_Iris_Pipeline_Should_Train_Without_Errors() {
    // STEP 1: Load training data using Flowthru.ML.Ext type-safe loader
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

    var trainDataResult = DataLoader.LoadFromTextFile<IrisRawSchema>(
        _mlContext,
        path: _trainDataPath,
        options: options);

    Assert.That(trainDataResult.IsSucc, Is.True,
        trainDataResult.IsSucc ? "" : $"Failed to load training data: {trainDataResult}");

    var trainingData = trainDataResult.ThrowIfFail();

    // STEP 2: Load test data
    var testDataResult = DataLoader.LoadFromTextFile<IrisRawSchema>(
        _mlContext,
        path: _testDataPath,
        options: options);

    Assert.That(testDataResult.IsSucc, Is.True,
        testDataResult.IsSucc ? "" : $"Failed to load test data: {testDataResult}");

    var testData = testDataResult.ThrowIfFail();

    // STEP 3: Build transformation pipeline
    // First convert label to key type (required for multiclass classification)
    var labelKeyEstimator = _mlContext.Transforms.Conversion.MapValueToKey("Label");
    var labelKeyWrapped = Flowthru.ML.Ext.Transform.Estimator<IrisRawSchema, IrisRawSchema>
        .From(labelKeyEstimator);

    // Then concatenate features
    var featurizeEstimator = _mlContext.Transforms.Concatenate(
        "Features",
        nameof(IrisData.SepalLength),
        nameof(IrisData.SepalWidth),
        nameof(IrisData.PetalLength),
        nameof(IrisData.PetalWidth));

    var featurizeWrapped = Flowthru.ML.Ext.Transform.Estimator<IrisRawSchema, IrisFeaturesSchema>
        .From(featurizeEstimator);

    // Compose label key and featurization
    var preprocessingPipeline = labelKeyWrapped.Append(featurizeWrapped);

    // STEP 4: Add multiclass classification trainer (SDCA - Stochastic Dual Coordinate Ascent)
    var classificationTrainer = _mlContext.MulticlassClassification.Trainers
        .SdcaMaximumEntropy(labelColumnName: "Label", featureColumnName: "Features");

    var classificationEstimator = Flowthru.ML.Ext.Transform.Estimator<IrisFeaturesSchema, IrisClassifiedSchema>
        .From(classificationTrainer);

    // STEP 5: Compose the full pipeline
    var fullPipeline = preprocessingPipeline.Append(classificationEstimator);

    // STEP 6: Train the model
    var modelResult = fullPipeline.Fit(trainingData);

    // Assert training succeeded
    Assert.That(modelResult.IsSucc, Is.True,
        modelResult.IsSucc ? "" : $"Failed to train model: {modelResult}");

    var trainedModel = modelResult.ThrowIfFail();

    // STEP 7: Transform test data to get predictions
    var predictionsResult = trainedModel.Transform(testData);

    // Assert transformation succeeded
    Assert.That(predictionsResult.IsSucc, Is.True,
        predictionsResult.IsSucc ? "" : $"Failed to transform test data: {predictionsResult}");

    var predictions = predictionsResult.ThrowIfFail();

    // STEP 8: Evaluate classification quality
    var metrics = _mlContext.MulticlassClassification.Evaluate(
        predictions.Underlying,
        labelColumnName: "Label",
        scoreColumnName: "Score");

    // Assert metrics show reasonable accuracy (should be > 0.8 for Iris dataset)
    Assert.That(metrics.MicroAccuracy, Is.GreaterThan(0.8),
        $"Micro-accuracy should be > 0.8, got {metrics.MicroAccuracy:F4}");

    Assert.That(metrics.MacroAccuracy, Is.GreaterThan(0.8),
        $"Macro-accuracy should be > 0.8, got {metrics.MacroAccuracy:F4}");

    // STEP 9: Test single prediction using type-safe prediction engine
    var predictionEngineResult = Flowthru.ML.Ext.Load.PredictionEngine<IrisData, IrisPrediction>
        .Create(_mlContext, trainedModel);

    Assert.That(predictionEngineResult.IsSucc, Is.True,
        predictionEngineResult.IsSucc ? "" : $"Failed to create prediction engine: {predictionEngineResult}");

    var predictionEngine = predictionEngineResult.ThrowIfFail();

    // Test sample prediction (Setosa characteristics)
    var sampleIris = new IrisData {
      SepalLength = 5.1f,
      SepalWidth = 3.5f,
      PetalLength = 1.4f,
      PetalWidth = 0.2f
    };

    var predictionResult = predictionEngine.Predict(sampleIris);

    Assert.That(predictionResult.IsSucc, Is.True,
        predictionResult.IsSucc ? "" : $"Failed to make prediction: {predictionResult}");

    var prediction = predictionResult.ThrowIfFail();

    // Assert we got probability scores for all 3 classes
    Assert.That(prediction.Score.Length, Is.EqualTo(3),
        "Should have probability scores for 3 iris classes");

    // Assert probabilities sum to approximately 1.0
    var probabilitySum = prediction.Score.Sum();
    Assert.That(probabilitySum, Is.EqualTo(1.0f).Within(0.01f),
        $"Probabilities should sum to 1.0, got {probabilitySum:F4}");

    Console.WriteLine($"✓ Multiclass classification pipeline completed successfully");
    Console.WriteLine($"  Micro-accuracy: {metrics.MicroAccuracy:F4}");
    Console.WriteLine($"  Macro-accuracy: {metrics.MacroAccuracy:F4}");
    Console.WriteLine($"  Log-loss: {metrics.LogLoss:F4}");
    Console.WriteLine($"  Sample prediction scores: [{string.Join(", ", prediction.Score.Select(s => s.ToString("F4")))}]");
  }
}
