# <a id="Flowthru_Meta_JsonMetadataProviderBuilder"></a> Class JsonMetadataProviderBuilder

Namespace: [Flowthru.Meta](Flowthru.Meta.md)  
Assembly: Flowthru.Core.dll  

Builder for configuring JSON metadata provider options.

```csharp
public class JsonMetadataProviderBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[JsonMetadataProviderBuilder](Flowthru.Meta.JsonMetadataProviderBuilder.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Meta_JsonMetadataProviderBuilder_Build"></a> Build\(\)

Builds the JSON metadata provider with the configured options.

```csharp
public JsonMetadataProvider Build()
```

#### Returns

 [JsonMetadataProvider](Flowthru.Meta.Providers.JsonMetadataProvider.md)

A configured <xref href="Flowthru.Meta.Providers.JsonMetadataProvider" data-throw-if-not-resolved="false"></xref> instance

### <a id="Flowthru_Meta_JsonMetadataProviderBuilder_UseCompactFormat"></a> UseCompactFormat\(\)

Enables compact JSON format (no indentation).

```csharp
public JsonMetadataProviderBuilder UseCompactFormat()
```

#### Returns

 [JsonMetadataProviderBuilder](Flowthru.Meta.JsonMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Meta_JsonMetadataProviderBuilder_UseIndentedFormat"></a> UseIndentedFormat\(\)

Enables indented JSON format (default).

```csharp
public JsonMetadataProviderBuilder UseIndentedFormat()
```

#### Returns

 [JsonMetadataProviderBuilder](Flowthru.Meta.JsonMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Meta_JsonMetadataProviderBuilder_WithFilenameTemplate_System_String_"></a> WithFilenameTemplate\(string\)

Sets the filename template for metadata files.

```csharp
public JsonMetadataProviderBuilder WithFilenameTemplate(string template)
```

#### Parameters

`template` [string](https://learn.microsoft.com/dotnet/api/system.string)

Template with placeholders: {PipelineName}, {Timestamp}, {SliceType}

#### Returns

 [JsonMetadataProviderBuilder](Flowthru.Meta.JsonMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Meta_JsonMetadataProviderBuilder_WithLogger_Microsoft_Extensions_Logging_ILogger_"></a> WithLogger\(ILogger\)

Sets a custom logger for this provider.

```csharp
public JsonMetadataProviderBuilder WithLogger(ILogger logger)
```

#### Parameters

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)

Logger instance

#### Returns

 [JsonMetadataProviderBuilder](Flowthru.Meta.JsonMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Meta_JsonMetadataProviderBuilder_WithOutputDirectory_System_String_"></a> WithOutputDirectory\(string\)

Sets the output directory for metadata files.

```csharp
public JsonMetadataProviderBuilder WithOutputDirectory(string directory)
```

#### Parameters

`directory` [string](https://learn.microsoft.com/dotnet/api/system.string)

Directory path (relative or absolute)

#### Returns

 [JsonMetadataProviderBuilder](Flowthru.Meta.JsonMetadataProviderBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Meta_JsonMetadataProviderBuilder_WithTimestamp_System_String_"></a> WithTimestamp\(string?\)

Sets the timestamp format for filename generation.

```csharp
public JsonMetadataProviderBuilder WithTimestamp(string? format = null)
```

#### Parameters

`format` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Timestamp format string (e.g., "yyyy-MM-dd_HH-mm-ss")

#### Returns

 [JsonMetadataProviderBuilder](Flowthru.Meta.JsonMetadataProviderBuilder.md)

This builder for fluent chaining

