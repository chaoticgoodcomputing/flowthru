# <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_BruteForceSearch"></a> Class BruteForceSearch

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.Implementations](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.Implementations.md)  
Assembly: Flowthru.Misc.ML.dll  

Brute-force exact k-nearest neighbor search.
Computes all pairwise distances - O(n²) time complexity.

```csharp
public sealed class BruteForceSearch : INeighborSearchStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BruteForceSearch](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.Implementations.BruteForceSearch.md)

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
This implementation computes the exact k-nearest neighbors by calculating all pairwise
distances and selecting the k smallest for each point. While this is computationally
expensive for large datasets, it guarantees 100% accuracy and is the fastest approach
for small datasets (typically &lt; 4096 samples).
</p>
<p>
<b>Characteristics:</b>
</p>
<ul><li><b>Time complexity</b>: O(n² × d) where n=samples, d=dimensions</li><li><b>Space complexity</b>: O(n × k) for output</li><li><b>Accuracy</b>: 100% (exact)</li><li><b>Recommended for</b>: Small datasets (&lt; 4096 samples)</li><li><b>Thread-safe</b>: Yes (read-only operations)</li></ul>
<p>
This is the reference implementation matching Python UMAP's behavior for small datasets
or when <code>metric='precomputed'</code> is not used.
</p>
<p>
Python reference: The brute-force path in <code>nearest_neighbors()</code> when exact k-NN
is computed via <code>pairwise_distances()</code> (Python UMAP lines ~2950-3000).
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_Implementations_BruteForceSearch_Search_System_Single_____System_Int32_Flowthru_Misc_ML_UMAP_Core_Markers_IMetric_System_Random_"></a> Search\(float\[\]\[\], int, IMetric, Random\)

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

