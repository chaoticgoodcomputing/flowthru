# <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_Implementations_EuclideanSGD"></a> Class EuclideanSGD

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.Implementations](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.Implementations.md)  
Assembly: Flowthru.Misc.ML.dll  

Standard Euclidean distance SGD optimizer for UMAP layout optimization.

```csharp
public sealed class EuclideanSGD : ILayoutOptimizationStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EuclideanSGD](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.Implementations.EuclideanSGD.md)

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
<b>⚠️ NOTE:</b> This is the reference implementation retained for testing and historical purposes.
For production use, prefer <xref href="Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.Implementations.EuclideanSGDOptimized" data-throw-if-not-resolved="false"></xref> which provides 1.5-2x speedup
through direct array access and early stopping while maintaining identical embedding quality.
</p>
<p>
This implementation follows the Python UMAP reference for Euclidean output spaces.
It uses stochastic gradient descent with:
</p>
<ul><li>Attractive forces based on graph edge weights and a/b curve parameters</li><li>Repulsive forces from negative sampling of non-neighbors</li><li>Linear learning rate decay</li></ul>
<p>
<b>Time complexity:</b> O(E × n_epochs + N × k × n_epochs) where E = edges, N = vertices, k = negative samples
</p>
<p>
Python UMAP reference: <code>optimize_layout_euclidean()</code> in <code>layouts.py</code> (lines 238-441)
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_Implementations_EuclideanSGD_Optimize_MathNet_Numerics_LinearAlgebra_Matrix_System_Single__Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_GraphEdge___System_Single___System_Int32_Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters_System_Random_"></a> Optimize\(Matrix<float\>, GraphEdge\[\], float\[\], int, OptimizationParameters, Random\)

Initializes the optimizer. No state is stored in this implementation, so this is a no-op. In more complex strategies, this could be used to set up data structures or precompute values based on the initial embedding and graph.

```csharp
public LayoutOptimizationResult Optimize(Matrix<float> initialEmbedding, GraphEdge[] graphEdges, float[] samplingSchedule, int nEpochs, OptimizationParameters parameters, Random random)
```

#### Parameters

`initialEmbedding` Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

`graphEdges` [GraphEdge](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.GraphEdge.md)\[\]

`samplingSchedule` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

`nEpochs` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`parameters` [OptimizationParameters](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.OptimizationParameters.md)

`random` [Random](https://learn.microsoft.com/dotnet/api/system.random)

#### Returns

 [LayoutOptimizationResult](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.LayoutOptimizationResult.md)

