# <a id="Flowthru_Core_Configuration_FlowOptions"></a> Class FlowOptions

Namespace: [Flowthru.Core.Configuration](Flowthru.Core.Configuration.md)  
Assembly: Flowthru.Core.dll  

Configuration options for a single flow.

```csharp
public class FlowOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowOptions](Flowthru.Core.Configuration.FlowOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Core_Configuration_FlowOptions_Description"></a> Description

Human-readable description of the Flow.

```csharp
public string? Description { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Core_Configuration_FlowOptions_FactoryMethod"></a> FactoryMethod

The name of the static factory method (default: "Create").

```csharp
public string FactoryMethod { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Configuration_FlowOptions_Parameters"></a> Parameters

Flow-specific parameters (nested configuration section).
The structure must match the Flow's parameter type.

```csharp
public Dictionary<string, object>? Parameters { get; set; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [object](https://learn.microsoft.com/dotnet/api/system.object)\>?

### <a id="Flowthru_Core_Configuration_FlowOptions_Type"></a> Type

The fully-qualified type name of the Flow factory class.
Must have a static Create method that accepts (catalog, parameters?).

```csharp
public string? Type { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Core_Configuration_FlowOptions_Validation"></a> Validation

Validation configuration for this flow.

```csharp
public FlowValidationOptions? Validation { get; set; }
```

#### Property Value

 [FlowValidationOptions](Flowthru.Core.Configuration.FlowValidationOptions.md)?

