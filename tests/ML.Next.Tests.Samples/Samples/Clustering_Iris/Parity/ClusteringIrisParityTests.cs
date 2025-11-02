using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using ML.Next.Clustering;
using ML.Next.Clustering.Trainers;
using ML.Next.Core.Schema;
using ML.Next.Data;
using ML.Next.Model;
using ML.Next.Tests.Samples.Samples.Clustering_Iris.Schemas;
using ML.Next.Tests.Samples.Shared;
using ML.Next.Transforms;
using NUnit.Framework;
using static ML.Next.Tests.Samples.Samples.Clustering_Iris.Schemas.IrisClusteringSchemas;

namespace ML.Next.Tests.Samples.Samples.Clustering_Iris.Parity;

/// <summary>
/// Parity tests verifying ML.Next produces identical clustering results to ML.NET.
/// Uses single-threaded KMeans execution to avoid non-deterministic parallel RNG behavior.
/// </summary>
[TestFixture]
[Category("Parity")]
[Category("Clustering")]
public class ClusteringIrisParityTests
{
  private string _dataPath = null!;

  [OneTimeSetUp]
  public void Setup()
  {
    _dataPath = TestHelpers.GetDataPath("Samples/Clustering_Iris/Data/iris-full.txt");
  }

  /// <summary>
  /// Pure ML.NET implementation from the official samples.
  /// This is our baseline "ground truth" implementation.
  /// Uses its own MLContext for complete independence.
  /// </summary>
  private ClusteringMetrics RunMLNetPipeline()
  {
    // Create dedicated MLContext with fixed seed
    var mlContext = new MLContext(seed: 1);

    // STEP 1: Load data using standard ML.NET
    IDataView fullData = mlContext.Data.LoadFromTextFile(
      path: _dataPath,
      columns: new[]
      {
        new TextLoader.Column("Label", DataKind.Single, 0),
        new TextLoader.Column(nameof(IrisData.SepalLength), DataKind.Single, 1),
        new TextLoader.Column(nameof(IrisData.SepalWidth), DataKind.Single, 2),
        new TextLoader.Column(nameof(IrisData.PetalLength), DataKind.Single, 3),
        new TextLoader.Column(nameof(IrisData.PetalWidth), DataKind.Single, 4),
      },
      hasHeader: true,
      separatorChar: '\t'
    );

    // STEP 2: Split dataset (80% train, 20% test)
    var trainTestData = mlContext.Data.TrainTestSplit(fullData, testFraction: 0.2);

    // STEP 3: Build pipeline using ML.NET string-based column names
    var dataProcessPipeline = mlContext.Transforms.Concatenate(
      "Features",
      nameof(IrisData.SepalLength),
      nameof(IrisData.SepalWidth),
      nameof(IrisData.PetalLength),
      nameof(IrisData.PetalWidth)
    );

    // STEP 4: Create and train K-Means clustering model (3 clusters, single-threaded for determinism)
    var trainer = mlContext.Clustering.Trainers.KMeans(
      new KMeansTrainer.Options
      {
        FeatureColumnName = "Features",
        NumberOfClusters = 3,
        NumberOfThreads =
          1 // Force single-threaded execution to avoid parallel RNG non-determinism
        ,
      }
    );
    var trainingPipeline = dataProcessPipeline.Append(trainer);
    var trainedModel = trainingPipeline.Fit(trainTestData.TrainSet);

    // STEP 5: Evaluate clustering quality
    IDataView predictions = trainedModel.Transform(trainTestData.TestSet);
    var metrics = mlContext.Clustering.Evaluate(
      predictions,
      scoreColumnName: "Score",
      featureColumnName: "Features"
    );

    return metrics;
  }

