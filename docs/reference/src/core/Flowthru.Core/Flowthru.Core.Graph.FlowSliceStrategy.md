# <a id="Flowthru_Core_Graph_FlowSliceStrategy"></a> Class FlowSliceStrategy

Namespace: [Flowthru.Core.Graph](Flowthru.Core.Graph.md)  
Assembly: Flowthru.Core.dll  

Defines a strategy for slicing a Flow to execute a subset of nodes.

```csharp
public sealed class FlowSliceStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowSliceStrategy](Flowthru.Core.Graph.FlowSliceStrategy.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Flow slicing allows executing only specific portions of a DAG while maintaining
execution validity. All slicing operations preserve the runnability guarantee:
the resulting sub-DAG must be executable without missing dependencies.
</p>
<p>
Because a Flowthru flow is a bipartite graph of steps and catalog items, all slice
targets are addressed uniformly by label — whether the label belongs to a step or a
catalog item. The resolver checks the step index first; if no step matches, it falls
back to the catalog item index and resolves to the relevant producer or consumer steps.
</p>
<p>
<strong>Slicing Strategies:</strong>
</p>
<ul><li><strong>Flows:</strong> Filter to nodes from specific named flows (in merged DAGs)</li><li><strong>From:</strong> Include specified nodes and all downstream dependents. Accepts step labels or catalog item labels (resolves to consumers).</li><li><strong>To:</strong> Include specified nodes and all upstream dependencies. Accepts step labels or catalog item labels (resolves to producer).</li><li><strong>Only:</strong> Explicit allowlist plus minimal required dependencies. Accepts step labels or catalog item labels (resolves to producer).</li></ul>
<p>
<strong>Composition:</strong> Multiple strategies compose via intersection (additive filtering).
For example, <code>--from A --to B</code> produces nodes in the downstream dependency
tree of A that are also required to produce B.
</p>
<p>
<strong>Runnability Guarantee:</strong> Slicing operations are additive only. Subtractive
operations (<code>--from A --except B</code>) would break the runnability guarantee and
are not supported.
</p>

## Properties

### <a id="Flowthru_Core_Graph_FlowSliceStrategy_Flows"></a> Flows

Filter to nodes from these named flows (applies to merged flows).

```csharp
public IReadOnlySet<string>? Flows { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

In merged flows, steps are prefixed with their Flow name (e.g., "DataScience.TrainModel").
This filter includes only steps from the specified flows.
Flow names are case-insensitive.

### <a id="Flowthru_Core_Graph_FlowSliceStrategy_From"></a> From

Start from these nodes, including all downstream dependents.

```csharp
public IReadOnlySet<string>? From { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

Each label is resolved against the step index first. If no step matches, the label is
treated as a catalog item and resolved to all steps that consume it.
Expands to include all transitively dependent steps.
Useful for impact analysis: "what is affected if I change this step or item?"

### <a id="Flowthru_Core_Graph_FlowSliceStrategy_IsSliced"></a> IsSliced

Whether any slicing is configured.

```csharp
public bool IsSliced { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Graph_FlowSliceStrategy_Only"></a> Only

Explicit allowlist of nodes (dependencies auto-included).

```csharp
public IReadOnlySet<string>? Only { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

Each label is resolved against the step index first. If no step matches, the label is
treated as a catalog item and resolved to the step that produces it.
Automatically includes all transitive upstream dependencies to maintain DAG validity.

### <a id="Flowthru_Core_Graph_FlowSliceStrategy_To"></a> To

End at these nodes, including all upstream dependencies needed to produce them.

```csharp
public IReadOnlySet<string>? To { get; init; }
```

#### Property Value

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

Each label is resolved against the step index first. If no step matches, the label is
treated as a catalog item and resolved to the step that produces it.
Expands to include all transitive dependencies.
Equivalent to "run everything up to and including these nodes".
Useful for targeted execution: "run everything needed to produce this step or item".

## Methods

### <a id="Flowthru_Core_Graph_FlowSliceStrategy_All"></a> All\(\)

No filtering - execute entire flow.

```csharp
public static FlowSliceStrategy All()
```

#### Returns

 [FlowSliceStrategy](Flowthru.Core.Graph.FlowSliceStrategy.md)

