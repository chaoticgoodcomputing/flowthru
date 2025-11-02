# Tutorial: Testing Parity Between ML.NET and ML.Next

In this tutorial, we'll walk through the process of verifying that ML.Next provides the same machine learning results as ML.NET while adding compile-time type safety. We'll use the **Clustering_Iris** sample from the official ML.NET samples repository as our concrete example.

## What You'll Learn

By the end of this tutorial, you will:
- Copy the ML.NET Clustering_Iris sample into your test project
- Migrate the ML.NET code to use ML.Next's type-safe APIs
- Write tests that compare clustering outputs for equivalence
- Understand what "parity" means for clustering tasks

## Prerequisites

- Familiarity with ML.NET pipelines
- Basic understanding of NUnit testing framework
- The ML.NET Clustering_Iris sample (located at `docs/reference/misc/external/ml-net-samples/repo/samples/csharp/getting-started/Clustering_Iris`)

## Step 1: Set Up Your Test Project Structure

We'll create the test structure for the Clustering_Iris sample:

```
tests/Flowthru.Tests.ML.Next.Samples/
└── Clustering_Iris/
    ├── Data/
    │   └── iris-full.txt              # 150 iris samples (copied from ML.NET sample)
    ├── Schemas/
    │   ├── IrisData.cs                # Input data schema
    │   ├── IrisPrediction.cs          # Prediction output schema
    │   └── IrisClusteringSchemas.cs   # ML.Next phantom type schemas
    ├── Parity/
    │   └── ParityTests.cs             # ML.NET vs ML.Next comparison
    └── Errors/
        ├── 01_ColumnNameTests.cs
        ├── 02_SchemaMismatchTests.cs
        └── ...
```

Create these directories if they don't exist:

```bash
cd tests/Flowthru.Tests.ML.Next.Samples
mkdir -p Clustering_Iris/{Data,Schemas,Parity,Errors}
```

## Step 2: Copy the ML.NET Reference Implementation

We'll start by bringing in the original ML.NET Clustering_Iris code. This serves as our "ground truth" for comparison.

First, copy the dataset:

```bash
cp docs/reference/misc/external/ml-net-samples/repo/samples/csharp/getting-started/Clustering_Iris/IrisClustering/Data/iris-full.txt \
   tests/Flowthru.Tests.ML.Next.Samples/Clustering_Iris/Data/
```

Create the data structures in `Schemas/IrisData.cs`:

```csharp
namespace Flowthru.Tests.ML.Next.Samples.Clustering_Iris.Schemas;

public class IrisData
{
    public float Label;
    public float SepalLength;
    public float SepalWidth;
    public float PetalLength;
    public float PetalWidth;
}
```

And `Schemas/IrisPrediction.cs`:

```csharp
using Microsoft.ML.Data;

namespace Flowthru.Tests.ML.Next.Samples.Clustering_Iris.Schemas;

public class IrisPrediction
{
    [ColumnName("PredictedLabel")]
    public uint SelectedClusterId;

    [ColumnName("Score")]
    public float[] Distance;
}
```

Now create `Parity/ParityTests.cs` with the ML.NET baseline implementation:

