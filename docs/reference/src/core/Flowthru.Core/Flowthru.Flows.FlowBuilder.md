# <a id="Flowthru_Flows_FlowBuilder"></a> Class FlowBuilder

Namespace: [Flowthru.Flows](Flowthru.Flows.md)  
Assembly: Flowthru.Core.dll  

Fluent builder for constructing type-safe flows with function-based steps.

```csharp
public class FlowBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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
types at flow construction time, catching type mismatches before execution.
</p>
<p>
<strong>Usage Patterns:</strong>
</p>
<pre><code class="lang-csharp">var flow = FlowBuilder.CreateFlow(builder =&gt;
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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__2_System_String_System_Func___0_System_Threading_Tasks_Task___1___Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__System_String_"></a> AddStep<TInput, TOutput\>\(string, Func<TInput, Task<TOutput\>\>, IItem<TInput\>, IItem<TOutput\>, string\)

Adds a step with single input and single output (asynchronous transformation).
All types are inferred from the transformation function signature.

```csharp
public FlowBuilder AddStep<TInput, TOutput>(string label, Func<TInput, Task<TOutput>> transform, IItem<TInput> input, IItem<TOutput> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TInput, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOutput\>\>

Asynchronous transformation function from input to output

`input` [IItem](Flowthru.Data.IItem\-1.md)<TInput\>

Catalog entry providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOutput\>

Catalog entry to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description for this step

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

This builder for method chaining

#### Type Parameters

`TInput` 

Input type (inferred from transform)

`TOutput` 

Output type (inferred from transform)

### <a id="Flowthru_Flows_FlowBuilder_AddStep__2_System_String_System_Func___0_System_Threading_CancellationToken_System_Threading_Tasks_Task___1___Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__System_String_"></a> AddStep<TInput, TOutput\>\(string, Func<TInput, CancellationToken, Task<TOutput\>\>, IItem<TInput\>, IItem<TOutput\>, string\)

Adds a step with single input and single output (asynchronous transformation with cancellation support).
All types are inferred from the transformation function signature.

```csharp
public FlowBuilder AddStep<TInput, TOutput>(string label, Func<TInput, CancellationToken, Task<TOutput>> transform, IItem<TInput> input, IItem<TOutput> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<TInput, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOutput\>\>

Asynchronous transformation function from input to output with cancellation token

`input` [IItem](Flowthru.Data.IItem\-1.md)<TInput\>

Catalog entry providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOutput\>

Catalog entry to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description for this step

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

This builder for method chaining

#### Type Parameters

`TInput` 

Input type (inferred from transform)

`TOutput` 

Output type (inferred from transform)

### <a id="Flowthru_Flows_FlowBuilder_AddStep__2_System_String_System_Func___0___1__Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__System_String_"></a> AddStep<TInput, TOutput\>\(string, Func<TInput, TOutput\>, IItem<TInput\>, IItem<TOutput\>, string\)

Adds a step with single input and single output (synchronous transformation).
All types are inferred from the transformation function signature.

```csharp
public FlowBuilder AddStep<TInput, TOutput>(string label, Func<TInput, TOutput> transform, IItem<TInput> input, IItem<TOutput> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TInput, TOutput\>

Synchronous transformation function from input to output

`input` [IItem](Flowthru.Data.IItem\-1.md)<TInput\>

Catalog entry providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOutput\>

Catalog entry to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description for this step

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

This builder for method chaining

#### Type Parameters

`TInput` 

Input type (inferred from transform)

`TOutput` 

Output type (inferred from transform)

### <a id="Flowthru_Flows_FlowBuilder_AddStep__2_System_String_System_Collections_Generic_IReadOnlyList_Flowthru_Data_IItem___0___Flowthru_Data_IItem___1__System_Func_System_Collections_Generic_IReadOnlyList___0____1__System_String_"></a> AddStep<TIn, TOut\>\(string, IReadOnlyList<IItem<TIn\>\>, IItem<TOut\>, Func<IReadOnlyList<TIn\>, TOut\>, string\)

Adds a homogeneous fan-in step: N catalog entries of the same element type collapse
into a single step whose transform receives all N loaded collections as a typed list.

```csharp
public FlowBuilder AddStep<TIn, TOut>(string label, IReadOnlyList<IItem<TIn>> inputs, IItem<TOut> output, Func<IReadOnlyList<TIn>, TOut> step, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`inputs` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[IItem](Flowthru.Data.IItem\-1.md)<TIn\>\>

Variable-length list of same-typed input entries

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut\>

Catalog entry to store the merged result

`step` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<TIn\>, TOut\>

Transform function receiving all N loaded values as a typed read-only list

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional human-readable description

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__3_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2____Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_String_"></a> AddStep<TIn1, TOut1, TOut2\>\(string, Func<TIn1, Task<\(TOut1, TOut2\)\>\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 1 input and 2 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2>(string label, Func<TIn1, Task<(TOut1, TOut2)>> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Flows_FlowBuilder_AddStep__3_System_String_System_Func___0_System_ValueTuple___1___2___Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_String_"></a> AddStep<TIn1, TOut1, TOut2\>\(string, Func<TIn1, \(TOut1, TOut2\)\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 1 input and 2 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2>(string label, Func<TIn1, (TOut1, TOut2)> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2\)\>

Synchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Flows_FlowBuilder_AddStep__4_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3____Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3\)\>\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 1 input and 3 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3)>> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__4_System_String_System_Func___0_System_ValueTuple___1___2___3___Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3\)\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 1 input and 3 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3>(string label, Func<TIn1, (TOut1, TOut2, TOut3)> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__5_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3___4____Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3, TOut4\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 1 input and 4 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3, TOut4>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3, TOut4)>> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__5_System_String_System_Func___0_System_ValueTuple___1___2___3___4___Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3, TOut4\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3, TOut4\)\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 1 input and 4 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3, TOut4>(string label, Func<TIn1, (TOut1, TOut2, TOut3, TOut4)> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__6_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3___4___5____Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 1 input and 5 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__6_System_String_System_Func___0_System_ValueTuple___1___2___3___4___5___Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 1 input and 5 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<TIn1, (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3___4___5___6____Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 1 input and 6 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func___0_System_ValueTuple___1___2___3___4___5___6___Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 1 input and 6 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<TIn1, (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3___4___5___6___7____Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 1 input and 7 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func___0_System_ValueTuple___1___2___3___4___5___6___7___Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 1 input and 7 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<TIn1, (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func___0_System_Threading_Tasks_Task_System_ValueTuple___1___2___3___4___5___6___7_System_ValueTuple___8_____Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__System_ValueTuple_Flowthru_Data_IItem___8____System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<TIn1, Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 1 input and 8 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<TIn1, Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func___0_System_ValueTuple___1___2___3___4___5___6___7_System_ValueTuple___8____Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__System_ValueTuple_Flowthru_Data_IItem___8____System_String_"></a> AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 1 input and 8 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<TIn1, (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TIn1, \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` [IItem](Flowthru.Data.IItem\-1.md)<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__3_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task___2___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___Flowthru_Data_IItem___2__System_String_"></a> AddStep<TIn1, TIn2, TOut1\>\(string, Func<\(TIn1, TIn2\), Task<TOut1\>\>, \(IItem<TIn1\>, IItem<TIn2\>\), IItem<TOut1\>, string\)

Adds a step with 2 inputs and 1 output (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1>(string label, Func<(TIn1, TIn2), Task<TOut1>> transform, (IItem<TIn1>, IItem<TIn2>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

### <a id="Flowthru_Flows_FlowBuilder_AddStep__3_System_String_System_Func_System_ValueTuple___0___1____2__System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___Flowthru_Data_IItem___2__System_String_"></a> AddStep<TIn1, TIn2, TOut1\>\(string, Func<\(TIn1, TIn2\), TOut1\>, \(IItem<TIn1\>, IItem<TIn2\>\), IItem<TOut1\>, string\)

Adds a step with 2 inputs and 1 output (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1>(string label, Func<(TIn1, TIn2), TOut1> transform, (IItem<TIn1>, IItem<TIn2>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), TOut1\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

### <a id="Flowthru_Flows_FlowBuilder_AddStep__4_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 2 inputs and 2 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2)>> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__4_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2\)\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 2 inputs and 2 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2>(string label, Func<(TIn1, TIn2), (TOut1, TOut2)> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__5_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 2 inputs and 3 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3)>> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__5_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3\)\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 2 inputs and 3 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3)> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__6_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4___5____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 2 inputs and 4 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__6_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___5___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 2 inputs and 4 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3, TOut4)> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4___5___6____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 2 inputs and 5 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___5___6___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 2 inputs and 5 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4___5___6___7____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 2 inputs and 6 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___5___6___7___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 2 inputs and 6 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4___5___6___7___8____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 2 inputs and 7 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___5___6___7___8___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 2 inputs and 7 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1__System_Threading_Tasks_Task_System_ValueTuple___2___3___4___5___6___7___8_System_ValueTuple___9_____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__System_ValueTuple_Flowthru_Data_IItem___9____System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 2 inputs and 8 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1__System_ValueTuple___2___3___4___5___6___7___8_System_ValueTuple___9____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__System_ValueTuple_Flowthru_Data_IItem___9____System_String_"></a> AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 2 inputs and 8 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__4_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task___3___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___Flowthru_Data_IItem___3__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<TOut1\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), IItem<TOut1\>, string\)

Adds a step with 3 inputs and 1 output (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1>(string label, Func<(TIn1, TIn2, TIn3), Task<TOut1>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__4_System_String_System_Func_System_ValueTuple___0___1___2____3__System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___Flowthru_Data_IItem___3__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3\), TOut1\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), IItem<TOut1\>, string\)

Adds a step with 3 inputs and 1 output (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1>(string label, Func<(TIn1, TIn2, TIn3), TOut1> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), TOut1\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__5_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 3 inputs and 2 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__5_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 3 inputs and 2 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__6_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 3 inputs and 3 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__6_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 3 inputs and 3 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5___6____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 3 inputs and 4 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___6___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 3 inputs and 4 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3, TOut4)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5___6___7____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 3 inputs and 5 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___6___7___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 3 inputs and 5 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5___6___7___8____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 3 inputs and 6 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___6___7___8___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 3 inputs and 6 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5___6___7___8___9____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 3 inputs and 7 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___6___7___8___9___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 3 inputs and 7 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2__System_Threading_Tasks_Task_System_ValueTuple___3___4___5___6___7___8___9_System_ValueTuple___10_____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__System_ValueTuple_Flowthru_Data_IItem___10____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 3 inputs and 8 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2__System_ValueTuple___3___4___5___6___7___8___9_System_ValueTuple___10____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__System_ValueTuple_Flowthru_Data_IItem___10____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 3 inputs and 8 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__5_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task___4___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___Flowthru_Data_IItem___4__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<TOut1\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), IItem<TOut1\>, string\)

Adds a step with 4 inputs and 1 output (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<TOut1>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__5_System_String_System_Func_System_ValueTuple___0___1___2___3____4__System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___Flowthru_Data_IItem___4__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), TOut1\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), IItem<TOut1\>, string\)

Adds a step with 4 inputs and 1 output (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4), TOut1> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), TOut1\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__6_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 4 inputs and 2 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__6_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 4 inputs and 2 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 4 inputs and 3 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 4 inputs and 3 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6___7____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 4 inputs and 4 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___7___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 4 inputs and 4 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3, TOut4)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6___7___8____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 4 inputs and 5 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___7___8___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 4 inputs and 5 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6___7___8___9____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 4 inputs and 6 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___7___8___9___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 4 inputs and 6 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6___7___8___9___10____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 4 inputs and 7 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___7___8___9___10___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 4 inputs and 7 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__12_System_String_System_Func_System_ValueTuple___0___1___2___3__System_Threading_Tasks_Task_System_ValueTuple___4___5___6___7___8___9___10_System_ValueTuple___11_____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__System_ValueTuple_Flowthru_Data_IItem___11____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 4 inputs and 8 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__12_System_String_System_Func_System_ValueTuple___0___1___2___3__System_ValueTuple___4___5___6___7___8___9___10_System_ValueTuple___11____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__System_ValueTuple_Flowthru_Data_IItem___11____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 4 inputs and 8 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__6_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task___5___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___Flowthru_Data_IItem___5__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<TOut1\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), IItem<TOut1\>, string\)

