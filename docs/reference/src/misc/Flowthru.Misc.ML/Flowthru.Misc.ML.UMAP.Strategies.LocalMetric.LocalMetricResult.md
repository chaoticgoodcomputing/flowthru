# <a id="Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_LocalMetricResult"></a> Class LocalMetricResult

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.LocalMetric](Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.md)  
Assembly: Flowthru.Misc.ML.dll  

Result of local metric computation.

```csharp
public sealed record LocalMetricResult : IEquatable<LocalMetricResult>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[LocalMetricResult](Flowthru.Misc.ML.UMAP.Strategies.LocalMetric.LocalMetricResult.md)

#### Implements

[IEquatable<LocalMetricResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_LocalMetricResult__ctor_System_Single___System_Single___"></a> LocalMetricResult\(float\[\], float\[\]\)

Result of local metric computation.

```csharp
public LocalMetricResult(float[] Sigmas, float[] Rhos)
```

#### Parameters

`Sigmas` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

Bandwidth parameters for exponential kernel.
Array shape: (n_samples,)

`Rhos` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

Distance to nearest connected neighbor.
Array shape: (n_samples,)

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_LocalMetricResult_Rhos"></a> Rhos

Distance to nearest connected neighbor.
Array shape: (n_samples,)

```csharp
public float[] Rhos { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LocalMetric_LocalMetricResult_Sigmas"></a> Sigmas

Bandwidth parameters for exponential kernel.
Array shape: (n_samples,)

```csharp
public float[] Sigmas { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

