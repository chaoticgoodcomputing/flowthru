# <a id="Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_ILocalMetricStrategy"></a> Interface ILocalMetricStrategy

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.LocalMetric](Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.md)  
Assembly: Flowthru.Misc.ML.dll  

Strategy interface for computing smooth approximations of local distances.
This is the second phase of the UMAP algorithm.

```csharp
public interface ILocalMetricStrategy
```

## Remarks

<p>
The local metric phase computes bandwidth parameters (σᵢ and ρᵢ) for each point that
normalize the local neighborhood structure. This handles varying local densities in the data:
</p>
<ul><li><b>σᵢ (sigma)</b>: Bandwidth of the exponential kernel for point i</li><li><b>ρᵢ (rho)</b>: Distance to the nearest connected neighbor for point i</li></ul>
<p>
These parameters ensure that each point has roughly the same "effective" number of neighbors
regardless of the local density, which is crucial for constructing a consistent fuzzy
simplicial set representation of the manifold.
</p>
<p>
<b>Mathematical goal:</b> Find σᵢ such that the fuzzy cardinality of the neighborhood equals k:
</p>
<pre><code class="lang-csharp">Σⱼ exp(-(dᵢⱼ - ρᵢ) / σᵢ) = log₂(k)</code></pre>
<p>
Python UMAP reference: <code>smooth_knn_dist()</code> function in <code>umap_.py</code> (lines ~143-250)
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_ILocalMetricStrategy_ComputeLocalMetrics_System_Single_____System_Single_System_Single_System_Single_"></a> ComputeLocalMetrics\(float\[\]\[\], float, float, float\)

Computes smooth local metric parameters (bandwidths and local connectivity distances).

```csharp
LocalMetricResult ComputeLocalMetrics(float[][] knnDistances, float k, float localConnectivity = 1, float bandwidth = 1)
```

#### Parameters

`knnDistances` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]\[\]

Distance to k-nearest neighbors for each point.
Array shape: (n_samples, n_neighbors)
Each row should be sorted in ascending order.

`k` [float](https://learn.microsoft.com/dotnet/api/system.single)

Target number of effective neighbors (typically the same as n_neighbors).
Used to calibrate the bandwidth parameter.

`localConnectivity` [float](https://learn.microsoft.com/dotnet/api/system.single)

Number of nearest neighbors that should be assumed to be connected at a local level.
Typically 1.0, meaning the nearest neighbor is always assumed connected (distance weight = 1.0).
Higher values (e.g., 2.0-5.0) increase local connectivity.
Range: [1.0, k]

`bandwidth` [float](https://learn.microsoft.com/dotnet/api/system.single)

Target bandwidth multiplier for the exponential kernel.
Default: 1.0. Affects the target cardinality (target = log₂(k) × bandwidth).

#### Returns

 [LocalMetricResult](Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.LocalMetricResult.md)

A result containing:
- <b>Sigmas</b>: Bandwidth parameter for each point (length n_samples)
- <b>Rhos</b>: Distance to nearest connected neighbor for each point (length n_samples)

#### Remarks

<p>
<b>Implementation requirements:</b>
</p>
<ol><li>Handle the case where points have fewer than k non-zero distances</li><li>Apply minimum distance scaling to prevent numerical instability</li><li>Ensure rho ≤ distance to k-th neighbor for all points</li><li>Thread-safe for parallel processing of points</li></ol>

