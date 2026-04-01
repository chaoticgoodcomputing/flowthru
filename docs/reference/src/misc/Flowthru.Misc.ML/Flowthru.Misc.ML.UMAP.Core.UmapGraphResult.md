# <a id="Flowthru_Misc_ML_UMAP_Core_UmapGraphResult"></a> Class UmapGraphResult

Namespace: [Flowthru.Misc.ML.UMAP.Core](Flowthru.Misc.ML.UMAP.Core.md)  
Assembly: Flowthru.Misc.ML.dll  

Result of computing the UMAP graph (phases 1-3).

```csharp
public sealed record UmapGraphResult : IEquatable<UmapGraphResult>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UmapGraphResult](Flowthru.Misc.ML.UMAP.Core.UmapGraphResult.md)

#### Implements

[IEquatable<UmapGraphResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapGraphResult__ctor_MathNet_Numerics_LinearAlgebra_Single_SparseMatrix_System_Int32_____System_Single_____System_Single___System_Single___System_Object_"></a> UmapGraphResult\(SparseMatrix, int\[\]\[\], float\[\]\[\], float\[\], float\[\], object?\)

Result of computing the UMAP graph (phases 1-3).

```csharp
public UmapGraphResult(SparseMatrix Graph, int[][] KnnIndices, float[][] KnnDistances, float[] Sigmas, float[] Rhos, object? SearchIndex)
```

#### Parameters

`Graph` SparseMatrix

Fuzzy simplicial set as a sparse symmetric matrix.

`KnnIndices` [int](https://learn.microsoft.com/dotnet/api/system.int32)\[\]\[\]

K-nearest neighbor indices for each point.

`KnnDistances` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]\[\]

K-nearest neighbor distances for each point.

`Sigmas` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

Bandwidth parameters from local metric computation.

`Rhos` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

Local connectivity distances from local metric computation.

`SearchIndex` [object](https://learn.microsoft.com/dotnet/api/system.object)?

Optional search index for transform operations.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapGraphResult_Graph"></a> Graph

Fuzzy simplicial set as a sparse symmetric matrix.

```csharp
public SparseMatrix Graph { get; init; }
```

#### Property Value

 SparseMatrix

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapGraphResult_KnnDistances"></a> KnnDistances

K-nearest neighbor distances for each point.

```csharp
public float[][] KnnDistances { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]\[\]

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapGraphResult_KnnIndices"></a> KnnIndices

K-nearest neighbor indices for each point.

```csharp
public int[][] KnnIndices { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)\[\]\[\]

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapGraphResult_Rhos"></a> Rhos

Local connectivity distances from local metric computation.

```csharp
public float[] Rhos { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapGraphResult_SearchIndex"></a> SearchIndex

Optional search index for transform operations.

```csharp
public object? SearchIndex { get; init; }
```

#### Property Value

 [object](https://learn.microsoft.com/dotnet/api/system.object)?

### <a id="Flowthru_Misc_ML_UMAP_Core_UmapGraphResult_Sigmas"></a> Sigmas

Bandwidth parameters from local metric computation.

```csharp
public float[] Sigmas { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