Adds a step with 5 inputs and 1 output (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<TOut1>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__6_System_String_System_Func_System_ValueTuple___0___1___2___3___4____5__System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___Flowthru_Data_IItem___5__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), TOut1\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), IItem<TOut1\>, string\)

Adds a step with 5 inputs and 1 output (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), TOut1> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), TOut1\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 5 inputs and 2 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 5 inputs and 2 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 5 inputs and 3 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 5 inputs and 3 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7___8____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 5 inputs and 4 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___8___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 5 inputs and 4 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3, TOut4)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7___8___9____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 5 inputs and 5 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___8___9___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 5 inputs and 5 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7___8___9___10____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 5 inputs and 6 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___8___9___10___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 5 inputs and 6 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7___8___9___10___11____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 5 inputs and 7 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___8___9___10___11___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 5 inputs and 7 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_Threading_Tasks_Task_System_ValueTuple___5___6___7___8___9___10___11_System_ValueTuple___12_____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__System_ValueTuple_Flowthru_Data_IItem___12____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 5 inputs and 8 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4__System_ValueTuple___5___6___7___8___9___10___11_System_ValueTuple___12____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__System_ValueTuple_Flowthru_Data_IItem___12____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 5 inputs and 8 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task___6___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___Flowthru_Data_IItem___6__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<TOut1\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), IItem<TOut1\>, string\)

