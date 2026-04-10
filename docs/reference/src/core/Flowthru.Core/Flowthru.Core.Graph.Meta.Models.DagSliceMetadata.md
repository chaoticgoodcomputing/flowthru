# <a id="Flowthru_Core_Graph_Meta_Models_DagSliceMetadata"></a> Class DagSliceMetadata

Namespace: [Flowthru.Core.Graph.Meta.Models](Flowthru.Core.Graph.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

Metadata describing how a flow was sliced during execution.

```csharp
public class DagSliceMetadata
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DagSliceMetadata](Flowthru.Core.Graph.Meta.Models.DagSliceMetadata.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Captures the criteria used to select a subset of steps from the full flow DAG.
This information is essential for:
<ul><li>Reproducibility - rerun the exact same slice</li><li>Debugging - understand what was included/excluded when failures occur</li><li>Auditing - track which flow subsets were executed in production</li><li>Visualization - indicate sliced vs. full DAG in metadata exports</li></ul>

## Properties

### <a id="Flowthru_Core_Graph_Meta_Models_DagSliceMetadata_Flows"></a> Flows

Flow names used to filter the merged DAG.

```csharp
[JsonPropertyName("flows")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string[]? Flows { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

### <a id="Flowthru_Core_Graph_Meta_Models_DagSliceMetadata_From"></a> From

Node labels from which the slice expanded downstream. Each label may be a step
label or a catalog item label (resolved to its consumer steps).

```csharp
[JsonPropertyName("from")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string[]? From { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

### <a id="Flowthru_Core_Graph_Meta_Models_DagSliceMetadata_Only"></a> Only

Explicit allowlist of node labels (with upstream dependencies auto-included). Each label
may be a step label or a catalog item label (resolved to its producer step).

```csharp
[JsonPropertyName("only")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string[]? Only { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

### <a id="Flowthru_Core_Graph_Meta_Models_DagSliceMetadata_To"></a> To

Node labels to which the slice expanded upstream. Each label may be a step
label or a catalog item label (resolved to its producer step).

```csharp
[JsonPropertyName("to")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string[]? To { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

