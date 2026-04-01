# <a id="Flowthru_Meta_TimestampConfiguration"></a> Class TimestampConfiguration

Namespace: [Flowthru.Meta](Flowthru.Meta.md)  
Assembly: Flowthru.Core.dll  

Configuration for timestamp handling in metadata file exports.

```csharp
public class TimestampConfiguration
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TimestampConfiguration](Flowthru.Meta.TimestampConfiguration.md)

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
Controls whether and how timestamps are included in metadata filenames.
This configuration applies to all metadata providers (JSON, Mermaid, etc.)
to ensure consistent filename generation.
</p>
<p>
<strong>Default behavior:</strong> Timestamps are included with format "yyyyMMdd-HHmmss"
</p>
<p>
<strong>Example filenames:</strong>
</p>
<ul><li>With timestamp: <code>dag-DataProcessing-20251024-143052.json</code></li><li>Without timestamp: <code>dag-DataProcessing.json</code></li></ul>
<p>
<strong>Warning:</strong> When timestamps are disabled, subsequent exports will overwrite
previous files with the same pipeline name.
</p>

## Properties

### <a id="Flowthru_Meta_TimestampConfiguration_Format"></a> Format

Gets or sets the timestamp format string.

```csharp
public string Format { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Default: "yyyyMMdd-HHmmss" (e.g., "20251024-143052")
Must be a valid DateTime format string compatible with DateTime.ToString().
Only used when IncludeTimestamp is true.

### <a id="Flowthru_Meta_TimestampConfiguration_IncludeTimestamp"></a> IncludeTimestamp

Gets or sets whether to include timestamps in metadata filenames.

```csharp
public bool IncludeTimestamp { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: true
When false, files will be named without timestamps and will overwrite on each export.

