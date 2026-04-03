# <a id="Flowthru_Services_Models_PipelineMetadata"></a> Class PipelineMetadata

Namespace: [Flowthru.Services.Models](Flowthru.Services.Models.md)  
Assembly: Flowthru.Core.dll  

Metadata about a pipeline's structure and configuration.

```csharp
public sealed record PipelineMetadata : IEquatable<PipelineMetadata>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PipelineMetadata](Flowthru.Services.Models.PipelineMetadata.md)

#### Implements

[IEquatable<PipelineMetadata\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Provides read-only information about a pipeline without executing it.
Useful for discovery, validation, and UI generation.

## Properties

### <a id="Flowthru_Services_Models_PipelineMetadata_Description"></a> Description

Optional description of the pipeline's purpose.

```csharp
public string? Description { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Services_Models_PipelineMetadata_ExternalInputs"></a> ExternalInputs

Labels of external data sources (Layer 0 inputs).

```csharp
public required IReadOnlyList<string> ExternalInputs { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="Flowthru_Services_Models_PipelineMetadata_IsBuilt"></a> IsBuilt

Whether the pipeline has been built (DAG analyzed).

```csharp
public required bool IsBuilt { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Services_Models_PipelineMetadata_LayerCount"></a> LayerCount

Number of execution layers in the pipeline's DAG.

```csharp
public required int LayerCount { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Services_Models_PipelineMetadata_Name"></a> Name

The pipeline's registered name.

```csharp
public required string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Services_Models_PipelineMetadata_NodeCount"></a> NodeCount

Total number of nodes in the pipeline.

```csharp
public required int NodeCount { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

