# <a id="Flowthru_Core_Data_XmlEnumerableItemExtensions"></a> Class XmlEnumerableItemExtensions

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Extensions.Xml.dll  

Extension methods that add XML directory support to <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class XmlEnumerableItemExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[XmlEnumerableItemExtensions](Flowthru.Core.Data.XmlEnumerableItemExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Core_Data_XmlEnumerableItemExtensions_XmlDocuments__1_Flowthru_Core_Data_EnumerableItemFactory_System_String_System_String_"></a> XmlDocuments<T\>\(EnumerableItemFactory, string, string\)

Creates a catalog entry over a directory of XML files where each file deserialises to
one <code class="typeparamref">T</code>. Read produces a <xref href="Flowthru.Core.Data.Directory%601" data-throw-if-not-resolved="false"></xref> keyed by full
file path; Save writes one XML file per entry, deleting any existing <code>*.xml</code> in
the directory first so re-runs are deterministic.

```csharp
public static Item<Directory<T>> XmlDocuments<T>(this EnumerableItemFactory _, string label, string directoryPath) where T : IStructuredSerializable
```

#### Parameters

`_` EnumerableItemFactory

The enumerable catalog entries factory (from <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory containing XML files

#### Returns

 Item<Directory<T\>\>

#### Type Parameters

`T` 

The document type for each XML file.

#### Remarks

All files must share the same schema. This is intentionally not a partitioning
primitive — each file represents an independent unit. If you need to chunk a single
logical dataset across files, do that in a step before write and reassemble in a step
after read.

