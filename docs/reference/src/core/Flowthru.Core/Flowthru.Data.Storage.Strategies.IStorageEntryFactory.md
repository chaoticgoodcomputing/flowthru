# <a id="Flowthru_Data_Storage_Strategies_IStorageEntryFactory"></a> Interface IStorageEntryFactory

Namespace: [Flowthru.Data.Storage.Strategies](Flowthru.Data.Storage.Strategies.md)  
Assembly: Flowthru.Core.dll  

Factory for creating catalog entries with environment-specific storage.

```csharp
public interface IStorageEntryFactory
```

## Remarks

<p>
The strategy pattern enables the same catalog to use different storage
backends based on the environment:
</p>
<ul><li><strong>Development:</strong> CSV files for easy inspection and version control</li><li><strong>Production:</strong> Database tables for scalability and transactions</li><li><strong>Testing:</strong> In-memory storage for fast, isolated tests</li></ul>
<p>
<strong>Usage Pattern:</strong>
</p>
<pre><code class="lang-csharp">public class MyCatalog : DataCatalogBase
{
    private readonly IStorageEntryFactory _storage;

    public MyCatalog(IStorageEntryFactory storage)
    {
        _storage = storage;
        InitializeCatalogProperties();
    }

    public IItem&lt;IEnumerable&lt;Company&gt;&gt; Companies =&gt;
        GetOrCreateEntry(() =&gt; _storage.CreateEnumerable&lt;Company&gt;("Companies"));
}</code></pre>

## Methods

### <a id="Flowthru_Data_Storage_Strategies_IStorageEntryFactory_CreateEnumerable__1_System_String_Flowthru_Data_Storage_Strategies_StorageOptions_"></a> CreateEnumerable<T\>\(string, StorageOptions?\)

Creates a catalog entry for an enumerable dataset.

```csharp
IItem<IEnumerable<T>> CreateEnumerable<T>(string label, StorageOptions? options = null) where T : notnull, IFlatSchema, ITextSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog label for the entry

`options` [StorageOptions](Flowthru.Data.Storage.Strategies.StorageOptions.md)?

Optional storage options

#### Returns

 [IItem](Flowthru.Data.IItem\-1.md)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

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

### <a id="Flowthru_Data_Storage_Strategies_IStorageEntryFactory_CreateSingle__1_System_String_Flowthru_Data_Storage_Strategies_StorageOptions_"></a> CreateSingle<T\>\(string, StorageOptions?\)

Creates a catalog entry for a singleton object.

```csharp
IItem<T> CreateSingle<T>(string label, StorageOptions? options = null) where T : IStructuredSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog label for the entry

`options` [StorageOptions](Flowthru.Data.Storage.Strategies.StorageOptions.md)?

Optional storage options

#### Returns

 [IItem](Flowthru.Data.IItem\-1.md)<T\>

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

