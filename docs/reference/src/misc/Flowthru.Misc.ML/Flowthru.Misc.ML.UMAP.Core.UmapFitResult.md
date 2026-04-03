# <a id="Flowthru_Misc_ML_UMAP_Core_UmapFitResult"></a> Class UmapFitResult

Namespace: [Flowthru.Misc.ML.UMAP.Core](Flowthru.Misc.ML.UMAP.Core.md)  
Assembly: Flowthru.Misc.ML.dll  

Result of the complete UMAP FitTransform operation.
Contains the final embedding and all intermediate results.

```csharp
public sealed record UmapFitResult : IEquatable<UmapFitResult>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UmapFitResult](Flowthru.Misc.ML.UMAP.Core.UmapFitResult.md)

#### Implements

[IEquatable<UmapFitResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFitResult__ctor_MathNet_Numerics_LinearAlgebra_Matrix_System_Single__Flowthru_Misc_ML_UMAP_Core_UmapGraphResult_Flowthru_Misc_ML_UMAP_Strategies_LayoutInit_LayoutInitResult_Flowthru_Misc_ML_UMAP_Strategies_SamplingSchedule_SamplingScheduleResult_Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_LayoutOptimizationResult_Flowthru_Misc_ML_UMAP_Core_UmapRuntimeReport_"></a> UmapFitResult\(Matrix<float\>, UmapGraphResult, LayoutInitResult, SamplingScheduleResult, LayoutOptimizationResult, UmapRuntimeReport\)

Result of the complete UMAP FitTransform operation.
Contains the final embedding and all intermediate results.

```csharp
public UmapFitResult(Matrix<float> Embedding, UmapGraphResult GraphResult, LayoutInitResult LayoutInitResult, SamplingScheduleResult SamplingScheduleResult, LayoutOptimizationResult OptimizationResult, UmapRuntimeReport RuntimeReport)
```

#### Parameters

`Embedding` Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Final optimized low-dimensional embedding. Shape: (n_samples, n_components)

`GraphResult` [UmapGraphResult](Flowthru.Misc.ML.UMAP.Core.UmapGraphResult.md)

Intermediate result from graph construction (Phases 1-4).

`LayoutInitResult` [LayoutInitResult](Flowthru.Misc.ML.UMAP.Strategies.LayoutInit.LayoutInitResult.md)

Intermediate result from layout initialization (Phase 5).

`SamplingScheduleResult` [SamplingScheduleResult](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.SamplingScheduleResult.md)

Intermediate result from sampling schedule computation (Phase 6).

`OptimizationResult` [LayoutOptimizationResult](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.LayoutOptimizationResult.md)

Result from layout optimization (Phase 7).

`RuntimeReport` [UmapRuntimeReport](Flowthru.Misc.ML.UMAP.Core.UmapRuntimeReport.md)

Performance timing metrics for each UMAP phase.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFitResult_Embedding"></a> Embedding

Final optimized low-dimensional embedding. Shape: (n_samples, n_components)

```csharp
public Matrix<float> Embedding { get; init; }
```

#### Property Value

 Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFitResult_GraphResult"></a> GraphResult

Intermediate result from graph construction (Phases 1-4).

```csharp
public UmapGraphResult GraphResult { get; init; }
```

#### Property Value

 [UmapGraphResult](Flowthru.Misc.ML.UMAP.Core.UmapGraphResult.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFitResult_LayoutInitResult"></a> LayoutInitResult

Intermediate result from layout initialization (Phase 5).

```csharp
public LayoutInitResult LayoutInitResult { get; init; }
```

#### Property Value

 [LayoutInitResult](Flowthru.Misc.ML.UMAP.Strategies.LayoutInit.LayoutInitResult.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFitResult_OptimizationResult"></a> OptimizationResult

Result from layout optimization (Phase 7).

```csharp
public LayoutOptimizationResult OptimizationResult { get; init; }
```

#### Property Value

 [LayoutOptimizationResult](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.LayoutOptimizationResult.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFitResult_RuntimeReport"></a> RuntimeReport

Performance timing metrics for each UMAP phase.

```csharp
public UmapRuntimeReport RuntimeReport { get; init; }
```

#### Property Value

 [UmapRuntimeReport](Flowthru.Misc.ML.UMAP.Core.UmapRuntimeReport.md)

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapFitResult_SamplingScheduleResult"></a> SamplingScheduleResult

Intermediate result from sampling schedule computation (Phase 6).

```csharp
public SamplingScheduleResult SamplingScheduleResult { get; init; }
```

#### Property Value

 [SamplingScheduleResult](Flowthru.Misc.ML.UMAP.Strategies.SamplingSchedule.SamplingScheduleResult.md)

