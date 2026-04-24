# <a id="Flowthru_Meta_Providers_JsonMetadataProvider"></a> Class JsonMetadataProvider

Namespace: [Flowthru.Meta.Providers](Flowthru.Meta.Providers.md)  
Assembly: Flowthru.Extensions.Metadata.Json.dll  

Exports DAG metadata as JSON files, and optionally exports post-run execution results.

```csharp
[MetadataProviderBuilder(typeof(JsonMetadataProviderBuilder))]
public class JsonMetadataProvider : IMetadataProvider, IPostRunMetadataProvider
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[JsonMetadataProvider](Flowthru.Meta.Providers.JsonMetadataProvider.md)

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

This provider creates timestamped JSON files containing the complete DAG structure
(nodes, catalog entries, edges, schema information). When post-run metadata is enabled,
it additionally exports a combined run result file containing both the DAG structure
and per-step execution outcomes.

## Constructors

### <a id="Flowthru_Meta_Providers_JsonMetadataProvider__ctor_System_String_System_String_System_String_Flowthru_Core_Meta_TimestampConfiguration_System_Boolean_Microsoft_Extensions_Logging_ILogger_"></a> JsonMetadataProvider\(string, string, string, TimestampConfiguration, bool, ILogger?\)

Initializes a new JSON metadata provider.

```csharp
public JsonMetadataProvider(string outputDirectory, string dagFilenameTemplate, string runFilenameTemplate, TimestampConfiguration timestampConfig, bool useCompactFormat = false, ILogger? logger = null)
```

#### Parameters

`outputDirectory` [string](https://learn.microsoft.com/dotnet/api/system.string)

`dagFilenameTemplate` [string](https://learn.microsoft.com/dotnet/api/system.string)

`runFilenameTemplate` [string](https://learn.microsoft.com/dotnet/api/system.string)

`timestampConfig` TimestampConfiguration

`useCompactFormat` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)?

## Properties

### <a id="Flowthru_Meta_Providers_JsonMetadataProvider_Name"></a> Name

Gets the unique name of this provider.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="Flowthru_Meta_Providers_JsonMetadataProvider_Consume_Flowthru_Core_Graph_Meta_Models_DagMetadata_"></a> Consume\(DagMetadata\)

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

### <a id="Flowthru_Meta_Providers_JsonMetadataProvider_Consume_Flowthru_Core_Graph_Meta_Models_RunMetadata_"></a> Consume\(RunMetadata\)

Consumes composite post-run metadata combining the DAG snapshot and execution results.

```csharp
public void Consume(RunMetadata run)
```

#### Parameters

`run` RunMetadata

The combined run metadata, containing both the pre-run DAG structure and
the execution outcome for all steps.

