# <a id="Flowthru_Misc_ML_UMAP_Core_UmapRuntimeReport"></a> Class UmapRuntimeReport

Namespace: [Flowthru.Misc.ML.UMAP.Core](Flowthru.Misc.ML.UMAP.Core.md)  
Assembly: Flowthru.Misc.ML.dll  

Runtime performance report for UMAP execution.

```csharp
public sealed record UmapRuntimeReport : IEquatable<UmapRuntimeReport>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UmapRuntimeReport](Flowthru.Misc.ML.UMAP.Core.UmapRuntimeReport.md)

#### Implements

[IEquatable<UmapRuntimeReport\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Generic schema capturing timing metrics for each UMAP algorithmic phase.
Does not include Flowthru serialization markers to keep it framework-agnostic.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapRuntimeReport_Timings"></a> Timings

Timing measurements for each UMAP phase.
Key is the stage name, value is elapsed time in milliseconds.

```csharp
public Dictionary<string, int> Timings { get; init; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [int](https://learn.microsoft.com/dotnet/api/system.int32)\>

#### Remarks

Expected stages:
- "NeighborSearch" - Phase 1: k-NN graph construction
- "LocalMetric" - Phase 2: Local metric parameter computation
- "GraphConstruction" - Phase 3: Fuzzy simplicial set construction
- "GraphRefinement" - Phase 4: Graph refinement (optional)
- "LayoutInit" - Phase 5: Low-dimensional layout initialization
- "SamplingSchedule" - Phase 6: Edge sampling schedule computation
- "LayoutOptimization" - Phase 7: Stochastic gradient descent optimization

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapRuntimeReport_TotalTimeMs"></a> TotalTimeMs

Total elapsed time for the complete FitTransform operation, in milliseconds.

```csharp
public int TotalTimeMs { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Sum of all individual phase timings. Useful for quick performance assessment.

