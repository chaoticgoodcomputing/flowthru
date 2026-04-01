# <a id="Flowthru_Services_Models_PipelineExecutionRequest"></a> Class PipelineExecutionRequest

Namespace: [Flowthru.Services.Models](Flowthru.Services.Models.md)  
Assembly: Flowthru.Core.dll  

Request model for pipeline execution.

```csharp
public record PipelineExecutionRequest : IEquatable<PipelineExecutionRequest>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PipelineExecutionRequest](Flowthru.Services.Models.PipelineExecutionRequest.md)

#### Implements

[IEquatable<PipelineExecutionRequest\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Encapsulates all configuration needed to execute a pipeline programmatically,
separate from CLI argument parsing.

## Properties

### <a id="Flowthru_Services_Models_PipelineExecutionRequest_ExportMetadata"></a> ExportMetadata

Whether to export DAG metadata.

```csharp
public bool ExportMetadata { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Defaults to true. Only applies if a metadata builder is configured.

### <a id="Flowthru_Services_Models_PipelineExecutionRequest_MetadataOutputDirectory"></a> MetadataOutputDirectory

Output directory for metadata (if null, uses default from metadata builder).

```csharp
public string? MetadataOutputDirectory { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Services_Models_PipelineExecutionRequest_Options"></a> Options

Execution options (dry run, parallel execution, etc.).

```csharp
public ExecutionOptions? Options { get; init; }
```

#### Property Value

 [ExecutionOptions](Flowthru.Pipelines.ExecutionOptions.md)?

#### Remarks

If null, uses default execution options.

### <a id="Flowthru_Services_Models_PipelineExecutionRequest_PipelineName"></a> PipelineName

Name of the pipeline to execute.

```csharp
public required string PipelineName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Services_Models_PipelineExecutionRequest_SliceStrategy"></a> SliceStrategy

Optional slicing strategy to execute a subset of the pipeline.

```csharp
public PipelineSliceStrategy? SliceStrategy { get; init; }
```

#### Property Value

 [PipelineSliceStrategy](Flowthru.Pipelines.PipelineSliceStrategy.md)?

#### Remarks

If null or IsSliced=false, the entire pipeline executes.