```csharp
using Microsoft.ML;
using Microsoft.ML.Data;
using Flowthru.Tests.ML.Next.Samples.Clustering_Iris.Schemas;
using Flowthru.Tests.ML.Next.Samples.Shared;

namespace Flowthru.Tests.ML.Next.Samples.Clustering_Iris.Parity;

[TestFixture]
[Category("Parity")]
public class ClusteringIrisParityTests
{
    private MLContext _mlContext = null!;
    private string _dataPath = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _mlContext = new MLContext(seed: 1); // Fixed seed for reproducibility
        _dataPath = TestHelpers.GetDataPath("Clustering_Iris/Data/iris-full.txt");
    }

    /// <summary>
    /// Pure ML.NET implementation from the official samples.
    /// This is our baseline "ground truth" implementation.
    /// </summary>
    private (ITransformer Model, ClusteringMetrics Metrics) RunMLNetPipeline()
    {
        // STEP 1: Load data using standard ML.NET
        IDataView fullData = _mlContext.Data.LoadFromTextFile(
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
        var trainTestData = _mlContext.Data.TrainTestSplit(fullData, testFraction: 0.2);

        // STEP 3: Build pipeline using ML.NET string-based column names
        var dataProcessPipeline = _mlContext.Transforms.Concatenate(
            "Features",
            nameof(IrisData.SepalLength),
            nameof(IrisData.SepalWidth),
            nameof(IrisData.PetalLength),
            nameof(IrisData.PetalWidth)
        );

        // STEP 4: Create and train K-Means clustering model (3 clusters)
        var trainer = _mlContext.Clustering.Trainers.KMeans(
            featureColumnName: "Features",
            numberOfClusters: 3
        );
        var trainingPipeline = dataProcessPipeline.Append(trainer);
        var trainedModel = trainingPipeline.Fit(trainTestData.TrainSet);

        // STEP 5: Evaluate clustering quality
        IDataView predictions = trainedModel.Transform(trainTestData.TestSet);
        var metrics = _mlContext.Clustering.Evaluate(
            predictions,
            scoreColumnName: "Score",
            featureColumnName: "Features"
        );

        return (trainedModel, metrics);
    }
}
```

**Key Points:**
- Use a **fixed seed** (`seed: 1`) to ensure reproducible results
- Keep this implementation **pure ML.NET** - no ML.Next imports
- Match the exact pipeline structure from the official ML.NET sample
- Note the use of `nameof()` for some safety, but column names are still strings

## Step 3: Migrate to ML.Next Type-Safe Implementation

Now we'll create the ML.Next version that provides compile-time safety. First, define your phantom type schemas.

**Create `Schemas/IrisClusteringSchemas.cs`:**

```csharp
using Flowthru.ML.Next.Core.Schema;

namespace Flowthru.Tests.ML.Next.Samples.Clustering_Iris.Schemas;

/// <summary>
/// Phantom type schemas for type-safe pipeline composition.
/// These interfaces never get instantiated - they exist purely for compile-time checking.
/// </summary>
public static class IrisClusteringSchemas
{
    /// <summary>
    /// Raw schema: columns as loaded from iris-full.txt
    /// </summary>
    public interface IRawSchema : ISchemaDefinition
    {
        ColumnName<float> Label { get; }
        ColumnName<float> SepalLength { get; }
        ColumnName<float> SepalWidth { get; }
        ColumnName<float> PetalLength { get; }
        ColumnName<float> PetalWidth { get; }
    }

    /// <summary>
    /// After feature concatenation (all measurements combined)
    /// </summary>
    public interface IFeaturesSchema : IRawSchema
    {
        ColumnName<float[]> Features { get; }
    }

    /// <summary>
    /// After K-Means clustering training/prediction
    /// </summary>
    public interface IClusteredSchema : IFeaturesSchema
    {
        ColumnName<uint> PredictedLabel { get; }
        ColumnName<float[]> Score { get; }  // Distances to cluster centroids
    }
}
```

**Then add the ML.Next implementation to `ParityTests.cs`:**

