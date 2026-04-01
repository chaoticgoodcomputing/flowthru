# <a id="Flowthru_Pipelines_PipelineBuilder"></a> Class PipelineBuilder

Namespace: [Flowthru.Pipelines](Flowthru.Pipelines.md)  
Assembly: Flowthru.Core.dll  

Fluent builder for constructing type-safe data pipelines with function-based nodes.

```csharp
public class PipelineBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

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
<strong>Function-Based Design (v0.5.0):</strong>
Nodes are pure transformation functions with compile-time type safety.
Both synchronous and asynchronous functions are supported:
- Sync: Func&lt;TInput, TOutput&gt;
- Async: Func&lt;TInput, Task&lt;TOutput&gt;&gt;
- Multi-input: Func&lt;(TIn1, TIn2, ...), TOutput&gt; or Task&lt;TOutput&gt;
- Multi-output: Func&lt;TInput, (TOut1, TOut2, ...)&gt; or Task&lt;(TOut1, TOut2, ...)&gt;
</p>
<p>
Use synchronous functions for pure data transformations. Use asynchronous functions
only when your node performs I/O operations (external APIs, databases, etc.).
</p>
<p>
The compiler infers all types from function signatures and validates catalog entry
types at pipeline construction time, catching type mismatches before execution.
</p>
<p>
<strong>Usage Patterns:</strong>
</p>
<pre><code class="lang-csharp">var pipeline = PipelineBuilder.CreatePipeline(builder =&gt;
{
    // Simple synchronous node
    builder.AddNode(
        name: "Preprocess",
        transform: PreprocessNode.Create(),
        input: catalog.RawData,
        output: catalog.ProcessedData
    );

    // Multi-input node: tuple → single output
    builder.AddNode(
        name: "TrainModel",
        transform: TrainModelNode.Create(),
        input: (catalog.XTrain, catalog.YTrain),
        output: catalog.Model
    );

    // Multi-output node: single input → tuple
    builder.AddNode(
        name: "SplitData",
        transform: SplitDataNode.Create(),
        input: catalog.Data,
        output: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest)
    );

    // Asynchronous node (only when needed for I/O)
    builder.AddNode(
        name: "FetchExternalData",
        transform: ExternalDataNode.Create(),
        input: catalog.Config,
        output: catalog.ExternalData
    );
});

pipeline.Build();
await pipeline.ExecuteAsync();</code></pre>

## Methods

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__2_System_String_System_Func___0_System_Threading_Tasks_Task___1___Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__System_String_"></a> AddNode<TInput, TOutput\>\(string, Func<TInput, Task<TOutput\>\>, ICatalogEntry<TInput\>, ICatalogEntry<TOutput\>, string\)

Adds a node with single input and single output (asynchronous transformation).
All types are inferred from the transformation function signature.

```csharp
public PipelineBuilder AddNode<TInput, TOutput>(string label, Func<TInput, Task<TOutput>> transform, ICatalogEntry<TInput> input, ICatalogEntry<TOutput> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TInput, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOutput\>\>

Asynchronous transformation function from input to output

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TInput\>

Catalog entry providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOutput\>

Catalog entry to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TInput` 

Input type (inferred from transform)

`TOutput` 

Output type (inferred from transform)

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__2_System_String_System_Func___0_System_Threading_CancellationToken_System_Threading_Tasks_Task___1___Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__System_String_"></a> AddNode<TInput, TOutput\>\(string, Func<TInput, CancellationToken, Task<TOutput\>\>, ICatalogEntry<TInput\>, ICatalogEntry<TOutput\>, string\)

Adds a node with single input and single output (asynchronous transformation with cancellation support).
All types are inferred from the transformation function signature.

```csharp
public PipelineBuilder AddNode<TInput, TOutput>(string label, Func<TInput, CancellationToken, Task<TOutput>> transform, ICatalogEntry<TInput> input, ICatalogEntry<TOutput> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<TInput, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOutput\>\>

Asynchronous transformation function from input to output with cancellation token

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TInput\>

Catalog entry providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOutput\>

Catalog entry to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TInput` 

Input type (inferred from transform)

`TOutput` 

Output type (inferred from transform)

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__2_System_String_System_Func___0___1__Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__System_String_"></a> AddNode<TInput, TOutput\>\(string, Func<TInput, TOutput\>, ICatalogEntry<TInput\>, ICatalogEntry<TOutput\>, string\)

Adds a node with single input and single output (synchronous transformation).
All types are inferred from the transformation function signature.

```csharp
public PipelineBuilder AddNode<TInput, TOutput>(string label, Func<TInput, TOutput> transform, ICatalogEntry<TInput> input, ICatalogEntry<TOutput> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TInput, TOutput\>

Synchronous transformation function from input to output

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TInput\>

Catalog entry providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOutput\>

Catalog entry to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TInput` 

Input type (inferred from transform)

`TOutput` 

Output type (inferred from transform)

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__2_System_String_System_Collections_Generic_IReadOnlyList_Flowthru_Data_ICatalogEntry___0___Flowthru_Data_ICatalogEntry___1__System_Func_System_Collections_Generic_IReadOnlyList___0____1__System_String_"></a> AddNode<TIn, TOut\>\(string, IReadOnlyList<ICatalogEntry<TIn\>\>, ICatalogEntry<TOut\>, Func<IReadOnlyList<TIn\>, TOut\>, string\)

Adds a homogeneous fan-in node: N catalog entries of the same element type collapse
into a single node whose transform receives all N loaded collections as a typed list.

```csharp
public PipelineBuilder AddNode<TIn, TOut>(string label, IReadOnlyList<ICatalogEntry<TIn>> inputs, ICatalogEntry<TOut> output, Func<IReadOnlyList<TIn>, TOut> node, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`inputs` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn\>\>

Variable-length list of same-typed input entries

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut\>

Catalog entry to store the merged result

`node` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TIn\>, TOut\>

Transform function receiving all N loaded values as a typed read-only list

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional human-readable description

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn` 

Element type of each input catalog entry

`TOut` 

Output type produced by the transform

#### Remarks

Use this when the number of inputs is not known at compile time — for example,
aggregating per-partition catalogs constructed in a loop. The function receives
<code>IReadOnlyList&lt;TIn&gt;</code> where each element corresponds to one input entry
in declaration order. An empty inputs list is allowed but produces an empty list argument.

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__3_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2____Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_String_"></a> AddNode<TIn1, TOut1, TOut2\>\(string, Func<TIn1, Task<\(TOut1, TOut2\)\>\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 1 input and 2 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2>(string label, Func<TIn1, Task<(TOut1, TOut2)>> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__3_System_String_System_Func___0_System_ValueTuple___1___2___Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_String_"></a> AddNode<TIn1, TOut1, TOut2\>\(string, Func<TIn1, \(TOut1, TOut2\)\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 1 input and 2 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2>(string label, Func<TIn1, (TOut1, TOut2)> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2\)\>

Synchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__4_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3____Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3\)\>\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 1 input and 3 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3)>> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__4_System_String_System_Func___0_System_ValueTuple___1___2___3___Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3\)\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 1 input and 3 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3>(string label, Func<TIn1, (TOut1, TOut2, TOut3)> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__5_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3___4____Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3, TOut4\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 1 input and 4 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3, TOut4>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3, TOut4)>> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__5_System_String_System_Func___0_System_ValueTuple___1___2___3___4___Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3, TOut4\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3, TOut4\)\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 1 input and 4 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3, TOut4>(string label, Func<TIn1, (TOut1, TOut2, TOut3, TOut4)> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__6_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3___4___5____Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 1 input and 5 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__6_System_String_System_Func___0_System_ValueTuple___1___2___3___4___5___Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 1 input and 5 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<TIn1, (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3___4___5___6____Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 1 input and 6 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func___0_System_ValueTuple___1___2___3___4___5___6___Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 1 input and 6 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<TIn1, (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3___4___5___6___7____Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 1 input and 7 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func___0_System_ValueTuple___1___2___3___4___5___6___7___Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 1 input and 7 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<TIn1, (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3___4___5___6___7_System_ValueTuple___8_____Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__System_ValueTuple_Flowthru_Data_ICatalogEntry___8____System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 1 input and 8 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func___0_System_ValueTuple___1___2___3___4___5___6___7_System_ValueTuple___8____Flowthru_Data_ICatalogEntry___0__System_ValueTuple_Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__System_ValueTuple_Flowthru_Data_ICatalogEntry___8____System_String_"></a> AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, ICatalogEntry<TIn1\>, \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 1 input and 8 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<TIn1, (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, ICatalogEntry<TIn1> input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__3_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___Flowthru_Data_ICatalogEntry___2__System_String_"></a> AddNode<TIn1, TIn2, TOut1\>\(string, Func<\(TIn1, TIn2\), Task<TOut1\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 2 inputs and 1 output (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1>(string label, Func<(TIn1, TIn2), Task<TOut1>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__3_System_String_System_Func_System_ValueTuple___0___1____2__System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___Flowthru_Data_ICatalogEntry___2__System_String_"></a> AddNode<TIn1, TIn2, TOut1\>\(string, Func<\(TIn1, TIn2\), TOut1\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 2 inputs and 1 output (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1>(string label, Func<(TIn1, TIn2), TOut1> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), TOut1\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__4_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 2 inputs and 2 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__4_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 2 inputs and 2 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2>(string label, Func<(TIn1, TIn2), (TOut1, TOut2)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__5_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 2 inputs and 3 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__5_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 2 inputs and 3 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__6_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4___5____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 2 inputs and 4 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__6_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 2 inputs and 4 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3, TOut4)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4___5___6____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 2 inputs and 5 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___5___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 2 inputs and 5 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4___5___6___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 2 inputs and 6 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___5___6___7___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 2 inputs and 6 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4___5___6___7___8____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 2 inputs and 7 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___5___6___7___8___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 2 inputs and 7 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4___5___6___7___8_System_ValueTuple___9_____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__System_ValueTuple_Flowthru_Data_ICatalogEntry___9____System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 2 inputs and 8 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___5___6___7___8_System_ValueTuple___9____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1___System_ValueTuple_Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__System_ValueTuple_Flowthru_Data_ICatalogEntry___9____System_String_"></a> AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 2 inputs and 8 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__4_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___Flowthru_Data_ICatalogEntry___3__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<TOut1\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 3 inputs and 1 output (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1>(string label, Func<(TIn1, TIn2, TIn3), Task<TOut1>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__4_System_String_System_Func_System_ValueTuple___0___1___2____3__System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___Flowthru_Data_ICatalogEntry___3__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3\), TOut1\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 3 inputs and 1 output (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1>(string label, Func<(TIn1, TIn2, TIn3), TOut1> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), TOut1\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__5_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 3 inputs and 2 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__5_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 3 inputs and 2 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__6_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 3 inputs and 3 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__6_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 3 inputs and 3 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5___6____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 3 inputs and 4 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 3 inputs and 4 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3, TOut4)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5___6___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 3 inputs and 5 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___6___7___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 3 inputs and 5 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5___6___7___8____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 3 inputs and 6 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___6___7___8___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 3 inputs and 6 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5___6___7___8___9____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 3 inputs and 7 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___6___7___8___9___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 3 inputs and 7 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5___6___7___8___9_System_ValueTuple___10_____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__System_ValueTuple_Flowthru_Data_ICatalogEntry___10____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 3 inputs and 8 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___6___7___8___9_System_ValueTuple___10____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2___System_ValueTuple_Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__System_ValueTuple_Flowthru_Data_ICatalogEntry___10____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 3 inputs and 8 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__5_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___Flowthru_Data_ICatalogEntry___4__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<TOut1\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 4 inputs and 1 output (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<TOut1>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__5_System_String_System_Func_System_ValueTuple___0___1___2___3____4__System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___Flowthru_Data_ICatalogEntry___4__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), TOut1\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 4 inputs and 1 output (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4), TOut1> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), TOut1\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__6_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 4 inputs and 2 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__6_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 4 inputs and 2 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 4 inputs and 3 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 4 inputs and 3 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 4 inputs and 4 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___7___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 4 inputs and 4 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3, TOut4)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6___7___8____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 4 inputs and 5 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___7___8___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 4 inputs and 5 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6___7___8___9____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 4 inputs and 6 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___7___8___9___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 4 inputs and 6 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6___7___8___9___10____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 4 inputs and 7 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___7___8___9___10___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 4 inputs and 7 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__12_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6___7___8___9___10_System_ValueTuple___11_____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__System_ValueTuple_Flowthru_Data_ICatalogEntry___11____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 4 inputs and 8 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__12_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___7___8___9___10_System_ValueTuple___11____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3___System_ValueTuple_Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__System_ValueTuple_Flowthru_Data_ICatalogEntry___11____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 4 inputs and 8 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__6_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___Flowthru_Data_ICatalogEntry___5__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<TOut1\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 5 inputs and 1 output (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<TOut1>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__6_System_String_System_Func_System_ValueTuple___0___1___2___3___4____5__System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___Flowthru_Data_ICatalogEntry___5__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), TOut1\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 5 inputs and 1 output (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), TOut1> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), TOut1\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 5 inputs and 2 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 5 inputs and 2 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 5 inputs and 3 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 5 inputs and 3 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7___8____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 5 inputs and 4 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___8___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 5 inputs and 4 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3, TOut4)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7___8___9____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 5 inputs and 5 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___8___9___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 5 inputs and 5 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7___8___9___10____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 5 inputs and 6 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___8___9___10___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 5 inputs and 6 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7___8___9___10___11____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 5 inputs and 7 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___8___9___10___11___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 5 inputs and 7 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7___8___9___10___11_System_ValueTuple___12_____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__System_ValueTuple_Flowthru_Data_ICatalogEntry___12____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 5 inputs and 8 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___8___9___10___11_System_ValueTuple___12____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4___System_ValueTuple_Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__System_ValueTuple_Flowthru_Data_ICatalogEntry___12____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 5 inputs and 8 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___Flowthru_Data_ICatalogEntry___6__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<TOut1\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 6 inputs and 1 output (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<TOut1>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__7_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5____6__System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___Flowthru_Data_ICatalogEntry___6__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), TOut1\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 6 inputs and 1 output (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), TOut1> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), TOut1\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 6 inputs and 2 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 6 inputs and 2 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 6 inputs and 3 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 6 inputs and 3 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8___9____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 6 inputs and 4 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___9___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 6 inputs and 4 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3, TOut4)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8___9___10____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 6 inputs and 5 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___9___10___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 6 inputs and 5 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8___9___10___11____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 6 inputs and 6 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___9___10___11___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 6 inputs and 6 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8___9___10___11___12____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 6 inputs and 7 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___9___10___11___12___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 6 inputs and 7 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8___9___10___11___12_System_ValueTuple___13_____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__System_ValueTuple_Flowthru_Data_ICatalogEntry___13____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 6 inputs and 8 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___9___10___11___12_System_ValueTuple___13____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5___System_ValueTuple_Flowthru_Data_ICatalogEntry___6__Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__System_ValueTuple_Flowthru_Data_ICatalogEntry___13____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 6 inputs and 8 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task___7___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___Flowthru_Data_ICatalogEntry___7__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<TOut1\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 7 inputs and 1 output (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<TOut1>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6____7__System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___Flowthru_Data_ICatalogEntry___7__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), TOut1\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 7 inputs and 1 output (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), TOut1> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), TOut1\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 7 inputs and 2 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 7 inputs and 2 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 7 inputs and 3 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 7 inputs and 3 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9___10____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 7 inputs and 4 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___10___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 7 inputs and 4 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3, TOut4)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9___10___11____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 7 inputs and 5 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___10___11___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 7 inputs and 5 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9___10___11___12____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 7 inputs and 6 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___10___11___12___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 7 inputs and 6 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9___10___11___12___13____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__Flowthru_Data_ICatalogEntry___13___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 7 inputs and 7 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___10___11___12___13___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__Flowthru_Data_ICatalogEntry___13___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 7 inputs and 7 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__15_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9___10___11___12___13_System_ValueTuple___14_____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__Flowthru_Data_ICatalogEntry___13__System_ValueTuple_Flowthru_Data_ICatalogEntry___14____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 7 inputs and 8 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__15_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___10___11___12___13_System_ValueTuple___14____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6___System_ValueTuple_Flowthru_Data_ICatalogEntry___7__Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__Flowthru_Data_ICatalogEntry___13__System_ValueTuple_Flowthru_Data_ICatalogEntry___14____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 7 inputs and 8 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task___8___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____Flowthru_Data_ICatalogEntry___8__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<TOut1\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 8 inputs and 1 output (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<TOut1>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7_____8__System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____Flowthru_Data_ICatalogEntry___8__System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), TOut1\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), ICatalogEntry<TOut1\>, string\)

Adds a node with 8 inputs and 1 output (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), TOut1> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, ICatalogEntry<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), TOut1\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 8 inputs and 2 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>\), string\)

Adds a node with 8 inputs and 2 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 8 inputs and 3 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>\), string\)

Adds a node with 8 inputs and 3 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10___11____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 8 inputs and 4 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___11___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>\), string\)

Adds a node with 8 inputs and 4 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3, TOut4)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10___11___12____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 8 inputs and 5 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___11___12___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>\), string\)

Adds a node with 8 inputs and 5 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10___11___12___13____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__Flowthru_Data_ICatalogEntry___13___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 8 inputs and 6 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___11___12___13___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__Flowthru_Data_ICatalogEntry___13___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>\), string\)

Adds a node with 8 inputs and 6 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__15_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10___11___12___13___14____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__Flowthru_Data_ICatalogEntry___13__Flowthru_Data_ICatalogEntry___14___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 8 inputs and 7 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__15_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___11___12___13___14___System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__Flowthru_Data_ICatalogEntry___13__Flowthru_Data_ICatalogEntry___14___System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>\), string\)

Adds a node with 8 inputs and 7 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__16_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10___11___12___13___14_System_ValueTuple___15_____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__Flowthru_Data_ICatalogEntry___13__Flowthru_Data_ICatalogEntry___14__System_ValueTuple_Flowthru_Data_ICatalogEntry___15____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 8 inputs and 8 outputs (asynchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_AddNode__16_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___11___12___13___14_System_ValueTuple___15____System_ValueTuple_Flowthru_Data_ICatalogEntry___0__Flowthru_Data_ICatalogEntry___1__Flowthru_Data_ICatalogEntry___2__Flowthru_Data_ICatalogEntry___3__Flowthru_Data_ICatalogEntry___4__Flowthru_Data_ICatalogEntry___5__Flowthru_Data_ICatalogEntry___6__System_ValueTuple_Flowthru_Data_ICatalogEntry___7____System_ValueTuple_Flowthru_Data_ICatalogEntry___8__Flowthru_Data_ICatalogEntry___9__Flowthru_Data_ICatalogEntry___10__Flowthru_Data_ICatalogEntry___11__Flowthru_Data_ICatalogEntry___12__Flowthru_Data_ICatalogEntry___13__Flowthru_Data_ICatalogEntry___14__System_ValueTuple_Flowthru_Data_ICatalogEntry___15____System_String_"></a> AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(ICatalogEntry<TIn1\>, ICatalogEntry<TIn2\>, ICatalogEntry<TIn3\>, ICatalogEntry<TIn4\>, ICatalogEntry<TIn5\>, ICatalogEntry<TIn6\>, ICatalogEntry<TIn7\>, ICatalogEntry<TIn8\>\), \(ICatalogEntry<TOut1\>, ICatalogEntry<TOut2\>, ICatalogEntry<TOut3\>, ICatalogEntry<TOut4\>, ICatalogEntry<TOut5\>, ICatalogEntry<TOut6\>, ICatalogEntry<TOut7\>, ICatalogEntry<TOut8\>\), string\)

Adds a node with 8 inputs and 8 outputs (synchronous transformation).

```csharp
public PipelineBuilder AddNode<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (ICatalogEntry<TIn1>, ICatalogEntry<TIn2>, ICatalogEntry<TIn3>, ICatalogEntry<TIn4>, ICatalogEntry<TIn5>, ICatalogEntry<TIn6>, ICatalogEntry<TIn7>, ICatalogEntry<TIn8>) input, (ICatalogEntry<TOut1>, ICatalogEntry<TOut2>, ICatalogEntry<TOut3>, ICatalogEntry<TOut4>, ICatalogEntry<TOut5>, ICatalogEntry<TOut6>, ICatalogEntry<TOut7>, ICatalogEntry<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this node

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TIn8\>\)

Catalog entry or tuple of catalog entries providing input data

`output` \([ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut1\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut2\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut3\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut4\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut5\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut6\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut7\>, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<TOut8\>\)

Catalog entry or tuple of catalog entries to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the node's purpose

#### Returns

 [PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TIn3` 

Input type 3

`TIn4` 

Input type 4

`TIn5` 

Input type 5

`TIn6` 

Input type 6

`TIn7` 

Input type 7

`TIn8` 

Input type 8

`TOut1` 

Output type 1

`TOut2` 

Output type 2

`TOut3` 

Output type 3

`TOut4` 

Output type 4

`TOut5` 

Output type 5

`TOut6` 

Output type 6

`TOut7` 

Output type 7

`TOut8` 

Output type 8

### <a id="Flowthru_Pipelines_PipelineBuilder_CreatePipeline_System_Action_Flowthru_Pipelines_PipelineBuilder__"></a> CreatePipeline\(Action<PipelineBuilder\>\)

Creates and configures a new pipeline using the builder pattern.

```csharp
public static Pipeline CreatePipeline(Action<PipelineBuilder> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[PipelineBuilder](Flowthru.Pipelines.PipelineBuilder.md)\>

Action to configure the pipeline by adding nodes

#### Returns

 [Pipeline](Flowthru.Pipelines.Pipeline.md)

Configured but not yet built pipeline

