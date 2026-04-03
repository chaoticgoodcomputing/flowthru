# <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters"></a> Class OptimizationParameters

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.md)  
Assembly: Flowthru.Misc.ML.dll  

Parameters for layout optimization.

```csharp
public sealed record OptimizationParameters : IEquatable<OptimizationParameters>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[OptimizationParameters](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.OptimizationParameters.md)

#### Implements

[IEquatable<OptimizationParameters\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters_A"></a> A

Curve parameter 'a' for attractive force.

```csharp
public required float A { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters_B"></a> B

Curve parameter 'b' for attractive force.

```csharp
public required float B { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters_Gamma"></a> Gamma

Weight applied to negative (repulsive) samples.

```csharp
public required float Gamma { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters_InitialAlpha"></a> InitialAlpha

Initial learning rate (decays linearly to 0).

```csharp
public required float InitialAlpha { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters_NegativeSampleRate"></a> NegativeSampleRate

Number of negative samples per positive sample.

```csharp
public required int NegativeSampleRate { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters_ProgressReporter"></a> ProgressReporter

Progress reporter for programmatic tracking.

```csharp
public IProgress<UmapProgress>? ProgressReporter { get; init; }
```

#### Property Value

 [IProgress](https://learn.microsoft.com/dotnet/api/system.iprogress\-1)<[UmapProgress](Flowthru.Misc.ML.UMAP.Core.UmapProgress.md)\>?

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_OptimizationParameters_Verbosity"></a> Verbosity

Verbosity level for progress reporting.

```csharp
public int Verbosity { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

