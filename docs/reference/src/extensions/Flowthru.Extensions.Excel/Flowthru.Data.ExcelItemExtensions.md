# <a id="Flowthru_Data_ExcelItemExtensions"></a> Class ExcelItemExtensions

Namespace: [Flowthru.Data](Flowthru.Data.md)  
Assembly: Flowthru.Extensions.Excel.dll  

Extension methods that add Excel support to <xref href="Flowthru.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class ExcelItemExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ExcelItemExtensions](Flowthru.Data.ExcelItemExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Data_ExcelItemExtensions_Excel__1_Flowthru_Data_EnumerableItemFactory_System_String_System_String_System_String_"></a> Excel<TRow\>\(EnumerableItemFactory, string, string, string\)

Creates a read-only Excel file catalog entry with IEnumerable container.

```csharp
public static Item<IEnumerable<TRow>> Excel<TRow>(this EnumerableItemFactory _, string label, string filePath, string sheetName) where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Parameters

`_` EnumerableItemFactory

The enumerable catalog entries factory (from <xref href="Flowthru.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to Excel file (.xlsx)

`sheetName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Name of the sheet to read

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

