# Flowthru.Tests.ML.Ext.Samples

Sample projects demonstrating **Flowthru.ML.Ext** using ML.NET's classic Iris dataset examples.

## Overview

This test project showcases Flowthru.ML.Ext's type-safe, functional wrappers for ML.NET through two complete machine learning pipelines:

1. **Clustering_Iris** - Unsupervised learning with K-Means clustering
2. **MulticlassClassification_Iris** - Supervised learning with SDCA classification

Each sample is structured as a self-contained NUnit test that demonstrates end-to-end ML.NET workflows using Flowthru.ML.Ext's compile-time schema tracking and monadic error handling.

## Project Structure

```
Flowthru.Tests.ML.Ext.Samples/
├── Shared/
│   └── TestHelpers.cs           # Common utilities for path resolution
├── Clustering_Iris/
│   ├── ClusteringIrisTests.cs   # K-Means clustering test
│   ├── DataStructures/
│   │   ├── IrisData.cs
│   │   └── IrisPrediction.cs
│   └── Data/
│       └── iris-full.txt         # Full Iris dataset (150 samples)
└── MulticlassClassification_Iris/
    ├── MulticlassClassificationIrisTests.cs  # SDCA classification test
    ├── DataStructures/
    │   ├── IrisData.cs
    │   └── IrisPrediction.cs
    └── Data/
        ├── iris-train.txt        # Training split (120 samples)
        └── iris-test.txt         # Test split (30 samples)
```

## Running the Samples

### Using dotnet CLI

```bash
# Build the project
cd tests/Flowthru.Tests.ML.Ext.Samples
dotnet build

# Run all sample tests
dotnet test

# Run specific sample
dotnet test --filter FullyQualifiedName~Clustering_Iris
dotnet test --filter FullyQualifiedName~MulticlassClassification_Iris
```

### Using Nx

```bash
# Build via Nx
nx build Flowthru.Tests.ML.Ext.Samples

# Run tests via Nx
nx test Flowthru.Tests.ML.Ext.Samples
```

## Sample Descriptions

### Clustering_Iris

**ML Task**: Unsupervised clustering  
**Algorithm**: K-Means++ (3 clusters)  
**Dataset**: iris-full.txt (150 samples, 80/20 train/test split)

**Pipeline Steps**:
1. Load data with type-safe `DataLoader.LoadFromTextFile<IrisRawSchema>()`
2. Split into training (80%) and test (20%) sets
3. Concatenate features (SepalLength, SepalWidth, PetalLength, PetalWidth)
4. Train K-Means model with 3 clusters
5. Evaluate clustering quality (Davies-Bouldin Index, Average Distance)
6. Make single-sample predictions with `PredictionEngine<IrisData, IrisPrediction>`

**Key Flowthru.ML.Ext Features Demonstrated**:
- `DataView<TSchema>` for compile-time schema tracking
- `Estimator<TSchemaIn, TSchemaOut>.Append()` for type-safe pipeline composition
- `Fin<T>` monad for explicit error handling
- Phantom types (`IrisRawSchema`, `IrisFeaturesSchema`, `IrisClusteredSchema`)

**Expected Output**:
```
✓ Clustering pipeline completed successfully
  Davies-Bouldin Index: ~0.60 (lower is better)
  Average Distance: ~0.60
  Sample prediction - Cluster: 0-2
```

### MulticlassClassification_Iris

**ML Task**: Supervised multiclass classification  
**Algorithm**: SDCA Maximum Entropy (Stochastic Dual Coordinate Ascent)  
**Dataset**: iris-train.txt (120 samples) + iris-test.txt (30 samples)

**Pipeline Steps**:
1. Load training and test data with `DataLoader.LoadFromTextFile<IrisRawSchema>()`
2. Convert labels to key type for multiclass classification
3. Concatenate features (SepalLength, SepalWidth, PetalLength, PetalWidth)
4. Train SDCA Maximum Entropy classifier
5. Evaluate classification accuracy (Micro-accuracy, Macro-accuracy, Log-loss)
6. Make single-sample predictions with probability scores

**Key Flowthru.ML.Ext Features Demonstrated**:
- Multiple `Estimator.Append()` calls for complex pipelines
- Label encoding with `MapValueToKey` wrapped in type-safe estimators
- `Fin<T>.ThrowIfFail()` for test assertions
- `Validation` monad patterns via assertions

**Expected Output**:
```
✓ Multiclass classification pipeline completed successfully
  Micro-accuracy: ~0.95-1.00
  Macro-accuracy: ~0.95-1.00
  Log-loss: ~0.01-0.05
  Sample prediction scores: [0.XX, 0.YY, 0.ZZ] (sum to 1.0)
```

## Flowthru.ML.Ext API Patterns

### Schema Definition

Define phantom types for compile-time schema tracking:

```csharp
public struct IrisRawSchema : ISchemaDefinition { }
public struct IrisFeaturesSchema : ISchemaDefinition { }
public struct ModelOutputSchema : ISchemaDefinition { }
```

### Data Loading

```csharp
var options = new TextLoader.Options
{
    Columns = new[]
    {
        new TextLoader.Column("Label", DataKind.Single, 0),
        new TextLoader.Column(nameof(IrisData.SepalLength), DataKind.Single, 1),
        // ...
    },
    HasHeader = true,
    Separators = new[] { '\t' }
};

var dataResult = DataLoader.LoadFromTextFile<IrisRawSchema>(
    mlContext, path: dataPath, options: options);
```

### Pipeline Construction

