# <a id="Flowthru_Meta_Models_DagSliceMetadata"></a> Class DagSliceMetadata

Namespace: [Flowthru.Meta.Models](Flowthru.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

Metadata describing how a pipeline was sliced during execution.

```csharp
public class DagSliceMetadata
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DagSliceMetadata](Flowthru.Meta.Models.DagSliceMetadata.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Captures the criteria used to select a subset of nodes from the full pipeline DAG.
This information is essential for:
<ul><li>Reproducibility - rerun the exact same slice</li><li>Debugging - understand what was included/excluded when failures occur</li><li>Auditing - track which pipeline subsets were executed in production</li><li>Visualization - indicate sliced vs. full DAG in metadata exports</li></ul>

## Properties

### <a id="Flowthru_Meta_Models_DagSliceMetadata_FromData"></a> FromData

Catalog entry labels whose consumers are included (expanded downstream).

```csharp
[JsonPropertyName("fromData")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string[]? FromData { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

### <a id="Flowthru_Meta_Models_DagSliceMetadata_FromNodes"></a> FromNodes

Node names from which the slice expanded downstream (dependents included).

```csharp
[JsonPropertyName("fromNodes")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string[]? FromNodes { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

### <a id="Flowthru_Meta_Models_DagSliceMetadata_OnlyNodes"></a> OnlyNodes

Explicit allowlist of node names (with dependencies auto-included).

```csharp
[JsonPropertyName("onlyNodes")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string[]? OnlyNodes { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

### <a id="Flowthru_Meta_Models_DagSliceMetadata_Pipelines"></a> Pipelines

Pipeline names to include in the merged DAG.

```csharp
[JsonPropertyName("pipelines")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string[]? Pipelines { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

### <a id="Flowthru_Meta_Models_DagSliceMetadata_ToData"></a> ToData

Catalog entry labels whose producers are included (expanded upstream).

```csharp
[JsonPropertyName("toData")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string[]? ToData { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

### <a id="Flowthru_Meta_Models_DagSliceMetadata_ToNodes"></a> ToNodes

Node names to which the slice expanded upstream (dependencies included).

```csharp
[JsonPropertyName("toNodes")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string[]? ToNodes { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