```csharp
using Flowthru.ML.Next.Core.Schema;
using Flowthru.ML.Next.Extract;
using Flowthru.ML.Next.Transform;
using static Flowthru.Tests.ML.Next.Samples.Clustering_Iris.Schemas.IrisClusteringSchemas;

// ... in ClusteringIrisParityTests class ...

/// <summary>
/// ML.Next implementation - type-safe version with phantom types.
/// Should produce identical clustering results to RunMLNetPipeline().
/// </summary>
private (ITransformer Model, ClusteringMetrics Metrics) RunMLNextPipeline()
{
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
        Separators = new[] { '\t' }
    };

    var dataResult = DataLoader.LoadFromTextFile<IRawSchema>(
        _mlContext,
        path: _dataPath,
        options: options
    );

    // Unwrap the Fin<T> monad (it will throw if load failed)
    var fullData = dataResult.ThrowIfFail();

    // STEP 2: Split data - convert to DataView<T>
    var trainTestSplit = _mlContext.Data.TrainTestSplit(fullData.Underlying, testFraction: 0.2);
    var trainingData = DataView<IRawSchema>.From(trainTestSplit.TrainSet);
    var testData = DataView<IRawSchema>.From(trainTestSplit.TestSet);

    // STEP 3: Build type-safe pipeline with lambda expression column selectors
    // ML.Next catches typos at compile-time!
    var featurize = ColumnTransforms.Concatenate<IRawSchema, IFeaturesSchema>(
        _mlContext,
        "Features",
        schema => schema.SepalLength,  // Compile error if typo!
        schema => schema.SepalWidth,
        schema => schema.PetalLength,
        schema => schema.PetalWidth
    );

    // STEP 4: Create K-Means trainer
    var trainer = _mlContext.Clustering.Trainers.KMeans(
        featureColumnName: "Features",
        numberOfClusters: 3
    );

    // Wrap trainer in type-safe Estimator<TIn, TOut>
    var clusteringEstimator = Estimator<IFeaturesSchema, IClusteredSchema>.From(trainer);

    // STEP 5: Compose pipeline - type system ensures IFeaturesSchema -> IClusteredSchema
    var pipeline = featurize.Append(clusteringEstimator);

    // STEP 6: Train model
    var modelResult = pipeline.Fit(trainingData);
    var trainedModel = modelResult.ThrowIfFail();

    // STEP 7: Evaluate clustering quality
    var predictionsResult = trainedModel.Transform(testData);
    var predictions = predictionsResult.ThrowIfFail();

    var metrics = _mlContext.Clustering.Evaluate(
        predictions.Underlying,
        scoreColumnName: "Score",
        featureColumnName: "Features"
    );

    return (trainedModel.Underlying, metrics);
}
```

**Key Migration Points:**
- **DataLoader.LoadFromTextFile<TSchema>** - Type parameter specifies expected columns
- **Lambda expressions** - `schema => schema.SepalLength` catches typos at compile-time
- **Estimator<TIn, TOut>** - Type parameters enforce schema continuity (IRawSchema → IFeaturesSchema → IClusteredSchema)
- **Fin<T> monad** - Explicit error handling (use `.ThrowIfFail()` in tests for simplicity)

## Step 4: Compare Results for Parity

Now we write the actual test that runs both implementations and compares them:

