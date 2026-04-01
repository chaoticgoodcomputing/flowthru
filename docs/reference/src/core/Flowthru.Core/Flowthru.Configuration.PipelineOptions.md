# <a id="Flowthru_Configuration_PipelineOptions"></a> Class PipelineOptions

Namespace: [Flowthru.Configuration](Flowthru.Configuration.md)  
Assembly: Flowthru.Core.dll  

Configuration options for a single pipeline.

```csharp
public class PipelineOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PipelineOptions](Flowthru.Configuration.PipelineOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Configuration_PipelineOptions_Description"></a> Description

Human-readable description of the pipeline.

```csharp
public string? Description { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Configuration_PipelineOptions_FactoryMethod"></a> FactoryMethod

The name of the static factory method (default: "Create").

```csharp
public string FactoryMethod { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Configuration_PipelineOptions_Parameters"></a> Parameters

Pipeline-specific parameters (nested configuration section).
The structure must match the pipeline's parameter type.

```csharp
public Dictionary<string, object>? Parameters { get; set; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [object](https://learn.microsoft.com/dotnet/api/system.object)\>?

### <a id="Flowthru_Configuration_PipelineOptions_Type"></a> Type

The fully-qualified type name of the pipeline factory class.
Must have a static Create method that accepts (catalog, parameters?).

```csharp
public string? Type { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Configuration_PipelineOptions_Validation"></a> Validation

Validation configuration for this pipeline.

```csharp
public PipelineValidationOptions? Validation { get; set; }
```

#### Property Value

 [PipelineValidationOptions](Flowthru.Configuration.PipelineValidationOptions.md)?

