# <a id="Flowthru_Core_Graph_Meta_Models_DagMetadata"></a> Class DagMetadata

Namespace: [Flowthru.Core.Graph.Meta.Models](Flowthru.Core.Graph.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

Root metadata model representing a complete FlowthruService DAG (Directed Acyclic Graph).

```csharp
public class DagMetadata
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DagMetadata](Flowthru.Core.Graph.Meta.Models.DagMetadata.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

#### Extension Methods

[MetadataJsonExtensions.ToCompactJson\(DagMetadata\)](Flowthru.Core.Meta.MetadataJsonExtensions.md\#Flowthru\_Core\_Meta\_MetadataJsonExtensions\_ToCompactJson\_Flowthru\_Core\_Graph\_Meta\_Models\_DagMetadata\_), 
[MetadataJsonExtensions.ToJson\(DagMetadata\)](Flowthru.Core.Meta.MetadataJsonExtensions.md\#Flowthru\_Core\_Meta\_MetadataJsonExtensions\_ToJson\_Flowthru\_Core\_Graph\_Meta\_Models\_DagMetadata\_), 
[MermaidMetadataExtensions.ToMermaidDiagram\(DagMetadata, string, string, string\)](Flowthru.Core.Meta.MermaidMetadataExtensions.md\#Flowthru\_Core\_Meta\_MermaidMetadataExtensions\_ToMermaidDiagram\_Flowthru\_Core\_Graph\_Meta\_Models\_DagMetadata\_System\_String\_System\_String\_System\_String\_)

## Remarks

This model captures the structure of a built flow, including all steps,
catalog items, and their relationships. It serves as the backbone for
Flowthru.Core.Viz visualization.

## Properties

### <a id="Flowthru_Core_Graph_Meta_Models_DagMetadata_AppliedSlice"></a> AppliedSlice

Slice criteria applied to generate this DAG, if any.

```csharp
[JsonPropertyName("appliedSlice")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public DagSliceMetadata? AppliedSlice { get; init; }
```

#### Property Value

 [DagSliceMetadata](Flowthru.Core.Graph.Meta.Models.DagSliceMetadata.md)?

#### Remarks

Present when the DAG represents a sliced subset of the full Flow.
Null when the DAG represents the complete, unsliced flow.
Used for reproducibility, debugging, and filename generation.

### <a id="Flowthru_Core_Graph_Meta_Models_DagMetadata_CatalogItems"></a> CatalogItems

All catalog items involved in the flow.

```csharp
[JsonPropertyName("catalogItems")]
public List<ItemMetadata> CatalogItems { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[ItemMetadata](Flowthru.Core.Graph.Meta.Models.ItemMetadata.md)\>

### <a id="Flowthru_Core_Graph_Meta_Models_DagMetadata_Edges"></a> Edges

All edges representing data Flow in the DAG.

```csharp
[JsonPropertyName("edges")]
public List<EdgeMetadata> Edges { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[EdgeMetadata](Flowthru.Core.Graph.Meta.Models.EdgeMetadata.md)\>

#### Remarks

Edges connect catalog items to steps and steps to catalog items,
forming the complete graph.

### <a id="Flowthru_Core_Graph_Meta_Models_DagMetadata_FlowName"></a> FlowName

Name of the Flow this DAG represents.

```csharp
[JsonPropertyName("flowName")]
public required string FlowName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Graph_Meta_Models_DagMetadata_GeneratedAt"></a> GeneratedAt

Timestamp when this metadata was generated.

```csharp
[JsonPropertyName("generatedAt")]
public DateTime GeneratedAt { get; init; }
```

#### Property Value

 [DateTime](https://learn.microsoft.com/dotnet/api/system.datetime)

### <a id="Flowthru_Core_Graph_Meta_Models_DagMetadata_SlicedCatalogItemIds"></a> SlicedCatalogItemIds

Catalog item IDsthat are produced by steps in the active execution slice.

```csharp
[JsonPropertyName("slicedCatalogItemIds")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public HashSet<string>? SlicedCatalogItemIds { get; init; }
```

#### Property Value

 [HashSet](https://learn.microsoft.com/dotnet/api/system.collections.generic.hashset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

When a slice is applied, this contains the keys of catalog items (data) that
will be written during execution. Derived from the outputs of sliced steps.
Null when no slice was applied (all data may be updated).
Enables visualization tools to highlight both steps and the data they produce.

### <a id="Flowthru_Core_Graph_Meta_Models_DagMetadata_SlicedStepIds"></a> SlicedStepIds

Step IDs that are in the active execution slice, if a slice was applied.

```csharp
[JsonPropertyName("slicedStepIds")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public HashSet<string>? SlicedStepIds { get; init; }
```

#### Property Value

 [HashSet](https://learn.microsoft.com/dotnet/api/system.collections.generic.hashset\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Remarks

When a slice is applied, this contains the IDs of steps that will actually execute.
The Steps collection contains the full DAG, while this set identifies the subset.
Null when no slice was applied (all steps execute).
Enables visualization tools to highlight execution paths while showing full context.

### <a id="Flowthru_Core_Graph_Meta_Models_DagMetadata_Steps"></a> Steps

All steps in the Flow with their metadata.

```csharp
[JsonPropertyName("steps")]
public List<StepMetadata> Steps { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[StepMetadata](Flowthru.Core.Graph.Meta.Models.StepMetadata.md)\>

