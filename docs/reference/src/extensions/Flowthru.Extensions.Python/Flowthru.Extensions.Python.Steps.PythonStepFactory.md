# <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory"></a> Class PythonStepFactory

Namespace: [Flowthru.Extensions.Python.Steps](Flowthru.Extensions.Python.Steps.md)  
Assembly: Flowthru.Extensions.Python.dll  

Extension methods for adding Python steps to flows.

```csharp
public static class PythonStepFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PythonStepFactory](Flowthru.Extensions.Python.Steps.PythonStepFactory.md)

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
<strong>Phases 2-5 implementation:</strong>
Hand-written 1×1 AddPythonStep(Phase 2-4).
Source-generated N×M overloads for multi-I/O support (Phase 5).
</p>
<p>
<strong>Usage:</strong>
</p>
<pre><code class="lang-csharp">public static Flow Create(
    Catalog catalog,
    IPythonExecutor executor,
    PythonRuntime runtime)
{
    return FlowBuilder.CreateFlow(flow =&gt;
    {
        flow.AddPythonStep(
            label: "Transform",
            module: "my_steps.transform",
            function: "process",
            input: catalog.RawData,
            output: catalog.ProcessedData,
            executor: executor,
            runtime: runtime
        );
    });
}</code></pre>
<p>
<strong>Future phases:</strong>
<ul><li>Phase 6: Async support</li></ul>
</p>

## Methods

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__2_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TInput, TOutput\>\(FlowBuilder, string, string, string, IItem<TInput\>, IItem<TOutput\>, IPythonExecutor, string\)

Adds a Python step with single input and single output.

```csharp
public static FlowBuilder AddPythonStep<TInput, TOutput>(this FlowBuilder builder, string label, string module, string function, IItem<TInput> input, IItem<TOutput> output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance.

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step.

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model").
Must be resolvable via sys.path.

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module.

`input` IItem<TInput\>

Catalog item providing input data.

`output` IItem<TOutput\>

Catalog item to store output data.

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function.

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description.

#### Returns

 FlowBuilder

This builder for method chaining.

#### Type Parameters

`TInput` 

Input type (must match catalog item type).

`TOutput` 

Output type (must match catalog item type).

#### Remarks

<p>
<strong>Compile-time type safety:</strong>
Generic type parameters are inferred from catalog items.
Mismatched types produce compiler errors.
</p>
<p>
<strong>Registration-time validation (Phase 4):</strong>
<ul><li>Module is importable (exists, no syntax errors)</li><li>Function exists in module</li><li>@step decorator is present</li></ul>
</p>
<p>
<strong>Pre-flight validation (Phase 4):</strong>
<ul><li>Decorator schemas match C# generic types</li><li>Function signature arity is correct</li><li>Dry-run with 0-row data validates output structure</li></ul>
</p>

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__3_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TOut1, TOut2\>\(FlowBuilder, string, string, string, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>\), IPythonExecutor, string\)

Adds a Python step with 1 input and 2 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TOut1, TOut2>(this FlowBuilder builder, string label, string module, string function, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` IItem<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TOut1` 

Output type 1

`TOut2` 

Output type 2

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__4_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TOut1, TOut2, TOut3\>\(FlowBuilder, string, string, string, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), IPythonExecutor, string\)

Adds a Python step with 1 input and 3 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TOut1, TOut2, TOut3>(this FlowBuilder builder, string label, string module, string function, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` IItem<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__5_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TOut1, TOut2, TOut3, TOut4\>\(FlowBuilder, string, string, string, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), IPythonExecutor, string\)

Adds a Python step with 1 input and 4 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TOut1, TOut2, TOut3, TOut4>(this FlowBuilder builder, string label, string module, string function, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` IItem<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__6_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5\>\(FlowBuilder, string, string, string, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), IPythonExecutor, string\)

Adds a Python step with 1 input and 5 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5>(this FlowBuilder builder, string label, string module, string function, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` IItem<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__7_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(FlowBuilder, string, string, string, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), IPythonExecutor, string\)

Adds a Python step with 1 input and 6 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(this FlowBuilder builder, string label, string module, string function, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` IItem<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__8_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(FlowBuilder, string, string, string, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), IPythonExecutor, string\)

Adds a Python step with 1 input and 7 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(this FlowBuilder builder, string label, string module, string function, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` IItem<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__9_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_Flowthru_Data_IItem___0__System_ValueTuple_Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__System_ValueTuple_Flowthru_Data_IItem___8____Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(FlowBuilder, string, string, string, IItem<TIn1\>, \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), IPythonExecutor, string\)

Adds a Python step with 1 input and 8 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(this FlowBuilder builder, string label, string module, string function, IItem<TIn1> input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` IItem<TIn1\>

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__3_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___Flowthru_Data_IItem___2__Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TOut1\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>\), IItem<TOut1\>, IPythonExecutor, string\)

