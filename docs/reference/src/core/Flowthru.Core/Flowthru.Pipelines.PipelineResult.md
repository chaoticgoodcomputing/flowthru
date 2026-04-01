# <a id="Flowthru_Pipelines_PipelineResult"></a> Class PipelineResult

Namespace: [Flowthru.Pipelines](Flowthru.Pipelines.md)  
Assembly: Flowthru.Core.dll  

Represents the result of a pipeline execution.

```csharp
public class PipelineResult
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PipelineResult](Flowthru.Pipelines.PipelineResult.md)

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
This class provides comprehensive execution information including success status,
timing, individual node results, and error details.
</p>
<p>
<strong>Usage Pattern:</strong>
</p>
<pre><code class="lang-csharp">var result = await pipeline.RunAsync();

if (result.Success)
{
    Console.WriteLine($"Pipeline completed in {result.ExecutionTime.TotalSeconds:F2}s");
    foreach (var nodeResult in result.NodeResults.Values)
    {
        Console.WriteLine($"  {nodeResult.NodeName}: {nodeResult.ExecutionTime.TotalSeconds:F2}s");
    }
}
else
{
    Console.WriteLine($"Pipeline failed: {result.Exception?.Message}");
}</code></pre>

## Properties

### <a id="Flowthru_Pipelines_PipelineResult_Exception"></a> Exception

Exception that caused pipeline failure, if any.

```csharp
public Exception? Exception { get; init; }
```

#### Property Value

 [Exception](https://learn.microsoft.com/dotnet/api/system.exception)?

#### Remarks

Null if Success is true. Contains the first exception that caused
pipeline execution to halt if Success is false.

### <a id="Flowthru_Pipelines_PipelineResult_ExecutionTime"></a> ExecutionTime

Total execution time for the entire pipeline.

```csharp
public TimeSpan ExecutionTime { get; init; }
```

#### Property Value

 [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

### <a id="Flowthru_Pipelines_PipelineResult_IsDryRun"></a> IsDryRun

Indicates whether this was a dry run (pre-flight checks only).

```csharp
public bool IsDryRun { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Pipelines_PipelineResult_NodeResults"></a> NodeResults

Results for individual nodes, keyed by node name.

```csharp
public Dictionary<string, NodeResult> NodeResults { get; init; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [NodeResult](Flowthru.Pipelines.NodeResult.md)\>

#### Remarks

Dictionary keys are the node names as specified in the pipeline definition.
Values contain execution details for each node.

### <a id="Flowthru_Pipelines_PipelineResult_PipelineName"></a> PipelineName

The name of the pipeline that was executed.

```csharp
public string? PipelineName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Pipelines_PipelineResult_Success"></a> Success

Indicates whether the pipeline executed successfully.

```csharp
public bool Success { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Pipelines_PipelineResult_CreateDryRunSuccess_System_TimeSpan_System_Int32_System_Int32_System_Int32_System_String_"></a> CreateDryRunSuccess\(TimeSpan, int, int, int, string?\)

Creates a successful dry run result.

```csharp
public static PipelineResult CreateDryRunSuccess(TimeSpan preFlightDuration, int nodeCount, int layerCount, int validatedInputCount, string? pipelineName = null)
```

#### Parameters

`preFlightDuration` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

Time spent on pre-flight checks

`nodeCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Total number of nodes in the pipeline

`layerCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of execution layers

`validatedInputCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of external inputs validated

`pipelineName` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Name of the pipeline

#### Returns

 [PipelineResult](Flowthru.Pipelines.PipelineResult.md)

A successful dry run result

### <a id="Flowthru_Pipelines_PipelineResult_CreateFailure_System_TimeSpan_System_Exception_System_Collections_Generic_Dictionary_System_String_Flowthru_Pipelines_NodeResult__System_String_"></a> CreateFailure\(TimeSpan, Exception, Dictionary<string, NodeResult\>?, string?\)

Creates a failed pipeline result.

```csharp
public static PipelineResult CreateFailure(TimeSpan executionTime, Exception exception, Dictionary<string, NodeResult>? nodeResults = null, string? pipelineName = null)
```

#### Parameters

`executionTime` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

`exception` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)

`nodeResults` [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [NodeResult](Flowthru.Pipelines.NodeResult.md)\>?

`pipelineName` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [PipelineResult](Flowthru.Pipelines.PipelineResult.md)

### <a id="Flowthru_Pipelines_PipelineResult_CreateSuccess_System_TimeSpan_System_Collections_Generic_Dictionary_System_String_Flowthru_Pipelines_NodeResult__System_String_"></a> CreateSuccess\(TimeSpan, Dictionary<string, NodeResult\>, string?\)

Creates a successful pipeline result.

```csharp
public static PipelineResult CreateSuccess(TimeSpan executionTime, Dictionary<string, NodeResult> nodeResults, string? pipelineName = null)
```

#### Parameters

`executionTime` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

`nodeResults` [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [NodeResult](Flowthru.Pipelines.NodeResult.md)\>

`pipelineName` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [PipelineResult](Flowthru.Pipelines.PipelineResult.md)

