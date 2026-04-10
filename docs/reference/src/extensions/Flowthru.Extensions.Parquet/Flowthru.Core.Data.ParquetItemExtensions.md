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

### <a id="Flowthru_Core_Data_ParquetItemExtensions_Parquet__1_Flowthru_Core_Data_EnumerableItemFactory_System_String_System_String_"></a> Parquet<TRow\>\(EnumerableItemFactory, string, string\)

Creates a Parquet file catalog entry with IEnumerable container.

```csharp
public static Item<IEnumerable<TRow>> Parquet<TRow>(this EnumerableItemFactory _, string label, string filePath) where TRow : notnull, IFlatSchema, IBinarySerializable
```

#### Parameters

`_` EnumerableItemFactory

The enumerable catalog entries factory (from <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to Parquet file

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
<strong>Performance:</strong> Optimized for large datasets with columnar storage.
</p>

