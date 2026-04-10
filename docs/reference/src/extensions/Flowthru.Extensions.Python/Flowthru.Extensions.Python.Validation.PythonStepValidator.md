# <a id="Flowthru_Extensions_Python_Validation_PythonStepValidator"></a> Class PythonStepValidator

Namespace: [Flowthru.Extensions.Python.Validation](Flowthru.Extensions.Python.Validation.md)  
Assembly: Flowthru.Extensions.Python.dll  

Validation hook for Python steps.

```csharp
public class PythonStepValidator : IFlowValidationHook
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PythonStepValidator](Flowthru.Extensions.Python.Validation.PythonStepValidator.md)

#### Implements

IFlowValidationHook

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
Validates Python steps during Flow pre-flight to catch schema mismatches,
incorrect function signatures, and structural errors before execution.
</p>
<p>
<strong>Checks performed:</strong>
<ul><li>@step decorator schemas match C# generic type parameters</li><li>Function signature arity is correct for input count</li><li>Dry-run with 0-row data validates output structure</li></ul>
</p>
<p>
<strong>Integration:</strong>
Register this hook via Flow.ValidationHooks during Flow setup.
The hook is automatically invoked during Flow.ValidateExternalInputsAsync().
</p>

## Constructors

### <a id="Flowthru_Extensions_Python_Validation_PythonStepValidator__ctor_Flowthru_Extensions_Python_Execution_IPythonExecutor_Flowthru_Extensions_Python_Runtime_PythonRuntime_"></a> PythonStepValidator\(IPythonExecutor, PythonRuntime\)

Initializes a new instance of <xref href="Flowthru.Extensions.Python.Validation.PythonStepValidator" data-throw-if-not-resolved="false"></xref>.

```csharp
public PythonStepValidator(IPythonExecutor executor, PythonRuntime runtime)
```

#### Parameters

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

Python executor for function inspection

`runtime` [PythonRuntime](Flowthru.Extensions.Python.Runtime.PythonRuntime.md)

Python runtime for GIL management

## Methods

### <a id="Flowthru_Extensions_Python_Validation_PythonStepValidator_ValidateAsync_Flowthru_Core_Flows_Flow_System_Threading_CancellationToken_"></a> ValidateAsync\(Flow, CancellationToken\)

Validates Flow steps during pre-flight checks.

```csharp
public Task<ValidationResult> ValidateAsync(Flow flow, CancellationToken cancellationToken)
```

#### Parameters

`flow` Flow

The Flow being validated

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token for async operations

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<ValidationResult\>

Validation result containing any errors found

#### Remarks

<p>
Implementations should:
<ul><li>Never throw exceptions (return errors in ValidationResult)</li><li>Be idempotent (safe to call multiple times)</li><li>Be reasonably fast (executed during pre-flight, blocks Flow start)</li><li>Only validate steps they understand (ignore other step types)</li></ul>
</p>
<p>
<strong>Example implementation (Python extension):</strong>
</p>
<pre><code class="lang-csharp">public async Task&lt;ValidationResult&gt; ValidateAsync(
  Flow flow,
  CancellationToken cancellationToken)
{
  var result = ValidationResult.Success();

  foreach (var step in flow.Steps)
  {
    if (IsPythonStep(step))
    {
      var stepResult = await ValidatePythonStep(step, cancellationToken);
      result.Merge(stepResult);
    }
  }

  return result;
}</code></pre>

