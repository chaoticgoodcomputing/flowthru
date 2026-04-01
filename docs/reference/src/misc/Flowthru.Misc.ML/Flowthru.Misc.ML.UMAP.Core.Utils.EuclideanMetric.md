# <a id="Flowthru_Misc_ML_UMAP_Core_Utils_EuclideanMetric"></a> Class EuclideanMetric

Namespace: [Flowthru.Misc.ML.UMAP.Core.Utils](Flowthru.Misc.ML.UMAP.Core.Utils.md)  
Assembly: Flowthru.Misc.ML.dll  

Euclidean (L2) distance metric with gradient support.

```csharp
public sealed class EuclideanMetric : IOutputMetric, IMetric
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EuclideanMetric](Flowthru.Misc.ML.UMAP.Core.Utils.EuclideanMetric.md)

#### Implements

[IOutputMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IOutputMetric.md), 
[IMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IMetric.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Euclidean distance is the straight-line distance in n-dimensional space:
d(x, y) = sqrt(sum((x[i] - y[i])^2))

This is the most common metric and has specialized optimizations in layout optimization.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_EuclideanMetric_DisconnectionDistance"></a> DisconnectionDistance

Maximum meaningful distance for bounded metrics, or null for unbounded metrics.
Used to handle disconnected components in the k-NN graph.

```csharp
public float? DisconnectionDistance { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

#### Remarks

<p>
Examples:
- Euclidean: null (unbounded)
- Cosine: 2.0 (ranges from 0 to 2)
- Jaccard: 1.0 (ranges from 0 to 1)
</p>
<p>
When set, distances at or beyond this value indicate maximally dissimilar points
that should be treated as disconnected in the manifold approximation.
</p>

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_EuclideanMetric_Instance"></a> Instance

Singleton instance of Euclidean metric.
Use this to avoid allocations.

```csharp
public static EuclideanMetric Instance { get; }
```

#### Property Value

 [EuclideanMetric](Flowthru.Misc.ML.UMAP.Core.Utils.EuclideanMetric.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_EuclideanMetric_Name"></a> Name

Human-readable name of the metric (e.g., "euclidean", "cosine").
Used for logging and serialization.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_EuclideanMetric_SupportsAngularProjection"></a> SupportsAngularProjection

Whether this metric benefits from angular (cosine-based) random projection forests.
Angular metrics (cosine, correlation) use different RP tree splits than Euclidean metrics.

```csharp
public bool SupportsAngularProjection { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_EuclideanMetric_Distance_System_ReadOnlySpan_System_Single__System_ReadOnlySpan_System_Single__"></a> Distance\(ReadOnlySpan<float\>, ReadOnlySpan<float\>\)

Compute Euclidean distance: sqrt(sum of squared differences).

```csharp
public float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
```

#### Parameters

`x` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

`y` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_EuclideanMetric_DistanceWithGradient_System_ReadOnlySpan_System_Single__System_ReadOnlySpan_System_Single__System_Single__System_Span_System_Single__"></a> DistanceWithGradient\(ReadOnlySpan<float\>, ReadOnlySpan<float\>, out float, Span<float\>\)

Compute Euclidean distance and its gradient: ∇d/∂x = (x - y) / ||x - y||

```csharp
public void DistanceWithGradient(ReadOnlySpan<float> x, ReadOnlySpan<float> y, out float distance, Span<float> gradient)
```

#### Parameters

`x` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

`y` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

`distance` [float](https://learn.microsoft.com/dotnet/api/system.single)

`gradient` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

