# <a id="Flowthru_Core_Flows_Flow"></a> Class Flow

Namespace: [Flowthru.Core.Flows](Flowthru.Core.Flows.md)  
Assembly: Flowthru.Core.dll  

Represents a complete data Flow with steps, dependencies, and execution order.

```csharp
public class Flow
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Flow](Flowthru.Core.Flows.Flow.md)

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
A Flow is a directed acyclic graph (DAG) of transformation steps.
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

### <a id="Flowthru_Core_Flows_Flow_Description"></a> Description

Optional description of what this Flow does.

```csharp
public string? Description { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Core_Flows_Flow_IsBuilt"></a> IsBuilt

Indicates whether the Flow has been built (dependencies analyzed and layers assigned).

```csharp
public bool IsBuilt { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Flows_Flow_Logger"></a> Logger

Optional logger for Flow execution.

```csharp
public ILogger? Logger { get; set; }
```

#### Property Value

 [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)?

### <a id="Flowthru_Core_Flows_Flow_Name"></a> Name

Flow name for identification and logging.

```csharp
public string? Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

Set by FlowRegistry during Flow registration.

### <a id="Flowthru_Core_Flows_Flow_ServiceProvider"></a> ServiceProvider

Optional service provider for dependency injection into steps.

```csharp
public IServiceProvider? ServiceProvider { get; set; }
```

#### Property Value

 [IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider)?

#### Remarks

Set by the service layer before Flow execution to enable steps
to resolve services (e.g., database connections, external APIs).

### <a id="Flowthru_Core_Flows_Flow_Steps"></a> Steps

All steps in this flow, in the order they were added.

```csharp
public IReadOnlyList<FlowStep> Steps { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[FlowStep](Flowthru.Core.Graph.FlowStep.md)\>

#### Remarks

Exposed as public to enable validation hooks (Phase 4) to inspect steps.
The collection is read-only - steps can only be added via FlowBuilder.

### <a id="Flowthru_Core_Flows_Flow_ValidationHooks"></a> ValidationHooks

Validation hooks that run during pre-flight checks.

```csharp
public List<IFlowValidationHook> ValidationHooks { get; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[IFlowValidationHook](Flowthru.Core.Graph.Validation.IFlowValidationHook.md)\>

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

### <a id="Flowthru_Core_Flows_Flow_ValidationOptions"></a> ValidationOptions

Validation options for this flow.

```csharp
public ValidationOptions ValidationOptions { get; }
```

#### Property Value

 [ValidationOptions](Flowthru.Core.Graph.Validation.ValidationOptions.md)

#### Remarks

Configures how external data sources (Layer 0 inputs) are validated
before Flow execution begins.

## Methods

### <a id="Flowthru_Core_Flows_Flow_Build_Flowthru_Core_Graph_FlowSliceStrategy_"></a> Build\(FlowSliceStrategy?\)

Builds the Flow by analyzing dependencies and assigning execution layers.
Must be called before executing the flow.

```csharp
public void Build(FlowSliceStrategy? sliceStrategy = null)
```

#### Parameters

`sliceStrategy` [FlowSliceStrategy](Flowthru.Core.Graph.FlowSliceStrategy.md)?

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

### <a id="Flowthru_Core_Flows_Flow_ExecuteAsync_System_Threading_CancellationToken_"></a> ExecuteAsync\(CancellationToken\)

Executes the flow in topological order, throwing on the first step failure.

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

For structured result-based execution (including parallel), use <xref href="Flowthru.Core.Flows.Flow.RunAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref>.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if the flow has not been built

### <a id="Flowthru_Core_Flows_Flow_ExportDag"></a> ExportDag\(\)

Exports DAG metadata for this Flow.

```csharp
public DagMetadata ExportDag()
```

#### Returns

 [DagMetadata](Flowthru.Core.Graph.Meta.Models.DagMetadata.md)

Complete DAG metadata including steps, catalog entries, and edges

#### Remarks

<p>
This method extracts structural metadata from the built Flow , creating
a complete representation of the DAG (Directed Acyclic Graph) that can be
serialized to JSON for visualization in Flowthru.Core.Viz.
</p>
<p>
<strong>Prerequisites:</strong> Flow must be built before calling this method.
Call Build() first if IsBuilt is false.
</p>
<p>
<strong>Usage:</strong>
</p>
<pre><code class="lang-csharp">var Flow = DataProcessingFlow.Create(catalog);
flow.Build();

var dag = flow.ExportDag();
var json = dag.ToJson();
File.WriteAllText("dag.json", json);</code></pre>
<p>
This method is non-destructive and idempotent - it can be called multiple
times without affecting the Flow state.
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if Flow has not been built

### <a id="Flowthru_Core_Flows_Flow_Merge_System_Collections_Generic_Dictionary_System_String_Flowthru_Core_Flows_Flow__"></a> Merge\(Dictionary<string, Flow\>\)

Merges multiple flows into a single Flow by combining all their steps.

```csharp
public static Flow Merge(Dictionary<string, Flow> flows)
```

#### Parameters

`flows` [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [Flow](Flowthru.Core.Flows.Flow.md)\>

Dictionary of flow names to Flow instances

#### Returns

 [Flow](Flowthru.Core.Flows.Flow.md)

A new Flow containing all steps from all input flows

#### Remarks

<p>
This method creates a new Flow by combining all steps from the input flows.
Step names are prefixed with their source Flow name (e.g., "data_processing.PreprocessCompanies")
to ensure uniqueness and maintain traceability in logs.
</p>
<p>
The existing DependencyAnalyzer will automatically resolve cross-flow dependencies
based on catalog entries. The single producer rule is enforced - if multiple flows
attempt to write to the same catalog entry, Build() will throw an InvalidOperationException.
</p>

### <a id="Flowthru_Core_Flows_Flow_RunAsync_System_Threading_CancellationToken_"></a> RunAsync\(CancellationToken\)

Builds and executes the flow, returning comprehensive execution results.

```csharp
public Task<FlowResult> RunAsync(CancellationToken cancellationToken)
```

#### Parameters

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token to signal graceful shutdown

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[FlowResult](Flowthru.Core.Flows.FlowResult.md)\>

FlowResult containing execution status, timing, and step results

#### Remarks

This is the primary high-level API for executing flows. It automatically
calls Build() if the Flow hasn't been built yet, then executes via the
task-graph scheduler with default options (sequential, stop on first error).

### <a id="Flowthru_Core_Flows_Flow_RunAsync_Flowthru_Core_Flows_ExecutionOptions_System_Threading_CancellationToken_"></a> RunAsync\(ExecutionOptions, CancellationToken\)

Builds and executes the flow with the supplied execution options.

```csharp
public Task<FlowResult> RunAsync(ExecutionOptions options, CancellationToken cancellationToken)
```

#### Parameters

`options` [ExecutionOptions](Flowthru.Core.Flows.ExecutionOptions.md)

Controls parallelism, error policy, and other execution behaviour.

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token to signal graceful shutdown

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[FlowResult](Flowthru.Core.Flows.FlowResult.md)\>

FlowResult containing execution status, timing, and step results

#### Remarks

<p>
Steps are dispatched by the task-graph scheduler as soon as all their dependencies
complete, up to <xref href="Flowthru.Core.Flows.ExecutionOptions.MaxDegreeOfParallelism" data-throw-if-not-resolved="false"></xref> concurrent steps.
</p>
<p>
With <code>MaxDegreeOfParallelism = 1</code> (default) execution is sequential and
behaviourally equivalent to the previous layer-by-layer loop.
</p>

### <a id="Flowthru_Core_Flows_Flow_ValidateExternalInputsAsync_System_Int32_System_Threading_CancellationToken_"></a> ValidateExternalInputsAsync\(int, CancellationToken\)

Validates all external inputs and write destinations before Flow execution.

```csharp
public Task<ValidationResult> ValidateExternalInputsAsync(int maxDegreeOfParallelism = 1, CancellationToken cancellationToken = default)
```

#### Parameters

`maxDegreeOfParallelism` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Maximum number of external inputs inspected concurrently. Defaults to 1 (sequential).
Pass the resolved <code>ExecutionOptions.MaxDegreeOfParallelism</code> to fan out I/O-bound
inspections in parallel.

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token for async operations.

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

ValidationResult containing any errors found

#### Remarks

<p>
This method runs two validation passes:
</p>
<ol><li>
  <strong>Source validation:</strong> Inspects catalog entries consumed but not produced
  by any step in the execution set. These are pre-existing external data sources
  (files, databases, APIs) that must exist and be valid before the flow can execute.
</li><li>
  <strong>Target validation:</strong> Calls <code>InspectTarget()</code> on all catalog entries
  that steps will write to. This validates write destinations (directories, database tables,
  API endpoints) are accessible before any step executes. Skipped for entries where
  <code>Traits.CanInspect = false</code> or explicitly disabled via
  <code>ValidationOptions.SkipTargetInspection()</code>.
</li></ol>
<p>
<strong>Slicing Support:</strong> In sliced flows, catalog entries that were
produced by steps outside the slice are correctly identified as external inputs
and validated. This prevents runtime failures from missing intermediate data.
</p>
<p>
<strong>Inspection Levels (source validation):</strong>
</p>
<ul><li><strong>None:</strong> Skip inspection entirely</li><li><strong>Shallow:</strong> Validate file exists, check headers/schema, deserialize sample rows</li><li><strong>Deep:</strong> Validate all rows in the dataset (expensive!)</li></ul>
<p>
<strong>Default Behavior:</strong>
</p>
<ul><li>If explicitly configured via WithValidation() → use that level</li><li>If entry has PreferredInspectionLevel set → use that level</li><li>Otherwise → Shallow (all storage adapters support inspection)</li></ul>
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

Thrown if Flow has not been built

