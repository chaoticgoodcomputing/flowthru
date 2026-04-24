# <a id="Flowthru_Core_Services_IFlowthruService"></a> Interface IFlowthruService

Namespace: [Flowthru.Core.Services](Flowthru.Core.Services.md)  
Assembly: Flowthru.Core.dll  

Core service for executing Flowthru flows programmatically.

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
            SliceStrategy = new FlowSliceStrategy
            {
                Flows = new HashSet&lt;string&gt; { "data_processing" }
            }
        };

        var result = await _flowthru.ExecuteFlowAsync(options);

        if (result.Success)
        {
            Console.WriteLine($"Processed {result.StepResults.Count} flow");
        }
    }
}</code></pre>
</p>

## Properties

### <a id="Flowthru_Core_Services_IFlowthruService_Catalogs"></a> Catalogs

Gets all registered catalog instances.

```csharp
IReadOnlyList<CatalogAbstract> Catalogs { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[CatalogAbstract](Flowthru.Core.Data.CatalogAbstract.md)\>

### <a id="Flowthru_Core_Services_IFlowthruService_FlowNames"></a> FlowNames

Gets the names of all registered flows.

```csharp
IReadOnlyCollection<string> FlowNames { get; }
```

#### Property Value

 [IReadOnlyCollection](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="Flowthru_Core_Services_IFlowthruService_ExecuteFlowAsync_Flowthru_Core_Flows_ExecutionOptions_System_Boolean_System_Threading_CancellationToken_"></a> ExecuteFlowAsync\(ExecutionOptions?, bool, CancellationToken\)

Executes all registered flows, optionally sliced by criteria.

```csharp
Task<FlowResult> ExecuteFlowAsync(ExecutionOptions? options = null, bool exportMetadata = true, CancellationToken cancellationToken = default)
```

#### Parameters

`options` [ExecutionOptions](Flowthru.Core.Flows.ExecutionOptions.md)?

Execution options with optional slice strategy

`exportMetadata` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether to export DAG metadata

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[FlowResult](Flowthru.Core.Flows.FlowResult.md)\>

Execution result with timing, step results, and status

#### Remarks

This method always merges all registered flows into a single DAG,
then applies optional slicing criteria from the execution options.
This enables cross-flow queries (e.g., --to-data across all flows).
To execute only specific flows, use SliceStrategy.Flows.

The method performs:
1. Flow merging into unified DAG
2. Service injection
3. DAG building and slice application
4. Metadata export (if requested)
5. External input validation
6. Flow execution (unless dry run)
7. Result formatting

### <a id="Flowthru_Core_Services_IFlowthruService_GetDagMetadata_System_String_Flowthru_Core_Graph_FlowSliceStrategy_"></a> GetDagMetadata\(string?, FlowSliceStrategy?\)

Gets the full DAG metadata for Flow introspection.

```csharp
DagMetadata GetDagMetadata(string? flowName = null, FlowSliceStrategy? sliceStrategy = null)
```

#### Parameters

`flowName` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional Flow name to inspect a single flow.
When null, all registered flows are merged into a unified DAG.

`sliceStrategy` [FlowSliceStrategy](Flowthru.Core.Graph.FlowSliceStrategy.md)?

Optional slice strategy to filter the DAG (e.g., from-node).
When provided, the returned metadata includes slice overlay information
(SlicedStepIds and SlicedItemKeys) identifying which nodes
and data are in the active execution subset.

#### Returns

 [DagMetadata](Flowthru.Core.Graph.Meta.Models.DagMetadata.md)

Full DAG metadata including steps, catalog entries, edges, schemas,
and producer-consumer relationships.

#### Remarks

This method does not execute the flow. It returns structural metadata
useful for visualization, impact analysis, data lineage, and debugging.

Examples:
<pre><code class="lang-csharp">// Inspect all Flow merged
var dag = flowthru.GetDagMetadata();

// Inspect a single flow
var dag = flowthru.GetDagMetadata("DataProcessing");

// Inspect downstream of a specific Flow node
var dag = flowthru.GetDagMetadata(sliceStrategy: new FlowSliceStrategy
{
    FromSteps = new HashSet&lt;string&gt; { "PreprocessCompanies" }
});</code></pre>

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

Thrown if <code class="paramref">flowName</code> is specified but not found.

### <a id="Flowthru_Core_Services_IFlowthruService_GetFlowMetadata_System_String_"></a> GetFlowMetadata\(string\)

Gets metadata about a Flow's structure.

```csharp
FlowMetadata GetFlowMetadata(string flowName)
```

#### Parameters

`flowName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Flow name

#### Returns

 [FlowMetadata](Flowthru.Core.Services.Models.FlowMetadata.md)

Flow metadata

#### Remarks

Returns structural information without executing the flow.
The Flow must be built for accurate layer and input information.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

Thrown if Flow name not found

### <a id="Flowthru_Core_Services_IFlowthruService_ValidateFlowAsync_System_String_System_Threading_CancellationToken_"></a> ValidateFlowAsync\(string, CancellationToken\)

Validates all external inputs (Layer 0) for a flow.

```csharp
Task<ValidationResult> ValidateFlowAsync(string flowName, CancellationToken cancellationToken = default)
```

#### Parameters

`flowName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Flow name

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Validation result

#### Remarks

Checks accessibility of external data sources without executing the flow.
Useful for pre-flight validation in CI/CD or scheduled jobs.

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

Thrown if Flow name not found