```csharp
[Test]
public void MLNext_Should_Produce_Identical_Clustering_Results_To_MLNet()
{
    // ACT: Run both implementations with same seed
    var (mlnetModel, mlnetMetrics) = RunMLNetPipeline();
    var (mlnextModel, mlnextMetrics) = RunMLNextPipeline();

    // ASSERT: Clustering metrics should be identical (or within floating-point tolerance)
    Assert.That(mlnextMetrics.DaviesBouldinIndex, 
        Is.EqualTo(mlnetMetrics.DaviesBouldinIndex).Within(0.0001),
        $"Davies-Bouldin Index mismatch: ML.NET={mlnetMetrics.DaviesBouldinIndex:F4}, ML.Next={mlnextMetrics.DaviesBouldinIndex:F4}");

    Assert.That(mlnextMetrics.AverageDistance, 
        Is.EqualTo(mlnetMetrics.AverageDistance).Within(0.0001),
        $"Average Distance mismatch: ML.NET={mlnetMetrics.AverageDistance:F4}, ML.Next={mlnextMetrics.AverageDistance:F4}");

    // OPTIONAL: Compare predictions on sample data
    ComparePredictionsOnSampleData(mlnetModel, mlnextModel);

    Console.WriteLine("✓ Clustering parity test passed!");
    Console.WriteLine($"  Davies-Bouldin Index: {mlnetMetrics.DaviesBouldinIndex:F4}");
    Console.WriteLine($"  Average Distance: {mlnetMetrics.AverageDistance:F4}");
}

/// <summary>
/// Helper method to compare cluster predictions on specific test cases
/// </summary>
private void ComparePredictionsOnSampleData(ITransformer mlnetModel, ITransformer mlnextModel)
{
    // Create prediction engines
    var mlnetEngine = _mlContext.Model.CreatePredictionEngine<IrisData, IrisPrediction>(mlnetModel);
    var mlnextEngine = _mlContext.Model.CreatePredictionEngine<IrisData, IrisPrediction>(mlnextModel);

    // Test samples (same as in ML.NET official sample)
    var testSamples = new[]
    {
        new IrisData { SepalLength = 3.3f, SepalWidth = 1.6f, PetalLength = 0.2f, PetalWidth = 5.1f },
        new IrisData { SepalLength = 5.1f, SepalWidth = 3.5f, PetalLength = 1.4f, PetalWidth = 0.2f },
        new IrisData { SepalLength = 6.4f, SepalWidth = 3.2f, PetalLength = 4.5f, PetalWidth = 1.5f }
    };

    foreach (var sample in testSamples)
    {
        var mlnetPred = mlnetEngine.Predict(sample);
        var mlnextPred = mlnextEngine.Predict(sample);

        // Cluster IDs should match
        Assert.That(mlnextPred.SelectedClusterId, Is.EqualTo(mlnetPred.SelectedClusterId),
            $"Cluster ID mismatch for sample (SepalLength={sample.SepalLength})");

        // Distance arrays should be nearly identical
        for (int i = 0; i < mlnetPred.Distance.Length; i++)
        {
            Assert.That(mlnextPred.Distance[i], Is.EqualTo(mlnetPred.Distance[i]).Within(0.0001),
                $"Distance[{i}] mismatch for sample (SepalLength={sample.SepalLength})");
        }
    }
}

// Helper class to hold metrics
private class ModelMetrics
{
    public double Accuracy { get; set; }
    public double AUC { get; set; }
}
```

## Step 5: Understanding What "Parity" Means for Different ML Tasks

The metrics you compare depend on the machine learning task:

### Binary Classification
```csharp
Assert.That(mlnextMetrics.Accuracy, Is.EqualTo(mlnetMetrics.Accuracy).Within(0.0001));
Assert.That(mlnextMetrics.AUC, Is.EqualTo(mlnetMetrics.AUC).Within(0.0001));
Assert.That(mlnextMetrics.F1Score, Is.EqualTo(mlnetMetrics.F1Score).Within(0.0001));
```

### Multiclass Classification
```csharp
Assert.That(mlnextMetrics.MicroAccuracy, Is.EqualTo(mlnetMetrics.MicroAccuracy).Within(0.0001));
Assert.That(mlnextMetrics.MacroAccuracy, Is.EqualTo(mlnetMetrics.MacroAccuracy).Within(0.0001));
Assert.That(mlnextMetrics.LogLoss, Is.EqualTo(mlnetMetrics.LogLoss).Within(0.0001));
```

### Clustering
```csharp
Assert.That(mlnextMetrics.DaviesBouldinIndex, Is.EqualTo(mlnetMetrics.DaviesBouldinIndex).Within(0.0001));
Assert.That(mlnextMetrics.AverageDistance, Is.EqualTo(mlnetMetrics.AverageDistance).Within(0.0001));
```

### Regression
```csharp
Assert.That(mlnextMetrics.MeanAbsoluteError, Is.EqualTo(mlnetMetrics.MeanAbsoluteError).Within(0.01));
Assert.That(mlnextMetrics.RSquared, Is.EqualTo(mlnetMetrics.RSquared).Within(0.0001));
```

