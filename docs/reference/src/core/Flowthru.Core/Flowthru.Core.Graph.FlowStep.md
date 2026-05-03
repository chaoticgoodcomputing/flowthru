# <a id="Flowthru_Core_Graph_FlowStep"></a> Class FlowStep

Namespace: [Flowthru.Core.Graph](Flowthru.Core.Graph.md)  
Assembly: Flowthru.Core.dll  

Represents a step within a flow, wrapping the transformation function with metadata
about its inputs, outputs, and dependencies.

```csharp
public class FlowStep
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowStep](Flowthru.Core.Graph.FlowStep.md)

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
FlowStep serves as the internal representation of a step during flow
construction and execution. It tracks:
- The transformation function (Func&lt;TInput, Task&lt;TOutput&gt;&gt;)
- Input catalog entries (what data it reads)
- Output catalog entries (what data it writes)
- Dependencies (other steps that must run first)
</p>
<p>
<strong>Single Producer Rule:</strong> Each catalog entry can be written by at most
one step in a flow. This constraint ensures deterministic dependency resolution
and enables simple DAG construction via topological sort.
</p>
<p>
Made public to enable validation hooks to inspect step properties.
This is necessary for extensions (e.g., Python) to validate their own step types.
</p>

## Constructors

### <a id="Flowthru_Core_Graph_FlowStep__ctor_System_String_System_String_System_Delegate_System_Collections_Generic_IReadOnlyList_Flowthru_Core_Graph_INode__System_Collections_Generic_IReadOnlyList_Flowthru_Core_Graph_INode__"></a> FlowStep\(string, string?, Delegate, IReadOnlyList<INode\>, IReadOnlyList<INode\>\)

Creates a new Flow step with a transformation function and no declared service
dependencies. Equivalent to calling the <xref href="Flowthru.Core.Graph.FlowStep.%23ctor(System.String%2cSystem.String%2cSystem.Delegate%2cSystem.Collections.Generic.IReadOnlyList%7bFlowthru.Core.Graph.INode%7d%2cSystem.Collections.Generic.IReadOnlyList%7bFlowthru.Core.Graph.INode%7d%2cSystem.Collections.Generic.IReadOnlyList%7bSystem.Type%7d)" data-throw-if-not-resolved="false"></xref> overload with
a null service-deps list.

```csharp
public FlowStep(string label, string? description, Delegate step, IReadOnlyList<INode> inputs, IReadOnlyList<INode> outputs)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional description of this step

