# <a id="Flowthru_Core_Graph_Meta_Models_SchemaField"></a> Class SchemaField

Namespace: [Flowthru.Core.Graph.Meta.Models](Flowthru.Core.Graph.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

A single field (property) in a schema.

```csharp
public class SchemaField
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SchemaField](Flowthru.Core.Graph.Meta.Models.SchemaField.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Core_Graph_Meta_Models_SchemaField_IsNullable"></a> IsNullable

Whether the property can be null.

```csharp
[JsonPropertyName("isNullable")]
public bool IsNullable { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Determined by nullable reference types (string?) or nullable value types (int?).

### <a id="Flowthru_Core_Graph_Meta_Models_SchemaField_Name"></a> Name

Name of the property.

```csharp
[JsonPropertyName("name")]
public required string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Example: "Id", "Name", "IataApproved"

### <a id="Flowthru_Core_Graph_Meta_Models_SchemaField_Type"></a> Type

C# type name of the property.

```csharp
[JsonPropertyName("type")]
public required string Type { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Simple type name without namespace.
Example: "string", "int", "DateTime", "double"

