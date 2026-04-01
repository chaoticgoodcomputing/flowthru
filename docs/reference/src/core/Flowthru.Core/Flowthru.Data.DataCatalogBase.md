# <a id="Flowthru_Data_DataCatalogBase"></a> Class DataCatalogBase

Namespace: [Flowthru.Data](Flowthru.Data.md)  
Assembly: Flowthru.Core.dll  

Base class for strongly-typed catalog implementations with automatic property caching.

```csharp
public abstract class DataCatalogBase
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DataCatalogBase](Flowthru.Data.DataCatalogBase.md)

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
<strong>Problem Solved:</strong>
Expression-bodied properties (<code>Property =&gt; new Entry()</code>) create new instances on each access,
breaking DAG dependency resolution which relies on object identity.
</p>
<p>
<strong>Solution:</strong>
Uses reflection to:
1. Discover all ICatalogEntry properties on derived classes
2. Create backing fields to cache instances
3. Intercept property getters to return cached instances
</p>
<p>
<strong>Usage Pattern:</strong>
<pre><code class="lang-csharp">public class MyCatalog : DataCatalogBase
{
    public MyCatalog(string basePath = "Data") : base()
    {
        BasePath = basePath;
        InitializeCatalogProperties();
    }

    protected string BasePath { get; }

    // Declare once - automatically cached!
    public ICatalogEntry&lt;IEnumerable&lt;MyData&gt;&gt; MyData =&gt;
        GetOrCreateEntry(() =&gt; new CsvCatalogEntry&lt;MyData&gt;("my_data", $"{BasePath}/data.csv"));
}</code></pre>
</p>
<p>
<strong>Key Benefits:</strong>
- Declare catalog entries ONCE (no redundant constructor code)
- Automatic instance caching (object identity preserved)
- Type-safe (compile-time checks)
- Zero runtime overhead after first access (cached delegates)
</p>

## Constructors

### <a id="Flowthru_Data_DataCatalogBase__ctor_System_String_"></a> DataCatalogBase\(string?\)

```csharp
protected DataCatalogBase(string? catalogLabel = null)
```

#### Parameters

`catalogLabel` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional display label for this catalog instance. When omitted, defaults to the
concrete class name via <code>GetType().Name</code>.

## Properties

### <a id="Flowthru_Data_DataCatalogBase_CatalogLabel"></a> CatalogLabel

The display label used to identify this catalog instance in pipeline metadata.
Defaults to the concrete class name when not specified.

```csharp
public string CatalogLabel { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Pass an explicit label when constructing multiple instances of the same catalog type
in a single pipeline (e.g., per-partition or per-shard catalogs) so their entries
receive distinct qualified identifiers in the DAG: <code>CatalogLabel.EntryLabel</code>.

### <a id="Flowthru_Data_DataCatalogBase_Services"></a> Services

Optional service provider for dependency injection into catalog entries.

```csharp
public IServiceProvider? Services { get; set; }
```

#### Property Value

 [IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider)?

#### Remarks

Set by the service layer before pipeline execution to enable catalog
entries to resolve services (e.g., database connections, HTTP clients).

## Methods

### <a id="Flowthru_Data_DataCatalogBase_ClearCache"></a> ClearCache\(\)

Clears the property cache. Use with caution!

```csharp
protected void ClearCache()
```

#### Remarks

<p>
<strong>Warning:</strong> Clearing the cache after pipeline construction will break
DAG dependencies since new instances will be created on next access.
</p>
<p>
<strong>Use Case:</strong> Primarily for testing scenarios where you need to reset
catalog state between test runs.
</p>

### <a id="Flowthru_Data_DataCatalogBase_GetAllEntries"></a> GetAllEntries\(\)

Gets all cached catalog entries.

```csharp
protected IEnumerable<ICatalogEntry> GetAllEntries()
```

#### Returns

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[ICatalogEntry](Flowthru.Data.ICatalogEntry.md)\>

Enumerable of all initialized catalog entries

#### Remarks

Useful for diagnostic purposes or when you need to iterate over all entries
(e.g., for validation, cleanup, or reporting).

### <a id="Flowthru_Data_DataCatalogBase_GetOrCreateEntry__1_System_Func_Flowthru_Data_ICatalogEntry___0___System_String_"></a> GetOrCreateEntry<T\>\(Func<ICatalogEntry<T\>\>, string\)

Gets or creates a unified catalog entry, caching it for subsequent accesses.

```csharp
protected ICatalogEntry<T> GetOrCreateEntry<T>(Func<ICatalogEntry<T>> factory, string propertyName = "")
```

#### Parameters

`factory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<T\>\>

Factory function to create the entry on first access

`propertyName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Auto-populated by compiler with calling property name

#### Returns

 [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<T\>

Cached catalog entry instance

#### Type Parameters

`T` 

The data type stored in this catalog entry.
For singletons: Use T directly (e.g., LinearRegressionModel)
For collections: Use IEnumerable&lt;T&gt; (e.g., IEnumerable&lt;FeatureRow&gt;)

#### Remarks

<p>
<strong>Unified API (v0.5.0):</strong> This single method replaces GetOrCreateObject
and GetOrCreateDataset. Cardinality is determined by the type parameter T.
</p>
<p>
<strong>Usage Examples:</strong>
<pre><code class="lang-csharp">// Singleton object
public ICatalogEntry&lt;LinearRegressionModel&gt; Model =&gt;
    GetOrCreateEntry(() =&gt; CatalogEntries.Single.Memory&lt;LinearRegressionModel&gt;("model"));

// Collection
public ICatalogEntry&lt;IEnumerable&lt;FeatureRow&gt;&gt; Features =&gt;
    GetOrCreateEntry(() =&gt; CatalogEntries.Enumerable.Csv&lt;FeatureRow&gt;("features", "data.csv"));</code></pre>
</p>

### <a id="Flowthru_Data_DataCatalogBase_GetOrCreateEntry__1_System_Func_System_IServiceProvider_Flowthru_Data_ICatalogEntry___0___System_String_"></a> GetOrCreateEntry<T\>\(Func<IServiceProvider?, ICatalogEntry<T\>\>, string\)

Gets or creates a unified catalog entry with service provider access.

```csharp
protected ICatalogEntry<T> GetOrCreateEntry<T>(Func<IServiceProvider?, ICatalogEntry<T>> factory, string propertyName = "")
```

#### Parameters

`factory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider)?, [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<T\>\>

Factory function that receives service provider

`propertyName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Auto-populated by compiler with calling property name

#### Returns

 [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<T\>

Cached catalog entry instance

#### Type Parameters

`T` 

The data type (singleton or collection)

### <a id="Flowthru_Data_DataCatalogBase_InitializeCatalogProperties"></a> InitializeCatalogProperties\(\)

Initializes all catalog entry properties by invoking their getters once.

```csharp
protected void InitializeCatalogProperties()
```

#### Remarks

<p>
<strong>Purpose:</strong> Eager initialization ensures all entries are cached
before pipeline construction begins, preventing any potential race conditions
or unexpected lazy initialization behavior.
</p>
<p>
<strong>When to Call:</strong> At the end of the derived catalog's constructor,
after all configuration properties (like BasePath) are set.
</p>
<p>
<strong>How It Works:</strong>
Uses reflection to find all public instance properties that return ICatalogEntry,
then invokes each getter once to populate the cache.
</p>

