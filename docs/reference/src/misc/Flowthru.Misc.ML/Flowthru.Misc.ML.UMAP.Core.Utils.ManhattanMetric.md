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

UMAP's optimization can be unstable for very large distances. Setting a disconnection distance allows the algorithm to treat points beyond this distance as effectively disconnected, which can improve convergence and embedding quality for certain datasets.

```csharp
public float? DisconnectionDistance { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_ManhattanMetric_Instance"></a> Instance

Singleton instance of Manhattan metric.
Use this to avoid allocations.

```csharp
public static ManhattanMetric Instance { get; }
```

#### Property Value

 [ManhattanMetric](Flowthru.Misc.ML.UMAP.Core.Utils.ManhattanMetric.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_ManhattanMetric_Name"></a> Name

Name of the metric, used for logging and strategy selection. This is a simple identifier and does not affect behavior.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_ManhattanMetric_SupportsAngularProjection"></a> SupportsAngularProjection

Indicates whether this metric benefits from angular random projection forests. Manhattan distance does not benefit from angular RPs, so this returns false. Metrics that do benefit (e.g., cosine) should return true to enable the use of angular RP forests for neighbor search, which can improve performance and embedding quality.

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

