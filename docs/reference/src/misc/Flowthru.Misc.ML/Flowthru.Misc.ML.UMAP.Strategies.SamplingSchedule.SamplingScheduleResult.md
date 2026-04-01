# <a id="Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_SamplingScheduleResult"></a> Class SamplingScheduleResult

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.md)  
Assembly: Flowthru.Misc.ML.dll  

Result of sampling schedule computation.

```csharp
public sealed record SamplingScheduleResult : IEquatable<SamplingScheduleResult>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SamplingScheduleResult](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.SamplingScheduleResult.md)

#### Implements

[IEquatable<SamplingScheduleResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_SamplingScheduleResult__ctor_System_Single___System_Int32_"></a> SamplingScheduleResult\(float\[\], int\)

Result of sampling schedule computation.

```csharp
public SamplingScheduleResult(float[] EpochsPerSample, int TotalExpectedSamples)
```

#### Parameters

`EpochsPerSample` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

Number of epochs between samples for each edge.
Array length matches the number of edges in the graph.
Value of -1 indicates the edge should never be sampled.

`TotalExpectedSamples` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Total number of edge samples expected across all epochs.
Useful for progress estimation.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_SamplingScheduleResult_EpochsPerSample"></a> EpochsPerSample

Number of epochs between samples for each edge.
Array length matches the number of edges in the graph.
Value of -1 indicates the edge should never be sampled.

```csharp
public float[] EpochsPerSample { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

### <a id="Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_SamplingScheduleResult_TotalExpectedSamples"></a> TotalExpectedSamples

Total number of edge samples expected across all epochs.
Useful for progress estimation.

```csharp
public int TotalExpectedSamples { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

