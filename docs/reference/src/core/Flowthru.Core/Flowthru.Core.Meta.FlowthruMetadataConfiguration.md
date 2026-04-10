# <a id="Flowthru_Core_Meta_FlowthruMetadataConfiguration"></a> Class FlowthruMetadataConfiguration

Namespace: [Flowthru.Core.Meta](Flowthru.Core.Meta.md)  
Assembly: Flowthru.Core.dll  

Configuration for Flowthru metadata collection and export.

```csharp
public class FlowthruMetadataConfiguration
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowthruMetadataConfiguration](Flowthru.Core.Meta.FlowthruMetadataConfiguration.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
This configuration controls whether and how pipeline metadata is collected
and persisted. Metadata includes DAG structure (nodes, catalog entries, edges)
that can be consumed by Flowthru.Core.Viz for visualization.
</p>
<p>
<strong>Usage:</strong>
</p>
<pre><code class="lang-csharp">builder.IncludeMetadata(metadata =&gt; {
    metadata
        .WithOutputDirectory("Data/Metadata")
        .EnableAutoExport();
});</code></pre>

## Properties

### <a id="Flowthru_Core_Meta_FlowthruMetadataConfiguration_AutoExportDag"></a> AutoExportDag

Whether to automatically export DAG metadata after Pipeline.Build().

```csharp
public bool AutoExportDag { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: true
When enabled, DAG JSON files are automatically created after each pipeline build.

### <a id="Flowthru_Core_Meta_FlowthruMetadataConfiguration_ExportMermaid"></a> ExportMermaid

Whether to export Mermaid diagram files (.md) alongside JSON files.

```csharp
public bool ExportMermaid { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: true
When enabled, a Markdown file with an embedded Mermaid diagram is created
alongside each JSON file for immediate visualization.

### <a id="Flowthru_Core_Meta_FlowthruMetadataConfiguration_OutputDirectory"></a> OutputDirectory

Directory where metadata JSON files will be written.

```csharp
public string OutputDirectory { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Default: "Data/Metadata"

## Methods

### <a id="Flowthru_Core_Meta_FlowthruMetadataConfiguration_DisableAutoExport"></a> DisableAutoExport\(\)

Disables automatic DAG export.

```csharp
public FlowthruMetadataConfiguration DisableAutoExport()
```

#### Returns

 [FlowthruMetadataConfiguration](Flowthru.Core.Meta.FlowthruMetadataConfiguration.md)

This configuration for fluent chaining

#### Remarks

Use this when you want manual control over metadata export via Pipeline.ExportDag().

### <a id="Flowthru_Core_Meta_FlowthruMetadataConfiguration_DisableMermaid"></a> DisableMermaid\(\)

Disables Mermaid diagram export.

```csharp
public FlowthruMetadataConfiguration DisableMermaid()
```

#### Returns

 [FlowthruMetadataConfiguration](Flowthru.Core.Meta.FlowthruMetadataConfiguration.md)

This configuration for fluent chaining

#### Remarks

Use this when you only want JSON output without Mermaid diagrams.

### <a id="Flowthru_Core_Meta_FlowthruMetadataConfiguration_EnableAutoExport"></a> EnableAutoExport\(\)

Enables automatic DAG export after pipeline builds.

```csharp
public FlowthruMetadataConfiguration EnableAutoExport()
```

#### Returns

 [FlowthruMetadataConfiguration](Flowthru.Core.Meta.FlowthruMetadataConfiguration.md)

This configuration for fluent chaining

### <a id="Flowthru_Core_Meta_FlowthruMetadataConfiguration_EnableMermaid"></a> EnableMermaid\(\)

Enables Mermaid diagram export.

```csharp
public FlowthruMetadataConfiguration EnableMermaid()
```

#### Returns

 [FlowthruMetadataConfiguration](Flowthru.Core.Meta.FlowthruMetadataConfiguration.md)

This configuration for fluent chaining

### <a id="Flowthru_Core_Meta_FlowthruMetadataConfiguration_WithOutputDirectory_System_String_"></a> WithOutputDirectory\(string\)

Sets the output directory for metadata files.

```csharp
public FlowthruMetadataConfiguration WithOutputDirectory(string directory)
```

#### Parameters

`directory` [string](https://learn.microsoft.com/dotnet/api/system.string)

Directory path (absolute or relative to working directory)

#### Returns

 [FlowthruMetadataConfiguration](Flowthru.Core.Meta.FlowthruMetadataConfiguration.md)

This configuration for fluent chaining

