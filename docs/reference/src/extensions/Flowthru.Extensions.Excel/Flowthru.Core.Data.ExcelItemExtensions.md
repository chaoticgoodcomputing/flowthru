# <a id="Flowthru_Core_Data_ExcelItemExtensions"></a> Class ExcelItemExtensions

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Extensions.Excel.dll  

Extension methods that add Excel support to <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class ExcelItemExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ExcelItemExtensions](Flowthru.Core.Data.ExcelItemExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Core_Data_ExcelItemExtensions_Excel__1_Flowthru_Core_Data_EnumerableItemFactory_System_String_System_String_System_String_System_Collections_Generic_IReadOnlyList_System_String__"></a> Excel<TRow\>\(EnumerableItemFactory, string, string, string, IReadOnlyList<string\>?\)

Creates a read-only Excel file catalog entry with IEnumerable container.

```csharp
public static Item<IEnumerable<TRow>> Excel<TRow>(this EnumerableItemFactory _, string label, string filePath, string sheetName, IReadOnlyList<string>? nullValues = null) where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Parameters

`_` EnumerableItemFactory

The enumerable catalog entries factory (from <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to Excel file (.xlsx)

`sheetName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Name of the sheet to read

`nullValues` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

Optional set of strings that should deserialize to null for nullable properties.
Defaults to <code>[""]</code> — only genuinely empty cells (DBNull) become null. Pass e.g.
<code>["", "NA", "N/A", "NULL"]</code> to also treat those string sentinels as null on read.

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>

Catalog entry with read-only Excel support

#### Type Parameters

`TRow` 

Row schema type (must be flat and text-serializable)

#### Remarks

<p>
<strong>Requirements:</strong>
</p>
<ul><li>TRow must implement IFlatSchema (Excel is tabular)</li><li>TRow must implement ITextSerializable</li></ul>
<p>
<strong>Limitations:</strong> Read-only support via ExcelDataReader.
Writing Excel files is not supported.
</p>
<p>
<strong>Storage Traits:</strong>
</p>
<ul><li>CanWrite: false (Excel adapter is read-only via ExcelDataReader)</li></ul>

### <a id="Flowthru_Core_Data_ExcelItemExtensions_ExcelDirectory__1_Flowthru_Core_Data_EnumerableItemFactory_System_String_System_String_System_String_System_Collections_Generic_IReadOnlyList_System_String__"></a> ExcelDirectory<TRow\>\(EnumerableItemFactory, string, string, string, IReadOnlyList<string\>?\)

Creates a read-only catalog entry over a directory of Excel files where each file's
designated sheet deserialises to one independent row collection of the same schema.
Read produces a <xref href="Flowthru.Core.Data.Directory%601" data-throw-if-not-resolved="false"></xref> keyed by full file path.

```csharp
public static Item<Directory<IEnumerable<TRow>>> ExcelDirectory<TRow>(this EnumerableItemFactory _, string label, string directoryPath, string sheetName, IReadOnlyList<string>? nullValues = null) where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Parameters

`_` EnumerableItemFactory

The enumerable catalog entries factory

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory containing the <code>.xlsx</code> files

`sheetName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Name of the sheet to read in each file

`nullValues` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

Optional null-sentinel list; see <xref href="Flowthru.Core.Data.ExcelItemExtensions.Excel%60%601(Flowthru.Core.Data.EnumerableItemFactory%2cSystem.String%2cSystem.String%2cSystem.String%2cSystem.Collections.Generic.IReadOnlyList%7bSystem.String%7d)" data-throw-if-not-resolved="false"></xref>.

#### Returns

 Item<Directory<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>\>

#### Type Parameters

`TRow` 

Row schema type (must be flat and text-serializable)

#### Remarks

All files must share the same schema and use the same <code class="paramref">sheetName</code>.
This entry is read-only — Excel write is not supported by the underlying adapter.

