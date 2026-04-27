# <a id="Flowthru_Core_Data_XmlItemFactory"></a> Class XmlItemFactory

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Extensions.Xml.dll  

Factory methods for creating <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> instances with XML storage adapters.

```csharp
public static class XmlItemFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[XmlItemFactory](Flowthru.Core.Data.XmlItemFactory.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Mirrors the <code>EFCoreItemFactory</code> / <code>GqlItemFactory</code> pattern: a parallel static
factory class for extension-specific storage types, since <code>ItemFactory.Single</code> is a
nested static class and cannot be extended from outside the core assembly.
</p>
<p>
The <xref href="System.Linq.Enumerable" data-throw-if-not-resolved="false"></xref> factory also extends <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>
via an extension method on <xref href="Flowthru.Core.Data.EnumerableItemFactory" data-throw-if-not-resolved="false"></xref>.
</p>