`step` [Delegate](https://learn.microsoft.com/dotnet/api/system.delegate)

The transformation function (Func&lt;TInput, Task&lt;TOutput&gt;&gt;)

`inputs` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[INode](Flowthru.Core.Graph.INode.md)\>

Catalog entries this step reads

`outputs` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[INode](Flowthru.Core.Graph.INode.md)\>

Catalog entries this step writes

### <a id="Flowthru_Core_Graph_FlowStep__ctor_System_String_System_String_System_Delegate_System_Collections_Generic_IReadOnlyList_Flowthru_Core_Graph_INode__System_Collections_Generic_IReadOnlyList_Flowthru_Core_Graph_INode__System_Collections_Generic_IReadOnlyList_System_Type__"></a> FlowStep\(string, string?, Delegate, IReadOnlyList<INode\>, IReadOnlyList<INode\>, IReadOnlyList<Type\>?\)

Creates a new Flow step with a transformation function and an explicit list of
service dependencies for preflight inspection.

```csharp
public FlowStep(string label, string? description, Delegate step, IReadOnlyList<INode> inputs, IReadOnlyList<INode> outputs, IReadOnlyList<Type>? serviceDependencies)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional description of this step

`step` [Delegate](https://learn.microsoft.com/dotnet/api/system.delegate)

The transformation function (Func&lt;TInput, Task&lt;TOutput&gt;&gt;)

`inputs` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[INode](Flowthru.Core.Graph.INode.md)\>

Catalog entries this step reads

`outputs` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[INode](Flowthru.Core.Graph.INode.md)\>

Catalog entries this step writes

`serviceDependencies` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Type](https://learn.microsoft.com/dotnet/api/system.type)\>?

Service types this step's transform depends on. The engine uses these to look up
matching <xref href="Flowthru.Core.Effects.IFlowthruInspector%601" data-throw-if-not-resolved="false"></xref> registrations during
preflight. Pass <code>null</code> for steps with no service dependencies (the default).

## Properties

### <a id="Flowthru_Core_Graph_FlowStep_Dependencies"></a> Dependencies

Other Flow steps that must execute before this step.
Populated during dependency analysis by checking which steps produce our inputs.

```csharp
public List<FlowStep> Dependencies { get; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[FlowStep](Flowthru.Core.Graph.FlowStep.md)\>

#### Remarks

This forms the edges of the execution DAG:
- If step A produces output X, and step B consumes input X, then B depends on A.
- Topological sort uses these dependencies to determine execution order.

### <a id="Flowthru_Core_Graph_FlowStep_Description"></a> Description

String description of the step's purpose.

```csharp
public string Description { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Graph_FlowStep_Height"></a> Height

Height in the DAG: the length of the longest path from this step to any sink (leaf).
Sinks have height 0. Used by critical-path scheduling to prioritise steps that unblock
the most downstream work.

```csharp
public int Height { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Populated by <xref href="Flowthru.Core.Graph.DependencyAnalyzer.ComputeHeights(System.Collections.Generic.List%7bFlowthru.Core.Graph.FlowStep%7d)" data-throw-if-not-resolved="false"></xref> after the dependency
graph has been built. A value of -1 indicates heights have not yet been computed.

### <a id="Flowthru_Core_Graph_FlowStep_Inputs"></a> Inputs

Catalog entries that this step reads as input.
These may be produced by other steps (dependencies) or be external prerequisites.

```csharp
public IReadOnlyList<INode> Inputs { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[INode](Flowthru.Core.Graph.INode.md)\>

### <a id="Flowthru_Core_Graph_FlowStep_Label"></a> Label

Unique identifier for this step within the flow.
Typically the step type name or user-provided name.

```csharp
public string Label { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Graph_FlowStep_Layer"></a> Layer

Execution layer determined by topological sort.
Steps in layer 0 have no dependencies. Steps in layer N depend on steps in layers 0..N-1.

```csharp
public int Layer { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Core_Graph_FlowStep_Outputs"></a> Outputs

Catalog entries that this step writes as output.
Per the single producer rule, each entry here must be unique across all steps.

```csharp
public IReadOnlyList<INode> Outputs { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[INode](Flowthru.Core.Graph.INode.md)\>

### <a id="Flowthru_Core_Graph_FlowStep_ServiceDependencies"></a> ServiceDependencies

Service types this step depends on, used by the preflight loop to look up
matching <xref href="Flowthru.Core.Effects.IFlowthruInspector%601" data-throw-if-not-resolved="false"></xref> registrations and
run reachability probes before the flow executes.

```csharp
public IReadOnlyList<Type> ServiceDependencies { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Type](https://learn.microsoft.com/dotnet/api/system.type)\>

#### Remarks

<p>
Defaults to an empty list when constructed via <code>FlowBuilder.AddStep</code>
— Phase 3 of the effects-as-steps initiative ships the inspection mechanism but
not the source-generated metadata that populates this list. Until the metadata
generator lands (Phase 4), real flows pay no preflight cost from service inspection;
tests construct <xref href="Flowthru.Core.Graph.FlowStep" data-throw-if-not-resolved="false"></xref> directly with explicit service deps to
exercise the inspection path.
</p>

### <a id="Flowthru_Core_Graph_FlowStep_TransformFunction"></a> TransformFunction

The transformation function that performs the step's work.
Type-erased to Delegate since we need to store different function signatures together.

```csharp
public Delegate TransformFunction { get; }
```

#### Property Value

 [Delegate](https://learn.microsoft.com/dotnet/api/system.delegate)

#### Remarks

<p>
At execution time, this delegate will be invoked via DynamicInvoke with the
appropriate input parameter(s) — or with no parameters at all for zero-input steps.
The function signature can be synchronous or asynchronous, across the full
0–8 inputs × 0–8 outputs arity matrix:
- Sync single: Func&lt;TInput, TOutput&gt;
- Async single: Func&lt;TInput, Task&lt;TOutput&gt;&gt;
- Sync multi-input: Func&lt;(TIn1, TIn2, ...), TOutput&gt;
- Async multi-input: Func&lt;(TIn1, TIn2, ...), Task&lt;TOutput&gt;&gt;
- Sync multi-output: Func&lt;TInput, (TOut1, TOut2, ...)&gt;
- Async multi-output: Func&lt;TInput, Task&lt;(TOut1, TOut2, ...)&gt;&gt;
- Zero-input sync: Func&lt;TOutput&gt; or Action (when also zero-output)
- Zero-input async: Func&lt;Task&lt;TOutput&gt;&gt; or Func&lt;Task&gt; (when also zero-output)
- Zero-output sync: Action&lt;TInput&gt;
- Zero-output async: Func&lt;TInput, Task&gt;
</p>
<p>
<strong>Optional Cancellation Support:</strong> Steps can opt-in to cancellation awareness
by accepting a CancellationToken as the last parameter:
- Func&lt;CancellationToken, Task&gt; (zero-input, zero-output)
- Func&lt;CancellationToken, Task&lt;TOutput&gt;&gt; (zero-input)
- Func&lt;TInput, CancellationToken, Task&lt;TOutput&gt;&gt;
- Func&lt;(TIn1, TIn2), CancellationToken, Task&lt;TOutput&gt;&gt;
</p>
<p>
When a Step accepts a CancellationToken, the Flow will pass the runtime token during
execution, allowing the step to cancel long-running operations cooperatively. Steps that
do not accept a CancellationToken will only be cancelled between step executions.
</p>
<p>
The execution engine detects whether the result is a Task and awaits it if needed.
</p>

