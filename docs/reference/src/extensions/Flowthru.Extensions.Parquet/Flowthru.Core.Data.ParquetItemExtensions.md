# <a id="Flowthru_Core_Data_ParquetItemExtensions"></a> Class ParquetItemExtensions

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Extensions.Parquet.dll  

Extension methods that add Parquet support to <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class ParquetItemExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ParquetItemExtensions](Flowthru.Core.Data.ParquetItemExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Core_Data_ParquetItemExtensions_Parquet__1_Flowthru_Core_Data_EnumerableItemFactory_System_String_System_String_Flowthru_Core_Data_ParquetItemOptions___0__Flowthru_Core_Data_Storage_IStorageMediumResolver_Flowthru_Core_Data_Storage_IStorageMedium_"></a> Parquet<TRow\>\(EnumerableItemFactory, string, string, ParquetItemOptions<TRow\>?, IStorageMediumResolver?, IStorageMedium?\)

Creates a Parquet file catalog entry with IEnumerable container.

```csharp
public static Item<IEnumerable<TRow>> Parquet<TRow>(this EnumerableItemFactory _, string label, string filePath, ParquetItemOptions<TRow>? options = null, IStorageMediumResolver? resolver = null, IStorageMedium? medium = null) where TRow : notnull, IFlatSchema, IBinarySerializable
```

#### Parameters

`_` EnumerableItemFactory

The enumerable catalog entries factory (from <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path or URI to Parquet file

`options` [ParquetItemOptions](Flowthru.Core.Data.ParquetItemOptions\-1.md)<TRow\>?

Optional performance and behavior tuning. When <code>null</code>, production-ready defaults are
used: Snappy compression, 1 000 000-row groups (≈100 MB), dictionary encoding enabled.

`resolver` IStorageMediumResolver?

Optional resolver for remote URIs (e.g., <code>https://</code>, <code>sftp://</code>).
Falls back to <xref href="Flowthru.Core.Data.Storage.Medium.FileStorageMedium" data-throw-if-not-resolved="false"></xref> when <code>null</code>.

`medium` IStorageMedium?

Explicit medium override. Takes precedence over <code class="paramref">resolver</code> when both
are supplied. Use for per-entry customisation or direct injection in tests.

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>

Catalog entry with file + Parquet + IEnumerable composition

#### Type Parameters

`TRow` 

Row schema type (must be flat and binary-serializable)

#### Remarks

<p>
<strong>Requirements:</strong>
</p>
<ul><li>TRow must implement IFlatSchema (Parquet is columnar)</li><li>TRow must implement IBinarySerializable</li></ul>
<p>
<strong>Performance:</strong> Write path streams in bounded row-group batches —
peak memory scales with row-group size, not total dataset size. Suitable for 1–10 GB datasets.
</p>

