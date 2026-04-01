# <a id="Flowthru_Services_IFlowthruService"></a> Interface IFlowthruService

Namespace: [Flowthru.Services](Flowthru.Services.md)  
Assembly: Flowthru.Core.dll  

Core service for executing Flowthru pipelines programmatically.

```csharp
public interface IFlowthruService
```

## Remarks

<p>
This service is DI-injectable and CLI-agnostic, enabling use in:
- Console applications (via shallow CLI wrapper)
- ASP.NET Core applications (controller/background service injection)
- Azure Functions (function injection)
- Unit tests (with mocked dependencies)
</p>
<p>
<strong>Usage Example:</strong>
<pre><code class="lang-csharp">public class DataProcessingService
{
    private readonly IFlowthruService _flowthru;

    public DataProcessingService(IFlowthruService flowthru)
    {
        _flowthru = flowthru;
    }

    public async Task ProcessData()
    {
        // Execute with optional slicing
        var options = new ExecutionOptions
        {
            DryRun = false,
            SliceStrategy = new PipelineSliceStrategy
            {
                Pipelines = new HashSet&lt;string&gt; { "data_processing" }
            }
        };

        var result = await _flowthru.ExecutePipelineAsync(options);

        if (result.Success)
        {
            Console.WriteLine($"Processed {result.NodeResults.Count} nodes");
        }
    }
}</code></pre>
</p>

## Properties

### <a id="Flowthru_Services_IFlowthruService_Catalogs"></a> Catalogs

Gets all registered catalog instances.

```csharp
IReadOnlyList<DataCatalogBase> Catalogs { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[DataCatalogBase](Flowthru.Data.DataCatalogBase.md)\>

### <a id="Flowthru_Services_IFlowthruService_PipelineNames"></a> PipelineNames

Gets the names of all registered pipelines.

```csharp
IReadOnlyCollection<string> PipelineNames { get; }
```

#### Property Value

 [IReadOnlyCollection](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="Flowthru_Services_IFlowthruService_ExecutePipelineAsync_Flowthru_Pipelines_ExecutionOptions_System_Boolean_System_String_System_Threading_CancellationToken_"></a> ExecutePipelineAsync\(ExecutionOptions?, bool, string?, CancellationToken\)

Executes all registered pipelines, optionally sliced by criteria.

```csharp
Task<PipelineResult> ExecutePipelineAsync(ExecutionOptions? options = null, bool exportMetadata = true, string? metadataOutputDirectory = null, CancellationToken cancellationToken = default)
```

#### Parameters

`options` [ExecutionOptions](Flowthru.Pipelines.ExecutionOptions.md)?

Execution options with optional slice strategy

`exportMetadata` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether to export DAG metadata

`metadataOutputDirectory` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Override for metadata output directory

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[PipelineResult](Flowthru.Pipelines.PipelineResult.md)\>

Execution result with timing, node results, and status

#### Remarks

This method always merges all registered pipelines into a single DAG,
then applies optional slicing criteria from the execution options.
This enables cross-pipeline queries (e.g., --to-data across all pipelines).
To execute only specific pipelines, use SliceStrategy.Pipelines.

The method performs:
1. Pipeline merging into unified DAG
2. Service injection
3. DAG building and slice application
4. Metadata export (if requested)
5. External input validation
6. Pipeline execution (unless dry run)
7. Result formatting

### <a id="Flowthru_Services_IFlowthruService_GetDagMetadata_System_String_Flowthru_Pipelines_PipelineSliceStrategy_"></a> GetDagMetadata\(string?, PipelineSliceStrategy?\)

Gets the full DAG metadata for pipeline introspection.

```csharp
DagMetadata GetDagMetadata(string? pipelineName = null, PipelineSliceStrategy? sliceStrategy = null)
```

#### Parameters

`pipelineName` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional pipeline name to inspect a single pipeline.
When null, all registered pipelines are merged into a unified DAG.

`sliceStrategy` [PipelineSliceStrategy](Flowthru.Pipelines.PipelineSliceStrategy.md)?

Optional slice strategy to filter the DAG (e.g., from-node, to-data).
When provided, the returned metadata includes slice overlay information
(SlicedNodeIds and SlicedCatalogEntryKeys) identifying which nodes
and data are in the active execution subset.

#### Returns

 [DagMetadata](Flowthru.Meta.Models.DagMetadata.md)

Full DAG metadata including nodes, catalog entries, edges, schemas,
and producer-consumer relationships.

#### Remarks

This method does not execute the pipeline. It returns structural metadata
useful for visualization, impact analysis, data lineage, and debugging.

Examples:
<pre><code class="lang-csharp">// Inspect all pipelines merged
var dag = flowthru.GetDagMetadata();

// Inspect a single pipeline
var dag = flowthru.GetDagMetadata("DataProcessing");

// Inspect downstream of a specific node
var dag = flowthru.GetDagMetadata(sliceStrategy: new PipelineSliceStrategy
{
    FromNodes = new HashSet&lt;string&gt; { "PreprocessCompanies" }
});</code></pre>

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

Thrown if <code class="paramref">pipelineName</code> is specified but not found.

### <a id="Flowthru_Services_IFlowthruService_GetPipelineMetadata_System_String_"></a> GetPipelineMetadata\(string\)

Gets metadata about a pipeline's structure.

```csharp
PipelineMetadata GetPipelineMetadata(string pipelineName)
```

#### Parameters

`pipelineName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Pipeline name

#### Returns

 [PipelineMetadata](Flowthru.Services.Models.PipelineMetadata.md)

Pipeline metadata

#### Remarks

Returns structural information without executing the pipeline.
The pipeline must be built for accurate layer and input information.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

Thrown if pipeline name not found

### <a id="Flowthru_Services_IFlowthruService_ValidatePipelineAsync_System_String_System_Threading_CancellationToken_"></a> ValidatePipelineAsync\(string, CancellationToken\)

Validates all external inputs (Layer 0) for a pipeline.

```csharp
Task<ValidationResult> ValidatePipelineAsync(string pipelineName, CancellationToken cancellationToken = default)
```

#### Parameters

`pipelineName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Pipeline name

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[ValidationResult](Flowthru.Data.Validation.ValidationResult.md)\>

Validation result

#### Remarks

Checks accessibility of external data sources without executing the pipeline.
Useful for pre-flight validation in CI/CD or scheduled jobs.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

Thrown if pipeline name not found

