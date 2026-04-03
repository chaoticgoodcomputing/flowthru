# <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_LayoutOptimizationResult"></a> Class LayoutOptimizationResult

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.md)  
Assembly: Flowthru.Misc.ML.dll  

Result of layout optimization.

```csharp
public sealed record LayoutOptimizationResult : IEquatable<LayoutOptimizationResult>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[LayoutOptimizationResult](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.LayoutOptimizationResult.md)

#### Implements

[IEquatable<LayoutOptimizationResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_LayoutOptimizationResult__ctor_MathNet_Numerics_LinearAlgebra_Matrix_System_Single__System_Nullable_System_Single__"></a> LayoutOptimizationResult\(Matrix<float\>, float?\)

Result of layout optimization.

```csharp
public LayoutOptimizationResult(Matrix<float> OptimizedEmbedding, float? FinalLoss)
```

#### Parameters

`OptimizedEmbedding` Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

The final optimized embedding.
Shape: (n_samples, n_components)

`FinalLoss` [float](https://learn.microsoft.com/dotnet/api/system.single)?

Final cross-entropy loss (if computed).
Null if loss tracking is disabled.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_LayoutOptimizationResult_ActualEpochs"></a> ActualEpochs

Actual number of epochs completed before termination.
May be less than requested epochs if early stopping was triggered.

```csharp
public int? ActualEpochs { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_LayoutOptimizationResult_EarlyStoppingSaved"></a> EarlyStoppingSaved

Number of epochs saved by early stopping.
Zero if optimization ran to completion or early stopping was disabled.

```csharp
public int? EarlyStoppingSaved { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_LayoutOptimizationResult_FinalLoss"></a> FinalLoss

Final cross-entropy loss (if computed).
Null if loss tracking is disabled.

```csharp
public float? FinalLoss { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_LayoutOptimizationResult_OptimizedEmbedding"></a> OptimizedEmbedding

The final optimized embedding.
Shape: (n_samples, n_components)

```csharp
public Matrix<float> OptimizedEmbedding { get; init; }
```

#### Property Value

 Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

