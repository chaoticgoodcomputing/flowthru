# <a id="Flowthru_Core_Data_Storage_IHasEfficientCount"></a> Interface IHasEfficientCount

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Optional interface for storage adapters that can return a row count without
materializing the full dataset.

```csharp
public interface IHasEfficientCount
```

## Remarks

<p>
By default, <xref href="Flowthru.Core.Data.Item%601.GetCountAsync" data-throw-if-not-resolved="false"></xref> counts by calling <xref href="Flowthru.Core.Data.Storage.IStorageAdapter%601.Load" data-throw-if-not-resolved="false"></xref>
and enumerating the result. For I/O-bound adapters (databases, APIs) this materializes the
entire dataset just to count rows.
</p>
<p>
Implement this interface on a storage adapter to provide a cheap server-side count
(e.g. <code>COUNT(*)</code> SQL) that <xref href="Flowthru.Core.Data.Item%601.GetCountAsync" data-throw-if-not-resolved="false"></xref> will use instead.
</p>
<p>
<strong>The Flowthru engine does not call <xref href="Flowthru.Core.Data.Item%601.GetCountAsync" data-throw-if-not-resolved="false"></xref> during step execution.</strong>
This interface signals to metadata providers and user code that an adapter can be counted
cheaply — count-interested providers should check for it and skip adapters that lack it
rather than triggering a forced materialization. The reference <code>RowCountProvider</code> in
<code>Flowthru.Extensions.Metadata.Diagnostics</code> demonstrates the canonical pattern.
</p>
<p>
Existing adapters that do not implement this interface continue to work without change.
</p>

## Methods

### <a id="Flowthru_Core_Data_Storage_IHasEfficientCount_GetCountAsync"></a> GetCountAsync\(\)

Returns the number of items in the backing store without materializing them.

```csharp
FlowIO<int> GetCountAsync()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

