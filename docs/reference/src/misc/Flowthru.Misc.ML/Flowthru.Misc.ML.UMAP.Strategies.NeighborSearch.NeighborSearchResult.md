# <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_NeighborSearchResult"></a> Class NeighborSearchResult

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.md)  
Assembly: Flowthru.Misc.ML.dll  

Result of a nearest neighbor search operation.

```csharp
public sealed record NeighborSearchResult : IEquatable<NeighborSearchResult>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NeighborSearchResult](Flowthru.Misc.ML.UMAP.Strategies.NeighborSearch.NeighborSearchResult.md)

#### Implements

[IEquatable<NeighborSearchResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_NeighborSearchResult__ctor_System_Int32_____System_Single_____System_Object_"></a> NeighborSearchResult\(int\[\]\[\], float\[\]\[\], object?\)

Result of a nearest neighbor search operation.

```csharp
public NeighborSearchResult(int[][] Indices, float[][] Distances, object? SearchIndex)
```

#### Parameters

`Indices` [int](https://learn.microsoft.com/dotnet/api/system.int32)\[\]\[\]

Indices of k-nearest neighbors for each point.
Array shape: (n_samples, n_neighbors)

`Distances` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]\[\]

Distances to k-nearest neighbors for each point.
Array shape: (n_samples, n_neighbors)

`SearchIndex` [object](https://learn.microsoft.com/dotnet/api/system.object)?

Optional search index for future queries (used in transform operations).
May be null if the strategy doesn't support indexing.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_NeighborSearchResult_Distances"></a> Distances

Distances to k-nearest neighbors for each point.
Array shape: (n_samples, n_neighbors)

```csharp
public float[][] Distances { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]\[\]

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_NeighborSearchResult_Indices"></a> Indices

Indices of k-nearest neighbors for each point.
Array shape: (n_samples, n_neighbors)

```csharp
public int[][] Indices { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)\[\]\[\]

### <a id="Flowthru_Misc_ML_UMAP_Strategies_NeighborSearch_NeighborSearchResult_SearchIndex"></a> SearchIndex

Optional search index for future queries (used in transform operations).
May be null if the strategy doesn't support indexing.

```csharp
public object? SearchIndex { get; init; }
```

#### Property Value

 [object](https://learn.microsoft.com/dotnet/api/system.object)?

