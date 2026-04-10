# <a id="Flowthru_Core_Data_Storage_IContainerAdapter_2"></a> Interface IContainerAdapter<TContainer, TRow\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Interface for container adaptation - converts between streaming rows and in-memory containers.

```csharp
public interface IContainerAdapter<TContainer, TRow>
```

#### Type Parameters

`TContainer` 

The in-memory container type (IEnumerable, IDataView, Seq, etc.)

`TRow` 

The row type (schema)

## Examples

<pre><code class="lang-csharp">// IEnumerable container adapter
var enumerableAdapter = new EnumerableContainerAdapter&lt;CompanySchema&gt;();
var companies = await enumerableAdapter.FromRows(rowStream);
// Type: IEnumerable&lt;CompanySchema&gt;

// IDataView container adapter
var dataViewAdapter = new DataViewContainerAdapter&lt;CompanySchema&gt;(mlContext);
var dataView = await dataViewAdapter.FromRows(rowStream);
// Type: IDataView (ML.NET)</code></pre>

## Remarks

<p>
<strong>Responsibility:</strong> Abstract WHAT in-memory representation to use for data.
</p>
<p>
<strong>Separation of Concerns:</strong>
</p>
<p>
The container adapter is isolated from:
- Storage location (file vs memory) - handled by <xref href="Flowthru.Core.Data.Storage.IStorageMedium" data-throw-if-not-resolved="false"></xref>
- Serialization format (CSV vs JSON) - handled by <xref href="Flowthru.Core.Data.Storage.IFormatSerializer%601" data-throw-if-not-resolved="false"></xref>
</p>
<p>
<strong>Bridge Pattern:</strong>
</p>
<p>
This adapter bridges between:
- <strong>Streaming rows</strong> (<xref href="System.Collections.Generic.IAsyncEnumerable%601" data-throw-if-not-resolved="false"></xref>) - format layer
- <strong>In-memory container</strong> (IEnumerable, IDataView) - application layer
</p>
<p>
<strong>Container Type Examples:</strong>
</p>
<ul><li><strong>IEnumerable&lt;TRow&gt;</strong> - Standard .NET collections</li><li><strong>IDataView</strong> - ML.NET's columnar data representation</li><li><strong>DataFrame</strong> - Pandas-style dataframe (future)</li><li><strong>IObservable&lt;TRow&gt;</strong> - Reactive streams (future)</li></ul>
<p>
<strong>Design Pattern:</strong>
</p>
<p>
This is the top layer in the composition pattern:
</p>
<pre><code class="lang-csharp">Medium (bytes) → Format (rows) → Container (in-memory)
Stream         → IAsyncEnumerable&lt;TRow&gt; → IEnumerable&lt;TRow&gt; / IDataView</code></pre>

## Methods

### <a id="Flowthru_Core_Data_Storage_IContainerAdapter_2_FromRows_System_Collections_Generic_IAsyncEnumerable__1__"></a> FromRows\(IAsyncEnumerable<TRow\>\)

Materializes an async stream of rows into an in-memory container.

```csharp
Task<TContainer> FromRows(IAsyncEnumerable<TRow> rows)
```

#### Parameters

`rows` [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<TRow\>

Async stream of rows from format deserialization

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TContainer\>

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

### <a id="Flowthru_Core_Data_Storage_IContainerAdapter_2_ToRows__0_"></a> ToRows\(TContainer\)

Converts an in-memory container back to an async stream of rows.

```csharp
IAsyncEnumerable<TRow> ToRows(TContainer container)
```

#### Parameters

`container` TContainer

The container to stream from

#### Returns

 [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<TRow\>

Async enumerable of rows for format serialization

#### Remarks

<p>
<strong>Streaming Strategy:</strong>
</p>
<p>
Rows should be yielded lazily if the container supports it:
</p>
<ul><li><strong>IEnumerable:</strong> Enumerate and yield</li><li><strong>Seq:</strong> Already lazy - expose as async enumerable</li><li><strong>IDataView:</strong> Enumerate rows from columnar format</li></ul>

