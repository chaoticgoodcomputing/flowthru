# <a id="Flowthru_Meta_Models_SchemaMetadata"></a> Class SchemaMetadata

Namespace: [Flowthru.Meta.Models](Flowthru.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

Schema information for a data type.

```csharp
public class SchemaMetadata
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SchemaMetadata](Flowthru.Meta.Models.SchemaMetadata.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Extracted from C# type definitions using reflection. Describes the structure
of data flowing through catalog entries, enabling Flowthru.Viz to display
data schemas and validate type compatibility.

## Properties

### <a id="Flowthru_Meta_Models_SchemaMetadata_Fields"></a> Fields

List of fields (properties) in the schema.

```csharp
[JsonPropertyName("fields")]
public List<SchemaField> Fields { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[SchemaField](Flowthru.Meta.Models.SchemaField.md)\>

