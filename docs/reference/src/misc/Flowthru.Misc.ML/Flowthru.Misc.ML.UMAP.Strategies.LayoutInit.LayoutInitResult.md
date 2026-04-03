# <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutInit_LayoutInitResult"></a> Class LayoutInitResult

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.LayoutInit](Flowthru.Misc.ML.UMAP.Strategies.LayoutInit.md)  
Assembly: Flowthru.Misc.ML.dll  

Result of layout initialization.

```csharp
public sealed record LayoutInitResult : IEquatable<LayoutInitResult>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[LayoutInitResult](Flowthru.Misc.ML.UMAP.Strategies.LayoutInit.LayoutInitResult.md)

#### Implements

[IEquatable<LayoutInitResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutInit_LayoutInitResult__ctor_MathNet_Numerics_LinearAlgebra_Matrix_System_Single__System_String_"></a> LayoutInitResult\(Matrix<float\>, string\)

Result of layout initialization.

```csharp
public LayoutInitResult(Matrix<float> Embedding, string InitializationMethod)
```

#### Parameters

`Embedding` Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Initial low-dimensional embedding coordinates.
Shape: (n_samples, n_components)
Values are normalized to approximately [-10, 10] range.

`InitializationMethod` [string](https://learn.microsoft.com/dotnet/api/system.string)

Human-readable description of the initialization method used.
Useful for logging and debugging.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutInit_LayoutInitResult_Embedding"></a> Embedding

Initial low-dimensional embedding coordinates.
Shape: (n_samples, n_components)
Values are normalized to approximately [-10, 10] range.

```csharp
public Matrix<float> Embedding { get; init; }
```

#### Property Value

 Matrix<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutInit_LayoutInitResult_InitializationMethod"></a> InitializationMethod

Human-readable description of the initialization method used.
Useful for logging and debugging.

```csharp
public string InitializationMethod { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

