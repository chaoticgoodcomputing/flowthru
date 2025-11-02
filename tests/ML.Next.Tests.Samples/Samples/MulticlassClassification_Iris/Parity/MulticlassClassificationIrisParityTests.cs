using Microsoft.ML;
using Microsoft.ML.Data;
using ML.Next.Core.Schema;
using ML.Next.Data;
using ML.Next.MulticlassClassification;
using ML.Next.MulticlassClassification.Trainers;
using ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas;
using ML.Next.Tests.Samples.Shared;
using ML.Next.Transforms;
using NUnit.Framework;
using static ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Schemas.IrisClassificationSchemas;

namespace ML.Next.Tests.Samples.Samples.MulticlassClassification_Iris.Parity;

/// <summary>
/// Parity tests verifying ML.Next produces identical multiclass classification results to ML.NET.
/// Uses the Iris dataset to classify flowers into 3 species based on measurements.
/// </summary>
[TestFixture]
[Category("Parity")]
[Category("MulticlassClassification")]
public class MulticlassClassificationIrisParityTests
{
  private string _trainDataPath = null!;
  private string _testDataPath = null!;

  [OneTimeSetUp]
  public void Setup()
  {
    _trainDataPath = TestHelpers.GetDataPath(
      "Samples/MulticlassClassification_Iris/Data/iris-train.txt"
    );
    _testDataPath = TestHelpers.GetDataPath(
      "Samples/MulticlassClassification_Iris/Data/iris-test.txt"
    );
  }

  /// <summary>
  /// Pure ML.NET implementation from the official samples.
  /// This is our baseline "ground truth" implementation.
  /// Uses its own MLContext for complete independence.
  /// </summary>
  private MulticlassClassificationMetrics RunMLNetPipeline()
  {
    // Create dedicated MLContext with fixed seed
    var mlContext = new MLContext(seed: 0);

    // STEP 1: Load training and test data using standard ML.NET
    var trainingDataView = mlContext.Data.LoadFromTextFile<IrisData>(
      _trainDataPath,
      hasHeader: true
    );
    var testDataView = mlContext.Data.LoadFromTextFile<IrisData>(_testDataPath, hasHeader: true);

    // STEP 2: Build data processing pipeline
    var dataProcessPipeline = mlContext
      .Transforms.Conversion.MapValueToKey(
        outputColumnName: "KeyColumn",
        inputColumnName: nameof(IrisData.Label)
      )
      .Append(
        mlContext.Transforms.Concatenate(
          "Features",
          nameof(IrisData.SepalLength),
          nameof(IrisData.SepalWidth),
          nameof(IrisData.PetalLength),
          nameof(IrisData.PetalWidth)
        )
      )
      .AppendCacheCheckpoint(mlContext);

    // STEP 3: Create trainer and append to pipeline (single-threaded for determinism)
    var trainer = mlContext
      .MulticlassClassification.Trainers.SdcaMaximumEntropy(
        new Microsoft.ML.Trainers.SdcaMaximumEntropyMulticlassTrainer.Options
        {
          LabelColumnName = "KeyColumn",
          FeatureColumnName = "Features",
          NumberOfThreads =
            1 // Force single-threaded execution for deterministic results
          ,
        }
      )
      .Append(
        mlContext.Transforms.Conversion.MapKeyToValue(
          outputColumnName: nameof(IrisData.Label),
          inputColumnName: "KeyColumn"
        )
      );

    var trainingPipeline = dataProcessPipeline.Append(trainer);

    // STEP 4: Train the model
    var trainedModel = trainingPipeline.Fit(trainingDataView);

    // STEP 5: Evaluate the model
    var predictions = trainedModel.Transform(testDataView);
    var metrics = mlContext.MulticlassClassification.Evaluate(predictions, "Label", "Score");

    return metrics;
  }

