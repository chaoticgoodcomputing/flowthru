# <a id="Flowthru_Data_Storage_Strategies_CsvStorageEntryFactory"></a> Class CsvStorageEntryFactory

Namespace: [Flowthru.Data.Storage.Strategies](Flowthru.Data.Storage.Strategies.md)  
Assembly: Flowthru.Extensions.Csv.dll  

CSV file-based storage strategy for local development.

```csharp
public sealed class CsvStorageEntryFactory : IStorageEntryFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CsvStorageEntryFactory](Flowthru.Data.Storage.Strategies.CsvStorageEntryFactory.md)

#### Implements

IStorageEntryFactory

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Uses CSV files for data storage, enabling:
</p>
<ul><li>Easy inspection with text editors or spreadsheet tools</li><li>Version control-friendly (human-readable diffs)</li><li>No external dependencies (no database required)</li></ul>
<p>
<strong>Path Resolution:</strong>
</p>
<pre><code class="lang-csharp">// With explicit path
factory.CreateEnumerable&lt;Company&gt;("Companies",
    StorageOptions.WithPath("_01_Raw/data.csv"))
// → {BasePath}/_01_Raw/data.csv

// With default path (label-based)
factory.CreateEnumerable&lt;Company&gt;("Companies")
// → {BasePath}/Companies.csv</code></pre>

## Constructors

### <a id="Flowthru_Data_Storage_Strategies_CsvStorageEntryFactory__ctor_Microsoft_Extensions_Configuration_IConfiguration_"></a> CsvStorageEntryFactory\(IConfiguration\)

Initializes a new CSV storage factory.

```csharp
public CsvStorageEntryFactory(IConfiguration configuration)
```

#### Parameters

`configuration` [IConfiguration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfiguration)

Configuration containing optional DataPath setting

### <a id="Flowthru_Data_Storage_Strategies_CsvStorageEntryFactory__ctor_System_String_"></a> CsvStorageEntryFactory\(string\)

Initializes a new CSV storage factory with explicit base path.

```csharp
public CsvStorageEntryFactory(string basePath)
```

#### Parameters

`basePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Base directory for all CSV files

## Methods

### <a id="Flowthru_Data_Storage_Strategies_CsvStorageEntryFactory_CreateEnumerable__1_System_String_Flowthru_Data_Storage_Strategies_StorageOptions_"></a> CreateEnumerable<T\>\(string, StorageOptions?\)

Creates a catalog entry for an enumerable dataset.

```csharp
public ICatalogEntry<IEnumerable<T>> CreateEnumerable<T>(string label, StorageOptions? options = null) where T : notnull, IFlatSchema, ITextSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog label for the entry

`options` StorageOptions?

Optional storage options

#### Returns

 ICatalogEntry<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

Configured catalog entry

#### Type Parameters

`T` 

Schema type (must implement IFlatSchema and ITextSerializable)

#### Remarks

<p>
Type constraints ensure compatibility with CSV and Parquet serialization.
Memory storage also works since it has no serialization requirements.
</p>
<p>
If options.Path is null, the label is used to derive a default path
(e.g., "Companies" → "Companies.csv" or "dbo.Companies").
</p>

### <a id="Flowthru_Data_Storage_Strategies_CsvStorageEntryFactory_CreateSingle__1_System_String_Flowthru_Data_Storage_Strategies_StorageOptions_"></a> CreateSingle<T\>\(string, StorageOptions?\)

Creates a catalog entry for a singleton object.

```csharp
public ICatalogEntry<T> CreateSingle<T>(string label, StorageOptions? options = null) where T : IStructuredSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog label for the entry

`options` StorageOptions?

Optional storage options

#### Returns

 ICatalogEntry<T\>

Configured catalog entry

#### Type Parameters

`T` 

Object type (must implement IStructuredSerializable)

#### Remarks

<p>
Type constraint ensures compatibility with JSON serialization.
Memory storage also works since it has no serialization requirements.
</p>
<p>
Typically uses structured formats (JSON, MessagePack) for singletons.
</p>

