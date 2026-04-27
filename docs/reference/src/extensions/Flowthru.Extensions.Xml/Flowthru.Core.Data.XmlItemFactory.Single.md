# <a id="Flowthru_Core_Data_XmlItemFactory_Single"></a> Class XmlItemFactory.Single

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Extensions.Xml.dll  

Factory methods for single XML document catalog entries.

```csharp
public static class XmlItemFactory.Single
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[XmlItemFactory.Single](Flowthru.Core.Data.XmlItemFactory.Single.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Core_Data_XmlItemFactory_Single_Xml__1_System_String_System_String_"></a> Xml<T\>\(string, string\)

Creates an XML file catalog entry for a single document.

```csharp
public static Item<T> Xml<T>(string label, string filePath) where T : IStructuredSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the XML file

#### Returns

 Item<T\>

Catalog entry backed by a single XML file

#### Type Parameters

`T` 

The document type. Must implement <xref href="Flowthru.Core.Abstractions.IStructuredSerializable" data-throw-if-not-resolved="false"></xref>.

#### Remarks

Decorate <code class="typeparamref">T</code> with <code>[XmlRoot]</code>, <code>[XmlElement]</code>, and
<code>[XmlAttribute]</code> as required by <xref href="System.Xml.Serialization.XmlSerializer" data-throw-if-not-resolved="false"></xref>.

