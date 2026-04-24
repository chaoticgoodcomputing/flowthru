# <a id="Flowthru_Core_Data_Storage_Medium_FileStorageMedium"></a> Class FileStorageMedium

Namespace: [Flowthru.Core.Data.Storage.Medium](Flowthru.Core.Data.Storage.Medium.md)  
Assembly: Flowthru.Core.dll  

Storage medium for file-based I/O operations.

```csharp
public sealed class FileStorageMedium : IStorageMedium
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FileStorageMedium](Flowthru.Core.Data.Storage.Medium.FileStorageMedium.md)

#### Implements

[IStorageMedium](Flowthru.Core.Data.Storage.IStorageMedium.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">var medium = new FileStorageMedium("data/companies.csv");

// Check if file exists
var exists = await medium.Exists().Run();

// Read from file
var readResult = await medium.ReadStream().Run();
readResult.Match(
    Succ: stream =&gt; { /* process stream */ },
    Fail: error =&gt; Console.WriteLine($"Read failed: {error}")
);

// Write to file
using var writeStream = new MemoryStream(data);
var writeResult = await medium.WriteStream(writeStream).Run();</code></pre>

## Remarks

<p>
<strong>Responsibility:</strong> Handle reading and writing raw byte streams to/from files.
</p>
<p>
<strong>Features:</strong>
</p>
<ul><li>Automatic directory creation for parent paths</li><li>Atomic writes via temp file + rename</li><li>Support for both absolute and relative paths</li><li>All storage traits use filesystem baseline defaults</li></ul>
<p>
<strong>Thread Safety:</strong>
</p>
<p>
This class is thread-safe for reads but writes should be coordinated externally
if multiple threads write to the same file.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_Medium_FileStorageMedium__ctor_System_String_"></a> FileStorageMedium\(string\)

Creates a new file storage medium.

```csharp
public FileStorageMedium(string filePath)
```

#### Parameters

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the file (absolute or relative)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

Thrown if filePath is null

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Thrown if filePath is empty or whitespace

## Properties

### <a id="Flowthru_Core_Data_Storage_Medium_FileStorageMedium_FilePath"></a> FilePath

Gets the file path for this storage medium.

```csharp
public string FilePath { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Data_Storage_Medium_FileStorageMedium_Traits"></a> Traits

Structural constraints and capabilities of this storage medium.

```csharp
public StorageTraits Traits { get; }
```

#### Property Value

 [StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md)

#### Remarks

Medium traits focus on WHERE data is stored and the access patterns it supports.
For composed adapters, these traits are merged with format and container traits.

## Methods

### <a id="Flowthru_Core_Data_Storage_Medium_FileStorageMedium_Exists"></a> Exists\(\)

Checks if data exists at this storage location.

```csharp
public FlowIO<bool> Exists()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Effect that produces true if data exists, false otherwise

#### Remarks

<p>
This is used to determine if a catalog entry is a "seed" (Layer 0 input)
or if it's produced by a step in the pipeline.
</p>

### <a id="Flowthru_Core_Data_Storage_Medium_FileStorageMedium_InspectTarget"></a> InspectTarget\(\)

Validates that this storage location is accessible as a write destination.

```csharp
public FlowIO<ValidationResult> InspectTarget()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

#### Remarks

Default implementation returns success — override in medium implementations that
can meaningfully probe write access before execution (e.g., filesystem path checks).

### <a id="Flowthru_Core_Data_Storage_Medium_FileStorageMedium_ReadStream"></a> ReadStream\(\)

Reads raw bytes from storage as a stream.

```csharp
public FlowIO<Stream> ReadStream()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)\>

Effect that produces a readable stream on success

#### Remarks

<p>
The returned stream should be positioned at the beginning and ready to read.
The caller is responsible for disposing the stream.
</p>
<p>
<strong>Error Conditions:</strong>
</p>
<ul><li>Storage location does not exist</li><li>Access denied (permissions)</li><li>Network failure (for remote storage)</li><li>I/O error</li></ul>

### <a id="Flowthru_Core_Data_Storage_Medium_FileStorageMedium_WriteStream_System_IO_Stream_"></a> WriteStream\(Stream\)

Writes raw bytes to storage from a stream.

```csharp
public FlowIO<FlowUnit> WriteStream(Stream stream)
```

#### Parameters

`stream` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

Stream containing data to write

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Core.Effects.FlowUnit.md)\>

Effect that completes on successful write

#### Remarks

<p>
The stream will be read from its current position to the end.
The implementation should handle creating parent directories if needed.
</p>
<p>
<strong>Atomicity:</strong>
</p>
<p>
Implementations should strive for atomic writes (write to temp, then rename)
to avoid partial writes on failure.
</p>
<p>
<strong>Error Conditions:</strong>
</p>
<ul><li>Insufficient disk space</li><li>Access denied (permissions)</li><li>Network failure (for remote storage)</li><li>I/O error</li></ul>