```csharp
// Wrap ML.NET estimators with type-safe wrappers
var estimator1 = Estimator<Schema1, Schema2>.From(mlnetEstimator1);
var estimator2 = Estimator<Schema2, Schema3>.From(mlnetEstimator2);

// Type-safe composition - compiler verifies Schema2 compatibility
var pipeline = estimator1.Append(estimator2);

// Fit to get a trained transformer
var transformerResult = pipeline.Fit(trainingData);
```

### Error Handling

```csharp
// Pattern match on Fin<T> result
dataResult.Match(
    Succ: data => ProcessData(data),
    Fail: err => Console.WriteLine($"Error: {err}")
);

// Or assert success in tests
Assert.That(dataResult.IsSucc, Is.True);
var data = dataResult.ThrowIfFail();
```

### Prediction

```csharp
var engineResult = PredictionEngine<InputClass, OutputClass>
    .Create(mlContext, transformer);

var predictionResult = engineResult
    .Bind(engine => engine.Predict(inputSample));

Assert.That(predictionResult.IsSucc, Is.True);
```

## Data Files

Data files are copied to the output directory at build time via:

```xml
<ItemGroup>
  <Content Include="Clustering_Iris\Data\iris-full.txt">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

The `TestHelpers.GetDataPath()` utility resolves paths relative to the test assembly location, matching ML.NET's sample patterns.

## Differences from Original ML.NET Samples

1. **Type Safety**: Schema changes tracked at compile-time via phantom types
2. **Error Handling**: Explicit `Fin<T>` monads instead of exceptions
3. **Composition**: Type-checked pipeline building with `Estimator.Append()`
4. **Test Format**: NUnit tests instead of console apps for automated validation
5. **Simplified**: No model persistence (save/load) - tests run entirely in-memory

## Compilation Tests

Each sample includes comprehensive **negative compilation tests** that verify ML.Ext's compile-time safety guarantees. These tests use Roslyn to compile code snippets and assert that incorrect usage patterns fail to compile.

### Running Compilation Tests

```bash
# Run only compilation tests
dotnet test --filter "Category=Compilation"

# Or via Nx
nx test Flowthru.Tests.ML.Ext.Samples --filter "Category=Compilation"
```

### What Compilation Tests Verify

**Clustering_Iris Compilation Tests** (`ClusteringIrisCompilationTests.cs`):
1. ✅ Schema mismatches in pipeline composition don't compile
2. ✅ Estimators cannot be used as Transformers without `Fit()`
3. ✅ `DataView<T>` requires explicit `.Underlying` for ML.NET interop
4. ✅ `Fin<T>` results must be unwrapped via `Match()` or `Bind()`
5. ✅ `Estimator.Append()` enforces schema compatibility
6. ✅ Column name typos with `nameof()` produce compile errors
7. ✅ Valid pipelines compile successfully (positive control)

**MulticlassClassification_Iris Compilation Tests** (`MulticlassClassificationIrisCompilationTests.cs`):
1. ✅ Three-stage pipeline with schema break doesn't compile
2. ✅ Label key and featurize schema mismatches don't compile
3. ✅ Featurize and classifier schema mismatches don't compile
4. ✅ Prediction engine type mismatches don't compile
5. ✅ Transformer `Fit()` returns correct schema types
6. ✅ Multiple `Append()` calls verify schema propagation
7. ✅ `Fin<T>` chaining without `Match()` doesn't compile
8. ✅ Valid three-stage pipeline compiles successfully (positive control)

### Educational Value

These tests serve three purposes:

1. **Documentation**: Show what ML.Ext prevents at compile-time
2. **Regression Prevention**: Ensure type safety isn't accidentally weakened
3. **Comparison**: Demonstrate advantages over raw ML.NET (runtime vs compile-time errors)

### Example: Schema Mismatch Test

```csharp
[Test]
public void Schema_Mismatch_In_Pipeline_Composition_Should_Not_Compile() {
  var code = @"
    var step1 = new Transformer<ISchema1, ISchema2>(null!);
    var step2 = new Transformer<ISchema3, ISchema1>(null!); // Wrong!
    var pipeline = step1.Append(step2); // ISchema2 != ISchema3
  ";
  
  var result = CompilationTestHelper.CompileWithMLExt(code);
  Assert.That(result.Success, Is.False); // Must not compile!
}
```

In raw ML.NET, this would fail at **runtime** when the pipeline executes. With ML.Ext, it fails at **compile-time** with a clear type error.

## Learning Path

1. **Start Here**: Read `Clustering_Iris/ClusteringIrisTests.cs` for basic pipeline
2. **Type Safety**: Study `Clustering_Iris/ClusteringIrisCompilationTests.cs` to see what ML.Ext prevents
3. **Advanced**: Study `MulticlassClassification_Iris` for multi-step transformations
4. **Compare**: Check original ML.NET samples in `docs/reference/misc/external/ml-net-samples`
5. **Experiment**: Modify pipelines, add transformations, try different algorithms

## Dependencies

- **.NET 9.0**
- **Flowthru.ML.Ext** (project reference)
- **Microsoft.ML 4.0.3**
- **NUnit 4.2.2**

## Original ML.NET Samples

These samples are adapted from:
- [ML.NET Samples - Clustering_Iris](https://github.com/dotnet/machinelearning-samples/tree/main/samples/csharp/getting-started/Clustering_Iris)
- [ML.NET Samples - MulticlassClassification_Iris](https://github.com/dotnet/machinelearning-samples/tree/main/samples/csharp/getting-started/MulticlassClassification_Iris)

## License

See LICENSE file in repository root.
