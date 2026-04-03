# <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_INeighborSearchStrategy"></a> Interface INeighborSearchStrategy

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.md)  
Assembly: Flowthru.Misc.ML.dll  

Strategy interface for computing k-nearest neighbors in high-dimensional space.
This is the first phase of the UMAP algorithm.

```csharp
public interface INeighborSearchStrategy
```

## Remarks

<p>
The neighbor search phase computes the k-nearest neighbors for each point in the dataset.
Different strategies provide different trade-offs between accuracy, speed, and memory usage:
</p>
<ul><li><b>Exact methods</b> (e.g., brute force): O(n²) time, 100% accurate, recommended for datasets &lt; 4096 samples</li><li><b>Tree methods</b> (e.g., KD-tree): O(n log n) time, exact or approximate, suitable for medium datasets with low-to-medium dimensions</li><li><b>Approximate methods</b> (e.g., NN-Descent): O(n^1.14) time, ~99% accurate, recommended for datasets ≥ 4096 samples</li><li><b>Precomputed</b>: O(1) time, user provides k-NN graph, suitable when neighbors are already known</li></ul>
<p>
Python UMAP reference: <code>nearest_neighbors()</code> function in <code>umap_.py</code> (lines ~260-300)
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_INeighborSearchStrategy_Search_System_Single_____System_Int32_Flowthru_Misc_ML_UMAP_Core_Markers_IMetric_System_Random_"></a> Search\(float\[\]\[\], int, IMetric, Random\)

Computes k-nearest neighbors for all points in the dataset.

```csharp
NeighborSearchResult Search(float[][] data, int nNeighbors, IMetric metric, Random random)
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

