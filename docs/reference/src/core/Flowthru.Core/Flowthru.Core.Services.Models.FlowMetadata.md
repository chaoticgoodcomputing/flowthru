# <a id="Flowthru_Core_Services_Models_FlowMetadata"></a> Class FlowMetadata

Namespace: [Flowthru.Core.Services.Models](Flowthru.Core.Services.Models.md)  
Assembly: Flowthru.Core.dll  

Metadata about a flow's structure and configuration.

```csharp
public sealed record FlowMetadata : IEquatable<FlowMetadata>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowMetadata](Flowthru.Core.Services.Models.FlowMetadata.md)

#### Implements

[IEquatable<FlowMetadata\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Provides read-only information about a Flow without executing it.
Useful for discovery, validation, and UI generation.

## Properties

### <a id="Flowthru_Core_Services_Models_FlowMetadata_Description"></a> Description

Optional description of the flow's purpose.

```csharp
public string? Description { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Core_Services_Models_FlowMetadata_ExternalInputs"></a> ExternalInputs

Labels of external data sources (Layer 0 inputs).

```csharp
public required IReadOnlyList<string> ExternalInputs { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="Flowthru_Core_Services_Models_FlowMetadata_IsBuilt"></a> IsBuilt

Whether the Flow has been built (DAG analyzed).

```csharp
public required bool IsBuilt { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Services_Models_FlowMetadata_LayerCount"></a> LayerCount

Number of execution layers in the flow's DAG.

```csharp
public required int LayerCount { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Core_Services_Models_FlowMetadata_Name"></a> Name

The flow's registered name.

```csharp
public required string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Services_Models_FlowMetadata_StepCount"></a> StepCount

Total number of steps in the flow.

```csharp
public required int StepCount { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

