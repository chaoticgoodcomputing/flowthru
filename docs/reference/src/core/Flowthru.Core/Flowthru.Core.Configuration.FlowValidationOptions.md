# <a id="Flowthru_Core_Configuration_FlowValidationOptions"></a> Class FlowValidationOptions

Namespace: [Flowthru.Core.Configuration](Flowthru.Core.Configuration.md)  
Assembly: Flowthru.Core.dll  

Configuration options for Flow validation behavior.

```csharp
public class FlowValidationOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowValidationOptions](Flowthru.Core.Configuration.FlowValidationOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Core_Configuration_FlowValidationOptions_DefaultInspectionLevel"></a> DefaultInspectionLevel

Default inspection level for all Layer 0 inputs.

```csharp
public string? DefaultInspectionLevel { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Core_Configuration_FlowValidationOptions_InspectionLevels"></a> InspectionLevels

Per-catalog-entry inspection level overrides.
Key: catalog entry key, Value: inspection level (None, Shallow, Deep).

```csharp
public Dictionary<string, string> InspectionLevels { get; set; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

