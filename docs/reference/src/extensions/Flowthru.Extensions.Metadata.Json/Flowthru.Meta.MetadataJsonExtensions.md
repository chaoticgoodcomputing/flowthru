# <a id="Flowthru_Meta_MetadataJsonExtensions"></a> Class MetadataJsonExtensions

Namespace: [Flowthru.Meta](Flowthru.Meta.md)  
Assembly: Flowthru.Extensions.Metadata.Json.dll  

Extension methods for serializing metadata to JSON.

```csharp
public static class MetadataJsonExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MetadataJsonExtensions](Flowthru.Meta.MetadataJsonExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Meta_MetadataJsonExtensions_FromJson_System_String_"></a> FromJson\(string\)

Deserializes DagMetadata from JSON string.

```csharp
public static DagMetadata FromJson(string json)
```

#### Parameters

`json` [string](https://learn.microsoft.com/dotnet/api/system.string)

JSON string to deserialize

#### Returns

 DagMetadata

Deserialized DagMetadata object

#### Exceptions

 [JsonException](https://learn.microsoft.com/dotnet/api/system.text.json.jsonexception)

Thrown if JSON is invalid or doesn't match schema

### <a id="Flowthru_Meta_MetadataJsonExtensions_ToCompactJson_Flowthru_Core_Graph_Meta_Models_DagMetadata_"></a> ToCompactJson\(DagMetadata\)

Serializes DagMetadata to compact JSON string (no indentation).

```csharp
public static string ToCompactJson(this DagMetadata metadata)
```

#### Parameters

`metadata` DagMetadata

The DAG metadata to serialize

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

Compact JSON string representation

### <a id="Flowthru_Meta_MetadataJsonExtensions_ToJson_Flowthru_Core_Graph_Meta_Models_DagMetadata_"></a> ToJson\(DagMetadata\)

Serializes DagMetadata to pretty-printed JSON string.

```csharp
public static string ToJson(this DagMetadata metadata)
```

#### Parameters

`metadata` DagMetadata

The DAG metadata to serialize

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

JSON string representation

