# <a id="Flowthru_Data_Storage_Strategies_DatabaseStorageEntryFactory"></a> Class DatabaseStorageEntryFactory

Namespace: [Flowthru.Data.Storage.Strategies](Flowthru.Data.Storage.Strategies.md)  
Assembly: Flowthru.Core.dll  

Database-backed storage strategy for production environments.

```csharp
public sealed class DatabaseStorageEntryFactory : IStorageEntryFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DatabaseStorageEntryFactory](Flowthru.Data.Storage.Strategies.DatabaseStorageEntryFactory.md)

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
<strong>⚠️ STUB IMPLEMENTATION - Phase 2</strong>
</p>
<p>
This is a placeholder for future database support. Currently throws
NotImplementedException for all operations.
</p>
<p>
<strong>Planned Features:</strong>
</p>
<ul><li>SQL Server, PostgreSQL, SQLite support</li><li>Connection pooling and retry logic</li><li>Schema migration support</li><li>Transaction coordination with pipelines</li></ul>
<p>
<strong>Proposed Usage:</strong>
</p>
<pre><code class="lang-csharp">services.AddFlowthru(flowthru =&gt;
{
    flowthru.RegisterCatalog&lt;MyCatalog&gt;();

    if (env.IsProduction())
    {
        flowthru.UseStorageStrategy&lt;DatabaseStorageEntryFactory&gt;();
    }
});</code></pre>

## Constructors

### <a id="Flowthru_Data_Storage_Strategies_DatabaseStorageEntryFactory__ctor_Microsoft_Extensions_Configuration_IConfiguration_"></a> DatabaseStorageEntryFactory\(IConfiguration\)

Initializes a new database storage factory.

```csharp
public DatabaseStorageEntryFactory(IConfiguration configuration)
```

#### Parameters

`configuration` [IConfiguration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfiguration)

Configuration containing connection string

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if database connection string is not configured

### <a id="Flowthru_Data_Storage_Strategies_DatabaseStorageEntryFactory__ctor_System_String_System_String_"></a> DatabaseStorageEntryFactory\(string, string\)

Initializes a new database storage factory with explicit settings.

```csharp
public DatabaseStorageEntryFactory(string connectionString, string schema = "dbo")
```

#### Parameters

`connectionString` [string](https://learn.microsoft.com/dotnet/api/system.string)

Database connection string

`schema` [string](https://learn.microsoft.com/dotnet/api/system.string)

Default schema for tables

## Methods

### <a id="Flowthru_Data_Storage_Strategies_DatabaseStorageEntryFactory_CreateEnumerable__1_System_String_Flowthru_Data_Storage_Strategies_StorageOptions_"></a> CreateEnumerable<T\>\(string, StorageOptions?\)

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

#### Exceptions

 [NotImplementedException](https://learn.microsoft.com/dotnet/api/system.notimplementedexception)

Phase 2 stub - database support not yet implemented

### <a id="Flowthru_Data_Storage_Strategies_DatabaseStorageEntryFactory_CreateSingle__1_System_String_Flowthru_Data_Storage_Strategies_StorageOptions_"></a> CreateSingle<T\>\(string, StorageOptions?\)

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

#### Exceptions

 [NotImplementedException](https://learn.microsoft.com/dotnet/api/system.notimplementedexception)

Phase 2 stub - database support not yet implemented

