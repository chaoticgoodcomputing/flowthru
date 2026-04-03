# <a id="Flowthru_Misc_ML_UMAP_Strategies_GraphRefinement_GraphRefinementResult"></a> Class GraphRefinementResult

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement](Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement.md)  
Assembly: Flowthru.Misc.ML.dll  

Result of graph refinement operation.

```csharp
public sealed record GraphRefinementResult : IEquatable<GraphRefinementResult>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphRefinementResult](Flowthru.Misc.ML.UMAP.Strategies.GraphRefinement.GraphRefinementResult.md)

#### Implements

[IEquatable<GraphRefinementResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Strategies_GraphRefinement_GraphRefinementResult__ctor_MathNet_Numerics_LinearAlgebra_Single_SparseMatrix_System_Int32_System_Single_"></a> GraphRefinementResult\(SparseMatrix, int, float\)

Result of graph refinement operation.

```csharp
public GraphRefinementResult(SparseMatrix RefinedGraph, int EdgesRemoved, float MinEdgeWeight)
```

#### Parameters

`RefinedGraph` SparseMatrix

The refined sparse graph with weak edges removed.
Shape: (n_samples, n_samples)

`EdgesRemoved` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of edges removed during refinement.
Useful for diagnostics and logging.

`MinEdgeWeight` [float](https://learn.microsoft.com/dotnet/api/system.single)

The minimum edge weight threshold that was applied.
Edges below this value were removed.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Strategies_GraphRefinement_GraphRefinementResult_EdgesRemoved"></a> EdgesRemoved

Number of edges removed during refinement.
Useful for diagnostics and logging.

```csharp
public int EdgesRemoved { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_GraphRefinement_GraphRefinementResult_MinEdgeWeight"></a> MinEdgeWeight

The minimum edge weight threshold that was applied.
Edges below this value were removed.

```csharp
public float MinEdgeWeight { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_GraphRefinement_GraphRefinementResult_RefinedGraph"></a> RefinedGraph

The refined sparse graph with weak edges removed.
Shape: (n_samples, n_samples)

```csharp
public SparseMatrix RefinedGraph { get; init; }
```

#### Property Value

 SparseMatrix

