# <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_GraphEdge"></a> Struct GraphEdge

Namespace: [Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization](Flowthru.Misc.ML.UMAP.Strategies.LayoutOptimization.md)  
Assembly: Flowthru.Misc.ML.dll  

Represents an edge in the fuzzy simplicial set graph.

```csharp
public readonly record struct GraphEdge : IEquatable<GraphEdge>
```

#### Implements

[IEquatable<GraphEdge\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Constructors

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_GraphEdge__ctor_System_Int32_System_Int32_System_Single_"></a> GraphEdge\(int, int, float\)

Represents an edge in the fuzzy simplicial set graph.

```csharp
public GraphEdge(int Head, int Tail, float Weight)
```

#### Parameters

`Head` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Index of the head vertex (source).

`Tail` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Index of the tail vertex (target).

`Weight` [float](https://learn.microsoft.com/dotnet/api/system.single)

Membership strength of this edge.

## Properties

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_GraphEdge_Head"></a> Head

Index of the head vertex (source).

```csharp
public int Head { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_GraphEdge_Tail"></a> Tail

Index of the tail vertex (target).

```csharp
public int Tail { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Misc_ML_UMAP_Strategies_LayoutOptimization_GraphEdge_Weight"></a> Weight

Membership strength of this edge.

```csharp
public float Weight { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

