# <a id="Flowthru_Core_Meta_Providers_JsonMetadataProvider"></a> Class JsonMetadataProvider

Namespace: [Flowthru.Core.Meta.Providers](Flowthru.Core.Meta.Providers.md)  
Assembly: Flowthru.Core.dll  

Exports DAG metadata as JSON files.

```csharp
[MetadataProviderBuilder(typeof(JsonMetadataProviderBuilder))]
public class JsonMetadataProvider : IMetadataProvider
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[JsonMetadataProvider](Flowthru.Core.Meta.Providers.JsonMetadataProvider.md)

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

This provider creates timestamped JSON files containing the complete DAG structure
(nodes, catalog entries, edges, schema information) for consumption by Flowthru.Core.Viz
or other visualization tools.

## Constructors

### <a id="Flowthru_Core_Meta_Providers_JsonMetadataProvider__ctor_System_String_System_String_Flowthru_Core_Meta_TimestampConfiguration_System_Boolean_Microsoft_Extensions_Logging_ILogger_"></a> JsonMetadataProvider\(string, string, TimestampConfiguration, bool, ILogger?\)

Initializes a new JSON metadata provider.

```csharp
public JsonMetadataProvider(string outputDirectory, string filenameTemplate, TimestampConfiguration timestampConfig, bool useCompactFormat = false, ILogger? logger = null)
```

#### Parameters

`outputDirectory` [string](https://learn.microsoft.com/dotnet/api/system.string)

Directory to write JSON files to

`filenameTemplate` [string](https://learn.microsoft.com/dotnet/api/system.string)

Template for generating output filenames

`timestampConfig` [TimestampConfiguration](Flowthru.Core.Meta.TimestampConfiguration.md)

Configuration for timestamp handling in filenames

`useCompactFormat` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether to use compact (minified) JSON format

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)?

Optional logger for diagnostic messages

## Properties

### <a id="Flowthru_Core_Meta_Providers_JsonMetadataProvider_Name"></a> Name

Gets the unique name of this provider.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="Flowthru_Core_Meta_Providers_JsonMetadataProvider_Consume_Flowthru_Core_Graph_Meta_Models_DagMetadata_"></a> Consume\(DagMetadata\)

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

