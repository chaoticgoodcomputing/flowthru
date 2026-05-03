# <a id="Flowthru_Core_Data_CatalogAbstract"></a> Class CatalogAbstract

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Core.dll  

Base class for strongly-typed catalog implementations with automatic property caching.

```csharp
public abstract class CatalogAbstract
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CatalogAbstract](Flowthru.Core.Data.CatalogAbstract.md)

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
Expression-bodied properties (<code>Property =&gt; new Item()</code>) create new instances on each access,
breaking DAG dependency resolution which relies on object identity.
</p>
<p>
<strong>Solution:</strong>
Uses reflection to:
1. Discover all IItem properties on derived classes
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
    public IItem&lt;IEnumerable&lt;MyData&gt;&gt; MyData =&gt;
        CreateItem(() =&gt; new CsvCatalogItem&lt;MyData&gt;("my_data", $"{BasePath}/data.csv"));
}</code></pre>
</p>
<p>
<strong>Key Benefits:</strong>
- Declare catalog items ONCE (no redundant constructor code)
- Automatic instance caching (object identity preserved)
- Type-safe (compile-time checks)
- Zero runtime overhead after first access (cached delegates)
</p>

## Constructors

### <a id="Flowthru_Core_Data_CatalogAbstract__ctor_System_String_"></a> CatalogAbstract\(string?\)

```csharp
protected CatalogAbstract(string? catalogLabel = null)
```

#### Parameters

`catalogLabel` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional display label for this catalog instance. When omitted, defaults to the
concrete class name via <code>GetType().Name</code>.

## Properties

### <a id="Flowthru_Core_Data_CatalogAbstract_CatalogLabel"></a> CatalogLabel

The display label used to identify this catalog instance in Flow metadata.
Defaults to the concrete class name when not specified.

```csharp
public string CatalogLabel { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Pass an explicit label when constructing multiple instances of the same catalog type
in a single Flow (e.g., per-partition or per-shard catalogs) so their items
receive distinct qualified identifiers in the DAG: <code>CatalogLabel.ItemLabel</code>.

### <a id="Flowthru_Core_Data_CatalogAbstract_Services"></a> Services

Optional service provider for dependency injection into catalog items.

```csharp
public IServiceProvider? Services { get; set; }
```

#### Property Value

 [IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider)?

#### Remarks

Set by the service layer before Flow execution to enable catalog
items to resolve services (e.g., database connections, HTTP clients).

## Methods

### <a id="Flowthru_Core_Data_CatalogAbstract_CreateItem__1_System_Func_Flowthru_Core_Data_IItem___0___System_String_"></a> CreateItem<T\>\(Func<IItem<T\>\>, string\)

Gets or creates a unified catalog item, caching it for subsequent accesses.

```csharp
protected IItem<T> CreateItem<T>(Func<IItem<T>> factory, string propertyName = "")
```

#### Parameters

`factory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[IItem](Flowthru.Core.Data.IItem\-1.md)<T\>\>

Factory function to create the item on first access

`propertyName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Auto-populated by compiler with calling property name

#### Returns

 [IItem](Flowthru.Core.Data.IItem\-1.md)<T\>

Cached catalog item instance

#### Type Parameters

`T` 

The data type stored in this catalog item.
For singletons: Use T directly (e.g., LinearRegressionModel)
For collections: Use IEnumerable&lt;T&gt; (e.g., IEnumerable&lt;FeatureRow&gt;)

#### Remarks

<p>
<strong>Unified API (v0.5.0):</strong> This single method replaces CreateObject
and CreateDataset. Cardinality is determined by the type parameter T.
</p>
<p>
<strong>Usage Examples:</strong>
<pre><code class="lang-csharp">// Singleton object
public IItem&lt;LinearRegressionModel&gt; Model =&gt;
    CreateItem(() =&gt; ItemFactory.Single.Memory&lt;LinearRegressionModel&gt;("model"));

// Collection
public IItem&lt;IEnumerable&lt;FeatureRow&gt;&gt; Features =&gt;
    CreateItem(() =&gt; ItemFactory.Enumerable.Csv&lt;FeatureRow&gt;("features", "data.csv"));</code></pre>
</p>

### <a id="Flowthru_Core_Data_CatalogAbstract_GetAllItems"></a> GetAllItems\(\)

Enumerates all <xref href="Flowthru.Core.Data.IItem" data-throw-if-not-resolved="false"></xref> instances registered against this catalog.

```csharp
public virtual IEnumerable<IItem> GetAllItems()
```

#### Returns

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[IItem](Flowthru.Core.Data.IItem.md)\>

#### Remarks

Items become enumerable after <xref href="Flowthru.Core.Data.CatalogAbstract.InitializeCatalogProperties" data-throw-if-not-resolved="false"></xref> runs (typically
at the end of a derived catalog's constructor). Provided so post-run metadata providers
and user diagnostic code can resolve live items by label without reflecting on the catalog
type. Returned in insertion order; not deduplicated across catalogs.

### <a id="Flowthru_Core_Data_CatalogAbstract_InitializeCatalogProperties"></a> InitializeCatalogProperties\(\)

Initializes all catalog item properties by invoking their getters once.

```csharp
protected void InitializeCatalogProperties()
```

#### Remarks

<p>
<strong>Purpose:</strong> Eager initialization ensures all items are cached
before Flow construction begins, preventing any potential race conditions
or unexpected lazy initialization behavior.
</p>
<p>
<strong>When to Call:</strong> At the end of the derived catalog's constructor,
after all configuration properties (like BasePath) are set.
</p>
<p>
<strong>How It Works:</strong>
Uses reflection to find all public instance properties that return IItem,
then invokes each getter once to populate the cache.
</p>

