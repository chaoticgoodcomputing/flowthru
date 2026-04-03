# <a id="Flowthru_Misc_ML_UMAP_Strategies_MembershipStrength_Implementations_ExponentialKernel"></a> Class ExponentialKernel

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength.Implementations](Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength.Implementations.md)  
Assembly: Flowthru.Misc.ML.dll  

Exponential kernel-based membership strength computation.
Uses the standard UMAP exponential kernel to convert distances into probabilities.

```csharp
public sealed class ExponentialKernel : IMembershipStrengthStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ExponentialKernel](Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength.Implementations.ExponentialKernel.md)

#### Implements

[IMembershipStrengthStrategy](Flowthru.Misc.ML.UMAP.Strategies.MembershipStrength.IMembershipStrengthStrategy.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
This is the standard UMAP approach for computing fuzzy set membership strengths.
It applies an exponential kernel centered at the local connectivity distance (ρᵢ)
with bandwidth σᵢ:
</p>
<pre><code class="lang-csharp">μᵢⱼ = {
  1.0                           if dᵢⱼ ≤ ρᵢ or σᵢ = 0
  exp(-(dᵢⱼ - ρᵢ) / σᵢ)        otherwise
}</code></pre>
<p>
After computing directed strengths, the algorithm applies fuzzy set operations to
symmetrize the graph. The set operation interpolates between fuzzy union and intersection:
</p>
<pre><code class="lang-csharp">μ = α(μ_forward + μ_reverse - μ_forward × μ_reverse) + (1-α)(μ_forward × μ_reverse)</code></pre>
<p>
where α is the set operation mix ratio (typically 1.0 for pure fuzzy union).
</p>
<p>
<b>Characteristics:</b>
</p>
<ul><li><b>Time complexity</b>: O(n × k) for computing strengths</li><li><b>Space complexity</b>: O(n × k) sparse matrix</li><li><b>Graph density</b>: Approximately k edges per node</li><li><b>Thread-safe</b>: Yes for reading, exclusive write access needed</li></ul>
<p>
Python reference: <code>compute_membership_strengths()</code> in <code>umap_.py</code> (lines ~260-330)
and fuzzy set operations in <code>fuzzy_simplicial_set()</code> (lines ~450-470).
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_MembershipStrength_Implementations_ExponentialKernel_ComputeMembershipStrengths_System_Int32_____System_Single_____System_Single___System_Single___System_Single_"></a> ComputeMembershipStrengths\(int\[\]\[\], float\[\]\[\], float\[\], float\[\], float\)

Computes membership strengths for the fuzzy simplicial set.

```csharp
public SparseMatrix ComputeMembershipStrengths(int[][] knnIndices, float[][] knnDistances, float[] sigmas, float[] rhos, float setOpMixRatio = 1)
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

