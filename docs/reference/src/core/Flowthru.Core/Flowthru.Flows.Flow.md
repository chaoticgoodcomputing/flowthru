# <a id="Flowthru_Flows_Flow"></a> Class Flow

Namespace: [Flowthru.Flows](Flowthru.Flows.md)  
Assembly: Flowthru.Core.dll  

Represents a complete data flow with steps, dependencies, and execution order.

```csharp
public class Flow
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Flow](Flowthru.Flows.Flow.md)

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
A flow is a directed acyclic graph (DAG) of transformation steps.
Each step reads data from catalog entries, performs transformations,
and writes results back to catalog entries.
</p>
<p>
<strong>Execution Model:</strong>
</p>
<ul><li>Steps are organized into layers via topological sort</li><li>Steps in layer 0 have no dependencies (read external data only)</li><li>Steps in layer N depend only on steps in layers 0..N-1</li><li>Sequential execution: Execute all steps in layer order</li><li>Parallel execution (Phase 2): Execute steps within same layer concurrently</li></ul>
<p>
<strong>Single Producer Rule:</strong> Each catalog entry can be written by at most
one step. This ensures deterministic execution order and prevents race conditions.
</p>

## Properties

### <a id="Flowthru_Flows_Flow_Description"></a> Description

Optional description of what this flow does.

```csharp
public string? Description { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Flows_Flow_IsBuilt"></a> IsBuilt

Indicates whether the flow has been built (dependencies analyzed and layers assigned).

```csharp
public bool IsBuilt { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Flows_Flow_Logger"></a> Logger

Optional logger for flow execution.

```csharp
public ILogger? Logger { get; set; }
```

#### Property Value

 [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)?

### <a id="Flowthru_Flows_Flow_Name"></a> Name

Flow name for identification and logging.

```csharp
public string? Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

Set by FlowRegistry during flow registration.

### <a id="Flowthru_Flows_Flow_ServiceProvider"></a> ServiceProvider

Optional service provider for dependency injection into steps.

```csharp
public IServiceProvider? ServiceProvider { get; set; }
```

#### Property Value

 [IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider)?

#### Remarks

Set by the service layer before flow execution to enable steps
to resolve services (e.g., database connections, external APIs).

### <a id="Flowthru_Flows_Flow_Steps"></a> Steps

All steps in this flow, in the order they were added.

```csharp
public IReadOnlyList<FlowStep> Steps { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[FlowStep](Flowthru.Flows.FlowStep.md)\>

#### Remarks

Exposed as public to enable validation hooks (Phase 4) to inspect steps.
The collection is read-only - steps can only be added via FlowBuilder.

### <a id="Flowthru_Flows_Flow_ValidationHooks"></a> ValidationHooks

Validation hooks that run during pre-flight checks.

```csharp
public List<IFlowValidationHook> ValidationHooks { get; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[IFlowValidationHook](Flowthru.Flows.Validation.IFlowValidationHook.md)\>

#### Remarks

<p>
Extensions can register hooks to validate their own step types during pre-flight.
Hooks are invoked after DAG analysis but before external input inspection.
</p>
<p>
<strong>Hook execution order:</strong>
</p>
<ol><li>Flow.Build() - DAG construction and layer assignment</li><li>ValidationHooks.ValidateAsync() - Extension-specific validation</li><li>Flow.ValidateExternalInputsAsync() - External input inspection</li></ol>
<p>
<strong>Example (Python extension):</strong>
</p>
<pre><code class="lang-csharp">flow.ValidationHooks.Add(new PythonStepValidator(executor, runtime));</code></pre>

### <a id="Flowthru_Flows_Flow_ValidationOptions"></a> ValidationOptions

Validation options for this flow.

```csharp
public ValidationOptions ValidationOptions { get; }
```

#### Property Value

 [ValidationOptions](Flowthru.Flows.Validation.ValidationOptions.md)

#### Remarks

Configures how external data sources (Layer 0 inputs) are validated
before flow execution begins.

## Methods

### <a id="Flowthru_Flows_Flow_Build_Flowthru_Flows_FlowSliceStrategy_"></a> Build\(FlowSliceStrategy?\)

Builds the flow by analyzing dependencies and assigning execution layers.
Must be called before executing the flow.

```csharp
public void Build(FlowSliceStrategy? sliceStrategy = null)
```

#### Parameters

`sliceStrategy` [FlowSliceStrategy](Flowthru.Flows.FlowSliceStrategy.md)?

Optional slicing strategy to filter steps before execution

#### Remarks

<p>
<strong>Slicing:</strong> If a slicing strategy is provided, only steps matching
the strategy will be included in the execution. The slice always forms a valid
sub-DAG with all required dependencies.
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if:
- Multiple steps write to the same catalog entry (single producer rule)
- Circular dependency is detected
- Slice strategy references non-existent steps or catalog entries

### <a id="Flowthru_Flows_Flow_ExecuteAsync_System_Threading_CancellationToken_"></a> ExecuteAsync\(CancellationToken\)

Executes the flow sequentially, layer by layer.

```csharp
public Task ExecuteAsync(CancellationToken cancellationToken)
```

#### Parameters

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token to signal graceful shutdown

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

Task representing the flow execution

#### Remarks

<p>
This method executes flow in topological order:
1. Execute all flow in layer 0 sequentially
2. Execute all flow in layer 1 sequentially
3. Continue until all layers are complete
</p>
<p>
<strong>Note:</strong> This method throws exceptions on failure. For result-based
execution with error handling, use RunAsync() instead.
</p>
<p>
In Phase 2, this will be replaced with a parallel executor that can run
steps within the same layer concurrently.
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if flow has not been built

### <a id="Flowthru_Flows_Flow_ExportDag"></a> ExportDag\(\)

Exports DAG metadata for this Flow.

```csharp
public DagMetadata ExportDag()
```

#### Returns

 [DagMetadata](Flowthru.Meta.Models.DagMetadata.md)

Complete DAG metadata including steps, catalog entries, and edges

#### Remarks

<p>
This method extracts structural metadata from the built flow , creating
a complete representation of the DAG (Directed Acyclic Graph) that can be
serialized to JSON for visualization in Flowthru.Viz.
</p>
<p>
<strong>Prerequisites:</strong> Flow must be built before calling this method.
Call Build() first if IsBuilt is false.
</p>
<p>
<strong>Usage:</strong>
</p>
<pre><code class="lang-csharp">var flow = DataProcessingFlow.Create(catalog);
flow.Build();

var dag = flow.ExportDag();
var json = dag.ToJson();
File.WriteAllText("dag.json", json);</code></pre>
<p>
This method is non-destructive and idempotent - it can be called multiple
times without affecting the flow state.
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if flow has not been built

### <a id="Flowthru_Flows_Flow_Merge_System_Collections_Generic_Dictionary_System_String_Flowthru_Flows_Flow__"></a> Merge\(Dictionary<string, Flow\>\)

Merges multiple flows into a single flow by combining all their steps.

```csharp
public static Flow Merge(Dictionary<string, Flow> flows)
```

#### Parameters

`flows` [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [Flow](Flowthru.Flows.Flow.md)\>

Dictionary of flow names to flow instances

#### Returns

 [Flow](Flowthru.Flows.Flow.md)

A new flow containing all steps from all input flows

#### Remarks

<p>
This method creates a new flow by combining all steps from the input flows.
Step names are prefixed with their source flow name (e.g., "data_processing.PreprocessCompanies")
to ensure uniqueness and maintain traceability in logs.
</p>
<p>
The existing DependencyAnalyzer will automatically resolve cross-flow dependencies
based on catalog entries. The single producer rule is enforced - if multiple flows
attempt to write to the same catalog entry, Build() will throw an InvalidOperationException.
</p>

### <a id="Flowthru_Flows_Flow_RunAsync_System_Threading_CancellationToken_"></a> RunAsync\(CancellationToken\)

/// Builds and executes the flow, returning comprehensive execution results.

```csharp
public Task<FlowResult> RunAsync(CancellationToken cancellationToken)
```

#### Parameters

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token to signal graceful shutdown

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[FlowResult](Flowthru.Flows.FlowResult.md)\>

FlowResult containing execution status, timing, and flow results

#### Remarks

<p>
This is the primary high-level API for executing flows. It automatically
calls Build() if the flow hasn't been built yet, then executes and tracks results.
</p>

### <a id="Flowthru_Flows_Flow_ValidateExternalInputsAsync_System_Threading_CancellationToken_"></a> ValidateExternalInputsAsync\(CancellationToken\)

Validates all external inputs before flow execution.

```csharp
public Task<ValidationResult> ValidateExternalInputsAsync(CancellationToken cancellationToken = default)
```

#### Parameters

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token for validation I/O operations

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[ValidationResult](Flowthru.Data.Validation.ValidationResult.md)\>

ValidationResult containing any errors found

#### Remarks

<p>
This method inspects catalog entries that are consumed by the flow but not
produced by any step in the execution set. These are pre-existing external data
sources (files, databases, APIs) that must exist and be valid before the flow
can execute.
</p>
<p>
<strong>Slicing Support:</strong> In sliced flows, catalog entries that were
produced by steps outside the slice are correctly identified as external inputs
and validated. This prevents runtime failures from missing intermediate data.
</p>
<p>
<strong>Inspection Levels:</strong>
</p>
<ul><li><strong>None:</strong> Skip inspection entirely</li><li><strong>Shallow:</strong> Validate file exists, check headers/schema, deserialize sample rows</li><li><strong>Deep:</strong> Validate all rows in the dataset (expensive!)</li></ul>
<p>
<strong>Default Behavior:</strong>
</p>
<ul><li>If explicitly configured via WithValidation() → use that level</li><li>If entry has PreferredInspectionLevel set → use that level</li><li>Otherwise → Shallow (all storage adapters support inspection)</li></ul>
<p>
<strong>Important:</strong> Only external inputs are inspected. Intermediate flow
outputs produced within the execution set are never inspected, as they don't exist yet.
</p>
<p>
<strong>Usage:</strong>
</p>
<pre><code class="lang-csharp">flow.Build();
var validationResult = await flow.ValidateExternalInputsAsync();
if (!validationResult.IsValid) {
  // Handle validation errors before execution
  validationResult.ThrowIfInvalid();
}
await flow.RunAsync();</code></pre>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if flow has not been built

