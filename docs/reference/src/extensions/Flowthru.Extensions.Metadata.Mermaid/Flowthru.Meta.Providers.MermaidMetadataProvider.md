# <a id="Flowthru_Meta_Providers_MermaidMetadataProvider"></a> Class MermaidMetadataProvider

Namespace: [Flowthru.Meta.Providers](Flowthru.Meta.Providers.md)  
Assembly: Flowthru.Extensions.Metadata.Mermaid.dll  

Exports DAG metadata as Mermaid flowchart diagrams, and optionally exports
a post-run diagram colored by step execution outcomes.

```csharp
[MetadataProviderBuilder(typeof(MermaidMetadataProviderBuilder))]
public class MermaidMetadataProvider : IMetadataProvider, IPostRunMetadataProvider
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MermaidMetadataProvider](Flowthru.Meta.Providers.MermaidMetadataProvider.md)

#### Implements

IMetadataProvider, 
IPostRunMetadataProvider

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
When post-run metadata is available, step nodes are colored by outcome: failed steps
are highlighted in red, steps that did not run are shown in grey, and successful steps
are colored on a green-to-amber heat map normalized to the slowest completed step.

## Constructors

### <a id="Flowthru_Meta_Providers_MermaidMetadataProvider__ctor_System_String_System_String_System_String_Flowthru_Core_Meta_TimestampConfiguration_Flowthru_Meta_Providers_MermaidMetadataProvider_MermaidFlowchartDirection_System_String_System_String_System_String_System_String_System_Boolean_Microsoft_Extensions_Logging_ILogger_"></a> MermaidMetadataProvider\(string, string, string, TimestampConfiguration, MermaidFlowchartDirection, string, string, string, string, bool, ILogger?\)

Initializes a new Mermaid metadata provider.

```csharp
public MermaidMetadataProvider(string outputDirectory, string dagFilenameTemplate, string runFilenameTemplate, TimestampConfiguration timestampConfig, MermaidMetadataProvider.MermaidFlowchartDirection direction = MermaidFlowchartDirection.TopToBottom, string activeStepColor = "#2E7D32", string activeDataColor = "#2E7D32", string failedStepColor = "#C62828", string notRunStepColor = "#757575", bool showFullDag = true, ILogger? logger = null)
```

#### Parameters

`outputDirectory` [string](https://learn.microsoft.com/dotnet/api/system.string)

`dagFilenameTemplate` [string](https://learn.microsoft.com/dotnet/api/system.string)

`runFilenameTemplate` [string](https://learn.microsoft.com/dotnet/api/system.string)

`timestampConfig` TimestampConfiguration

`direction` [MermaidMetadataProvider](Flowthru.Meta.Providers.MermaidMetadataProvider.md).[MermaidFlowchartDirection](Flowthru.Meta.Providers.MermaidMetadataProvider.MermaidFlowchartDirection.md)

`activeStepColor` [string](https://learn.microsoft.com/dotnet/api/system.string)

`activeDataColor` [string](https://learn.microsoft.com/dotnet/api/system.string)

`failedStepColor` [string](https://learn.microsoft.com/dotnet/api/system.string)

`notRunStepColor` [string](https://learn.microsoft.com/dotnet/api/system.string)

`showFullDag` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)?

## Properties

### <a id="Flowthru_Meta_Providers_MermaidMetadataProvider_Name"></a> Name

Gets the unique name of this provider.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="Flowthru_Meta_Providers_MermaidMetadataProvider_Consume_Flowthru_Core_Graph_Meta_Models_DagMetadata_"></a> Consume\(DagMetadata\)

Consumes DAG metadata.

```csharp
public void Consume(DagMetadata dag)
```

#### Parameters

`dag` DagMetadata

The DAG metadata to consume

#### Remarks

This method is called after pipeline builds. Providers can process
the metadata in any way: write files, send to APIs, store in memory, etc.

Implementations should handle their own error recovery - exceptions thrown
from this method will be logged but will not fail the pipeline execution.

### <a id="Flowthru_Meta_Providers_MermaidMetadataProvider_Consume_Flowthru_Core_Graph_Meta_Models_RunMetadata_"></a> Consume\(RunMetadata\)

Consumes composite post-run metadata combining the DAG snapshot and execution results.

```csharp
public void Consume(RunMetadata run)
```

#### Parameters

`run` RunMetadata

The combined run metadata, containing both the pre-run DAG structure and
the execution outcome for all steps.

