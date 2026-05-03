# <a id="Flowthru_Core_Data_CsvItemExtensions"></a> Class CsvItemExtensions

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Extensions.Csv.dll  

Extension methods that add CSV support to <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class CsvItemExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CsvItemExtensions](Flowthru.Core.Data.CsvItemExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Core_Data_CsvItemExtensions_Csv__1_Flowthru_Core_Data_EnumerableItemFactory_System_String_System_String_Flowthru_Core_Data_Storage_IStorageMediumResolver_Flowthru_Core_Data_Storage_IStorageMedium_System_Collections_Generic_IReadOnlyList_System_String__"></a> Csv<TRow\>\(EnumerableItemFactory, string, string, IStorageMediumResolver?, IStorageMedium?, IReadOnlyList<string\>?\)

Creates a CSV file catalog entry with IEnumerable container.

```csharp
public static Item<IEnumerable<TRow>> Csv<TRow>(this EnumerableItemFactory _, string label, string filePath, IStorageMediumResolver? resolver = null, IStorageMedium? medium = null, IReadOnlyList<string>? nullValues = null) where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Parameters

`_` EnumerableItemFactory

The enumerable catalog entries factory (from <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path or URI to CSV file

`resolver` IStorageMediumResolver?

Optional resolver for remote URIs (e.g., <code>https://</code>, <code>sftp://</code>).
Falls back to <xref href="Flowthru.Core.Data.Storage.Medium.FileStorageMedium" data-throw-if-not-resolved="false"></xref> when <code>null</code>.

`medium` IStorageMedium?

Explicit medium override. Takes precedence over <code class="paramref">resolver</code> when both
are supplied. Use for per-entry customisation or direct injection in tests.

`nullValues` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

Optional set of strings that should deserialize to null for nullable properties.
Defaults to <code>[""]</code> — empty cells (<code>,,</code>) are treated as null, matching CSV
convention. Pass e.g. <code>["", "NA", "N/A", "NULL"]</code> for pandas-style handling of
messy real-world data. The first entry is also used on the write side as the
canonical representation of null.

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>

Catalog entry with file + CSV + IEnumerable composition

#### Type Parameters

`TRow` 

Row schema type (must be flat and text-serializable)

#### Remarks

<p>
<strong>Requirements:</strong>
</p>
<ul><li>TRow must implement IFlatSchema (no nested objects)</li><li>TRow must implement ITextSerializable</li></ul>
<p>
<strong>Storage Traits:</strong>
</p>
<ul><li>CanStream: true (CSV supports row-by-row streaming)</li><li>All other traits use filesystem baseline defaults</li></ul>

### <a id="Flowthru_Core_Data_CsvItemExtensions_CsvDirectory__1_Flowthru_Core_Data_EnumerableItemFactory_System_String_System_String_System_Collections_Generic_IReadOnlyList_System_String__"></a> CsvDirectory<TRow\>\(EnumerableItemFactory, string, string, IReadOnlyList<string\>?\)

Creates a catalog entry over a directory of CSV files where each file is one
independent row collection of the same schema. Read produces a
<xref href="Flowthru.Core.Data.Directory%601" data-throw-if-not-resolved="false"></xref> keyed by full file path; Save writes one CSV per entry,
deleting any existing <code>*.csv</code> in the directory first so re-runs are deterministic.

```csharp
public static Item<Directory<IEnumerable<TRow>>> CsvDirectory<TRow>(this EnumerableItemFactory _, string label, string directoryPath, IReadOnlyList<string>? nullValues = null) where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Parameters

`_` EnumerableItemFactory

The enumerable catalog entries factory (from <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory containing the CSV files

`nullValues` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

Optional set of strings that should deserialize to null for nullable properties; see
<xref href="Flowthru.Core.Data.CsvItemExtensions.Csv%60%601(Flowthru.Core.Data.EnumerableItemFactory%2cSystem.String%2cSystem.String%2cFlowthru.Core.Data.Storage.IStorageMediumResolver%2cFlowthru.Core.Data.Storage.IStorageMedium%2cSystem.Collections.Generic.IReadOnlyList%7bSystem.String%7d)" data-throw-if-not-resolved="false"></xref> for details. Applied uniformly to every file in the directory.

#### Returns

 Item<Directory<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>\>

#### Type Parameters

`TRow` 

Row schema type (must be flat and text-serializable)

#### Remarks

All files must share the same schema (identical column headers). This is intentionally
not a partitioning primitive — each file represents an independent unit. If you need
to chunk a single logical dataset across files, do that in a step before write and
reassemble in a step after read.

