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

UMAP's optimization can be unstable for very large distances. Setting a disconnection distance allows the algorithm to treat points beyond this distance as effectively disconnected, which can improve convergence and embedding quality for certain datasets.

```csharp
public float? DisconnectionDistance { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_EuclideanMetric_Instance"></a> Instance

Singleton instance of Euclidean metric.
Use this to avoid allocations.

```csharp
public static EuclideanMetric Instance { get; }
```

#### Property Value

 [EuclideanMetric](Flowthru.Misc.ML.UMAP.Core.Utils.EuclideanMetric.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_EuclideanMetric_Name"></a> Name

Name of the metric, used for logging and strategy selection. This is a simple identifier and does not affect behavior.
It should be unique among built-in metrics to allow strategies to recognize it, but can be arbitrary for custom metrics.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_EuclideanMetric_SupportsAngularProjection"></a> SupportsAngularProjection

Indicates whether this metric benefits from angular random projection forests. Euclidean distance does not benefit from angular RPs, so this returns false. Metrics that do benefit (e.g., cosine) should return true to enable the use of angular RP forests for neighbor search, which can improve performance and embedding quality.

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

