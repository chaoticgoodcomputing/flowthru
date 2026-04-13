# <a id="Flowthru_Core_Flows_FlowBuilder"></a> Class FlowBuilder

Namespace: [Flowthru.Core.Flows](Flowthru.Core.Flows.md)  
Assembly: Flowthru.Core.dll  

Fluent builder for constructing type-safe flows with function-based steps.

```csharp
public class FlowBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowBuilder](Flowthru.Core.Flows.FlowBuilder.md)

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
Steps are pure transformation functions with compile-time type safety.
Both synchronous and asynchronous functions are supported:
- Sync: Func&lt;TInput, TOutput&gt;
- Async: Func&lt;TInput, Task&lt;TOutput&gt;&gt;
- Multi-input: Func&lt;(TIn1, TIn2, ...), TOutput&gt; or Task&lt;TOutput&gt;
- Multi-output: Func&lt;TInput, (TOut1, TOut2, ...)&gt; or Task&lt;(TOut1, TOut2, ...)&gt;
</p>
<p>
Use synchronous functions for pure data transformations. Use asynchronous functions
only when your step performs I/O operations (external APIs, databases, etc.).
</p>
<p>
The compiler infers all types from function signatures and validates catalog entry
types at Flow construction time, catching type mismatches before execution.
</p>
<p>
<strong>Usage Patterns:</strong>
</p>
<pre><code class="lang-csharp">var Flow = FlowBuilder.CreateFlow(builder =&gt;
{
    // Simple synchronous step
    builder.AddStep(
        name: "Preprocess",
        transform: PreprocessStep.Create(),
        input: catalog.RawData,
        output: catalog.ProcessedData
    );

    // Multi-input step: tuple → single output
    builder.AddStep(
        name: "TrainModel",
        transform: TrainModelStep.Create(),
        input: (catalog.XTrain, catalog.YTrain),
        output: catalog.Model
    );

    // Multi-output step: single input → tuple
    builder.AddStep(
        name: "SplitData",
        transform: SplitDataStep.Create(),
        input: catalog.Data,
        output: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest)
    );

    // Asynchronous step (only when needed for I/O)
    builder.AddStep(
        name: "FetchExternalData",
        transform: ExternalDataStep.Create(),
        input: catalog.Config,
        output: catalog.ExternalData
    );
});

flow.Build();
await flow.ExecuteAsync();</code></pre>

## Methods

### <a id="Flowthru_Core_Flows_FlowBuilder_AddStep__2_System_String_System_Func___0_System_Threading_Tasks_Task___1___Flowthru_Core_Graph_INode___0__Flowthru_Core_Graph_INode___1__System_String_"></a> AddStep<TInput, TOutput\>\(string, Func<TInput, Task<TOutput\>\>, INode<TInput\>, INode<TOutput\>, string\)

Adds a step with single input and single output (asynchronous transformation).
All types are inferred from the transformation function signature.

```csharp
public FlowBuilder AddStep<TInput, TOutput>(string label, Func<TInput, Task<TOutput>> transform, INode<TInput> input, INode<TOutput> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TInput, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOutput\>\>

Asynchronous transformation function from input to output

`input` [INode](Flowthru.Core.Graph.INode\-1.md)<TInput\>

Catalog entry providing input data

`output` [INode](Flowthru.Core.Graph.INode\-1.md)<TOutput\>

Catalog entry to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description for this step

#### Returns

 [FlowBuilder](Flowthru.Core.Flows.FlowBuilder.md)

This builder for method chaining

#### Type Parameters

`TInput` 

Input type (inferred from transform)

`TOutput` 

Output type (inferred from transform)

### <a id="Flowthru_Core_Flows_FlowBuilder_AddStep__2_System_String_System_Func___0_System_Threading_CancellationToken_System_Threading_Tasks_Task___1___Flowthru_Core_Graph_INode___0__Flowthru_Core_Graph_INode___1__System_String_"></a> AddStep<TInput, TOutput\>\(string, Func<TInput, CancellationToken, Task<TOutput\>\>, INode<TInput\>, INode<TOutput\>, string\)

Adds a step with single input and single output (asynchronous transformation with cancellation support).
All types are inferred from the transformation function signature.

```csharp
public FlowBuilder AddStep<TInput, TOutput>(string label, Func<TInput, CancellationToken, Task<TOutput>> transform, INode<TInput> input, INode<TOutput> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<TInput, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOutput\>\>

Asynchronous transformation function from input to output with cancellation token

`input` [INode](Flowthru.Core.Graph.INode\-1.md)<TInput\>

Catalog entry providing input data

`output` [INode](Flowthru.Core.Graph.INode\-1.md)<TOutput\>

Catalog entry to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description for this step

#### Returns

 [FlowBuilder](Flowthru.Core.Flows.FlowBuilder.md)

This builder for method chaining

#### Type Parameters

`TInput` 

Input type (inferred from transform)

`TOutput` 

Output type (inferred from transform)

### <a id="Flowthru_Core_Flows_FlowBuilder_AddStep__2_System_String_System_Func___0___1__Flowthru_Core_Graph_INode___0__Flowthru_Core_Graph_INode___1__System_String_"></a> AddStep<TInput, TOutput\>\(string, Func<TInput, TOutput\>, INode<TInput\>, INode<TOutput\>, string\)

Adds a step with single input and single output (synchronous transformation).
All types are inferred from the transformation function signature.

```csharp
public FlowBuilder AddStep<TInput, TOutput>(string label, Func<TInput, TOutput> transform, INode<TInput> input, INode<TOutput> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TInput, TOutput\>

Synchronous transformation function from input to output

`input` [INode](Flowthru.Core.Graph.INode\-1.md)<TInput\>

Catalog entry providing input data

`output` [INode](Flowthru.Core.Graph.INode\-1.md)<TOutput\>

Catalog entry to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description for this step

#### Returns

 [FlowBuilder](Flowthru.Core.Flows.FlowBuilder.md)

This builder for method chaining

#### Type Parameters

`TInput` 

Input type (inferred from transform)

`TOutput` 

Output type (inferred from transform)

### <a id="Flowthru_Core_Flows_FlowBuilder_AddStep__2_System_String_System_Collections_Generic_IReadOnlyList_Flowthru_Core_Graph_INode___0___Flowthru_Core_Graph_INode___1__System_Func_System_Collections_Generic_IReadOnlyList___0____1__System_String_"></a> AddStep<TIn, TOut\>\(string, IReadOnlyList<INode<TIn\>\>, INode<TOut\>, Func<IReadOnlyList<TIn\>, TOut\>, string\)

Adds a homogeneous fan-in step: N catalog entries of the same element type collapse
into a single step whose transform receives all N loaded collections as a typed list.

```csharp
public FlowBuilder AddStep<TIn, TOut>(string label, IReadOnlyList<INode<TIn>> inputs, INode<TOut> output, Func<IReadOnlyList<TIn>, TOut> step, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`inputs` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[INode](Flowthru.Core.Graph.INode\-1.md)<TIn\>\>

Variable-length list of same-typed input entries

`output` [INode](Flowthru.Core.Graph.INode\-1.md)<TOut\>

Catalog entry to store the merged result

`step` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TIn\>, TOut\>

Transform function receiving all N loaded values as a typed read-only list

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional human-readable description

#### Returns

 [FlowBuilder](Flowthru.Core.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Core_Flows_FlowBuilder_CreateFlow_System_Action_Flowthru_Core_Flows_FlowBuilder__"></a> CreateFlow\(Action<FlowBuilder\>\)

Creates and configures a new Flow using the builder pattern.

```csharp
public static Flow CreateFlow(Action<FlowBuilder> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[FlowBuilder](Flowthru.Core.Flows.FlowBuilder.md)\>

Action to configure the Flow by adding steps

#### Returns

 [Flow](Flowthru.Core.Flows.Flow.md)

Configured but not yet built flow

