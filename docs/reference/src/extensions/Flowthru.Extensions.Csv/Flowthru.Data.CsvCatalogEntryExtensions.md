# <a id="Flowthru_Data_CsvItemExtensions"></a> Class CsvItemExtensions

Namespace: [Flowthru.Data](Flowthru.Data.md)  
Assembly: Flowthru.Extensions.Csv.dll  

Extension methods that add CSV support to <xref href="Flowthru.Data.Items.Enumerable" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class CsvItemExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CsvItemExtensions](Flowthru.Data.CsvItemExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Data_CsvItemExtensions_Csv__1_Flowthru_Data_EnumerableItems_System_String_System_String_"></a> Csv<TRow\>\(EnumerableItems, string, string\)

Creates a CSV file catalog entry with IEnumerable container.

```csharp
public static Item<IEnumerable<TRow>> Csv<TRow>(this EnumerableItems _, string label, string filePath) where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Parameters

`_` EnumerableItems

The enumerable catalog entries factory (from <xref href="Flowthru.Data.Items.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to CSV file

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

### <a id="Flowthru_Data_CsvItemExtensions_CsvDirectory__1_Flowthru_Data_EnumerableItems_System_String_System_String_"></a> CsvDirectory<TRow\>\(EnumerableItems, string, string\)

Creates a catalog entry that reads all CSV files in a directory and
concatenates them into a single <xref href="System.Collections.Generic.IEnumerable%601" data-throw-if-not-resolved="false"></xref>.

```csharp
public static Item<IEnumerable<TRow>> CsvDirectory<TRow>(this EnumerableItems _, string label, string directoryPath) where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Parameters

`_` EnumerableItems

The enumerable catalog entries factory (from <xref href="Flowthru.Data.Items.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory containing the CSV files

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>

Read-only catalog entry that concatenates every <code>*.csv</code> in the directory

#### Type Parameters

`TRow` 

Row schema type (must be flat and text-serializable)

#### Remarks

Files are read in lexicographic order. All files must share the same schema.
This entry is <strong>read-only</strong> — attempting to save will fail with
<xref href="System.NotSupportedException" data-throw-if-not-resolved="false"></xref>.

