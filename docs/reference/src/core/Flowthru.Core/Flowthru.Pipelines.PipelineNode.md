# <a id="Flowthru_Pipelines_PipelineNode"></a> Class PipelineNode

Namespace: [Flowthru.Flows](Flowthru.Flows.md)  
Assembly: Flowthru.Core.dll  

Represents a node within a pipeline, wrapping the transformation function with metadata
about its inputs, outputs, and dependencies.

```csharp
public class PipelineNode
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PipelineNode](Flowthru.Flows.PipelineNode.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
PipelineNode serves as the internal representation of a node during pipeline
construction and execution. It tracks:
- The transformation function (Func&lt;TInput, Task&lt;TOutput&gt;&gt;)
- Input catalog entries (what data it reads)
- Output catalog entries (what data it writes)
- Dependencies (other nodes that must run first)
</p>
<p>
<strong>Single Producer Rule:</strong> Each catalog entry can be written by at most
one node in a pipeline. This constraint ensures deterministic dependency resolution
and enables simple DAG construction via topological sort.
</p>
<p>
<strong>Function-Based Architecture (v0.5.0):</strong> Nodes are now pure transformation
functions instead of class instances. This enables compile-time type safety through
generic type inference at the pipeline construction site.
</p>
<p>
<strong>Visibility (Phase 4):</strong>
Made public to enable validation hooks to inspect node properties.
This is necessary for extensions (e.g., Python) to validate their own node types.
</p>

## Constructors

### <a id="Flowthru_Pipelines_PipelineNode__ctor_System_String_System_String_System_Delegate_System_Collections_Generic_IReadOnlyList_Flowthru_Data_IItem__System_Collections_Generic_IReadOnlyList_Flowthru_Data_IItem__"></a> PipelineNode\(string, string?, Delegate, IReadOnlyList<IItem\>, IReadOnlyList<IItem\>\)

Creates a new pipeline node with a transformation function.

```csharp
public PipelineNode(string label, string? description, Delegate node, IReadOnlyList<IItem> inputs, IReadOnlyList<IItem> outputs)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`node` [Delegate](https://learn.microsoft.com/dotnet/api/system.delegate)

The transformation function (Func&lt;TInput, Task&lt;TOutput&gt;&gt;)

`inputs` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[IItem](Flowthru.Data.IItem.md)\>

Catalog entries this node reads

`outputs` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[IItem](Flowthru.Data.IItem.md)\>

Catalog entries this node writes

## Properties

### <a id="Flowthru_Pipelines_PipelineNode_Dependencies"></a> Dependencies

Other pipeline nodes that must execute before this node.
Populated during dependency analysis by checking which nodes produce our inputs.

```csharp
public List<PipelineNode> Dependencies { get; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[PipelineNode](Flowthru.Flows.PipelineNode.md)\>

#### Remarks

This forms the edges of the execution DAG:
- If node A produces output X, and node B consumes input X, then B depends on A.
- Topological sort uses these dependencies to determine execution order.

### <a id="Flowthru_Pipelines_PipelineNode_Description"></a> Description

String description of the node's purpose.

```csharp
public string Description { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Pipelines_PipelineNode_Inputs"></a> Inputs

Catalog entries that this node reads as input.
These may be produced by other nodes (dependencies) or be external prerequisites.

```csharp
public IReadOnlyList<IItem> Inputs { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[IItem](Flowthru.Data.IItem.md)\>

### <a id="Flowthru_Pipelines_PipelineNode_Label"></a> Label

Unique identifier for this node within the pipeline.
Typically the node type name or user-provided name.

```csharp
public string Label { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Pipelines_PipelineNode_Layer"></a> Layer

Execution layer determined by topological sort.
Nodes in layer 0 have no dependencies. Nodes in layer N depend on nodes in layers 0..N-1.

```csharp
public int Layer { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Pipelines_PipelineNode_Outputs"></a> Outputs

Catalog entries that this node writes as output.
Per the single producer rule, each entry here must be unique across all nodes.

```csharp
public IReadOnlyList<IItem> Outputs { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[IItem](Flowthru.Data.IItem.md)\>

### <a id="Flowthru_Pipelines_PipelineNode_TransformFunction"></a> TransformFunction

The transformation function that performs the node's work.
Type-erased to Delegate since we need to store different function signatures together.

```csharp
public Delegate TransformFunction { get; }
```

#### Property Value

 [Delegate](https://learn.microsoft.com/dotnet/api/system.delegate)

#### Remarks

<p>
At execution time, this delegate will be invoked via DynamicInvoke with the
appropriate input parameter(s). The function signature can be either synchronous
or asynchronous:
- Sync single: Func&lt;TInput, TOutput&gt;
- Async single: Func&lt;TInput, Task&lt;TOutput&gt;&gt;
- Sync multi-input: Func&lt;(TIn1, TIn2, ...), TOutput&gt;
- Async multi-input: Func&lt;(TIn1, TIn2, ...), Task&lt;TOutput&gt;&gt;
- Sync multi-output: Func&lt;TInput, (TOut1, TOut2, ...)&gt;
- Async multi-output: Func&lt;TInput, Task&lt;(TOut1, TOut2, ...)&gt;&gt;
</p>
<p>
<strong>Optional Cancellation Support:</strong> Nodes can opt-in to cancellation awareness
by accepting a CancellationToken as the last parameter:
- Func&lt;TInput, CancellationToken, Task&lt;TOutput&gt;&gt;
- Func&lt;(TIn1, TIn2), CancellationToken, Task&lt;TOutput&gt;&gt;
</p>
<p>
When a node accepts a CancellationToken, the pipeline will pass the runtime token during
execution, allowing the node to cancel long-running operations cooperatively. Nodes that
do not accept a CancellationToken will only be cancelled between node executions.
</p>
<p>
The execution engine detects whether the result is a Task and awaits it if needed.
</p>

## Methods

### <a id="Flowthru_Pipelines_PipelineNode_ToString"></a> ToString\(\)

Returns a string representation for debugging.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