Adds a step with 6 inputs and 1 output (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<TOut1>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__7_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5____6__System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___Flowthru_Data_IItem___6__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), TOut1\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), IItem<TOut1\>, string\)

Adds a step with 6 inputs and 1 output (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), TOut1> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), TOut1\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 6 inputs and 2 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 6 inputs and 2 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 6 inputs and 3 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 6 inputs and 3 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8___9____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 6 inputs and 4 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___9___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 6 inputs and 4 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3, TOut4)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8___9___10____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 6 inputs and 5 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___9___10___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 6 inputs and 5 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8___9___10___11____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 6 inputs and 6 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___9___10___11___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 6 inputs and 6 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8___9___10___11___12____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 6 inputs and 7 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___9___10___11___12___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 6 inputs and 7 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_Threading_Tasks_Task_System_ValueTuple___6___7___8___9___10___11___12_System_ValueTuple___13_____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__System_ValueTuple_Flowthru_Data_IItem___13____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 6 inputs and 8 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5__System_ValueTuple___6___7___8___9___10___11___12_System_ValueTuple___13____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__System_ValueTuple_Flowthru_Data_IItem___13____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 6 inputs and 8 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task___7___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___Flowthru_Data_IItem___7__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<TOut1\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), IItem<TOut1\>, string\)