  /// <summary>
  /// ML.Next implementation - type-safe version with phantom types.
  /// Should produce identical classification results to RunMLNetPipeline().
  /// Uses its own MLContext for complete independence.
  /// </summary>
  private MulticlassClassificationMetrics RunMLNextPipeline()
  {
    // Create dedicated MLContext with fixed seed (same seed as ML.NET)
    var mlContext = new MLContext(seed: 0);

    // STEP 1: Load data using type-safe loader
    var trainingDataResult = DataLoader.LoadFromTextFile<IrisData, IRawSchema>(
      mlContext,
      path: _trainDataPath,
      hasHeader: true
    );
    var testDataResult = DataLoader.LoadFromTextFile<IrisData, IRawSchema>(
      mlContext,
      path: _testDataPath,
      hasHeader: true
    );

    var trainingData = trainingDataResult.ThrowIfFail();
    var testData = testDataResult.ThrowIfFail();

    // STEP 2: Build type-safe pipeline with expression-based column selectors

    // MapValueToKey: Label -> KeyColumn
    var mapValueToKey = ColumnTransforms.MapValueToKey<IRawSchema, IKeyedSchema, float>(
      mlContext,
      columnSelector: schema => schema.Label,
      outputColumn: "KeyColumn"
    );

    // Concatenate features
    var concatenate = ColumnTransforms.Concatenate<IKeyedSchema, IFeaturesSchema>(
      mlContext,
      "Features",
      schema => schema.SepalLength,
      schema => schema.SepalWidth,
      schema => schema.PetalLength,
      schema => schema.PetalWidth
    );

    // Compose preprocessing pipeline
    var preprocessingPipeline = mapValueToKey.Append(concatenate);

    // Fit preprocessing to get transformer
    var preprocessingTransformerResult = preprocessingPipeline.Fit(trainingData);
    var preprocessingTransformer = preprocessingTransformerResult.ThrowIfFail();

    // Transform training data
    var processedTrainingDataResult = preprocessingTransformer.Transform(trainingData);
    var processedTrainingData = processedTrainingDataResult.ThrowIfFail();

    // STEP 3: Create type-safe multiclass classification trainer (single-threaded for determinism)
    var trainer = MulticlassClassificationTrainers.SdcaMaximumEntropy<
      IFeaturesSchema,
      IModelSchema
    >(
      mlContext,
      labelColumnSelector: schema => schema.KeyColumn,
      featureColumnSelector: schema => schema.Features,
      numberOfThreads: 1 // Force single-threaded execution for deterministic results
    );

    // MapKeyToValue: KeyColumn -> Label (restore original label values)
    // Append this to trainer before fitting so it's all one pipeline
    var mapKeyToValue = ColumnTransforms.MapKeyToValue<IModelSchema, IModelSchema, uint>(
      mlContext,
      columnSelector: schema => schema.KeyColumn,
      outputColumn: nameof(IrisData.Label)
    );

    // Compose training pipeline with key-to-value restoration
    var trainingPipeline = trainer.Append(mapKeyToValue);

    // STEP 4: Train the full pipeline
    var trainingResult = trainingPipeline.Fit(processedTrainingData);
    var trainedModel = trainingResult.ThrowIfFail();

    // Compose full pipeline: preprocessing -> training (includes key-to-value)
    var fullTransformer = preprocessingTransformer.Append(trainedModel);

    // STEP 5: Transform test data and evaluate
    var testPredictionsResult = fullTransformer.Transform(testData);
    var testPredictions = testPredictionsResult.ThrowIfFail();

    var metrics = MulticlassClassificationEvaluation.Evaluate(
      mlContext,
      testPredictions,
      labelColumnSelector: schema => schema.Label,
      scoreColumnSelector: schema => schema.Score,
      predictedLabelColumnSelector: schema => schema.PredictedLabel
    );

    return metrics;
  }

  [Test]
  public void MLNext_Should_Produce_Identical_Classification_Results_To_MLNet()
  {
    // ACT: Run both implementations with separate contexts and same seed
    var mlnetMetrics = RunMLNetPipeline();
    var mlnextMetrics = RunMLNextPipeline();

    // ASSERT: Metrics should be identical (within floating-point tolerance)
    Assert.That(
      mlnextMetrics.MicroAccuracy,
      Is.EqualTo(mlnetMetrics.MicroAccuracy).Within(0.0001),
      $"MicroAccuracy mismatch: ML.NET={mlnetMetrics.MicroAccuracy:F4}, ML.Next={mlnextMetrics.MicroAccuracy:F4}"
    );

    Assert.That(
      mlnextMetrics.MacroAccuracy,
      Is.EqualTo(mlnetMetrics.MacroAccuracy).Within(0.0001),
      $"MacroAccuracy mismatch: ML.NET={mlnetMetrics.MacroAccuracy:F4}, ML.Next={mlnextMetrics.MacroAccuracy:F4}"
    );

    Assert.That(
      mlnextMetrics.LogLoss,
      Is.EqualTo(mlnetMetrics.LogLoss).Within(0.001),
      $"LogLoss mismatch: ML.NET={mlnetMetrics.LogLoss:F4}, ML.Next={mlnextMetrics.LogLoss:F4}"
    );

    Assert.That(
      mlnextMetrics.LogLossReduction,
      Is.EqualTo(mlnetMetrics.LogLossReduction).Within(0.01),
      $"LogLossReduction mismatch: ML.NET={mlnetMetrics.LogLossReduction:F4}, ML.Next={mlnextMetrics.LogLossReduction:F4}"
    );

    Console.WriteLine("✓ Multiclass classification parity test passed!");
    Console.WriteLine($"  MicroAccuracy: {mlnetMetrics.MicroAccuracy:F4}");
    Console.WriteLine($"  MacroAccuracy: {mlnetMetrics.MacroAccuracy:F4}");
    Console.WriteLine($"  LogLoss: {mlnetMetrics.LogLoss:F4}");
    Console.WriteLine($"  LogLossReduction: {mlnetMetrics.LogLossReduction:F4}");
  }
}
