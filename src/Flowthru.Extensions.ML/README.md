# Flowthru.Extensions.ML

**Type-safe, composable UMAP implementation with compile-time validation**

This project implements UMAP (Uniform Manifold Approximation and Projection) using a modern strategy pattern architecture that emphasizes:

1. **Interface/Implementation/Composition** - Strategies are independently optimizable and testable
2. **Smart Defaults** - Automatic pipeline configuration based on data characteristics
3. **Compile-Time Safety** - Invalid strategy combinations are prevented at compile time

## Architecture

UMAP is decomposed into **9 individually-optimizable phases**, each with its own strategy interface:

- **Phase 1**: Neighbor Search - Find k-nearest neighbors
- **Phase 2**: Local Metric - Compute bandwidth parameters
- **Phase 3**: Membership Strength - Build fuzzy simplicial set
- **Phase 4**: Graph Refinement - Post-process graph (TODO)
- **Phase 5**: Layout Initialization - Initialize embedding (TODO)
- **Phase 6**: Sampling Schedule - Determine sampling frequencies (TODO)
- **Phase 7**: Layout Optimization - Refine via SGD (TODO)
- **Phase 8**: Transform - Out-of-sample extension (TODO)
- **Phase 9**: Inverse Transform - Map back to high-dimensional space (TODO)

See [UMAP/STRATEGY_ARCHITECTURE.md](UMAP/STRATEGY_ARCHITECTURE.md) for detailed documentation.

## Attribution

This implementation is a direct port of the original UMAP Python implementation by **Leland McInnes**.

**Original Repository:** https://github.com/lmcinnes/umap  
**License:** BSD 3-Clause (see `UMAP/UMAP_LICENSE.txt`)

### Citations

```bibtex
@article{mcinnes2018umap,
  title={UMAP: Uniform Manifold Approximation and Projection for Dimension Reduction},
  author={McInnes, Leland and Healy, John and Melville, James},
  journal={arXiv preprint arXiv:1802.03426},
  year={2018}
}
```

## Quick Start

```csharp
using Flowthru.Extensions.ML.UMAP.Core;
using Flowthru.Extensions.ML.UMAP.Core.Markers;
using Flowthru.Extensions.ML.UMAP.Strategies.NeighborSearch.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.LocalMetric.Implementations;
using Flowthru.Extensions.ML.UMAP.Strategies.MembershipStrength.Implementations;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.Distributions;

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

// Build type-safe pipeline
var pipeline = UmapPipeline<ISmallData, IEuclideanMetric>
    .CreateBuilder()
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>())
    .WithLocalMetric(new BinarySearchSmoothing())
    .WithMembershipStrength(new ExponentialKernel())
    .Build();

// Create sample data and compute graph
var data = DenseMatrix.CreateRandom(1000, 50, new Normal());
var result = pipeline.ComputeGraph(data, EuclideanDistance);

Console.WriteLine($"Graph: {result.Graph.NonZerosCount} edges");
```

## Key Features

### Compile-Time Safety

Invalid strategy combinations are prevented at compile time:

```csharp
// ✅ COMPILES: BruteForce is valid for small data
var valid = UmapPipeline<ISmallData, IEuclideanMetric>
    .CreateBuilder()
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>())
    // ...
    .Build();

// ❌ COMPILE ERROR: BruteForce only works with ISmallData
var invalid = UmapPipeline<ILargeData, IEuclideanMetric>
    .CreateBuilder()
    .WithNeighborSearch(new BruteForceSearch<IEuclideanMetric>()) // Won't compile!
    .Build();
```

### Independent Strategy Optimization

Each strategy can be optimized, tested, and benchmarked independently:

```csharp
// Benchmark different neighbor search strategies
var bruteForce = new BruteForceSearch<IEuclideanMetric>();
var kdTree = new KdTreeSearch<ISmallData, IEuclideanMetric>(); // TODO

// Test in isolation
var result1 = bruteForce.Search(data, 15, EuclideanDistance, random);
var result2 = kdTree.Search(data, 15, EuclideanDistance, random);
```

### Extensibility

Implement custom strategies by implementing the appropriate interface:

```csharp
public class MyCustomSearch<TMetric> : INeighborSearchStrategy<ILargeData, TMetric>
    where TMetric : IMetricMarker
{
    public NeighborSearchResult Search(/* ... */)
    {
        // Your custom algorithm
    }
}
```

## Current Status

**Phase 1 Complete** (v0.1.0):
- ✅ Core infrastructure with phantom types
- ✅ Type-safe builder with compile-time validation
- ✅ Neighbor Search strategy + BruteForce implementation
- ✅ Local Metric strategy + Binary Search implementation
- ✅ Membership Strength strategy + Exponential Kernel implementation

**In Progress**:
- 🚧 Remaining 6 strategy phases
- 🚧 Smart factory with data shape analysis
- 🚧 Additional strategy implementations (KD-tree, approximate search, etc.)
