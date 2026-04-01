# <a id="Flowthru_Data_Storage_Strategies_MemoryStorageEntryFactory"></a> Class MemoryStorageEntryFactory

Namespace: [Flowthru.Data.Storage.Strategies](Flowthru.Data.Storage.Strategies.md)  
Assembly: Flowthru.Core.dll  

In-memory storage strategy for unit tests.

```csharp
public sealed class MemoryStorageEntryFactory : IStorageEntryFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MemoryStorageEntryFactory](Flowthru.Data.Storage.Strategies.MemoryStorageEntryFactory.md)

#### Implements

[IStorageEntryFactory](Flowthru.Data.Storage.Strategies.IStorageEntryFactory.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Stores all data in memory for:
</p>
<ul><li>Fast test execution (no I/O)</li><li>Test isolation (no shared state between tests)</li><li>Simple setup (no files or databases)</li></ul>
<p>
<strong>Usage in Tests:</strong>
</p>
<pre><code class="lang-csharp">[Test]
public async Task MyTest()
{
    var storage = new MemoryStorageEntryFactory();
    var catalog = new MyCatalog(storage);

    // All data stays in memory - no files created
    await catalog.Companies.Save(companies).Run();
    var result = await catalog.Companies.Load().Run();
}</code></pre>

## Methods

### <a id="Flowthru_Data_Storage_Strategies_MemoryStorageEntryFactory_CreateEnumerable__1_System_String_Flowthru_Data_Storage_Strategies_StorageOptions_"></a> CreateEnumerable<T\>\(string, StorageOptions?\)

Creates a catalog entry for an enumerable dataset.

```csharp
public ICatalogEntry<IEnumerable<T>> CreateEnumerable<T>(string label, StorageOptions? options = null) where T : notnull, IFlatSchema, ITextSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog label for the entry

`options` [StorageOptions](Flowthru.Data.Storage.Strategies.StorageOptions.md)?

Optional storage options

#### Returns

 [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

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

### <a id="Flowthru_Data_Storage_Strategies_MemoryStorageEntryFactory_CreateSingle__1_System_String_Flowthru_Data_Storage_Strategies_StorageOptions_"></a> CreateSingle<T\>\(string, StorageOptions?\)

Creates a catalog entry for a singleton object.

```csharp
public ICatalogEntry<T> CreateSingle<T>(string label, StorageOptions? options = null) where T : IStructuredSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog label for the entry

`options` [StorageOptions](Flowthru.Data.Storage.Strategies.StorageOptions.md)?

Optional storage options

#### Returns

 [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<T\>

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

