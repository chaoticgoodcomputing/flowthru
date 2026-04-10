# <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CosineMetric"></a> Class CosineMetric

Namespace: [Flowthru.Misc.ML.UMAP.Core.Utils](Flowthru.Misc.ML.UMAP.Core.Utils.md)  
Assembly: Flowthru.Misc.ML.dll  

Cosine distance metric (angular distance).

```csharp
public sealed class CosineMetric : IMetric
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CosineMetric](Flowthru.Misc.ML.UMAP.Core.Utils.CosineMetric.md)

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

Cosine distance measures the angle between vectors:
d(x, y) = 1 - (x·y) / (||x|| ||y||)

Range: [0, 2] where 0 = identical direction, 1 = orthogonal, 2 = opposite direction.
Ignores magnitude, only considers direction.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CosineMetric_DisconnectionDistance"></a> DisconnectionDistance

UMAP's optimization can be unstable for very large distances. Setting a disconnection distance allows the algorithm to treat points beyond this distance as effectively disconnected, which can improve convergence and embedding quality for certain datasets. For cosine distance, a natural choice is 2.0, which corresponds to opposite direction.

```csharp
public float? DisconnectionDistance { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CosineMetric_Instance"></a> Instance

Singleton instance of Cosine metric.
Use this to avoid allocations.

```csharp
public static CosineMetric Instance { get; }
```

#### Property Value

 [CosineMetric](Flowthru.Misc.ML.UMAP.Core.Utils.CosineMetric.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CosineMetric_Name"></a> Name

Name of the metric, used for logging and strategy selection. This is a simple identifier and does not affect behavior.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CosineMetric_SupportsAngularProjection"></a> SupportsAngularProjection

Indicates whether this metric benefits from angular random projection forests. Cosine distance benefits from angular RPs, so this returns true. This allows the use of angular RP forests for neighbor search, which can improve performance and embedding quality when using cosine distance.

```csharp
public bool SupportsAngularProjection { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CosineMetric_Distance_System_ReadOnlySpan_System_Single__System_ReadOnlySpan_System_Single__"></a> Distance\(ReadOnlySpan<float\>, ReadOnlySpan<float\>\)

Compute cosine distance: 1 - (dot product / product of norms).

```csharp
public float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
```

#### Parameters

`x` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

`y` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

