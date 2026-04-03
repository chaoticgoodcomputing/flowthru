# <a id="Flowthru_Misc_ML_UMAP_Core_Utils_ManhattanMetric"></a> Class ManhattanMetric

Namespace: [Flowthru.Misc.ML.UMAP.Core.Utils](Flowthru.Misc.ML.UMAP.Core.Utils.md)  
Assembly: Flowthru.Misc.ML.dll  

Manhattan (L1) distance metric.

```csharp
public sealed class ManhattanMetric : IMetric
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ManhattanMetric](Flowthru.Misc.ML.UMAP.Core.Utils.ManhattanMetric.md)

#### Implements

[IMetric](Flowthru.Misc.ML.UMAP.Core.Markers.IMetric.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Manhattan distance is the sum of absolute differences:
d(x, y) = sum(|x[i] - y[i]|)

Also known as taxicab or city block distance.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_ManhattanMetric_DisconnectionDistance"></a> DisconnectionDistance

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

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_ManhattanMetric_Instance"></a> Instance

Singleton instance of Manhattan metric.
Use this to avoid allocations.

```csharp
public static ManhattanMetric Instance { get; }
```

#### Property Value

 [ManhattanMetric](Flowthru.Misc.ML.UMAP.Core.Utils.ManhattanMetric.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_ManhattanMetric_Name"></a> Name

Human-readable name of the metric (e.g., "euclidean", "cosine").
Used for logging and serialization.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_ManhattanMetric_SupportsAngularProjection"></a> SupportsAngularProjection

Whether this metric benefits from angular (cosine-based) random projection forests.
Angular metrics (cosine, correlation) use different RP tree splits than Euclidean metrics.

```csharp
public bool SupportsAngularProjection { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_ManhattanMetric_Distance_System_ReadOnlySpan_System_Single__System_ReadOnlySpan_System_Single__"></a> Distance\(ReadOnlySpan<float\>, ReadOnlySpan<float\>\)

Compute Manhattan distance: sum of absolute differences.

```csharp
public float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
```

#### Parameters

`x` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

`y` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

