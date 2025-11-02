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
tests/ML.Next.Tests.Samples/
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
cd tests/ML.Next.Tests.Samples
mkdir -p Clustering_Iris/{Data,Schemas,Parity,Errors}
```

## Step 2: Copy the ML.NET Reference Implementation

We'll start by bringing in the original ML.NET Clustering_Iris code. This serves as our "ground truth" for comparison.

First, copy the dataset:

```bash
cp docs/reference/misc/external/ml-net-samples/repo/samples/csharp/getting-started/Clustering_Iris/IrisClustering/Data/iris-full.txt \
   tests/ML.Next.Tests.Samples/Clustering_Iris/Data/
```

Create the data structures in `Schemas/IrisData.cs`:

```csharp
namespace ML.Next.Tests.Samples.Clustering_Iris.Schemas;

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

namespace ML.Next.Tests.Samples.Clustering_Iris.Schemas;

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
using Microsoft.ML.Trainers;
using NUnit.Framework;
using ML.Next.Tests.Samples.Clustering_Iris.Schemas;
using ML.Next.Tests.Samples.Shared;
using ML.Next.Core.Schema;
using ML.Next.Extract;
using ML.Next.Transform;
using ML.Next.Load;
using ML.Next.Train;
using static ML.Next.Tests.Samples.Clustering_Iris.Schemas.IrisClusteringSchemas;

namespace ML.Next.Tests.Samples.Clustering_Iris.Parity;

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
        _dataPath = TestHelpers.GetDataPath("Clustering_Iris/Data/iris-full.txt");
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
                NumberOfThreads = 1  // Force single-threaded execution to avoid parallel RNG non-determinism
            });
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
}
```

**Key Points:**
- Use a **fixed seed** (`seed: 1`) in a fresh `MLContext` to ensure reproducible results
- Use **single-threaded execution** (`NumberOfThreads: 1`) to avoid ML.NET's parallel RNG non-determinism
- Keep this implementation **pure ML.NET** - no ML.Next imports
- Match the exact pipeline structure from the official ML.NET sample
- Note the use of `nameof()` for some safety, but column names are still strings

**Why Single-Threaded?** ML.NET's KMeans uses parallel threads that race for RNG state (see `KMeansPlusPlusTrainer.cs` line 1735: `Random rand = RandomUtils.Create(baseHost.Rand)`). Thread scheduling order is non-deterministic, causing 6% variance in metrics even with fixed seeds. Single-threading eliminates this race condition.

## Step 3: Migrate to ML.Next Type-Safe Implementation

Now we'll create the ML.Next version that provides compile-time safety. First, define your phantom type schemas.

**Create `Schemas/IrisClusteringSchemas.cs`:**

```csharp
using ML.Next.Core.Schema;

namespace ML.Next.Tests.Samples.Clustering_Iris.Schemas;

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
// Already imported at top:
// using ML.Next.Load;
// using ML.Next.Train;

// ... in ClusteringIrisParityTests class ...

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
        Separators = new[] { '\t' }
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
        numberOfThreads: 1  // Force single-threaded execution to avoid parallel RNG non-determinism
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
```

**Key Migration Points:**
- **DataLoader.LoadFromTextFile<TSchema>** - Type parameter specifies expected columns
- **DataLoader.TrainTestSplit<TSchema>** - Returns typed tuple, eliminates `.Underlying` escapes
- **Lambda expressions** - `schema => schema.SepalLength` catches typos at compile-time
- **ClusteringTrainers.KMeans** - Type-safe wrapper with expression-based feature column selector
- **ClusteringEvaluation.Evaluate** - Expression-based column selectors, no string literals
- **Fin<T> monad** - Explicit error handling (use `.ThrowIfFail()` in tests for simplicity)
- **Zero `.Underlying` escapes** - All typed wrappers maintain type safety throughout

## Step 4: Compare Results for Parity

Now we write the actual test that runs both implementations and compares them:

```csharp
[Test]
public void MLNext_Should_Produce_Identical_Clustering_Results_To_MLNet()
{
    // ACT: Run both implementations with separate contexts, same seed, and single-threaded execution
    var mlnetMetrics = RunMLNetPipeline();
    var mlnextMetrics = RunMLNextPipeline();

    // ASSERT: With single-threaded KMeans, metrics should be identical (within floating-point tolerance)
    Assert.That(mlnextMetrics.DaviesBouldinIndex, 
        Is.EqualTo(mlnetMetrics.DaviesBouldinIndex).Within(0.0001),
        $"Davies-Bouldin Index mismatch: ML.NET={mlnetMetrics.DaviesBouldinIndex:F4}, ML.Next={mlnextMetrics.DaviesBouldinIndex:F4}");

    Assert.That(mlnextMetrics.AverageDistance, 
        Is.EqualTo(mlnetMetrics.AverageDistance).Within(0.0001),
        $"Average Distance mismatch: ML.NET={mlnetMetrics.AverageDistance:F4}, ML.Next={mlnextMetrics.AverageDistance:F4}");

    Console.WriteLine("✓ Clustering parity test passed!");
    Console.WriteLine($"  Davies-Bouldin Index: {mlnetMetrics.DaviesBouldinIndex:F4}");
    Console.WriteLine($"  Average Distance: {mlnetMetrics.AverageDistance:F4}");
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
nx test ML.Next.Tests.Samples --filter Category=Parity
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

### Predictions Are Different Even With Same Seed

**Symptom:** Metrics differ across runs (e.g., Davies-Bouldin: 0.5910 vs 0.6531), even with fixed `MLContext(seed: 1)`

**Root Cause:** ML.NET's parallel training algorithms have non-deterministic RNG behavior. In `KMeansPlusPlusTrainer.cs` (line 1735), each parallel thread gets its own `Random` instance seeded from the shared `baseHost.Rand`. Thread scheduling order is non-deterministic, causing different threads to get different seed values on each run.

**Solution:**
1. Use **single-threaded execution** to eliminate parallel RNG races:
   ```csharp
   var trainer = mlContext.Clustering.Trainers.KMeans(
       new KMeansTrainer.Options {
           NumberOfThreads = 1,  // Forces deterministic execution
           // ... other options
       });
   ```
2. Ensure both implementations use **same seed**: `new MLContext(seed: 1)`
3. Check that data split uses **same test fraction** and **same seed**
4. Verify **column order** is identical in both implementations

**Note:** This is an ML.NET framework limitation, not an ML.Next issue. The ML.Next wrappers produce identical data at every pipeline stage (load, split, featurize) - the non-determinism only occurs in ML.NET's parallel trainer implementation.

### Shared MLContext Produces Different Results

**Symptom:** Parity test fails with different metrics when using the same `MLContext` instance for both ML.NET and ML.Next pipelines

**Cause:** While ML.NET's `MLContext` can maintain some internal state, the primary issue is the **parallel RNG non-determinism** described above. However, using separate contexts is still good practice to ensure complete pipeline independence.

**Solution:**
Create **separate, independent** `MLContext` instances for each pipeline:

```csharp
// ❌ AVOID - Shared context (may cause subtle issues)
private MLContext _mlContext = new MLContext(seed: 1);

public void TestMethod() {
    var mlnetMetrics = RunMLNetPipeline(_mlContext);
    var mlnextMetrics = RunMLNextPipeline(_mlContext);
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

**Key Insight:** While the main source of non-determinism is parallel RNG races (solved by `numberOfThreads: 1`), using separate `MLContext` instances ensures complete pipeline independence and eliminates any potential state pollution issues.

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