Adds a Python step with 2 inputs and 1 output.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TOut1>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>) input, IItem<TOut1> output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` IItem<TOut1\>

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

This builder for method chaining

#### Type Parameters

`TIn1` 

Input type 1

`TIn2` 

Input type 2

`TOut1` 

Output type 1

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__4_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TOut1, TOut2\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>\), IPythonExecutor, string\)

Adds a Python step with 2 inputs and 2 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TOut1, TOut2>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__5_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), IPythonExecutor, string\)

Adds a Python step with 2 inputs and 3 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__6_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), IPythonExecutor, string\)

Adds a Python step with 2 inputs and 4 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__7_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), IPythonExecutor, string\)

Adds a Python step with 2 inputs and 5 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__8_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), IPythonExecutor, string\)

Adds a Python step with 2 inputs and 6 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__9_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), IPythonExecutor, string\)

Adds a Python step with 2 inputs and 7 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__10_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1___System_ValueTuple_Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__System_ValueTuple_Flowthru_Data_IItem___9____Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), IPythonExecutor, string\)

Adds a Python step with 2 inputs and 8 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__4_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___Flowthru_Data_IItem___3__Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TOut1\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), IItem<TOut1\>, IPythonExecutor, string\)

Adds a Python step with 3 inputs and 1 output.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TOut1>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, IItem<TOut1> output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` IItem<TOut1\>

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__5_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>\), IPythonExecutor, string\)

Adds a Python step with 3 inputs and 2 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__6_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), IPythonExecutor, string\)

Adds a Python step with 3 inputs and 3 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__7_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), IPythonExecutor, string\)

Adds a Python step with 3 inputs and 4 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__8_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), IPythonExecutor, string\)

Adds a Python step with 3 inputs and 5 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__9_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), IPythonExecutor, string\)

Adds a Python step with 3 inputs and 6 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__10_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), IPythonExecutor, string\)

Adds a Python step with 3 inputs and 7 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__11_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2___System_ValueTuple_Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__System_ValueTuple_Flowthru_Data_IItem___10____Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), IPythonExecutor, string\)

Adds a Python step with 3 inputs and 8 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__5_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___Flowthru_Data_IItem___4__Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), IItem<TOut1\>, IPythonExecutor, string\)

Adds a Python step with 4 inputs and 1 output.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, IItem<TOut1> output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` IItem<TOut1\>

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__6_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>\), IPythonExecutor, string\)

Adds a Python step with 4 inputs and 2 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__7_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), IPythonExecutor, string\)

Adds a Python step with 4 inputs and 3 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__8_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), IPythonExecutor, string\)

Adds a Python step with 4 inputs and 4 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__9_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), IPythonExecutor, string\)

Adds a Python step with 4 inputs and 5 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__10_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), IPythonExecutor, string\)

Adds a Python step with 4 inputs and 6 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__11_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), IPythonExecutor, string\)

Adds a Python step with 4 inputs and 7 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__12_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3___System_ValueTuple_Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__System_ValueTuple_Flowthru_Data_IItem___11____Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), IPythonExecutor, string\)

Adds a Python step with 4 inputs and 8 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__6_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___Flowthru_Data_IItem___5__Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), IItem<TOut1\>, IPythonExecutor, string\)

Adds a Python step with 5 inputs and 1 output.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, IItem<TOut1> output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` IItem<TOut1\>

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__7_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>\), IPythonExecutor, string\)

Adds a Python step with 5 inputs and 2 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__8_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), IPythonExecutor, string\)

Adds a Python step with 5 inputs and 3 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__9_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), IPythonExecutor, string\)

Adds a Python step with 5 inputs and 4 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__10_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), IPythonExecutor, string\)

Adds a Python step with 5 inputs and 5 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__11_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), IPythonExecutor, string\)

Adds a Python step with 5 inputs and 6 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__12_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), IPythonExecutor, string\)

Adds a Python step with 5 inputs and 7 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__13_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4___System_ValueTuple_Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__System_ValueTuple_Flowthru_Data_IItem___12____Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), IPythonExecutor, string\)

Adds a Python step with 5 inputs and 8 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__7_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___Flowthru_Data_IItem___6__Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), IItem<TOut1\>, IPythonExecutor, string\)

