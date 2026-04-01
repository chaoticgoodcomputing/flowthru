# <a id="Flowthru_Configuration_PipelineValidationOptions"></a> Class PipelineValidationOptions

Namespace: [Flowthru.Configuration](Flowthru.Configuration.md)  
Assembly: Flowthru.Core.dll  

Configuration options for pipeline validation behavior.

```csharp
public class PipelineValidationOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PipelineValidationOptions](Flowthru.Configuration.PipelineValidationOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Configuration_PipelineValidationOptions_DefaultInspectionLevel"></a> DefaultInspectionLevel

Default inspection level for all Layer 0 inputs.

```csharp
public string? DefaultInspectionLevel { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Configuration_PipelineValidationOptions_InspectionLevels"></a> InspectionLevels

Per-catalog-entry inspection level overrides.
Key: catalog entry key, Value: inspection level (None, Shallow, Deep).

```csharp
public Dictionary<string, string> InspectionLevels { get; set; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

