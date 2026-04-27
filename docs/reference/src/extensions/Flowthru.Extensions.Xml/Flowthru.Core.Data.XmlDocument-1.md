# <a id="Flowthru_Core_Data_XmlDocument_1"></a> Class XmlDocument<T\>

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Extensions.Xml.dll  

Wraps a deserialized XML document with its source file name.

```csharp
public record XmlDocument<T> : IEquatable<XmlDocument<T>>
```

#### Type Parameters

`T` 

The deserialized document type.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[XmlDocument<T\>](Flowthru.Core.Data.XmlDocument\-1.md)

#### Implements

[IEquatable<XmlDocument<T\>\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

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
Used by <code>XmlDirectoryStorageAdapter&lt;T&gt;</code> so downstream pipeline steps can
identify which file each document originated from — useful when file names carry
semantic meaning (e.g., a test project name encoded as the file name).
</p>

## Constructors

### <a id="Flowthru_Core_Data_XmlDocument_1__ctor_System_String__0_"></a> XmlDocument\(string, T\)

Wraps a deserialized XML document with its source file name.

```csharp
public XmlDocument(string FileName, T Document)
```

#### Parameters

`FileName` [string](https://learn.microsoft.com/dotnet/api/system.string)

The file name (without directory path) of the source XML file.

`Document` T

The deserialized document.

#### Remarks

<p>
Used by <code>XmlDirectoryStorageAdapter&lt;T&gt;</code> so downstream pipeline steps can
identify which file each document originated from — useful when file names carry
semantic meaning (e.g., a test project name encoded as the file name).
</p>

## Properties

### <a id="Flowthru_Core_Data_XmlDocument_1_Document"></a> Document

The deserialized document.

```csharp
public T Document { get; init; }
```

#### Property Value

 T

### <a id="Flowthru_Core_Data_XmlDocument_1_FileName"></a> FileName

The file name (without directory path) of the source XML file.

```csharp
public string FileName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