Adds a Python step with 6 inputs and 1 output.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, IItem<TOut1> output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` IItem<TOut1\>

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__8_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>\), IPythonExecutor, string\)

Adds a Python step with 6 inputs and 2 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__9_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), IPythonExecutor, string\)

Adds a Python step with 6 inputs and 3 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__10_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), IPythonExecutor, string\)

Adds a Python step with 6 inputs and 4 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__11_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), IPythonExecutor, string\)

Adds a Python step with 6 inputs and 5 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__12_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), IPythonExecutor, string\)

Adds a Python step with 6 inputs and 6 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__13_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), IPythonExecutor, string\)

Adds a Python step with 6 inputs and 7 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__14_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5___System_ValueTuple_Flowthru_Data_IItem___6__Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__System_ValueTuple_Flowthru_Data_IItem___13____Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), IPythonExecutor, string\)

Adds a Python step with 6 inputs and 8 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__8_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___Flowthru_Data_IItem___7__Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), IItem<TOut1\>, IPythonExecutor, string\)

Adds a Python step with 7 inputs and 1 output.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, IItem<TOut1> output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` IItem<TOut1\>

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__9_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>\), IPythonExecutor, string\)

Adds a Python step with 7 inputs and 2 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__10_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), IPythonExecutor, string\)

Adds a Python step with 7 inputs and 3 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__11_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), IPythonExecutor, string\)

Adds a Python step with 7 inputs and 4 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__12_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), IPythonExecutor, string\)

Adds a Python step with 7 inputs and 5 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__13_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), IPythonExecutor, string\)

Adds a Python step with 7 inputs and 6 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__14_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), IPythonExecutor, string\)

Adds a Python step with 7 inputs and 7 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__15_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6___System_ValueTuple_Flowthru_Data_IItem___7__Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13__System_ValueTuple_Flowthru_Data_IItem___14____Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), IPythonExecutor, string\)

Adds a Python step with 7 inputs and 8 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__9_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____Flowthru_Data_IItem___8__Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), IItem<TOut1\>, IPythonExecutor, string\)

Adds a Python step with 8 inputs and 1 output.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, IItem<TOut1> output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` IItem<TOut1\>

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__10_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>\), IPythonExecutor, string\)

Adds a Python step with 8 inputs and 2 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__11_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\), IPythonExecutor, string\)

Adds a Python step with 8 inputs and 3 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__12_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\), IPythonExecutor, string\)

Adds a Python step with 8 inputs and 4 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__13_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\), IPythonExecutor, string\)

Adds a Python step with 8 inputs and 5 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__14_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\), IPythonExecutor, string\)

Adds a Python step with 8 inputs and 6 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__15_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13__Flowthru_Data_IItem___14___Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\), IPythonExecutor, string\)

Adds a Python step with 8 inputs and 7 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__16_Flowthru_Flows_FlowBuilder_System_String_System_String_System_String_System_ValueTuple_Flowthru_Data_IItem___0__Flowthru_Data_IItem___1__Flowthru_Data_IItem___2__Flowthru_Data_IItem___3__Flowthru_Data_IItem___4__Flowthru_Data_IItem___5__Flowthru_Data_IItem___6__System_ValueTuple_Flowthru_Data_IItem___7____System_ValueTuple_Flowthru_Data_IItem___8__Flowthru_Data_IItem___9__Flowthru_Data_IItem___10__Flowthru_Data_IItem___11__Flowthru_Data_IItem___12__Flowthru_Data_IItem___13__Flowthru_Data_IItem___14__System_ValueTuple_Flowthru_Data_IItem___15____Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8\>\(FlowBuilder, string, string, string, \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\), \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\), IPythonExecutor, string\)

Adds a Python step with 8 inputs and 8 outputs.

```csharp
public static FlowBuilder AddPythonStep<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut1, TOut2, TOut3, TOut4, TOut5, TOut6, TOut7, TOut8>(this FlowBuilder builder, string label, string module, string function, (IItem<TIn1>, IItem<TIn2>, IItem<TIn3>, IItem<TIn4>, IItem<TIn5>, IItem<TIn6>, IItem<TIn7>, IItem<TIn8>) input, (IItem<TOut1>, IItem<TOut2>, IItem<TOut3>, IItem<TOut4>, IItem<TOut5>, IItem<TOut6>, IItem<TOut7>, IItem<TOut8>) output, IPythonExecutor executor, string description = "")
```

#### Parameters

`builder` FlowBuilder

Flow builder instance

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this step

`module` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name (e.g., "Flows.DataScience.train_model")

`function` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module

`input` \(IItem<TIn1\>, IItem<TIn2\>, IItem<TIn3\>, IItem<TIn4\>, IItem<TIn5\>, IItem<TIn6\>, IItem<TIn7\>, IItem<TIn8\>\)

Catalog item or tuple of catalog items providing input data

`output` \(IItem<TOut1\>, IItem<TOut2\>, IItem<TOut3\>, IItem<TOut4\>, IItem<TOut5\>, IItem<TOut6\>, IItem<TOut7\>, IItem<TOut8\>\)

Catalog item or tuple of catalog items to store output data

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for invoking the function

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Optional step description

#### Returns

 FlowBuilder

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

