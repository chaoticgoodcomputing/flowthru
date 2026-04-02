# <a id="Flowthru_Meta_Models_ItemMetadata"></a> Class ItemMetadata

Namespace: [Flowthru.Meta.Models](Flowthru.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

Metadata describing a single catalog item (dataset, json object, API response, whatever) in the flow.

```csharp
public class ItemMetadata
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ItemMetadata](Flowthru.Meta.Models.ItemMetadata.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Catalog items represent data sources and sinks. They can be external files,
intermediate flow outputs, or final results. Each item is uniquely identified
by its key.

## Properties

### <a id="Flowthru_Meta_Models_ItemMetadata_Consumers"></a> Consumers

List of step IDs that consume (read from) this catalog item.

```csharp
[JsonPropertyName("consumers")]
public List<string> Consumers { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Remarks

Empty for item outputs that aren't consumed by other steps.
Example: ["CreateModelInputTable", "ValidateData"]

### <a id="Flowthru_Meta_Models_ItemMetadata_DataType"></a> DataType

The C# type name of data stored in this catalog item.

```csharp
[JsonPropertyName("dataType")]
public required string DataType { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Simple type name without namespace.
Example: "Company", "Shuttle", "ModelInput"

### <a id="Flowthru_Meta_Models_ItemMetadata_Fields"></a> Fields

Additional metadata fields specific to the catalog item type.

```csharp
[JsonPropertyName("fields")]
public Dictionary<string, object> Fields { get; init; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [object](https://learn.microsoft.com/dotnet/api/system.object)\>

#### Remarks

<p>Examples of fields:</p>
<ul><li><code>filepath</code>: Path to file for file-based datasets</li><li><code>catalogType</code>: Type of catalog dataset (CsvCatalogDataset, ParquetCatalogDataset, etc.)</li><li><code>isReadOnly</code>: Whether the dataset is read-only</li><li><code>inspectionLevel</code>: Validation inspection level (None, Shallow, Deep)</li></ul>

### <a id="Flowthru_Meta_Models_ItemMetadata_Key"></a> Key

Unique key identifying this catalog item.

```csharp
[JsonPropertyName("key")]
public required string Key { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Corresponds to the catalog property name or explicitly set key.
Example: "Companies", "CleanedCompanies", "ModelInputTable"

### <a id="Flowthru_Meta_Models_ItemMetadata_Label"></a> Label

Human-readable display label for this catalog item.

```csharp
[JsonPropertyName("label")]
public required string Label { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

May be formatted for better display in Flowthru.Viz.
Example: "Companies", "Cleaned Companies", "Model Input Table"

### <a id="Flowthru_Meta_Models_ItemMetadata_Producer"></a> Producer

Step ID that produces (writes to) this catalog item.

```csharp
[JsonPropertyName("producer")]
public string? Producer { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

Null for external inputs (Layer 0 inputs that exist before flow execution).
Example: "PreprocessCompanies"

### <a id="Flowthru_Meta_Models_ItemMetadata_Schema"></a> Schema

Schema information inferred from the data type.

```csharp
[JsonPropertyName("schema")]
public SchemaMetadata? Schema { get; init; }
```

#### Property Value

 [SchemaMetadata](Flowthru.Meta.Models.SchemaMetadata.md)?

#### Remarks

Null for simple types or when schema inference fails.
Contains property names, types, and nullability for complex types.

