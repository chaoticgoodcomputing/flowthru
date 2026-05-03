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

### <a id="Flowthru_Core_Data_ParquetItemExtensions_ParquetDirectory__1_Flowthru_Core_Data_EnumerableItemFactory_System_String_System_String_Flowthru_Core_Data_ParquetItemOptions___0__"></a> ParquetDirectory<TRow\>\(EnumerableItemFactory, string, string, ParquetItemOptions<TRow\>?\)

Creates a catalog entry over a directory of Parquet files where each file is one
independent row collection of the same schema. Read produces a
<xref href="Flowthru.Core.Data.Directory%601" data-throw-if-not-resolved="false"></xref> keyed by full file path; Save writes one Parquet file per
entry, deleting any existing <code>*.parquet</code> in the directory first so re-runs are
deterministic.

```csharp
public static Item<Directory<IEnumerable<TRow>>> ParquetDirectory<TRow>(this EnumerableItemFactory _, string label, string directoryPath, ParquetItemOptions<TRow>? options = null) where TRow : notnull, IFlatSchema, IBinarySerializable
```

#### Parameters

`_` EnumerableItemFactory

The enumerable catalog entries factory (from <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory containing the Parquet files

`options` [ParquetItemOptions](Flowthru.Core.Data.ParquetItemOptions\-1.md)<TRow\>?

Optional performance and behavior tuning applied uniformly to every file in the
directory; see <xref href="Flowthru.Core.Data.ParquetItemExtensions.Parquet%60%601(Flowthru.Core.Data.EnumerableItemFactory%2cSystem.String%2cSystem.String%2cFlowthru.Core.Data.ParquetItemOptions%7b%60%600%7d%2cFlowthru.Core.Data.Storage.IStorageMediumResolver%2cFlowthru.Core.Data.Storage.IStorageMedium)" data-throw-if-not-resolved="false"></xref> for details.

#### Returns

 Item<Directory<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>\>

#### Type Parameters

`TRow` 

Row schema type (must be flat and binary-serializable)

#### Remarks

All files must share the same schema. This is intentionally not a partitioning
primitive — each file represents an independent unit. If you need to chunk a single
logical dataset across files, do that in a step before write and reassemble in a step
after read.

