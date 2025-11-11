# UMAP Strategy Architecture

**Type-safe, composable UMAP implementation with compile-time validation**

This document describes the reorganized UMAP architecture that emphasizes:

1. **Interface/Implementation/Composition** - Strategies are independently optimizable
2. **Smart Defaults** - Automatic pipeline configuration based on data shape
3. **Compile-Time Safety** - Invalid strategy combinations prevented at compile time

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Core Concepts](#core-concepts)
- [Strategy Interfaces](#strategy-interfaces)
- [Compile-Time Safety](#compile-time-safety)
- [Usage Examples](#usage-examples)
- [Implementing Custom Strategies](#implementing-custom-strategies)

## Architecture Overview

The UMAP algorithm is decomposed into **9 individually-optimizable phases**, each with:

- A **strategy interface** defining the phase's contract
- Multiple **implementations** offering different trade-offs
- **Phantom type markers** enforcing compatibility at compile time

### Phase Decomposition

```
┌─────────────────────────────────────────────────────────┐
│                    UMAP Pipeline                        │
├─────────────────────────────────────────────────────────┤
│ Phase 1: Neighbor Search    │ INeighborSearchStrategy   │
│ Phase 2: Local Metric        │ ILocalMetricStrategy      │
│ Phase 3: Membership Strength │ IMembershipStrengthStrat. │
│ Phase 4: Graph Refinement    │ IGraphRefinementStrategy  │ (TODO)
│ Phase 5: Layout Init         │ ILayoutInitStrategy       │ (TODO)
│ Phase 6: Sampling Schedule   │ ISamplingScheduleStrategy │ (TODO)
│ Phase 7: Layout Optimization │ ILayoutOptimizationStrat. │ (TODO)
│ Phase 8: Transform           │ ITransformStrategy        │ (TODO)
│ Phase 9: Inverse Transform   │ IInverseTransformStrategy │ (TODO)
└─────────────────────────────────────────────────────────┘
```

## Core Concepts

### Phantom Types

Phantom types are marker interfaces used purely for compile-time type checking:

```csharp
// Data size markers
public interface IDataSizeMarker { }
public interface ISmallData : IDataSizeMarker { }   // < 4096 samples
public interface ILargeData : IDataSizeMarker { }   // ≥ 4096 samples

// Metric markers
public interface IMetricMarker { }
public interface IEuclideanMetric : IMetricMarker { }
public interface ICosineMetric : IMetricMarker { }
```

These markers enable the type system to prevent invalid combinations:

```csharp
// ✅ Valid: BruteForce works with small data
INeighborSearchStrategy<ISmallData, IEuclideanMetric> valid = 
    new BruteForceSearch<IEuclideanMetric>();

// ❌ Compile error: BruteForce doesn't support large data
INeighborSearchStrategy<ILargeData, IEuclideanMetric> invalid = 
    new BruteForceSearch<IEuclideanMetric>(); // Won't compile!
```

### Type-State Pattern

The builder uses type-state to enforce configuration order:

```csharp
public class UmapPipelineBuilder<TState, TDataSize, TMetric>
    where TState : notnull
    where TDataSize : IDataSizeMarker
    where TMetric : IMetricMarker
{
    // Can only call WithNeighborSearch in unconfigured state
    public UmapPipelineBuilder<INeighborSearchConfigured, TDataSize, TMetric>
        WithNeighborSearch<TSearch>(TSearch strategy)
        where TSearch : INeighborSearchStrategy<TDataSize, TMetric>
        where TState : IUnconfigured
    { }
    
    // Can only build when complete
    public UmapPipeline<TDataSize, TMetric> Build()
        where TState : IComplete
    { }
}
```

## Strategy Interfaces

### Phase 1: Neighbor Search

```csharp
public interface INeighborSearchStrategy<TDataSize, TMetric>
    where TDataSize : IDataSizeMarker
    where TMetric : IMetricMarker
{
    NeighborSearchResult Search(
        Matrix<float> data,
        int nNeighbors,
        Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric,
        Random random
    );
}
```

**Implementations:**

| Strategy                                | Data Size    | Time Complexity | Accuracy | Use Case                  |
| --------------------------------------- | ------------ | --------------- | -------- | ------------------------- |
| `BruteForceSearch<TMetric>`             | `ISmallData` | O(n²)           | 100%     | Small datasets            |
| `KdTreeSearch<TDataSize, TMetric>`      | `IAnySize`   | O(n log n)      | 100%     | Medium dimensional (TODO) |
| `ApproximateSearch<TDataSize, TMetric>` | `ILargeData` | O(n^1.14)       | ~99%     | Large datasets (TODO)     |

### Phase 2: Local Metric

```csharp
public interface ILocalMetricStrategy
{
    LocalMetricResult ComputeLocalMetrics(
        float[][] knnDistances,
        float k,
        float localConnectivity = 1.0f,
        float bandwidth = 1.0f
    );
}
```

**Implementations:**

| Strategy                 | Description                 | Convergence             |
| ------------------------ | --------------------------- | ----------------------- |
| `BinarySearchSmoothing`  | Standard UMAP binary search | ~10-20 iterations       |
| `NewtonRaphsonSmoothing` | Faster convergence variant  | ~5-10 iterations (TODO) |

### Phase 3: Membership Strength

```csharp
public interface IMembershipStrengthStrategy
{
    SparseMatrix ComputeMembershipStrengths(
        int[][] knnIndices,
        float[][] knnDistances,
        float[] sigmas,
        float[] rhos,
        float setOpMixRatio = 1.0f
    );
}
```

**Implementations:**

| Strategy            | Kernel        | Use Case                          |
| ------------------- | ------------- | --------------------------------- |
| `ExponentialKernel` | exp(-(d-ρ)/σ) | Standard UMAP                     |
| `PowerLawKernel`    | (d-ρ)^(-α)    | Heavy-tailed distributions (TODO) |

## Compile-Time Safety

### Example 1: Data Size Constraints

```csharp
// ✅ COMPILES: BruteForce is valid for small data
var smallPipeline = UmapPipeline<ISmallData, IEuclideanMetric>
    .CreateBuilder()
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>())
    .WithLocalMetric(new BinarySearchSmoothing())
    .WithMembershipStrength(new ExponentialKernel())
    .Build();

// ❌ COMPILE ERROR: BruteForce only implements INeighborSearchStrategy<ISmallData, _>
var largePipeline = UmapPipeline<ILargeData, IEuclideanMetric>
    .CreateBuilder()
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>()) // Error here!
    .Build();
```

### Example 2: Configuration Order

```csharp
// ❌ COMPILE ERROR: Cannot build without all strategies
var incomplete = UmapPipeline<ISmallData, IEuclideanMetric>
    .CreateBuilder()
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>())
    .Build(); // Error: INeighborSearchConfigured ≠ IComplete

// ✅ COMPILES: All required strategies configured
var complete = UmapPipeline<ISmallData, IEuclideanMetric>
    .CreateBuilder()
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>())
    .WithLocalMetric(new BinarySearchSmoothing())
    .WithMembershipStrength(new ExponentialKernel())
    .Build();
```

### Example 3: Metric Compatibility

```csharp
// ✅ COMPILES: Euclidean-optimized strategies match Euclidean metric
var euclidean = UmapPipeline<ISmallData, IEuclideanMetric>
    .CreateBuilder()
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>())
    .WithLocalMetric(new BinarySearchSmoothing())
    .WithMembershipStrength(new ExponentialKernel())
    .Build();

// Future: Layout optimization will also be constrained
// ✅ COMPILES: EuclideanSGD only works with IEuclideanMetric
// .WithOptimization(new EuclideanSGD<IEuclideanOutput>())

// ❌ COMPILE ERROR: Cannot use Euclidean-optimized SGD with non-Euclidean metric
// var cosine = UmapPipeline<ISmallData, ICosineMetric>
//     .CreateBuilder()
//     .WithNeighborSearch(new BruteForceSearch<ICosineMetric>())
//     .WithLocalMetric(new BinarySearchSmoothing())
//     .WithMembershipStrength(new ExponentialKernel())
//     .WithOptimization(new EuclideanSGD<IEuclideanOutput>()) // Error!
//     .Build();
```

## Usage Examples

### Basic Usage

```csharp
using Flowthru.Extensions.ML.UMAP.Core;
using Flowthru.Extensions.ML.UMAP.Core.Markers;
using MathNet.Numerics.LinearAlgebra.Single;

// Define distance metric
static float EuclideanDistance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
{
    float sum = 0f;
    for (int i = 0; i < x.Length; i++)
    {
        float diff = x[i] - y[i];
        sum += diff * diff;
    }
    return MathF.Sqrt(sum);
}

// Create pipeline
var pipeline = UmapPipeline<ISmallData, IEuclideanMetric>
    .CreateBuilder()
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>())
    .WithLocalMetric(new BinarySearchSmoothing())
    .WithMembershipStrength(new ExponentialKernel())
    .Build();

// Compute graph (phases 1-3)
var data = DenseMatrix.CreateRandom(1000, 50);
var result = pipeline.ComputeGraph(data, EuclideanDistance);

Console.WriteLine($"Graph: {result.Graph.NonZerosCount} edges");
```

### Custom Parameters

```csharp
var parameters = new UmapParameters
{
    NumberOfNeighbors = 30,
    LocalConnectivity = 2.0f,
    SetOpMixRatio = 0.8f,
    RandomSeed = 42,
    Verbosity = 1
};

var pipeline = UmapPipeline<ISmallData, IEuclideanMetric>
    .CreateBuilder(parameters)
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>())
    .WithLocalMetric(new BinarySearchSmoothing { MaxIterations = 128 })
    .WithMembershipStrength(new ExponentialKernel())
    .Build();
```

### Progress Monitoring

```csharp
var progress = new Progress<UmapProgress>(p =>
{
    Console.WriteLine($"[{p.Stage}] {p.Progress:P0} - {p.Details}");
});

var parameters = new UmapParameters 
{ 
    ProgressReporter = progress,
    Verbosity = 2
};

var pipeline = UmapPipeline<ISmallData, IEuclideanMetric>
    .CreateBuilder(parameters)
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>())
    .WithLocalMetric(new BinarySearchSmoothing())
    .WithMembershipStrength(new ExponentialKernel())
    .Build();
```

## Implementing Custom Strategies

### Step 1: Choose the Strategy Phase

Identify which phase you're implementing (e.g., `INeighborSearchStrategy`).

### Step 2: Declare Phantom Type Constraints

```csharp
// Example: Approximate neighbor search for large datasets
public class ApproximateSearch<TMetric> 
    : INeighborSearchStrategy<ILargeData, TMetric> // ← Constrain to ILargeData
    where TMetric : IMetricMarker
{
    // Implementation...
}
```

### Step 3: Implement the Interface

```csharp
public NeighborSearchResult Search(
    Matrix<float> data,
    int nNeighbors,
    Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> metric,
    Random random)
{
    // Your custom implementation
    var indices = new int[data.RowCount][];
    var distances = new float[data.RowCount][];
    
    // ... algorithm here ...
    
    return new NeighborSearchResult(indices, distances, SearchIndex: null);
}
```

### Step 4: Add Documentation

```csharp
/// <summary>
/// Approximate k-nearest neighbor search using NN-Descent.
/// </summary>
/// <remarks>
/// <para><b>Time complexity</b>: O(n^1.14 × d)</para>
/// <para><b>Accuracy</b>: ~99%</para>
/// <para><b>Recommended for</b>: Large datasets (≥ 4096 samples)</para>
/// </remarks>
public class ApproximateSearch<TMetric> : INeighborSearchStrategy<ILargeData, TMetric>
    where TMetric : IMetricMarker
{ }
```

### Step 5: Write Unit Tests

```csharp
[Fact]
public void ApproximateSearch_ShouldFindNearlyExactNeighbors()
{
    var strategy = new ApproximateSearch<IEuclideanMetric>();
    var data = GenerateTestData(10000, 100);
    
    var result = strategy.Search(data, 15, EuclideanDistance, new Random(42));
    
    Assert.Equal(10000, result.Indices.Length);
    Assert.All(result.Indices, indices => Assert.Equal(15, indices.Length));
}
```

## Directory Structure

```
UMAP/
├── Core/
│   ├── Markers/                     # Phantom type markers
│   │   ├── IDataSizeMarker.cs
│   │   ├── IMetricMarker.cs
│   │   ├── IOutputMetricMarker.cs
│   │   └── ICompatibilityMarker.cs
│   ├── DataShape.cs                 # Data analysis
│   ├── UmapParameters.cs            # Configuration
│   ├── UmapPipeline.cs              # Main orchestrator
│   └── UmapPipelineBuilder.cs       # Fluent builder
│
├── Strategies/
│   ├── NeighborSearch/
│   │   ├── INeighborSearchStrategy.cs
│   │   └── Implementations/
│   │       ├── BruteForceSearch.cs
│   │       ├── KdTreeSearch.cs      (TODO)
│   │       └── ApproximateSearch.cs (TODO)
│   ├── LocalMetric/
│   │   ├── ILocalMetricStrategy.cs
│   │   └── Implementations/
│   │       └── BinarySearchSmoothing.cs
│   ├── MembershipStrength/
│   │   ├── IMembershipStrengthStrategy.cs
│   │   └── Implementations/
│   │       └── ExponentialKernel.cs
│   └── ... (7 more strategies)
│
├── Factories/                       # Smart defaults (TODO)
│   ├── UmapStrategyFactory.cs
│   └── DataShapeAnalyzer.cs
│
├── Examples/
│   └── CustomPipelines.cs
│
└── Legacy/                          # Backwards compatibility
    ├── UmapOptions.cs
    ├── UmapTrainer.cs
    └── Algorithms/
```

## Roadmap

### Phase 1: Core Infrastructure ✅

- [x] Phantom type markers
- [x] Builder with type-state pattern
- [x] NeighborSearch strategy + BruteForce implementation
- [x] LocalMetric strategy + BinarySearch implementation
- [x] MembershipStrength strategy + ExponentialKernel implementation

### Phase 2: Complete Basic Strategies (TODO)

- [ ] GraphRefinement strategy
- [ ] LayoutInitialization strategy
- [ ] SamplingSchedule strategy
- [ ] LayoutOptimization strategy
- [ ] Transform strategy

### Phase 3: Smart Defaults (TODO)

- [ ] DataShapeAnalyzer
- [ ] UmapStrategyFactory with heuristics
- [ ] Auto-configuration based on data shape

### Phase 4: Advanced Implementations (TODO)

- [ ] ApproximateSearch (NN-Descent)
- [ ] KdTreeSearch
- [ ] SpectralLayout
- [ ] EuclideanSGD optimization
- [ ] DensMAP support

### Phase 5: Performance Optimization (TODO)

- [ ] Parallel processing
- [ ] SIMD operations
- [ ] Memory pooling
- [ ] Benchmarking suite

## References

- Python UMAP: https://github.com/lmcinnes/umap
- UMAP Paper: McInnes et al., "UMAP: Uniform Manifold Approximation and Projection", ArXiv 1802.03426 (2018)
