# <a id="Flowthru_Misc_ML_UMAP_Strategies_GraphRefinement_Implementations_AdaptiveThresholding"></a> Class AdaptiveThresholding

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement.Implementations](Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement.Implementations.md)  
Assembly: Flowthru.Misc.ML.dll  

Standard UMAP graph refinement using adaptive threshold based on optimization epochs.

```csharp
public sealed class AdaptiveThresholding : IGraphRefinementStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AdaptiveThresholding](Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement.Implementations.AdaptiveThresholding.md)

#### Implements

[IGraphRefinementStrategy](Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement.IGraphRefinementStrategy.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
This implementation follows the standard UMAP algorithm's approach to graph refinement:
edges with weight below <code>max_weight / n_epochs</code> are removed, as they would be
sampled less than once during the optimization process.
</p>
<p>
<b>Rationale:</b> During stochastic gradient descent, edges are sampled proportionally
to their weights. An edge with weight <code>w</code> in a graph with maximum weight <code>w_max</code>
will be sampled approximately <code>(w / w_max) × n_epochs</code> times. Edges sampled less
than once have negligible impact on the final embedding.
</p>
<p>
<b>Implementation:</b> Uses direct CSR (Compressed Sparse Row) storage manipulation for
O(nnz) performance. Single-pass filter through non-zero entries only, avoiding O(n²) iteration.
</p>
<p>
<b>Time complexity:</b> O(nnz) where nnz is the number of non-zero entries in the graph
</p>
<p>
<b>Space complexity:</b> O(nnz) - creates new storage arrays during filtering
</p>
<p>
Python UMAP reference: Lines 1063-1076 in <code>simplicial_set_embedding()</code>
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_GraphRefinement_Implementations_AdaptiveThresholding_RefineGraph_MathNet_Numerics_LinearAlgebra_Single_SparseMatrix_System_Int32_"></a> RefineGraph\(SparseMatrix, int\)

Refines the graph by removing edges below an adaptive threshold using CSR direct access.

```csharp
public GraphRefinementResult RefineGraph(SparseMatrix graph, int nEpochs)
```

#### Parameters

`graph` SparseMatrix

Fuzzy simplicial set to refine (modified in-place).

`nEpochs` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of optimization epochs planned.

#### Returns

 [GraphRefinementResult](Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement.GraphRefinementResult.md)

Refinement result with statistics.