Adds a step with 7 inputs and 1 output (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<TOut1>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__8_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6____7__System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___Flowthru_Data_IItem___7__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), TOut1\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), IItem<TOut1\>, string\)

Adds a step with 7 inputs and 1 output (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), TOut1> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), TOut1\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 7 inputs and 2 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 7 inputs and 2 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 7 inputs and 3 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 7 inputs and 3 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9___10____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 7 inputs and 4 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___10___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 7 inputs and 4 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3, TOut4)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9___10___11____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 7 inputs and 5 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___10___11___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 7 inputs and 5 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9___10___11___12____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 7 inputs and 6 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___10___11___12___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 7 inputs and 6 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9___10___11___12___13____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 7 inputs and 7 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___10___11___12___13___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 7 inputs and 7 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__15_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_Threading_Tasks_Task_System_ValueTuple___7___8___9___10___11___12___13_System_ValueTuple___14_____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13__System_ValueTuple_Flowthru_Data_IItem___14____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 7 inputs and 8 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__15_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6__System_ValueTuple___7___8___9___10___11___12___13_System_ValueTuple___14____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13__System_ValueTuple_Flowthru_Data_IItem___14____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 7 inputs and 8 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task___8___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____Flowthru_Data_IItem___8__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<TOut1\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), IItem<TOut1\>, string\)

