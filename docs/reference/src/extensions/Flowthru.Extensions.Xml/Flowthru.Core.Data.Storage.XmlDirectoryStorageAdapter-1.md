# <a id="Flowthru_Core_Data_Storage_XmlDirectoryStorageAdapter_1"></a> Class XmlDirectoryStorageAdapter<T\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Extensions.Xml.dll  

Read-only storage adapter that deserializes every <code>*.xml</code> file in a directory,
yielding each as an <xref href="Flowthru.Core.Data.XmlDocument%601" data-throw-if-not-resolved="false"></xref> wrapper that carries the source file name.

```csharp
public sealed class XmlDirectoryStorageAdapter<T> : ReadOnlyDirectoryStorageAdapter<XmlDocument<T>>, IStorageAdapter<IEnumerable<XmlDocument<T>>> where T : IStructuredSerializable
```

#### Type Parameters

`T` 

The document type for each XML file.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
ReadOnlyDirectoryStorageAdapter<XmlDocument<T\>\> ← 
[XmlDirectoryStorageAdapter<T\>](Flowthru.Core.Data.Storage.XmlDirectoryStorageAdapter\-1.md)

#### Implements

IStorageAdapter<IEnumerable<XmlDocument<T\>\>\>

#### Inherited Members

ReadOnlyDirectoryStorageAdapter<XmlDocument<T\>\>.Traits, 
ReadOnlyDirectoryStorageAdapter<XmlDocument<T\>\>.Load\(\), 
ReadOnlyDirectoryStorageAdapter<XmlDocument<T\>\>.Save\(IEnumerable<XmlDocument<T\>\>\), 
ReadOnlyDirectoryStorageAdapter<XmlDocument<T\>\>.Exists\(\), 
ReadOnlyDirectoryStorageAdapter<XmlDocument<T\>\>.InspectShallow\(int\), 
ReadOnlyDirectoryStorageAdapter<XmlDocument<T\>\>.InspectDeep\(\), 
ReadOnlyDirectoryStorageAdapter<XmlDocument<T\>\>.InspectTarget\(\), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Files are processed in lexicographic order for deterministic output across runs.
</p>
<p>
The <xref href="Flowthru.Core.Data.XmlDocument%601.FileName" data-throw-if-not-resolved="false"></xref> property contains only the file name
(not the full path), so downstream steps can derive semantic meaning from the
naming convention used when staging the files.
</p>
<p>
<strong>Read-only:</strong> This adapter cannot be written to. It represents an
immutable staged input layer.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_XmlDirectoryStorageAdapter_1__ctor_System_String_"></a> XmlDirectoryStorageAdapter\(string\)

Creates a new XML directory storage adapter.

```csharp
public XmlDirectoryStorageAdapter(string directoryPath)
```

#### Parameters

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory containing XML files.

## Methods

### <a id="Flowthru_Core_Data_Storage_XmlDirectoryStorageAdapter_1_LoadFile_System_String_System_Threading_CancellationToken_"></a> LoadFile\(string, CancellationToken\)

Deserializes one file into an async stream of <xref href="Flowthru.Core.Data.XmlDocument%7b%600%7d" data-throw-if-not-resolved="false"></xref> values.

```csharp
protected override IAsyncEnumerable<XmlDocument<T>> LoadFile(string filePath, CancellationToken ct)
```

#### Parameters

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Absolute or relative path to the file.

`ct` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token.

#### Returns

 [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<[XmlDocument](Flowthru.Core.Data.XmlDocument\-1.md)<T\>\>

### <a id="Flowthru_Core_Data_Storage_XmlDirectoryStorageAdapter_1_ValidateFileAsync_System_String_System_Int32_System_Threading_CancellationToken_"></a> ValidateFileAsync\(string, int, CancellationToken\)

Validates one file at the given sample depth.

```csharp
protected override Task<ValidationResult> ValidateFileAsync(string filePath, int sampleSize, CancellationToken ct)
```

#### Parameters

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

The file to validate.

`sampleSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Maximum items to read; <code>0</code> means read all items (used by
<xref href="Flowthru.Core.Data.Storage.ReadOnlyDirectoryStorageAdapter%601.InspectDeep" data-throw-if-not-resolved="false"></xref>).

`ct` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token.

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<ValidationResult\>

