# <a id="Flowthru_Core_Configuration_FlowthruOptions"></a> Class FlowthruOptions

Namespace: [Flowthru.Core.Configuration](Flowthru.Core.Configuration.md)  
Assembly: Flowthru.Core.dll  

Root configuration options for Flowthru applications.

```csharp
public class FlowthruOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowthruOptions](Flowthru.Core.Configuration.FlowthruOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

This class represents the top-level "Flowthru" section in configuration files.
All Flowthru-specific configuration should be nested under this section.

## Fields

### <a id="Flowthru_Core_Configuration_FlowthruOptions_SectionName"></a> SectionName

Configuration section name in appsettings.json.

```csharp
public const string SectionName = "Flowthru"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="Flowthru_Core_Configuration_FlowthruOptions_Catalog"></a> Catalog

Data catalog configuration.

```csharp
public CatalogOptions Catalog { get; set; }
```

#### Property Value

 [CatalogOptions](Flowthru.Core.Configuration.CatalogOptions.md)

### <a id="Flowthru_Core_Configuration_FlowthruOptions_Flows"></a> Flows

Flow registration and configuration.

```csharp
public Dictionary<string, FlowOptions> Flows { get; set; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [FlowOptions](Flowthru.Core.Configuration.FlowOptions.md)\>

### <a id="Flowthru_Core_Configuration_FlowthruOptions_Logging"></a> Logging

Logging configuration (extends standard .NET logging configuration).

```csharp
public LoggingOptions? Logging { get; set; }
```

#### Property Value

 [LoggingOptions](Flowthru.Core.Configuration.LoggingOptions.md)?

### <a id="Flowthru_Core_Configuration_FlowthruOptions_Metadata"></a> Metadata

Metadata collection and export configuration.

```csharp
public MetadataOptions Metadata { get; set; }
```

#### Property Value

 [MetadataOptions](Flowthru.Core.Configuration.MetadataOptions.md)