## Step 6: Running Your Parity Test

Execute your test via NUnit test runner or command line:

```bash
# Run just parity tests
dotnet test --filter Category=Parity

# Run specific sample's parity test
dotnet test --filter "FullyQualifiedName~YourSample_Name.Parity"

# Via Nx (if configured)
nx test Flowthru.Tests.ML.Next.Samples --filter Category=Parity
```

You should see output like:
```
✓ MLNext_Should_Produce_Identical_Results_To_MLNet
  Accuracy: 0.9333
  AUC: 0.9821
```

## Troubleshooting Common Parity Issues

### Metrics Don't Match Exactly

**Symptom:** Small differences in metrics (e.g., 0.9333 vs 0.9334)

**Cause:** Floating-point precision or random seed differences

**Solution:** Use appropriate tolerance in assertions:
```csharp
Within(0.0001)  // For most metrics
Within(0.01)    // For metrics like MAE that have larger scales
```

### Predictions Are Different

**Symptom:** Same metrics but different individual predictions

**Cause:** Non-deterministic behavior in algorithm or data loading

**Solution:**
1. Ensure both implementations use **same seed**: `new MLContext(seed: 1)`
2. Check that data split uses **same test fraction**
3. Verify **column order** is identical in both implementations

### Shared MLContext Produces Different Results

**Symptom:** Parity test fails with different metrics when using the same `MLContext` instance for both ML.NET and ML.Next pipelines

**Cause:** ML.NET's `MLContext` maintains internal state that can affect subsequent operations. When you reuse the same context, the second pipeline may be influenced by state from the first pipeline execution.

**Solution:**
Create **separate, independent** `MLContext` instances for each pipeline:

```csharp
// ❌ INCORRECT - Shared context causes state pollution
private MLContext _mlContext = new MLContext(seed: 1);

public void TestMethod() {
    var mlnetMetrics = RunMLNetPipeline(_mlContext);   // Modifies context state
    var mlnextMetrics = RunMLNextPipeline(_mlContext);  // Affected by previous run
    // Metrics won't match!
}

// ✅ CORRECT - Independent contexts
private ClusteringMetrics RunMLNetPipeline() {
    var mlContext = new MLContext(seed: 1);  // Fresh context
    // ... use mlContext
}

private ClusteringMetrics RunMLNextPipeline() {
    var mlContext = new MLContext(seed: 1);  // Independent context
    // ... use mlContext
}
```

**Key Insight:** Even with the same seed, reusing an `MLContext` can cause non-determinism because ML.NET may cache transformers, maintain random number generator state, or track other internal state that affects subsequent operations. For true parity testing, always use fresh contexts.

### Model Training Fails in ML.Next but Not ML.NET

**Symptom:** ML.NET runs fine, ML.Next throws exception

**Cause:** Likely a bug in ML.Next wrapper or incorrect schema definition

**Solution:**
1. Check that schema interfaces match actual column names/types
2. Verify `Fin<T>` results with `.IsSucc` before calling `.ThrowIfFail()`
3. Examine the error message in the `Fin.Fail` case

## What You've Learned

You have successfully:
- ✅ Structured a test project with separate ML.NET and ML.Next implementations
- ✅ Defined phantom type schemas for compile-time safety
- ✅ Migrated ML.NET string-based APIs to ML.Next expression-based APIs
- ✅ Written tests that verify behavioral equivalence (parity)
- ✅ Understood how to compare different ML task types

## Next Steps

Now that you can verify ML.Next produces correct results, proceed to:
- **[Tutorial 02: Testing Error Boundaries](./02-testing-error-boundaries.md)** - Learn how to write tests that confirm ML.Next catches errors at compile-time that ML.NET would only catch at runtime
- Try migrating your own ML.NET projects with confidence!
