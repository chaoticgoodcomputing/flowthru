# <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_Implementations_EuclideanSGDOptimized"></a> Class EuclideanSGDOptimized

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.Implementations](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.Implementations.md)  
Assembly: Flowthru.Misc.ML.dll  

Optimized Euclidean distance SGD optimizer for UMAP layout optimization (default implementation).

```csharp
public sealed class EuclideanSGDOptimized : ILayoutOptimizationStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EuclideanSGDOptimized](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.Implementations.EuclideanSGDOptimized.md)

#### Implements

[ILayoutOptimizationStrategy](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.ILayoutOptimizationStrategy.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<b>✓ This is the default layout optimization strategy as of November 2025.</b>
Provides strict performance improvements over <xref href="Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.Implementations.EuclideanSGD" data-throw-if-not-resolved="false"></xref> with identical embedding quality.
</p>
<p>
This implementation optimizes the standard UMAP SGD algorithm with:
</p>
<ul><li><b>Direct array access</b>: Uses <xref href="MathNet.Numerics.LinearAlgebra.Storage.DenseColumnMajorMatrixStorage%601.Data" data-throw-if-not-resolved="false"></xref> for vectorized operations</li><li><b>Cache-friendly memory access</b>: Exploits column-major layout for better locality</li><li><b>Early stopping</b>: Monitors convergence and terminates when vertex movement stabilizes</li><li><b>Reduced overhead</b>: Eliminates repeated matrix indexing overhead</li></ul>
<p>
<b>Validated performance improvements (Fashion MNIST 70k samples):</b>
</p>
<ul><li>Layout Optimization: 62.4s → ~42s (~33% faster)</li><li>Total UMAP Runtime: 121.7s → ~101s (~17% faster overall)</li><li>Embedding Quality: Identical (validated via neighborhood preservation)</li></ul>
<p>
<b>Usage:</b> Automatically selected by <code>UmapPipeline.Create()</code>. To use the reference
implementation for testing, explicitly call <code>.WithLayoutOptimization(new EuclideanSGD())</code>.
</p>
<p>
Python UMAP reference: <code>optimize_layout_euclidean()</code> in <code>layouts.py</code> (lines 238-441)
</p>

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_Implementations_EuclideanSGDOptimized__ctor_System_Single_"></a> EuclideanSGDOptimized\(float\)

Initializes a new instance of the optimized SGD optimizer.

```csharp
public EuclideanSGDOptimized(float convergenceThreshold = 0.001)
```

#### Parameters

`convergenceThreshold` [float](https://learn.microsoft.com/dotnet/api/system.single)

Average vertex movement threshold for early stopping.
Default is 0.001 (0.1% of coordinate space).
Set to 0 to disable early stopping.

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_Implementations_EuclideanSGDOptimized_Optimize_MathNet_Numerics_LinearAlgebra_Matrix_System_Single__Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_GraphEdge___System_Single___System_Int32_Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters_System_Random_"></a> Optimize\(Matrix<float\>, GraphEdge\[\], float\[\], int, OptimizationParameters, Random\)

Optimizes the embedding layout using stochastic gradient descent.

```csharp
public LayoutOptimizationResult Optimize(Matrix<float> initialEmbedding, GraphEdge[] graphEdges, float[] samplingSchedule, int nEpochs, OptimizationParameters parameters, Random random)
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

