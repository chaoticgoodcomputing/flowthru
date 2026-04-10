# <a id="Flowthru_Core_Meta_Providers_MermaidMetadataProvider"></a> Class MermaidMetadataProvider

Namespace: [Flowthru.Core.Meta.Providers](Flowthru.Core.Meta.Providers.md)  
Assembly: Flowthru.Core.dll  

Exports DAG metadata as Mermaid flowchart diagrams.

```csharp
[MetadataProviderBuilder(typeof(MermaidMetadataProviderBuilder))]
public class MermaidMetadataProvider : IMetadataProvider
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MermaidMetadataProvider](Flowthru.Core.Meta.Providers.MermaidMetadataProvider.md)

#### Implements

[IMetadataProvider](Flowthru.Core.Meta.Providers.IMetadataProvider.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

This provider creates Markdown files containing Mermaid flowchart diagrams
for immediate visualization in GitHub, VS Code, and other Mermaid-compatible viewers.

## Constructors

### <a id="Flowthru_Core_Meta_Providers_MermaidMetadataProvider__ctor_System_String_System_String_Flowthru_Core_Meta_TimestampConfiguration_Flowthru_Core_Meta_Providers_MermaidMetadataProvider_MermaidFlowchartDirection_System_String_System_String_Microsoft_Extensions_Logging_ILogger_"></a> MermaidMetadataProvider\(string, string, TimestampConfiguration, MermaidFlowchartDirection, string, string, ILogger?\)

Initializes a new Mermaid metadata provider.

```csharp
public MermaidMetadataProvider(string outputDirectory, string filenameTemplate, TimestampConfiguration timestampConfig, MermaidMetadataProvider.MermaidFlowchartDirection direction = MermaidFlowchartDirection.TopToBottom, string activeStepColor = "#2E7D32", string activeDataColor = "#2E7D32", ILogger? logger = null)
```

#### Parameters

`outputDirectory` [string](https://learn.microsoft.com/dotnet/api/system.string)

Directory to write Mermaid files to

`filenameTemplate` [string](https://learn.microsoft.com/dotnet/api/system.string)

Template for generating output filenames

`timestampConfig` [TimestampConfiguration](Flowthru.Core.Meta.TimestampConfiguration.md)

Configuration for timestamp handling in filenames

`direction` [MermaidMetadataProvider](Flowthru.Core.Meta.Providers.MermaidMetadataProvider.md).[MermaidFlowchartDirection](Flowthru.Core.Meta.Providers.MermaidMetadataProvider.MermaidFlowchartDirection.md)

Flow direction for the diagram

`activeStepColor` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color for active (sliced) nodes

`activeDataColor` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color for active (sliced) catalog entries

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)?

Optional logger for diagnostic messages

## Properties

### <a id="Flowthru_Core_Meta_Providers_MermaidMetadataProvider_Name"></a> Name

Gets the unique name of this provider.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="Flowthru_Core_Meta_Providers_MermaidMetadataProvider_Consume_Flowthru_Core_Graph_Meta_Models_DagMetadata_"></a> Consume\(DagMetadata\)

Consumes DAG metadata.

```csharp
public void Consume(DagMetadata dag)
```

#### Parameters

`dag` [DagMetadata](Flowthru.Core.Graph.Meta.Models.DagMetadata.md)

The DAG metadata to consume

#### Remarks

This method is called after pipeline builds. Providers can process
the metadata in any way: write files, send to APIs, store in memory, etc.

Implementations should handle their own error recovery - exceptions thrown
from this method will be logged but will not fail the pipeline execution.

