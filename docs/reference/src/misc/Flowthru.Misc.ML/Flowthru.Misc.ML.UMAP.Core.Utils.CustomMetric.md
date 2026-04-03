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

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CustomMetric_Name"></a> Name

Human-readable name of the metric (e.g., "euclidean", "cosine").
Used for logging and serialization.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CustomMetric_SupportsAngularProjection"></a> SupportsAngularProjection

Whether this metric benefits from angular (cosine-based) random projection forests.
Angular metrics (cosine, correlation) use different RP tree splits than Euclidean metrics.

```csharp
public bool SupportsAngularProjection { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_Utils_CustomMetric_Distance_System_ReadOnlySpan_System_Single__System_ReadOnlySpan_System_Single__"></a> Distance\(ReadOnlySpan<float\>, ReadOnlySpan<float\>\)

Compute the distance between two points.

```csharp
public float Distance(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
```

#### Parameters

`x` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

First point

`y` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Second point

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

Distance value (non-negative)

#### Remarks

Must satisfy metric properties:
- Non-negativity: Distance(x, y) ≥ 0
- Identity: Distance(x, x) = 0
- Symmetry: Distance(x, y) = Distance(y, x)
- Triangle inequality: Distance(x, z) ≤ Distance(x, y) + Distance(y, z)

