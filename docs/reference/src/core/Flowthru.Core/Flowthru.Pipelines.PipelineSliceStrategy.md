# <a id="Flowthru_Pipelines_FlowSliceStrategy"></a> Class FlowSliceStrategy

Namespace: [Flowthru.Flows](Flowthru.Flows.md)  
Assembly: Flowthru.Core.dll  

Defines a strategy for slicing a pipeline to execute a subset of nodes.

```csharp
public sealed class FlowSliceStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowSliceStrategy](Flowthru.Flows.FlowSliceStrategy.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Pipeline slicing allows executing only specific portions of a DAG while maintaining
execution validity. All slicing operations preserve the runnability guarantee:
the resulting sub-DAG must be executable without missing dependencies.
</p>
<p>
<strong>Slicing Strategies:</strong>
</p>
<ul><li><strong>Pipelines:</strong> Filter to nodes from specific named pipelines (in merged DAGs)</li><li><strong>FromNodes:</strong> Include specified nodes and all downstream dependents</li><li><strong>ToNodes:</strong> Include specified nodes and all upstream dependencies (run "up to" these nodes)</li><li><strong>FromData:</strong> Include nodes consuming specified catalog entries and all downstream dependents</li><li><strong>ToData:</strong> Include nodes producing specified catalog entries and all upstream dependencies</li><li><strong>OnlyNodes:</strong> Explicit allowlist of nodes plus minimal required dependencies</li></ul>
<p>
<strong>Composition:</strong> Multiple strategies compose via intersection (additive filtering).
For example, <code>--from-nodes A --to-data B</code> produces nodes in the downstream dependency
tree of A that are also required to produce data B.
</p>
<p>
<strong>Runnability Guarantee:</strong> Slicing operations are additive only. Subtractive
operations (<code>--from-nodes A --except B</code>) would break the runnability guarantee and
are not supported.
</p>

## Properties

### <a id="Flowthru_Pipelines_FlowSliceStrategy_FromData"></a> FromData

Start from nodes that consume these catalog entry labels, including all downstream dependents.

```csharp
public IReadOnlySet<string>? FromData { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

Finds all nodes that read the specified catalog entries, then expands downstream.
Useful for impact analysis - "what breaks if I change this data?"

### <a id="Flowthru_Pipelines_FlowSliceStrategy_FromNodes"></a> FromNodes

Start from these nodes, including all downstream dependents.

```csharp
public IReadOnlySet<string>? FromNodes { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

Expands to include all nodes that depend on these nodes (transitively).
Useful for impact analysis - "what breaks if I change this node?"

### <a id="Flowthru_Pipelines_FlowSliceStrategy_IsSliced"></a> IsSliced

Whether any slicing is configured.

```csharp
public bool IsSliced { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Pipelines_FlowSliceStrategy_OnlyNodes"></a> OnlyNodes

Explicit allowlist of node names (dependencies auto-included).

```csharp
public IReadOnlySet<string>? OnlyNodes { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

Specifies exactly which nodes to execute, then automatically includes any
required dependencies to maintain DAG validity.

### <a id="Flowthru_Pipelines_FlowSliceStrategy_Pipelines"></a> Pipelines

Filter to nodes from these named pipelines (applies to merged pipelines).

```csharp
public IReadOnlySet<string>? Pipelines { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

In merged pipelines, nodes are prefixed with their pipeline name (e.g., "DataScience.TrainModel").
This filter includes only nodes from the specified pipelines.
Pipeline names are case-insensitive.

### <a id="Flowthru_Pipelines_FlowSliceStrategy_ToData"></a> ToData

End at nodes that produce these catalog entry labels, including all upstream dependencies.

```csharp
public IReadOnlySet<string>? ToData { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

Finds the nodes that write the specified catalog entries, then expands upstream.
Useful for targeted execution - "run everything needed to produce this data".

### <a id="Flowthru_Pipelines_FlowSliceStrategy_ToNodes"></a> ToNodes

End at these nodes, including all upstream dependencies needed to produce them.

```csharp
public IReadOnlySet<string>? ToNodes { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

Expands to include all transitive dependencies needed to run these nodes.
Equivalent to "run everything up to and including these nodes".
Useful for testing specific outputs without running the entire pipeline.

## Methods

### <a id="Flowthru_Pipelines_FlowSliceStrategy_All"></a> All\(\)

No filtering - execute entire pipeline.

```csharp
public static FlowSliceStrategy All()
```

#### Returns

 [FlowSliceStrategy](Flowthru.Flows.FlowSliceStrategy.md)

