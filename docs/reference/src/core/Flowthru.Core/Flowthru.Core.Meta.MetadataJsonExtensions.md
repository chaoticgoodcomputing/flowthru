# <a id="Flowthru_Core_Meta_MetadataJsonExtensions"></a> Class MetadataJsonExtensions

Namespace: [Flowthru.Core.Meta](Flowthru.Core.Meta.md)  
Assembly: Flowthru.Core.dll  

Extension methods for serializing metadata to JSON.

```csharp
public static class MetadataJsonExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MetadataJsonExtensions](Flowthru.Core.Meta.MetadataJsonExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Core_Meta_MetadataJsonExtensions_FromJson_System_String_"></a> FromJson\(string\)

Deserializes DagMetadata from JSON string.

```csharp
public static DagMetadata FromJson(string json)
```

#### Parameters

`json` [string](https://learn.microsoft.com/dotnet/api/system.string)

JSON string to deserialize

#### Returns

 [DagMetadata](Flowthru.Core.Graph.Meta.Models.DagMetadata.md)

Deserialized DagMetadata object

#### Exceptions

 [JsonException](https://learn.microsoft.com/dotnet/api/system.text.json.jsonexception)

Thrown if JSON is invalid or doesn't match schema

### <a id="Flowthru_Core_Meta_MetadataJsonExtensions_ToCompactJson_Flowthru_Core_Graph_Meta_Models_DagMetadata_"></a> ToCompactJson\(DagMetadata\)

Serializes DagMetadata to compact JSON string (no indentation).

```csharp
public static string ToCompactJson(this DagMetadata metadata)
```

#### Parameters

`metadata` [DagMetadata](Flowthru.Core.Graph.Meta.Models.DagMetadata.md)

The DAG metadata to serialize

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

Compact JSON string representation

#### Remarks

Use this for minimizing file size when human readability is not a concern,
such as API responses or embedded metadata.

### <a id="Flowthru_Core_Meta_MetadataJsonExtensions_ToJson_Flowthru_Core_Graph_Meta_Models_DagMetadata_"></a> ToJson\(DagMetadata\)

Serializes DagMetadata to pretty-printed JSON string.

```csharp
public static string ToJson(this DagMetadata metadata)
```

#### Parameters

`metadata` [DagMetadata](Flowthru.Core.Graph.Meta.Models.DagMetadata.md)

The DAG metadata to serialize

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

JSON string representation

#### Remarks

<p>
Output format uses:
</p>
<ul><li>camelCase property names (pipelineName, not FlowName)</li><li>Indented formatting for readability</li><li>Null properties omitted</li><li>Enums serialized as strings</li></ul>
<p>
This format is optimized for Flowthru.Core.Viz consumption and human readability.
</p>

