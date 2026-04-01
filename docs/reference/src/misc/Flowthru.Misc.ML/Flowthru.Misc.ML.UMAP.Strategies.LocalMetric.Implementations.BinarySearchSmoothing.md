# <a id="Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_Implementations_BinarySearchSmoothing"></a> Class BinarySearchSmoothing

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.Implementations](Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.Implementations.md)  
Assembly: Flowthru.Misc.ML.dll  

Binary search-based local metric smoothing.
Computes bandwidth parameters using iterative binary search to match target cardinality.

```csharp
public sealed class BinarySearchSmoothing : ILocalMetricStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BinarySearchSmoothing](Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.Implementations.BinarySearchSmoothing.md)

#### Implements

[ILocalMetricStrategy](Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.ILocalMetricStrategy.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
This is the standard UMAP approach for computing local metric parameters. For each point,
it uses binary search to find the bandwidth σ that makes the fuzzy cardinality of its
neighborhood equal to the target value (log₂(k)).
</p>
<p>
<b>Algorithm:</b>
</p>
<ol><li>Compute ρᵢ (distance to nearest connected neighbor) based on local connectivity</li><li>Use binary search to find σᵢ such that Σⱼ exp(-(dᵢⱼ - ρᵢ)/σᵢ) ≈ log₂(k)</li><li>Apply minimum distance scaling to prevent numerical instability</li></ol>
<p>
<b>Characteristics:</b>
</p>
<ul><li><b>Time complexity</b>: O(n × k × log(max_iter)) ≈ O(n × k)</li><li><b>Space complexity</b>: O(n) for output</li><li><b>Convergence</b>: Typically within 10-20 iterations per point</li><li><b>Thread-safe</b>: Yes (each point computed independently)</li></ul>
<p>
Python reference: <code>smooth_knn_dist()</code> function in <code>umap_.py</code> (lines ~143-250).
This is a direct port of the numba-jitted Python implementation.
</p>

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_Implementations_BinarySearchSmoothing_MaxIterations"></a> MaxIterations

Maximum number of binary search iterations per point.
Typically converges much faster, but this provides a safety limit.

```csharp
public int MaxIterations { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_Implementations_BinarySearchSmoothing_ComputeLocalMetrics_System_Single_____System_Single_System_Single_System_Single_"></a> ComputeLocalMetrics\(float\[\]\[\], float, float, float\)

Computes smooth local metric parameters (bandwidths and local connectivity distances).

```csharp
public LocalMetricResult ComputeLocalMetrics(float[][] knnDistances, float k, float localConnectivity = 1, float bandwidth = 1)
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

