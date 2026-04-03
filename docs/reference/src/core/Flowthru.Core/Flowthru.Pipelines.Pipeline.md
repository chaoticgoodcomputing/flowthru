# <a id="Flowthru_Pipelines_Pipeline"></a> Class Pipeline

Namespace: [Flowthru.Flows](Flowthru.Flows.md)  
Assembly: Flowthru.Core.dll  

Represents a complete data pipeline with nodes, dependencies, and execution order.

```csharp
public class Pipeline
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Pipeline](Flowthru.Flows.Pipeline.md)

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
A pipeline is a directed acyclic graph (DAG) of transformation nodes.
Each node reads data from catalog entries, performs transformations,
and writes results back to catalog entries.
</p>
<p>
<strong>Execution Model:</strong>
</p>
<ul><li>Nodes are organized into layers via topological sort</li><li>Nodes in layer 0 have no dependencies (read external data only)</li><li>Nodes in layer N depend only on nodes in layers 0..N-1</li><li>Sequential execution: Execute all nodes in layer order</li><li>Parallel execution (Phase 2): Execute nodes within same layer concurrently</li></ul>
<p>
<strong>Single Producer Rule:</strong> Each catalog entry can be written by at most
one node. This ensures deterministic execution order and prevents race conditions.
</p>

## Properties

### <a id="Flowthru_Pipelines_Pipeline_Description"></a> Description

Optional description of what this pipeline does.

```csharp
public string? Description { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Pipelines_Pipeline_IsBuilt"></a> IsBuilt

Indicates whether the pipeline has been built (dependencies analyzed and layers assigned).

```csharp
public bool IsBuilt { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Pipelines_Pipeline_Logger"></a> Logger

Optional logger for pipeline execution.

```csharp
public ILogger? Logger { get; set; }
```

#### Property Value

 [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)?

### <a id="Flowthru_Pipelines_Pipeline_Name"></a> Name

Pipeline name for identification and logging.

```csharp
public string? Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

Set by PipelineRegistry during pipeline registration.

### <a id="Flowthru_Pipelines_Pipeline_Nodes"></a> Nodes

All nodes in this pipeline, in the order they were added.

```csharp
public IReadOnlyList<PipelineNode> Nodes { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PipelineNode](Flowthru.Flows.PipelineNode.md)\>

#### Remarks

Exposed as public to enable validation hooks (Phase 4) to inspect nodes.
The collection is read-only - nodes can only be added via FlowBuilder.

### <a id="Flowthru_Pipelines_Pipeline_ServiceProvider"></a> ServiceProvider

Optional service provider for dependency injection into nodes.

```csharp
public IServiceProvider? ServiceProvider { get; set; }
```

#### Property Value

 [IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider)?

#### Remarks

Set by the service layer before pipeline execution to enable nodes
to resolve services (e.g., database connections, external APIs).

### <a id="Flowthru_Pipelines_Pipeline_ValidationHooks"></a> ValidationHooks

Validation hooks that run during pre-flight checks.

```csharp
public List<IPipelineValidationHook> ValidationHooks { get; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[IPipelineValidationHook](Flowthru.Flows.Validation.IPipelineValidationHook.md)\>

#### Remarks

<p>
Extensions can register hooks to validate their own node types during pre-flight.
Hooks are invoked after DAG analysis but before external input inspection.
</p>
<p>
<strong>Hook execution order:</strong>
</p>
<ol><li>Pipeline.Build() - DAG construction and layer assignment</li><li>ValidationHooks.ValidateAsync() - Extension-specific validation</li><li>Pipeline.ValidateExternalInputsAsync() - External input inspection</li></ol>
<p>
<strong>Example (Python extension):</strong>
</p>
<pre><code class="lang-csharp">pipeline.ValidationHooks.Add(new PythonNodeValidator(executor, runtime));</code></pre>

### <a id="Flowthru_Pipelines_Pipeline_ValidationOptions"></a> ValidationOptions

Validation options for this pipeline.

```csharp
public ValidationOptions ValidationOptions { get; }
```

#### Property Value

 [ValidationOptions](Flowthru.Flows.Validation.ValidationOptions.md)

#### Remarks

Configures how external data sources (Layer 0 inputs) are validated
before pipeline execution begins.

## Methods

### <a id="Flowthru_Pipelines_Pipeline_Build_Flowthru_Pipelines_FlowSliceStrategy_"></a> Build\(FlowSliceStrategy?\)

Builds the pipeline by analyzing dependencies and assigning execution layers.
Must be called before executing the pipeline.

```csharp
public void Build(FlowSliceStrategy? sliceStrategy = null)
```

#### Parameters

`sliceStrategy` [FlowSliceStrategy](Flowthru.Flows.FlowSliceStrategy.md)?

Optional slicing strategy to filter nodes before execution

#### Remarks

<p>
<strong>Slicing:</strong> If a slicing strategy is provided, only nodes matching
the strategy will be included in the execution. The slice always forms a valid
sub-DAG with all required dependencies.
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if:
- Multiple nodes write to the same catalog entry (single producer rule)
- Circular dependency is detected
- Slice strategy references non-existent nodes or catalog entries

### <a id="Flowthru_Pipelines_Pipeline_ExecuteAsync_System_Threading_CancellationToken_"></a> ExecuteAsync\(CancellationToken\)

Executes the pipeline sequentially, layer by layer.

```csharp
public Task ExecuteAsync(CancellationToken cancellationToken)
```

#### Parameters

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token to signal graceful shutdown

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

Task representing the pipeline execution

#### Remarks

<p>
This method executes nodes in topological order:
1. Execute all nodes in layer 0 sequentially
2. Execute all nodes in layer 1 sequentially
3. Continue until all layers are complete
</p>
<p>
<strong>Note:</strong> This method throws exceptions on failure. For result-based
execution with error handling, use RunAsync() instead.
</p>
<p>
In Phase 2, this will be replaced with a parallel executor that can run
nodes within the same layer concurrently.
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if pipeline has not been built

### <a id="Flowthru_Pipelines_Pipeline_ExportDag"></a> ExportDag\(\)

Exports DAG metadata for this pipeline.

```csharp
public DagMetadata ExportDag()
```

#### Returns

 [DagMetadata](Flowthru.Meta.Models.DagMetadata.md)

Complete DAG metadata including nodes, catalog entries, and edges

#### Remarks

<p>
This method extracts structural metadata from the built pipeline, creating
a complete representation of the DAG (Directed Acyclic Graph) that can be
serialized to JSON for visualization in Flowthru.Viz.
</p>
<p>
<strong>Prerequisites:</strong> Pipeline must be built before calling this method.
Call Build() first if IsBuilt is false.
</p>
<p>
<strong>Usage:</strong>
</p>
<pre><code class="lang-csharp">var pipeline = DataProcessingPipeline.Create(catalog);
pipeline.Build();

var dag = pipeline.ExportDag();
var json = dag.ToJson();
File.WriteAllText("dag.json", json);</code></pre>
<p>
This method is non-destructive and idempotent - it can be called multiple
times without affecting the pipeline state.
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if pipeline has not been built

### <a id="Flowthru_Pipelines_Pipeline_Merge_System_Collections_Generic_Dictionary_System_String_Flowthru_Pipelines_Pipeline__"></a> Merge\(Dictionary<string, Pipeline\>\)

Merges multiple pipelines into a single pipeline by combining all their nodes.

```csharp
public static Pipeline Merge(Dictionary<string, Pipeline> pipelines)
```

#### Parameters

`pipelines` [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [Pipeline](Flowthru.Flows.Pipeline.md)\>

Dictionary of pipeline names to pipeline instances

#### Returns

 [Pipeline](Flowthru.Flows.Pipeline.md)

A new pipeline containing all nodes from all input pipelines

#### Remarks

<p>
This method creates a new pipeline by combining all nodes from the input pipelines.
Node names are prefixed with their source pipeline name (e.g., "data_processing.PreprocessCompanies")
to ensure uniqueness and maintain traceability in logs.
</p>
<p>
The existing DependencyAnalyzer will automatically resolve cross-pipeline dependencies
based on catalog entries. The single producer rule is enforced - if multiple pipelines
attempt to write to the same catalog entry, Build() will throw an InvalidOperationException.
</p>

### <a id="Flowthru_Pipelines_Pipeline_RunAsync_System_Threading_CancellationToken_"></a> RunAsync\(CancellationToken\)

/// Builds and executes the pipeline, returning comprehensive execution results.

```csharp
public Task<FlowResult> RunAsync(CancellationToken cancellationToken)
```

#### Parameters

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token to signal graceful shutdown

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[FlowResult](Flowthru.Flows.FlowResult.md)\>

FlowResult containing execution status, timing, and node results

#### Remarks

<p>
This is the primary high-level API for executing pipelines. It automatically
calls Build() if the pipeline hasn't been built yet, then executes and tracks results.
</p>

### <a id="Flowthru_Pipelines_Pipeline_ValidateExternalInputsAsync_System_Threading_CancellationToken_"></a> ValidateExternalInputsAsync\(CancellationToken\)

Validates all external inputs before pipeline execution.

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
This method inspects catalog entries that are consumed by the pipeline but not
produced by any node in the execution set. These are pre-existing external data
sources (files, databases, APIs) that must exist and be valid before the pipeline
can execute.
</p>
<p>
<strong>Slicing Support:</strong> In sliced pipelines, catalog entries that were
produced by nodes outside the slice are correctly identified as external inputs
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
<strong>Important:</strong> Only external inputs are inspected. Intermediate pipeline
outputs produced within the execution set are never inspected, as they don't exist yet.
</p>
<p>
<strong>Usage:</strong>
</p>
<pre><code class="lang-csharp">pipeline.Build();
var validationResult = await pipeline.ValidateExternalInputsAsync();
if (!validationResult.IsValid) {
  // Handle validation errors before execution
  validationResult.ThrowIfInvalid();
}
await pipeline.RunAsync();</code></pre>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if pipeline has not been built

