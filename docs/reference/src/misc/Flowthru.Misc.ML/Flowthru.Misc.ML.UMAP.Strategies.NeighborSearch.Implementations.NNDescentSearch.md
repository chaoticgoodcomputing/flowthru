# <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_NNDescentSearch"></a> Class NNDescentSearch

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.Implementations](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.Implementations.md)  
Assembly: Flowthru.Misc.ML.dll  

NN-Descent approximate k-nearest neighbor search.
Achieves ~99% accuracy with O(n^1.14) time complexity for large datasets.

```csharp
public sealed class NNDescentSearch : INeighborSearchStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NNDescentSearch](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.Implementations.NNDescentSearch.md)

#### Implements

[INeighborSearchStrategy](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.INeighborSearchStrategy.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
NN-Descent is an iterative algorithm that efficiently constructs approximate k-nearest neighbor
graphs through a local join operation. It achieves sub-quadratic time complexity while maintaining
high accuracy (typically 99%+ recall).
</p>
<p>
<b>Algorithm overview:</b>
</p>
<ol><li>Initialize with random projection trees (RP-trees) for quality starting neighbors</li><li>Fill remaining slots with random neighbors</li><li>Iteratively refine via local join: compare candidate neighbor pairs</li><li>Converge when update rate falls below threshold</li></ol>
<p>
<b>Performance characteristics:</b>
</p>
<ul><li><b>Time complexity</b>: O(n^1.14 × d) empirically, vs O(n² × d) for brute-force</li><li><b>Space complexity</b>: O(n × k + trees × n / leaf_size)</li><li><b>Accuracy</b>: ~99% (approximate, configurable via parameters)</li><li><b>Recommended for</b>: Large datasets (≥ 4096 samples)</li><li><b>Thread-safe</b>: No (constructs new index per call)</li></ul>
<p>
Based on: Dong, Moses, and Li. "Efficient K-Nearest Neighbor Graph Construction for Generic
Similarity Measures" (WWW 2011). Implementation follows PyNNDescent reference.
</p>
<p>
Python reference: <code>nn_descent()</code> function in <code>pynndescent_.py</code> and supporting functions
in <code>utils.py</code> and <code>rp_trees.py</code> from the PyNNDescent library.
</p>

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_NNDescentSearch_DeltaThreshold"></a> DeltaThreshold

Convergence threshold as fraction of total edges.
Algorithm stops when updates per iteration drop below: delta × k × n.
Typical value: 0.001 (0.1% of edges changing).

```csharp
public float DeltaThreshold { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_NNDescentSearch_LeafSize"></a> LeafSize

Leaf size for random projection trees.
Smaller leaves increase tree depth and initialization quality.
Typical range: 10-20.

```csharp
public int LeafSize { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_NNDescentSearch_LowMemory"></a> LowMemory

If true, uses block-based processing to reduce memory usage at cost of ~2x speed.
If false, maintains in-memory set for faster duplicate checking.

```csharp
public bool LowMemory { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_NNDescentSearch_MaxCandidates"></a> MaxCandidates

Maximum number of candidate neighbors to consider per point per iteration.
If 0 (default), auto-configures as: min(60, k).
Higher values improve accuracy but increase iteration cost.

```csharp
public int MaxCandidates { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Controls the breadth of the local join search. Typical values: 30-60.
Each iteration costs O(n × max_candidates² × d).

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_NNDescentSearch_MaxIterations"></a> MaxIterations

Maximum number of NN-descent iterations.
If 0 (default), auto-configures as: max(5, round(log2(n))).
More iterations improve accuracy but increase runtime.

```csharp
public int MaxIterations { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Algorithm typically converges in 5-10 iterations via delta threshold.
Early stopping prevents unnecessary work.

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_NNDescentSearch_NumTrees"></a> NumTrees

Number of random projection trees to build for initialization.
If 0 (default), auto-configures as: min(32, 5 + round(n^0.25)).
More trees improve initialization quality but increase build time.

```csharp
public int NumTrees { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Python UMAP typically uses 5-32 trees depending on dataset size.
Each tree costs O(n log n × d) to build.

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_NNDescentSearch_Verbose"></a> Verbose

If true, prints progress information to console during search.

```csharp
public bool Verbose { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_NNDescentSearch_Search_System_Single_____System_Int32_Flowthru_Misc_ML_UMAP_Core_Markers_IMetric_System_Random_"></a> Search\(float\[\]\[\], int, IMetric, Random\)

Computes k-nearest neighbors for all points in the dataset.

```csharp
public NeighborSearchResult Search(float[][] data, int nNeighbors, IMetric metric, Random random)
```

#### Parameters

`data` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]\[\]

Input data as jagged array where each row represents a data point (n_samples × n_features).
data[i] is a float array of length n_features containing the feature values for sample i.
All rows must have the same length.

`nNeighbors` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of nearest neighbors to find for each point.
Must be at least 2 and at most n_samples - 1.

`metric` [IMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IMetric.md)

Distance metric for computing pairwise distances between points.

`random` [Random](https://learn.microsoft.com/dotnet/api/system.random)

Random number generator for any randomized algorithms (e.g., approximate search).
Ensures reproducibility when a seed is provided.

#### Returns

 [NeighborSearchResult](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.NeighborSearchResult.md)

A result containing:
- <b>Indices</b>: n_samples × n_neighbors array where Indices[i][j] is the index of the j-th nearest neighbor of point i
- <b>Distances</b>: n_samples × n_neighbors array where Distances[i][j] is the distance to that neighbor
- <b>SearchIndex</b>: Optional search index structure for future queries (e.g., for transform), or null if not applicable

Note: Indices[i][0] should always be i (each point is its own nearest neighbor with distance 0).

#### Remarks

<p>
<b>Implementation requirements:</b>
</p>
<ol><li>Results must be sorted by distance (ascending) for each point</li><li>First neighbor of each point should typically be itself (distance 0)</li><li>For precomputed distances with disconnected components, use index -1 and distance ∞</li><li>Thread-safe if marked as such in implementation</li></ol>

