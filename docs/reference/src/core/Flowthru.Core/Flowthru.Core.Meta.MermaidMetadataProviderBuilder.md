# <a id="Flowthru_Core_Meta_MermaidMetadataProviderBuilder"></a> Class MermaidMetadataProviderBuilder

Namespace: [Flowthru.Core.Meta](Flowthru.Core.Meta.md)  
Assembly: Flowthru.Core.dll  

Builder for configuring Mermaid diagram provider options.

```csharp
public class MermaidMetadataProviderBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MermaidMetadataProviderBuilder](Flowthru.Core.Meta.MermaidMetadataProviderBuilder.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Core_Meta_MermaidMetadataProviderBuilder_Build"></a> Build\(\)

Builds the Mermaid metadata provider with the configured options.

```csharp
public MermaidMetadataProvider Build()
```

#### Returns

 [MermaidMetadataProvider](Flowthru.Core.Meta.Providers.MermaidMetadataProvider.md)

A configured <xref href="Flowthru.Core.Meta.Providers.MermaidMetadataProvider" data-throw-if-not-resolved="false"></xref> instance

### <a id="Flowthru_Core_Meta_MermaidMetadataProviderBuilder_WithActiveDataColor_System_String_"></a> WithActiveDataColor\(string\)

Sets the color for active (sliced) catalog entries.

```csharp
public MermaidMetadataProviderBuilder WithActiveDataColor(string color)
```

#### Parameters

`color` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color code (e.g., "#2E7D32")

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Core.Meta.MermaidMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Core_Meta_MermaidMetadataProviderBuilder_WithActiveStepColor_System_String_"></a> WithActiveStepColor\(string\)

Sets the color for active (sliced) nodes.

```csharp
public MermaidMetadataProviderBuilder WithActiveStepColor(string color)
```

#### Parameters

`color` [string](https://learn.microsoft.com/dotnet/api/system.string)

Hex color code (e.g., "#2E7D32")

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Core.Meta.MermaidMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Core_Meta_MermaidMetadataProviderBuilder_WithDirection_Flowthru_Core_Meta_Providers_MermaidMetadataProvider_MermaidFlowchartDirection_"></a> WithDirection\(MermaidFlowchartDirection\)

Sets the flowchart direction.

```csharp
public MermaidMetadataProviderBuilder WithDirection(MermaidMetadataProvider.MermaidFlowchartDirection direction)
```

#### Parameters

`direction` [MermaidMetadataProvider](Flowthru.Core.Meta.Providers.MermaidMetadataProvider.md).[MermaidFlowchartDirection](Flowthru.Core.Meta.Providers.MermaidMetadataProvider.MermaidFlowchartDirection.md)

Direction for the flowchart (TB, LR, BT, RL)

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Core.Meta.MermaidMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Core_Meta_MermaidMetadataProviderBuilder_WithFilenameTemplate_System_String_"></a> WithFilenameTemplate\(string\)

Sets the filename template for metadata files.

```csharp
public MermaidMetadataProviderBuilder WithFilenameTemplate(string template)
```

#### Parameters

`template` [string](https://learn.microsoft.com/dotnet/api/system.string)

Template with placeholders: {FlowName}, {Timestamp}, {SliceType}

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Core.Meta.MermaidMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Core_Meta_MermaidMetadataProviderBuilder_WithLogger_Microsoft_Extensions_Logging_ILogger_"></a> WithLogger\(ILogger\)

Sets a custom logger for this provider.

```csharp
public MermaidMetadataProviderBuilder WithLogger(ILogger logger)
```

#### Parameters

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)

Logger instance

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Core.Meta.MermaidMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Core_Meta_MermaidMetadataProviderBuilder_WithOutputDirectory_System_String_"></a> WithOutputDirectory\(string\)

Sets the output directory for metadata files.

```csharp
public MermaidMetadataProviderBuilder WithOutputDirectory(string directory)
```

#### Parameters

`directory` [string](https://learn.microsoft.com/dotnet/api/system.string)

Directory path (relative or absolute)

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Core.Meta.MermaidMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Core_Meta_MermaidMetadataProviderBuilder_WithTimestamp_System_String_"></a> WithTimestamp\(string?\)

Sets the timestamp format for filename generation.

```csharp
public MermaidMetadataProviderBuilder WithTimestamp(string? format = null)
```

#### Parameters

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Timestamp format string (e.g., "yyyy-MM-dd_HH-mm-ss")

#### Returns

 [MermaidMetadataProviderBuilder](Flowthru.Core.Meta.MermaidMetadataProviderBuilder.md)

This builder for fluent chaining

