# <a id="Flowthru_Misc_ML_UMAP_Core_UmapProgress"></a> Class UmapProgress

Namespace: [Flowthru.Misc.ML.UMAP.Core](Flowthru.Misc.ML.UMAP.Core.md)  
Assembly: Flowthru.Misc.ML.dll  

Progress information reported during UMAP execution.

```csharp
public sealed record UmapProgress : IEquatable<UmapProgress>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UmapProgress](Flowthru.Misc.ML.UMAP.Core.UmapProgress.md)

#### Implements

[IEquatable<UmapProgress\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapProgress_CurrentEpoch"></a> CurrentEpoch

Current epoch number, if applicable (during optimization).

```csharp
public int? CurrentEpoch { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapProgress_Details"></a> Details

Optional detailed status message.

```csharp
public string? Details { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapProgress_Progress"></a> Progress

Progress within the current stage as a fraction [0.0, 1.0].

```csharp
public required float Progress { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapProgress_Stage"></a> Stage

Name of the current pipeline stage (e.g., "K-NN", "Graph Construction", "Optimization").

```csharp
public required string Stage { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapProgress_TotalEpochs"></a> TotalEpochs

Total number of epochs, if applicable (during optimization).

```csharp
public int? TotalEpochs { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