Adds a step with 8 inputs and 1 output (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<TOut1>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOut1\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__9_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7_____8__System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____Flowthru_Data_IItem___8__System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), TOut1\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), IItem<TOut1\>, string\)

Adds a step with 8 inputs and 1 output (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), TOut1> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, IItem<TOut1> output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), TOut1\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` [IItem](Flowthru.Data.IItem\-1.md)<TOut1\>

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 8 inputs and 2 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__10_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>\), string\)

Adds a step with 8 inputs and 2 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 8 inputs and 3 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__11_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), string\)

Adds a step with 8 inputs and 3 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10___11____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3, TOut4\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 8 inputs and 4 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3, TOut4)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__12_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___11___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), string\)

Adds a step with 8 inputs and 4 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3, TOut4)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10___11___12____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 8 inputs and 5 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3, TOut4, TOut5)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__13_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___11___12___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), string\)

Adds a step with 8 inputs and 5 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3, TOut4, TOut5)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10___11___12___13____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 8 inputs and 6 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__14_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___11___12___13___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), string\)

Adds a step with 8 inputs and 6 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__15_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10___11___12___13___14____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13__Flowthru_Data_IItem___14___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 8 inputs and 7 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__15_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___11___12___13___14___System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13__Flowthru_Data_IItem___14___System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), string\)

Adds a step with 8 inputs and 7 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__16_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_Threading_Tasks_Task_System_ValueTuple___8___9___10___11___12___13___14_System_ValueTuple___15_____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13__Flowthru_Data_IItem___14__System_ValueTuple_Flowthru_Data_IItem___15____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), Task<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 8 inputs and 8 outputs (asynchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), Task<(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)>> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<\(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>\>

Asynchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_AddStep__16_System_String_System_Func_System_ValueTuple___0___1___2___3___4___5___6_System_ValueTuple___7___System_ValueTuple___8___9___10___11___12___13___14_System_ValueTuple___15____System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13__Flowthru_Data_IItem___14__System_ValueTuple_Flowthru_Data_IItem___15____System_String_"></a> AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(string, Func<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), string\)

Adds a step with 8 inputs and 8 outputs (synchronous transformation).

```csharp
public FlowBuilder AddStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(string label, Func<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), (TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8)> transform, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, string description = "")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<\(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8\), \(TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\)\>

Synchronous transformation function

`input` \([IItem](Flowthru.Data.IItem\-1.md)<TIn1\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn2\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn3\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn4\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn5\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn6\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn7\>, [IItem](Flowthru.Data.IItem\-1.md)<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \([IItem](Flowthru.Data.IItem\-1.md)<TOut1\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut2\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut3\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut4\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut5\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut6\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut7\>, [IItem](Flowthru.Data.IItem\-1.md)<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional description of the step's purpose

#### Returns

 [FlowBuilder](Flowthru.Flows.FlowBuilder.md)

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

### <a id="Flowthru_Flows_FlowBuilder_CreateFlow_System_Action_Flowthru_Flows_FlowBuilder__"></a> CreateFlow\(Action<FlowBuilder\>\)

Creates and configures a new flow using the builder pattern.

```csharp
public static Flow CreateFlow(Action<FlowBuilder> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[FlowBuilder](Flowthru.Flows.FlowBuilder.md)\>

Action to configure the flow by adding steps

#### Returns

 [Flow](Flowthru.Flows.Flow.md)

Configured but not yet built flow

