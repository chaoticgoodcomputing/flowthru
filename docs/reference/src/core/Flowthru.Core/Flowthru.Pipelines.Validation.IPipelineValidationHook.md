# <a id="Flowthru_Pipelines_Validation_IPipelineValidationHook"></a> Interface IPipelineValidationHook

Namespace: [Flowthru.Flows.Validation](Flowthru.Flows.Validation.md)  
Assembly: Flowthru.Core.dll  

Validation hook that runs during pipeline pre-flight checks.

```csharp
public interface IPipelineValidationHook
```

## Remarks

<p>
Validation hooks provide an extensibility point for extensions to contribute
their own validation logic during the pre-flight phase. Hooks are invoked after
DAG analysis but before external input inspection, allowing extensions to
validate their own node types.
</p>
<p>
<strong>Example use cases:</strong>
<ul><li>Python extension validates @node decorators match C# types</li><li>Custom extensions validate node-specific configuration</li><li>Third-party plugins validate external dependencies</li></ul>
</p>
<p>
<strong>Hook execution order:</strong>
</p>
<ol><li>Pipeline.Build() - DAG construction and layer assignment</li><li>ValidationHooks.ValidateAsync() - Extension-specific validation</li><li>Pipeline.ValidateExternalInputsAsync() - External input inspection</li></ol>
<p>
<strong>Error handling:</strong>
Hooks should return ValidationResult with errors, never throw exceptions.
Multiple hooks may run, and all errors are aggregated into a single result.
</p>

## Methods

### <a id="Flowthru_Pipelines_Validation_IPipelineValidationHook_ValidateAsync_Flowthru_Pipelines_Pipeline_System_Threading_CancellationToken_"></a> ValidateAsync\(Pipeline, CancellationToken\)

Validates pipeline nodes during pre-flight checks.

```csharp
Task<ValidationResult> ValidateAsync(Pipeline pipeline, CancellationToken cancellationToken)
```

#### Parameters

`pipeline` [Pipeline](Flowthru.Flows.Pipeline.md)

The pipeline being validated

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token for async operations

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[ValidationResult](Flowthru.Data.Validation.ValidationResult.md)\>

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

