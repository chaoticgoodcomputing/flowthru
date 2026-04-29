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

### <a id="Flowthru_Extensions_Python_Steps_PythonStepFactory_AddPythonStep__2_Flowthru_Core_Flows_FlowBuilder_System_String_System_String_System_String_Flowthru_Core_Graph_INode___0__Flowthru_Core_Graph_INode___1__Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_"></a> AddPythonStep<TInput, TOutput\>\(FlowBuilder, string, string, string, INode<TInput\>, INode<TOutput\>, IPythonExecutor, string\)

Adds a Python step with single input and single output.

```csharp
public static FlowBuilder AddPythonStep<TInput, TOutput>(this FlowBuilder builder, string label, string module, string function, INode<TInput> input, INode<TOutput> output, IPythonExecutor executor, string description = "")
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

`input` INode<TInput\>

Catalog item providing input data.

`output` INode<TOutput\>

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

