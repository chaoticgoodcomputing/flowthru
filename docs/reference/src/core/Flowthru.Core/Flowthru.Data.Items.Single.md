# <a id="Flowthru_Data_Items_Single"></a> Class Items.Single

Namespace: [Flowthru.Data](Flowthru.Data.md)  
Assembly: Flowthru.Core.dll  

Factory methods for single (non-collection) values.

```csharp
public static class Items.Single
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Items.Single](Flowthru.Data.Items.Single.md)

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
These methods create catalog items for single objects rather than collections.
</p>
<p>
<strong>Use Cases:</strong>
</p>
<ul><li>Model files (ML models, configuration objects)</li><li>Metrics and evaluation results (single JSON objects)</li><li>Text reports (Markdown, plain text)</li><li>Binary files (images, PDFs)</li><li>Side-effect-only steps (null/void semantics)</li></ul>

## Methods

### <a id="Flowthru_Data_Items_Single_Binary_System_String_System_String_"></a> Binary\(string, string\)

Creates a binary file catalog item.

```csharp
public static Item<byte[]> Binary(string label, string filePath)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to binary file (.png, .jpg, .pdf, etc.)

#### Returns

 [Item](Flowthru.Data.Item\-1.md)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]\>

Catalog item for binary file with byte array content

#### Remarks

<p>
<strong>Use Case:</strong> Images (PNG, JPG), PDFs, any binary data
</p>
<p>
<strong>Implementation:</strong> Reads entire file as byte array.
</p>
<p>
<strong>Storage Traits:</strong> All traits use filesystem baseline defaults
</p>

### <a id="Flowthru_Data_Items_Single_Json__1_System_String_System_String_"></a> Json<T\>\(string, string\)

Creates a JSON file catalog item for a single object (non-collection).

```csharp
public static Item<T> Json<T>(string label, string filePath) where T : IStructuredSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to JSON file

#### Returns

 [Item](Flowthru.Data.Item\-1.md)<T\>

Catalog item for singleton JSON object

#### Type Parameters

`T` 

Object type (must be structured-serializable)

#### Remarks

<p>
<strong>Use Case:</strong> Model files, configuration objects, metrics, single records
</p>
<p>
<strong>Serialization:</strong> Single JSON object (not wrapped in array)
</p>
<p>
<strong>Implementation:</strong> Uses SingletonJsonStorageAdapter which bypasses
format/container composition for direct object serialization.
</p>

### <a id="Flowthru_Data_Items_Single_Memory__1_System_String_"></a> Memory<T\>\(string\)

Creates a memory catalog item for a single object (non-collection).

```csharp
public static Item<T> Memory<T>(string label)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

#### Returns

 [Item](Flowthru.Data.Item\-1.md)<T\>

Catalog item for in-memory singleton

#### Type Parameters

`T` 

Object type

#### Remarks

<p>
<strong>Use Case:</strong> Models, charts, computed metrics that stay in memory
</p>
<p>
<strong>Examples:</strong>
</p>
<ul><li>ML models (LinearRegressionModel)</li><li>Charts (GenericChart from Plotly.NET)</li><li>Metrics objects (ModelMetrics, CrossValidationResults)</li><li>Any singleton data that doesn't need persistence</li></ul>

### <a id="Flowthru_Data_Items_Single_Null__1_System_String_"></a> Null<T\>\(string\)

Creates a null catalog item for side-effect-only steps.

```csharp
public static Item<T> Null<T>(string label)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

#### Returns

 [Item](Flowthru.Data.Item\-1.md)<T\>

Catalog item for void/no-data semantics

#### Type Parameters

`T` 

The data type (typically NoData)

#### Remarks

<p>
<strong>Use Case:</strong> Steps that perform side effects (logging, visualization) without producing meaningful data
</p>
<p>
<strong>Implementation:</strong> Uses NullStorageAdapter which performs no I/O operations.
</p>
<p>
<strong>Storage Traits:</strong>
</p>
<ul><li>CanWrite: false (Save is a no-op)</li><li>CanRead: false (Load throws NotSupportedException)</li></ul>

### <a id="Flowthru_Data_Items_Single_Text_System_String_System_String_"></a> Text\(string, string\)

Creates a plain text file catalog item .

```csharp
public static Item<string> Text(string label, string filePath)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to text file (.txt, .md, etc.)

#### Returns

 [Item](Flowthru.Data.Item\-1.md)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

Catalog item for text file with string content

#### Remarks

<p>
<strong>Use Case:</strong> Markdown reports, plain text logs, configuration files
</p>
<p>
<strong>Implementation:</strong> Reads entire file as single string.
</p>
<p>
<strong>Storage Traits:</strong> All traits use filesystem baseline defaults
</p>

