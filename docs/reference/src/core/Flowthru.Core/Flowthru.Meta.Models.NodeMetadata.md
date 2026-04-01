# <a id="Flowthru_Meta_Models_NodeMetadata"></a> Class NodeMetadata

Namespace: [Flowthru.Meta.Models](Flowthru.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

Metadata describing a single node in the pipeline DAG.

```csharp
public class NodeMetadata
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NodeMetadata](Flowthru.Meta.Models.NodeMetadata.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Nodes are the processing units in a pipeline. Each node reads from one or more
catalog entries (inputs), performs a transformation, and writes to one or more
catalog entries (outputs).

## Properties

### <a id="Flowthru_Meta_Models_NodeMetadata_Id"></a> Id

Unique identifier for this node within the pipeline.

```csharp
[JsonPropertyName("id")]
public required string Id { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Typically the node name as defined when adding it to the pipeline.
Example: "PreprocessCompanies", "TrainModel"

### <a id="Flowthru_Meta_Models_NodeMetadata_Inputs"></a> Inputs

List of catalog entry keys this node reads from.

```csharp
[JsonPropertyName("inputs")]
public List<string> Inputs { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Remarks

For multi-input nodes using CatalogMap, this contains all mapped entries.
Example: ["Companies", "Shuttles", "Reviews"]

### <a id="Flowthru_Meta_Models_NodeMetadata_Label"></a> Label

Human-readable display label for this node.

```csharp
[JsonPropertyName("label")]
public required string Label { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

May be formatted for better display in Flowthru.Viz.
Example: "Preprocess Companies", "Train Model"

### <a id="Flowthru_Meta_Models_NodeMetadata_Layer"></a> Layer

Execution layer assigned by the dependency analyzer.

```csharp
[JsonPropertyName("layer")]
public int Layer { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Layer 0 nodes have no dependencies (read external data only).
Layer N nodes depend only on nodes in layers 0..N-1.

### <a id="Flowthru_Meta_Models_NodeMetadata_NodeType"></a> NodeType

The C# class type name implementing this node.

```csharp
[JsonPropertyName("nodeType")]
public required string NodeType { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Simple type name without namespace or generic parameters.
Example: "PreprocessCompaniesNode", "TrainModelNode"

### <a id="Flowthru_Meta_Models_NodeMetadata_Outputs"></a> Outputs

List of catalog entry keys this node writes to.

```csharp
[JsonPropertyName("outputs")]
public List<string> Outputs { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Remarks

For multi-output nodes using CatalogMap, this contains all mapped entries.
Example: ["XTrain", "XTest", "YTrain", "YTest"]

### <a id="Flowthru_Meta_Models_NodeMetadata_PipelineName"></a> PipelineName

Name of the parent pipeline this node belongs to.

```csharp
[JsonPropertyName("pipelineName")]
public required string PipelineName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Important for merged pipelines where nodes from multiple pipelines
are combined into a single DAG.

