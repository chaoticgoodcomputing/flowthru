# <a id="Flowthru_Configuration_MetadataOptions"></a> Class MetadataOptions

Namespace: [Flowthru.Configuration](Flowthru.Configuration.md)  
Assembly: Flowthru.Core.dll  

Configuration options for metadata collection and export.

```csharp
public class MetadataOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MetadataOptions](Flowthru.Configuration.MetadataOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Configuration_MetadataOptions_Enabled"></a> Enabled

Whether metadata collection is enabled.

```csharp
public bool Enabled { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Configuration_MetadataOptions_FilenameTemplate"></a> FilenameTemplate

Filename template for metadata exports.

```csharp
public string FilenameTemplate { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

<p>
Supports dynamic tokens that are replaced during export:
</p>
<ul><li><code>{PipelineName}</code> - Sanitized pipeline name</li><li><code>{Timestamp}</code> - Formatted timestamp (empty if disabled in Timestamp.IncludeTimestamp)</li><li><code>{SliceType}</code> - "FromNodes", "Tags", "Mixed", or empty if unsliced</li><li><code>{FromNodes}</code> - Comma-separated list of from-nodes</li><li><code>{ToNodes}</code> - Comma-separated list of to-nodes</li><li><code>{FromInputs}</code> - Comma-separated list of from-inputs</li><li><code>{OnlyNodes}</code> - Comma-separated list of only-nodes</li><li><code>{Tags}</code> - Comma-separated list of tags</li></ul>
<p>
Empty tokens are automatically collapsed to prevent double-separators.
File extensions are added by individual providers (.json, .md, etc.).
</p>
<p>
<strong>Default:</strong> <code>"dag-{PipelineName}-{Timestamp}-{SliceType}"</code>
</p>
<p>
<strong>Examples:</strong>
</p>
<ul><li>Unsliced: <code>dag-DataProcessing-20260304-153045.json</code></li><li>Sliced: <code>dag-DataProcessing-20260304-153045-FromNodes.json</code></li></ul>

### <a id="Flowthru_Configuration_MetadataOptions_Json"></a> Json

Configuration specific to the JSON metadata provider.

```csharp
public JsonMetadataOptions? Json { get; set; }
```

#### Property Value

 [JsonMetadataOptions](Flowthru.Configuration.JsonMetadataOptions.md)?

### <a id="Flowthru_Configuration_MetadataOptions_Mermaid"></a> Mermaid

Configuration specific to the Mermaid metadata provider.

```csharp
public MermaidMetadataOptions? Mermaid { get; set; }
```

#### Property Value

 [MermaidMetadataOptions](Flowthru.Configuration.MermaidMetadataOptions.md)?

### <a id="Flowthru_Configuration_MetadataOptions_OutputDirectory"></a> OutputDirectory

Directory where metadata files will be written.

```csharp
public string OutputDirectory { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Configuration_MetadataOptions_Providers"></a> Providers

List of metadata providers to enable (e.g., "Json", "Mermaid", "Csv").

```csharp
public List<string> Providers { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="Flowthru_Configuration_MetadataOptions_Timestamp"></a> Timestamp

Configuration for timestamp generation in metadata filenames.

```csharp
public TimestampConfiguration Timestamp { get; set; }
```

#### Property Value

 [TimestampConfiguration](Flowthru.Configuration.TimestampConfiguration.md)

