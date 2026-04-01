# <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_ILayoutOptimizationStrategy"></a> Interface ILayoutOptimizationStrategy

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.md)  
Assembly: Flowthru.Misc.ML.dll  

Strategy interface for optimizing low-dimensional embeddings via stochastic gradient descent.
This is the seventh phase of the UMAP algorithm.

```csharp
public interface ILayoutOptimizationStrategy
```

## Remarks

<p>
The layout optimization phase refines the initial embedding by minimizing the fuzzy set
cross entropy between the high-dimensional and low-dimensional fuzzy simplicial sets.
This is done through stochastic gradient descent with two types of forces:
</p>
<ul><li><b>Attractive forces</b>: Pull connected points closer based on graph edge weights</li><li><b>Repulsive forces</b>: Push non-connected points apart via negative sampling</li></ul>
<p>
The force curves are parameterized by <code>a</code> and <code>b</code>, which are derived from
the <code>min_dist</code> and <code>spread</code> hyperparameters via curve fitting.
</p>
<p>
Python UMAP reference: <code>optimize_layout_euclidean()</code> in <code>layouts.py</code> (lines 238-441)
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_ILayoutOptimizationStrategy_Optimize_MathNet_Numerics_LinearAlgebra_Matrix_System_Single__Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_GraphEdge___System_Single___System_Int32_Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters_System_Random_"></a> Optimize\(Matrix<float\>, GraphEdge\[\], float\[\], int, OptimizationParameters, Random\)

Optimizes the embedding layout using stochastic gradient descent.

```csharp
LayoutOptimizationResult Optimize(Matrix<float> initialEmbedding, GraphEdge[] graphEdges, float[] samplingSchedule, int nEpochs, OptimizationParameters parameters, Random random)
```

#### Parameters

`initialEmbedding` Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Initial embedding from layout initialization strategy.
Shape: (n_samples, n_components)
This matrix will be modified in-place during optimization.

`graphEdges` [GraphEdge](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.GraphEdge.md)\[\]

Edges in the fuzzy simplicial set to optimize.
Contains (head_index, tail_index, weight) tuples.

`samplingSchedule` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

Sampling schedule that determines how often each edge is sampled.
Array length matches number of edges.

`nEpochs` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of optimization epochs to run.
Must match the value used to compute the sampling schedule.

`parameters` [OptimizationParameters](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.OptimizationParameters.md)

Optimization parameters including learning rate, repulsion strength, etc.

`random` [Random](https://learn.microsoft.com/dotnet/api/system.random)

Random number generator for negative sampling and reproducibility.

#### Returns

 [LayoutOptimizationResult](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.LayoutOptimizationResult.md)

The optimized embedding (same matrix as initialEmbedding, modified in-place).

#### Remarks

<p>
<b>Implementation requirements:</b>
</p>
<ol><li>Initialize epoch-tracking arrays for sampling schedule</li><li>For each epoch:</li><li>Report progress if verbosity enabled</li></ol>

