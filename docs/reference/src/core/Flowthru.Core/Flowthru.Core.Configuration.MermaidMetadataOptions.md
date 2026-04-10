# <a id="Flowthru_Core_Configuration_MermaidMetadataOptions"></a> Class MermaidMetadataOptions

Namespace: [Flowthru.Core.Configuration](Flowthru.Core.Configuration.md)  
Assembly: Flowthru.Core.dll  

Configuration options for Mermaid diagram export.

```csharp
public class MermaidMetadataOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MermaidMetadataOptions](Flowthru.Core.Configuration.MermaidMetadataOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Core_Configuration_MermaidMetadataOptions_ActiveDataColor"></a> ActiveDataColor

Hex color code for active (sliced) catalog entries.

```csharp
public string ActiveDataColor { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Color applied to data catalog entries produced by sliced nodes.
Default: #2E7D32 (Material Design green-800).

### <a id="Flowthru_Core_Configuration_MermaidMetadataOptions_ActiveStepColor"></a> ActiveStepColor

Hex color code for active (sliced) nodes.

```csharp
public string ActiveStepColor { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Color applied to nodes that are in the execution slice.
Default: #2E7D32 (Material Design green-800).

### <a id="Flowthru_Core_Configuration_MermaidMetadataOptions_Direction"></a> Direction

Flowchart direction (TopToBottom, LeftToRight, etc.).

```csharp
public string Direction { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Configuration_MermaidMetadataOptions_ShowDatasetDetails"></a> ShowDatasetDetails

Whether to include dataset details in nodes.

```csharp
public bool ShowDatasetDetails { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Configuration_MermaidMetadataOptions_ShowParameters"></a> ShowParameters

Whether to include parameter information in nodes.

```csharp
public bool ShowParameters { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