  /// <summary>
  /// ML.Next implementation - type-safe version with phantom types.
  /// Should produce identical clustering results to RunMLNetPipeline().
  /// Uses its own MLContext for complete independence.
  /// </summary>
  private ClusteringMetrics RunMLNextPipeline()
  {
    // Create dedicated MLContext with fixed seed (same seed as ML.NET)
    var mlContext = new MLContext(seed: 1);

    // STEP 1: Load data using type-safe loader
    var options = new TextLoader.Options
    {
      Columns = new[]
      {
        new TextLoader.Column("Label", DataKind.Single, 0),
        new TextLoader.Column(nameof(IrisData.SepalLength), DataKind.Single, 1),
        new TextLoader.Column(nameof(IrisData.SepalWidth), DataKind.Single, 2),
        new TextLoader.Column(nameof(IrisData.PetalLength), DataKind.Single, 3),
        new TextLoader.Column(nameof(IrisData.PetalWidth), DataKind.Single, 4),
      },
      HasHeader = true,
      Separators = new[] { '\t' },
    };

    var dataResult = DataLoader.LoadFromTextFile<IRawSchema>(
      mlContext,
      path: _dataPath,
      options: options
    );

    // Unwrap the Fin<T> monad (it will throw if load failed)
    var fullData = dataResult.ThrowIfFail();

    // STEP 2: Split data using type-safe wrapper
    var (trainingData, testData) = DataLoader.TrainTestSplit<IRawSchema>(
      mlContext,
      fullData,
      testFraction: 0.2,
      seed: 1
    );

    // STEP 3: Build type-safe pipeline with expression-based column selectors
    var featurize = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
      mlContext,
      "Features",
      schema => schema.SepalLength,
      schema => schema.SepalWidth,
      schema => schema.PetalLength,
      schema => schema.PetalWidth
    );

    // STEP 4: Create type-safe K-Means trainer (single-threaded for determinism)
    var clusteringEstimator = ClusteringTrainers.KMeans<IFeaturesSchema, IClusteredSchema>(
      mlContext,
      schema => schema.Features,
      numberOfClusters: 3,
      numberOfThreads: 1 // Force single-threaded execution to avoid parallel RNG non-determinism
    );

    // STEP 5: Compose pipeline - type system ensures IFeaturesSchema -> IClusteredSchema
    var pipeline = featurize.Append(clusteringEstimator);

    // STEP 6: Train model
    var modelResult = pipeline.Fit(trainingData);
    var trainedModel = modelResult.ThrowIfFail();

    // STEP 7: Evaluate clustering quality using type-safe evaluation
    var predictionsResult = trainedModel.Transform(testData);
    var predictions = predictionsResult.ThrowIfFail();

    var metrics = ClusteringEvaluation.Evaluate(
      mlContext,
      predictions,
      schema => schema.Score,
      schema => schema.Features
    );

    return metrics;
  }

  [Test]
  public void MLNext_Should_Produce_Identical_Clustering_Results_To_MLNet()
  {
    // ACT: Run both implementations with separate contexts, same seed, and single-threaded execution
    var mlnetMetrics = RunMLNetPipeline();
    var mlnextMetrics = RunMLNextPipeline();

    // ASSERT: With single-threaded KMeans, metrics should be identical (within floating-point tolerance)
    Assert.That(
      mlnextMetrics.DaviesBouldinIndex,
      Is.EqualTo(mlnetMetrics.DaviesBouldinIndex).Within(0.0001),
      $"Davies-Bouldin Index mismatch: ML.NET={mlnetMetrics.DaviesBouldinIndex:F4}, ML.Next={mlnextMetrics.DaviesBouldinIndex:F4}"
    );

    Assert.That(
      mlnextMetrics.AverageDistance,
      Is.EqualTo(mlnetMetrics.AverageDistance).Within(0.0001),
      $"Average Distance mismatch: ML.NET={mlnetMetrics.AverageDistance:F4}, ML.Next={mlnextMetrics.AverageDistance:F4}"
    );

    Console.WriteLine("✓ Clustering parity test passed!");
    Console.WriteLine($"  Davies-Bouldin Index: {mlnetMetrics.DaviesBouldinIndex:F4}");
    Console.WriteLine($"  Average Distance: {mlnetMetrics.AverageDistance:F4}");
  }
}
