# <a id="Flowthru_Meta_Models_DagMetadata"></a> Class DagMetadata

Namespace: [Flowthru.Meta.Models](Flowthru.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

Root metadata model representing a complete pipeline DAG (Directed Acyclic Graph).

```csharp
public class DagMetadata
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DagMetadata](Flowthru.Meta.Models.DagMetadata.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

#### Extension Methods

[MetadataJsonExtensions.ToCompactJson\(DagMetadata\)](Flowthru.Meta.MetadataJsonExtensions.md\#Flowthru\_Meta\_MetadataJsonExtensions\_ToCompactJson\_Flowthru\_Meta\_Models\_DagMetadata\_), 
[MetadataJsonExtensions.ToJson\(DagMetadata\)](Flowthru.Meta.MetadataJsonExtensions.md\#Flowthru\_Meta\_MetadataJsonExtensions\_ToJson\_Flowthru\_Meta\_Models\_DagMetadata\_), 
[MermaidMetadataExtensions.ToMermaidDiagram\(DagMetadata, string, string, string\)](Flowthru.Meta.MermaidMetadataExtensions.md\#Flowthru\_Meta\_MermaidMetadataExtensions\_ToMermaidDiagram\_Flowthru\_Meta\_Models\_DagMetadata\_System\_String\_System\_String\_System\_String\_)

## Remarks

This model captures the structure of a built pipeline, including all nodes,
catalog entries, and their relationships. It serves as the backbone for
Flowthru.Viz visualization.

## Properties

### <a id="Flowthru_Meta_Models_DagMetadata_AppliedSlice"></a> AppliedSlice

Slice criteria applied to generate this DAG, if any.

```csharp
[JsonPropertyName("appliedSlice")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public DagSliceMetadata? AppliedSlice { get; init; }
```

#### Property Value

 [DagSliceMetadata](Flowthru.Meta.Models.DagSliceMetadata.md)?

#### Remarks

Present when the DAG represents a sliced subset of the full pipeline.
Null when the DAG represents the complete, unsliced pipeline.
Used for reproducibility, debugging, and filename generation.

### <a id="Flowthru_Meta_Models_DagMetadata_CatalogEntries"></a> CatalogEntries

All catalog entries (datasets) involved in the pipeline.

```csharp
[JsonPropertyName("catalogEntries")]
public List<CatalogEntryMetadata> CatalogEntries { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[CatalogEntryMetadata](Flowthru.Meta.Models.CatalogEntryMetadata.md)\>

### <a id="Flowthru_Meta_Models_DagMetadata_Edges"></a> Edges

All edges representing data flow in the DAG.

```csharp
[JsonPropertyName("edges")]
public List<EdgeMetadata> Edges { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[EdgeMetadata](Flowthru.Meta.Models.EdgeMetadata.md)\>

#### Remarks

Edges connect catalog entries to nodes and nodes to catalog entries,
forming the complete data flow graph.

### <a id="Flowthru_Meta_Models_DagMetadata_GeneratedAt"></a> GeneratedAt

Timestamp when this metadata was generated.

```csharp
[JsonPropertyName("generatedAt")]
public DateTime GeneratedAt { get; init; }
```

#### Property Value

 [DateTime](https://learn.microsoft.com/dotnet/api/system.datetime)

### <a id="Flowthru_Meta_Models_DagMetadata_Nodes"></a> Nodes

All nodes in the pipeline with their metadata.

```csharp
[JsonPropertyName("nodes")]
public List<NodeMetadata> Nodes { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[NodeMetadata](Flowthru.Meta.Models.NodeMetadata.md)\>

### <a id="Flowthru_Meta_Models_DagMetadata_PipelineName"></a> PipelineName

Name of the pipeline this DAG represents.

```csharp
[JsonPropertyName("pipelineName")]
public required string PipelineName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Meta_Models_DagMetadata_SlicedCatalogEntryKeys"></a> SlicedCatalogEntryKeys

Catalog entry keys that are produced by nodes in the active execution slice.

```csharp
[JsonPropertyName("slicedCatalogEntryKeys")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public HashSet<string>? SlicedCatalogEntryKeys { get; init; }
```

#### Property Value

 [HashSet](https://learn.microsoft.com/dotnet/api/system.collections.generic.hashset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

When a slice is applied, this contains the keys of catalog entries (data) that
will be written during execution. Derived from the outputs of sliced nodes.
Null when no slice was applied (all data may be updated).
Enables visualization tools to highlight both nodes and the data they produce.

### <a id="Flowthru_Meta_Models_DagMetadata_SlicedNodeIds"></a> SlicedNodeIds

Node IDs that are in the active execution slice, if a slice was applied.

```csharp
[JsonPropertyName("slicedNodeIds")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public HashSet<string>? SlicedNodeIds { get; init; }
```

#### Property Value

 [HashSet](https://learn.microsoft.com/dotnet/api/system.collections.generic.hashset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

When a slice is applied, this contains the IDs of nodes that will actually execute.
The Nodes collection contains the full DAG, while this set identifies the subset.
Null when no slice was applied (all nodes execute).
Enables visualization tools to highlight execution paths while showing full context.

