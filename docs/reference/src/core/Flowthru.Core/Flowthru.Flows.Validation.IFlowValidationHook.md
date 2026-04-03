# <a id="Flowthru_Flows_Validation_IFlowValidationHook"></a> Interface IFlowValidationHook

Namespace: [Flowthru.Flows.Validation](Flowthru.Flows.Validation.md)  
Assembly: Flowthru.Core.dll  

Validation hook that runs during Flow pre-flight checks.

```csharp
public interface IFlowValidationHook
```

## Remarks

<p>
Validation hooks provide an extensibility point for extensions to contribute
their own validation logic during the pre-flight phase. Hooks are invoked after
DAG analysis but before external input inspection, allowing extensions to
validate their own step types.
</p>
<p>
<strong>Example use cases:</strong>
<ul><li>Python extension validates @step decorators match C# types</li><li>Custom extensions validate step-specific configuration</li><li>Third-party plugins validate external dependencies</li></ul>
</p>
<p>
<strong>Hook execution order:</strong>
</p>
<ol><li>Flow.Build() - DAG construction and layer assignment</li><li>ValidationHooks.ValidateAsync() - Extension-specific validation</li><li>Flow.ValidateExternalInputsAsync() - External input inspection</li></ol>
<p>
<strong>Error handling:</strong>
Hooks should return ValidationResult with errors, never throw exceptions.
Multiple hooks may run, and all errors are aggregated into a single result.
</p>

## Methods

### <a id="Flowthru_Flows_Validation_IFlowValidationHook_ValidateAsync_Flowthru_Flows_Flow_System_Threading_CancellationToken_"></a> ValidateAsync\(Flow, CancellationToken\)

Validates Flow steps during pre-flight checks.

```csharp
Task<ValidationResult> ValidateAsync(Flow flow, CancellationToken cancellationToken)
```

#### Parameters

`flow` [Flow](Flowthru.Flows.Flow.md)

The Flow being validated

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token for async operations

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[ValidationResult](Flowthru.Data.Validation.ValidationResult.md)\>

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

