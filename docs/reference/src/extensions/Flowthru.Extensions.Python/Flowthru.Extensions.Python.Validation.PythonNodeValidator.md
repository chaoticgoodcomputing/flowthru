# <a id="Flowthru_Extensions_Python_Validation_PythonNodeValidator"></a> Class PythonNodeValidator

Namespace: [Flowthru.Extensions.Python.Validation](Flowthru.Extensions.Python.Validation.md)  
Assembly: Flowthru.Extensions.Python.dll  

Validation hook for Python nodes.

```csharp
public class PythonNodeValidator : IPipelineValidationHook
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PythonNodeValidator](Flowthru.Extensions.Python.Validation.PythonNodeValidator.md)

#### Implements

IPipelineValidationHook

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
<strong>Phase 4 pre-flight validation:</strong>
Validates Python nodes during pipeline pre-flight to catch schema mismatches,
incorrect function signatures, and structural errors before execution.
</p>
<p>
<strong>Checks performed:</strong>
<ul><li>@node decorator schemas match C# generic type parameters</li><li>Function signature arity is correct for input count</li><li>Dry-run with 0-row data validates output structure</li></ul>
</p>
<p>
<strong>Integration:</strong>
Register this hook via Pipeline.ValidationHooks during pipeline setup.
The hook is automatically invoked during Pipeline.ValidateExternalInputsAsync().
</p>

## Constructors

### <a id="Flowthru_Extensions_Python_Validation_PythonNodeValidator__ctor_Flowthru_Extensions_Python_Execution_IPythonExecutor_Flowthru_Extensions_Python_Runtime_PythonRuntime_"></a> PythonNodeValidator\(IPythonExecutor, PythonRuntime\)

Initializes a new instance of <xref href="Flowthru.Extensions.Python.Validation.PythonNodeValidator" data-throw-if-not-resolved="false"></xref>.

```csharp
public PythonNodeValidator(IPythonExecutor executor, PythonRuntime runtime)
```

#### Parameters

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for function inspection

`runtime` [PythonRuntime](Flowthru.Extensions.Python.Runtime.PythonRuntime.md)

Python runtime for GIL management

## Methods

### <a id="Flowthru_Extensions_Python_Validation_PythonNodeValidator_ValidateAsync_Flowthru_Pipelines_Pipeline_System_Threading_CancellationToken_"></a> ValidateAsync\(Pipeline, CancellationToken\)

Validates pipeline nodes during pre-flight checks.

```csharp
public Task<ValidationResult> ValidateAsync(Pipeline pipeline, CancellationToken cancellationToken)
```

#### Parameters

`pipeline` Pipeline

The pipeline being validated

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token for async operations

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<ValidationResult\>

Validation result containing any errors found

#### Remarks

<p>
Implementations should:
<ul><li>Never throw exceptions (return errors in ValidationResult)</li><li>Be idempotent (safe to call multiple times)</li><li>Be reasonably fast (executed during pre-flight, blocks pipeline start)</li><li>Only validate nodes they understand (ignore other node types)</li></ul>
</p>
<p>
<strong>Example implementation (Python extension):</strong>
</p>
<pre><code class="lang-csharp">public async Task&lt;ValidationResult&gt; ValidateAsync(
  Pipeline pipeline,
  CancellationToken cancellationToken)
{
  var result = ValidationResult.Success();

  foreach (var node in pipeline.Nodes)
  {
    if (IsPythonNode(node))
    {
      var nodeResult = await ValidatePythonNode(node, cancellationToken);
      result.Merge(nodeResult);
    }
  }

  return result;
}</code></pre>

