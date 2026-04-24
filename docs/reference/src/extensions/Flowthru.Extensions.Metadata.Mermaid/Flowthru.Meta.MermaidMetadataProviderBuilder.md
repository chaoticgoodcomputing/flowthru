# <a id="Flowthru_Meta_MermaidMetadataProviderBuilder"></a> Class MermaidMetadataProviderBuilder

Namespace: [Flowthru.Meta](Flowthru.Meta.md)  
Assembly: Flowthru.Extensions.Metadata.Mermaid.dll  

Builder for configuring <xref href="Flowthru.Meta.Providers.MermaidMetadataProvider" data-throw-if-not-resolved="false"></xref> options.

```csharp
public class MermaidMetadataProviderBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_Build"></a> Build\(\)

Builds the Mermaid metadata provider with the configured options.

```csharp
public MermaidMetadataProvider Build()
```

#### Returns

 [MermaidMetadataProvider](Flowthru.Meta.Providers.MermaidMetadataProvider.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithActiveDataColor_System_String_"></a> WithActiveDataColor\(string\)

Sets the color for active (sliced) catalog entries in the pre-run DAG diagram.

```csharp
public MermaidMetadataProviderBuilder WithActiveDataColor(string color)
```

#### Parameters

`color` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color code (e.g., "#2E7D32")

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithActiveStepColor_System_String_"></a> WithActiveStepColor\(string\)

Sets the color for active (sliced) step nodes in the pre-run DAG diagram.

```csharp
public MermaidMetadataProviderBuilder WithActiveStepColor(string color)
```

#### Parameters

`color` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color code (e.g., "#2E7D32")

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithDirection_Flowthru_Meta_Providers_MermaidMetadataProvider_MermaidFlowchartDirection_"></a> WithDirection\(MermaidFlowchartDirection\)

Sets the flowchart direction.

```csharp
public MermaidMetadataProviderBuilder WithDirection(MermaidMetadataProvider.MermaidFlowchartDirection direction)
```

#### Parameters

`direction` [MermaidMetadataProvider](Flowthru.Meta.Providers.MermaidMetadataProvider.md).[MermaidFlowchartDirection](Flowthru.Meta.Providers.MermaidMetadataProvider.MermaidFlowchartDirection.md)

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithFailedStepColor_System_String_"></a> WithFailedStepColor\(string\)

Sets the color for failed step nodes in the post-run diagram.

```csharp
public MermaidMetadataProviderBuilder WithFailedStepColor(string color)
```

#### Parameters

`color` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color code (e.g., "#C62828")

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithFilenameTemplate_System_String_"></a> WithFilenameTemplate\(string\)

Sets the filename template for pre-run DAG diagram files.

```csharp
public MermaidMetadataProviderBuilder WithFilenameTemplate(string template)
```

#### Parameters

`template` [string](https://learn.microsoft.com/dotnet/api/system.string)

Template with placeholders: {FlowName}, {Timestamp}, {SliceType}, {Flows}, {From}, {To}, {Only}

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithLogger_Microsoft_Extensions_Logging_ILogger_"></a> WithLogger\(ILogger\)

Sets a custom logger for this provider.

```csharp
public MermaidMetadataProviderBuilder WithLogger(ILogger logger)
```

#### Parameters

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithNotRunStepColor_System_String_"></a> WithNotRunStepColor\(string\)

Sets the color for steps that did not run in the post-run diagram.

```csharp
public MermaidMetadataProviderBuilder WithNotRunStepColor(string color)
```

#### Parameters

`color` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color code (e.g., "#757575")

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithOutputDirectory_System_String_"></a> WithOutputDirectory\(string\)

Sets the output directory for metadata files.

```csharp
public MermaidMetadataProviderBuilder WithOutputDirectory(string directory)
```

#### Parameters

`directory` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithRunFilenameTemplate_System_String_"></a> WithRunFilenameTemplate\(string\)

Sets the filename template for post-run result diagram files.

```csharp
public MermaidMetadataProviderBuilder WithRunFilenameTemplate(string template)
```

#### Parameters

`template` [string](https://learn.microsoft.com/dotnet/api/system.string)

Template with placeholders: {FlowName}, {Timestamp}, {SliceType}, {Flows}, {From}, {To}, {Only}

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithShowFullDag_System_Boolean_"></a> WithShowFullDag\(bool\)

Sets whether the full DAG is shown (with active nodes highlighted) or only the sliced portion.
Defaults to true. When false and no slice is applied, has no effect.

```csharp
public MermaidMetadataProviderBuilder WithShowFullDag(bool showFullDag)
```

#### Parameters

`showFullDag` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

### <a id="Flowthru_Meta_MermaidMetadataProviderBuilder_WithTimestamp_System_String_"></a> WithTimestamp\(string?\)

Sets the timestamp format for filename generation.

```csharp
public MermaidMetadataProviderBuilder WithTimestamp(string? format = null)
```

#### Parameters

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Timestamp format string (e.g., "yyyy-MM-dd_HH-mm-ss")

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Meta.MermaidMetadataProviderBuilder.md)

