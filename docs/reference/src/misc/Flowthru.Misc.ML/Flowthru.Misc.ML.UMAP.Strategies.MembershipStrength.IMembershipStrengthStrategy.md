# <a id="Flowthru_Misc_ML_UMAP_Strategies_MembershipStrength_IMembershipStrengthStrategy"></a> Interface IMembershipStrengthStrategy

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength](Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength.md)  
Assembly: Flowthru.Misc.ML.dll  

Strategy interface for computing fuzzy simplicial set membership strengths.
This is the third phase of the UMAP algorithm.

```csharp
public interface IMembershipStrengthStrategy
```

## Remarks

<p>
The membership strength phase converts k-NN distances into membership probabilities
for the fuzzy simplicial set. Each edge (i,j) gets a membership strength μᵢⱼ ∈ [0,1]
that represents how strongly point j belongs to the fuzzy neighborhood of point i.
</p>
<p>
<b>Standard approach (exponential kernel):</b>
</p>
<pre><code class="lang-csharp">μᵢⱼ = exp(-(max(0, dᵢⱼ - ρᵢ)) / σᵢ)</code></pre>
<p>
where dᵢⱼ is the distance, ρᵢ is the local connectivity distance, and σᵢ is the bandwidth.
</p>
<p>
After computing directed membership strengths, fuzzy set operations (union/intersection)
combine them into a symmetric global graph structure.
</p>
<p>
Python UMAP reference: <code>compute_membership_strengths()</code> and <code>fuzzy_simplicial_set()</code>
functions in <code>umap_.py</code> (lines ~260-450)
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_MembershipStrength_IMembershipStrengthStrategy_ComputeMembershipStrengths_System_Int32_____System_Single_____System_Single___System_Single___System_Single_"></a> ComputeMembershipStrengths\(int\[\]\[\], float\[\]\[\], float\[\], float\[\], float\)

Computes membership strengths for the fuzzy simplicial set.

```csharp
SparseMatrix ComputeMembershipStrengths(int[][] knnIndices, float[][] knnDistances, float[] sigmas, float[] rhos, float setOpMixRatio = 1)
```

#### Parameters

`knnIndices` [int](https://learn.microsoft.com/dotnet/api/system.int32)\[\]\[\]

Indices of k-nearest neighbors for each point.
Array shape: (n_samples, n_neighbors)

`knnDistances` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]\[\]

Distances to k-nearest neighbors for each point.
Array shape: (n_samples, n_neighbors)

`sigmas` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

Bandwidth parameters from local metric computation.
Array shape: (n_samples,)

`rhos` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

Local connectivity distances from local metric computation.
Array shape: (n_samples,)

`setOpMixRatio` [float](https://learn.microsoft.com/dotnet/api/system.single)

Interpolation between fuzzy union (1.0) and intersection (0.0).
Controls how local fuzzy sets are combined into global structure.
Range: [0.0, 1.0]

#### Returns

 SparseMatrix

A sparse matrix representing the fuzzy simplicial set.
Shape: (n_samples, n_samples)
Matrix[i,j] represents the membership strength of the edge from i to j.
After set operations, the matrix should be symmetric.

#### Remarks

<p>
<b>Implementation requirements:</b>
</p>
<ol><li>Compute directed membership strengths μᵢⱼ for each edge</li><li>Apply fuzzy set operation: μ = α(μᵢⱼ + μⱼᵢ - μᵢⱼμⱼᵢ) + (1-α)μᵢⱼμⱼᵢ</li><li>Eliminate zero entries from sparse matrix</li><li>Ensure matrix is symmetric after set operations</li></ol>

