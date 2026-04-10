# <a id="Flowthru_Core_Graph_Meta_Models_EdgeMetadata"></a> Class EdgeMetadata

Namespace: [Flowthru.Core.Graph.Meta.Models](Flowthru.Core.Graph.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

Metadata describing an edge in the pipeline DAG.

```csharp
public class EdgeMetadata
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EdgeMetadata](Flowthru.Core.Graph.Meta.Models.EdgeMetadata.md)

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
Edges represent data Flow between catalog entries and nodes. The DAG contains
two types of edges:
</p>
<ul><li><strong>Catalog → Step:</strong> A node reads from a catalog entry</li><li><strong>Step → Catalog:</strong> A node writes to a catalog entry</li></ul>
<p>
Together, these edges form the complete data flow:
<code>Item → Step → Item → Step → ...</code>
</p>

## Properties

### <a id="Flowthru_Core_Graph_Meta_Models_EdgeMetadata_DataType"></a> DataType

C# type name of data flowing through this edge.

```csharp
[JsonPropertyName("dataType")]
public required string DataType { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Simple type name without namespace.
Example: "Company", "Shuttle", "ModelInput"

### <a id="Flowthru_Core_Graph_Meta_Models_EdgeMetadata_Source"></a> Source

Source identifier (either a catalog entry key or node ID).

```csharp
[JsonPropertyName("source")]
public required string Source { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

For Catalog → Step edges, this is a catalog entry key.
For Step → Catalog edges, this is a node ID.

### <a id="Flowthru_Core_Graph_Meta_Models_EdgeMetadata_Target"></a> Target

Target identifier (either a node ID or catalog entry key).

```csharp
[JsonPropertyName("target")]
public required string Target { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

For Catalog → Step edges, this is a node ID.
For Step → Catalog edges, this is a catalog entry key.

