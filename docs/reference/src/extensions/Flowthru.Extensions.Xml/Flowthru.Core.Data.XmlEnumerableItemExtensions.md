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

Creates a read-only catalog entry that deserializes all <code>*.xml</code> files in a directory,
yielding each as an <xref href="Flowthru.Core.Data.XmlDocument%601" data-throw-if-not-resolved="false"></xref> that carries the source file name.

```csharp
public static Item<IEnumerable<XmlDocument<T>>> XmlDocuments<T>(this EnumerableItemFactory _, string label, string directoryPath) where T : IStructuredSerializable
```

#### Parameters

`_` EnumerableItemFactory

The enumerable catalog entries factory (from <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>)

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory containing XML files

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[XmlDocument](Flowthru.Core.Data.XmlDocument\-1.md)<T\>\>\>

Read-only catalog entry yielding one <xref href="Flowthru.Core.Data.XmlDocument%601" data-throw-if-not-resolved="false"></xref> per file

#### Type Parameters

`T` 

The document type for each XML file.

#### Remarks

<p>
Files are processed in lexicographic order for deterministic output across runs.
The <xref href="Flowthru.Core.Data.XmlDocument%601.FileName" data-throw-if-not-resolved="false"></xref> carries the file name without directory path,
allowing downstream steps to derive semantic meaning from the naming convention.
</p>
<p>
This entry is <strong>read-only</strong> — attempting to save will fail with
<xref href="System.NotSupportedException" data-throw-if-not-resolved="false"></xref>.
</p>

