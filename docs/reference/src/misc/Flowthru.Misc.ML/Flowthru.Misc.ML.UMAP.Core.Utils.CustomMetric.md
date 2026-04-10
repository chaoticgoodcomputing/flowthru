# <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CustomMetric"></a> Class CustomMetric

Namespace: [Flowthru.Misc.ML.UMAP.Core.Utils](Flowthru.Misc.ML.UMAP.Core.Utils.md)  
Assembly: Flowthru.Misc.ML.dll  

Custom metric wrapper for user-defined distance functions.

```csharp
public sealed class CustomMetric : IMetric
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CustomMetric](Flowthru.Misc.ML.UMAP.Core.Utils.CustomMetric.md)

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

Allows users to provide arbitrary distance functions while maintaining
the IMetric interface contract. Useful for experimentation and custom metrics.

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CustomMetric__ctor_System_String_System_Func_System_ReadOnlySpan_System_Single__System_ReadOnlySpan_System_Single__System_Single__System_Nullable_System_Single__System_Boolean_"></a> CustomMetric\(string, Func<ReadOnlySpan<float\>, ReadOnlySpan<float\>, float\>, float?, bool\)

Creates a custom metric from a distance function.

```csharp
public CustomMetric(string name, Func<ReadOnlySpan<float>, ReadOnlySpan<float>, float> distanceFunc, float? disconnectionDistance = null, bool supportsAngularProjection = false)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Human-readable name for the metric

`distanceFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<[ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>, [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>, [float](https://learn.microsoft.com/dotnet/api/system.single)\>

Function computing distance between two points

`disconnectionDistance` [float](https://learn.microsoft.com/dotnet/api/system.single)?

Optional maximum distance for bounded metrics

`supportsAngularProjection` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether angular RP forests benefit this metric

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CustomMetric_DisconnectionDistance"></a> DisconnectionDistance

UMAP's optimization can be unstable for very large distances. Setting a disconnection distance allows the algorithm to treat points beyond this distance as effectively disconnected, which can improve convergence and embedding quality for certain datasets. If null, the metric is unbounded and all distances are treated as valid.

```csharp
public float? DisconnectionDistance { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CustomMetric_Name"></a> Name

Name of the metric, used for logging and strategy selection. This is a simple identifier and does not affect behavior, but should be unique among built-in metrics to allow strategies to recognize it. For custom metrics, this can be arbitrary but should ideally describe the metric's behavior for clarity in logs and strategy selection.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CustomMetric_SupportsAngularProjection"></a> SupportsAngularProjection

Indicates whether this metric benefits from angular random projection forests. This should be set to true for metrics where angular RPs can improve neighbor search performance and embedding quality (e.g., cosine), and false for metrics where they do not provide a benefit (e.g., Euclidean). This allows the UMAP implementation to optimize neighbor search appropriately based on the metric's characteristics.

```csharp
public bool SupportsAngularProjection { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CustomMetric_Distance_System_ReadOnlySpan_System_Single__System_ReadOnlySpan_System_Single__"></a> Distance\(ReadOnlySpan<float\>, ReadOnlySpan<float\>\)

Compute distance using the provided distance function.

```csharp
public float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
```

#### Parameters

`x` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

`y` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

