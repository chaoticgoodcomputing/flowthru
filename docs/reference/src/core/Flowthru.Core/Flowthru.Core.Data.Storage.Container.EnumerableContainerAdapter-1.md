# <a id="Flowthru_Core_Data_Storage_Container_EnumerableContainerAdapter_1"></a> Class EnumerableContainerAdapter<T\>

Namespace: [Flowthru.Core.Data.Storage.Container](Flowthru.Core.Data.Storage.Container.md)  
Assembly: Flowthru.Core.dll  

Container adapter for IEnumerable&lt;T&gt; - standard .NET collection type.

```csharp
public sealed class EnumerableContainerAdapter<T> : IContainerAdapter<IEnumerable<T>, T>
```

#### Type Parameters

`T` 

The element type

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EnumerableContainerAdapter<T\>](Flowthru.Core.Data.Storage.Container.EnumerableContainerAdapter\-1.md)

#### Implements

[IContainerAdapter<IEnumerable<T\>, T\>](Flowthru.Core.Data.Storage.IContainerAdapter\-2.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">var adapter = new EnumerableContainerAdapter&lt;CompanySchema&gt;();

// From rows (materializes to List)
var companies = await adapter.FromRows(rowStream);

// Multiple enumeration - safe
var count = companies.Count();
var firstFive = companies.Take(5).ToList();

// Back to rows
var rowsAgain = adapter.ToRows(companies);</code></pre>

## Remarks

<p>
<strong>Characteristics:</strong>
</p>
<ul><li><strong>Eager materialization:</strong> Loads all rows into memory (List)</li><li><strong>Standard .NET:</strong> Works with all .NET LINQ operations</li><li><strong>Multiple enumeration:</strong> Safe - data is cached in memory</li><li><strong>Memory bound:</strong> Not suitable for very large datasets</li></ul>
<p>
<strong>Use Cases:</strong>
</p>
<ul><li>Small to medium datasets (&lt;100K rows)</li><li>When multiple enumerations are needed</li><li>Standard .NET pipelines without functional dependencies</li><li>Testing and prototyping</li></ul>
<p>
<strong>Alternatives:</strong>
</p>
<ul></ul>

## Constructors

### <a id="Flowthru_Core_Data_Storage_Container_EnumerableContainerAdapter_1__ctor"></a> EnumerableContainerAdapter\(\)

Creates a new enumerable container adapter.

```csharp
public EnumerableContainerAdapter()
```

## Methods

### <a id="Flowthru_Core_Data_Storage_Container_EnumerableContainerAdapter_1_FromRows_System_Collections_Generic_IAsyncEnumerable__0__"></a> FromRows\(IAsyncEnumerable<T\>\)

Materializes an async stream of rows into an in-memory container.

```csharp
public Task<IEnumerable<T>> FromRows(IAsyncEnumerable<T> rows)
```

#### Parameters

`rows` [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<T\>

Async stream of rows from format deserialization

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

Task producing the populated container

#### Remarks

<p>
<strong>Materialization Strategy:</strong>
</p>
<p>
Different containers have different materialization approaches:
</p>
<ul><li><strong>IEnumerable:</strong> Eager - load all rows into List</li><li><strong>Seq:</strong> Lazy - wrap async enumerable</li><li><strong>IDataView:</strong> Columnar - convert to columnar format</li></ul>
<p>
<strong>Memory Considerations:</strong>
</p>
<p>
Be aware that eager containers (IEnumerable) will load all data into memory.
For large datasets, prefer streaming containers (Seq, IDataView with lazy loading).
</p>

### <a id="Flowthru_Core_Data_Storage_Container_EnumerableContainerAdapter_1_ToRows_System_Collections_Generic_IEnumerable__0__"></a> ToRows\(IEnumerable<T\>\)

Converts an in-memory container back to an async stream of rows.

```csharp
public IAsyncEnumerable<T> ToRows(IEnumerable<T> container)
```

#### Parameters

`container` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>

The container to stream from

#### Returns

 [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<T\>

Async enumerable of rows for format serialization

#### Remarks

<p>
<strong>Streaming Strategy:</strong>
</p>
<p>
Rows should be yielded lazily if the container supports it:
</p>
<ul><li><strong>IEnumerable:</strong> Enumerate and yield</li><li><strong>Seq:</strong> Already lazy - expose as async enumerable</li><li><strong>IDataView:</strong> Enumerate rows from columnar format</li></ul>

