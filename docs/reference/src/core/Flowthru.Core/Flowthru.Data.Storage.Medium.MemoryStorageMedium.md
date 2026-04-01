# <a id="Flowthru_Data_Storage_Medium_MemoryStorageMedium"></a> Class MemoryStorageMedium

Namespace: [Flowthru.Data.Storage.Medium](Flowthru.Data.Storage.Medium.md)  
Assembly: Flowthru.Core.dll  

Storage medium for in-memory byte storage.

```csharp
public sealed class MemoryStorageMedium : IStorageMedium
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MemoryStorageMedium](Flowthru.Data.Storage.Medium.MemoryStorageMedium.md)

#### Implements

[IStorageMedium](Flowthru.Data.Storage.IStorageMedium.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">var medium = new MemoryStorageMedium();

// Write some data
using var writeStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello, World!"));
await medium.WriteStream(writeStream).Run();

// Read it back
var readResult = await medium.ReadStream().Run();
readResult.Match(
    Succ: stream =&gt;
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        Console.WriteLine(content); // "Hello, World!"
    },
    Fail: error =&gt; Console.WriteLine($"Read failed: {error}")
);</code></pre>

## Remarks

<p>
<strong>Responsibility:</strong> Store data in memory without any file I/O.
</p>
<p>
<strong>Use Cases:</strong>
</p>
<ul><li>Testing without file system dependencies</li><li>Transient pipeline intermediates that don't need persistence</li><li>Fast prototyping and experimentation</li><li>In-memory caching of computed results</li></ul>
<p>
<strong>Characteristics:</strong>
</p>
<ul><li>IsPersistent: false - data lost when process exits</li><li>Very fast - no I/O overhead</li><li>Memory-bound - not suitable for large datasets</li></ul>
<p>
<strong>Thread Safety:</strong>
</p>
<p>
This class uses locking to ensure thread-safe access to the internal buffer.
Multiple threads can safely read/write concurrently.
</p>

## Constructors

### <a id="Flowthru_Data_Storage_Medium_MemoryStorageMedium__ctor"></a> MemoryStorageMedium\(\)

Creates a new memory storage medium with no initial data.

```csharp
public MemoryStorageMedium()
```

### <a id="Flowthru_Data_Storage_Medium_MemoryStorageMedium__ctor_System_Byte___"></a> MemoryStorageMedium\(byte\[\]\)

Creates a new memory storage medium with initial data.

```csharp
public MemoryStorageMedium(byte[] initialData)
```

#### Parameters

`initialData` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

Initial byte buffer

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

Thrown if initialData is null

## Properties

### <a id="Flowthru_Data_Storage_Medium_MemoryStorageMedium_BufferSize"></a> BufferSize

Gets the current buffer size in bytes, or null if no data is stored.

```csharp
public int? BufferSize { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="Flowthru_Data_Storage_Medium_MemoryStorageMedium_Traits"></a> Traits

Structural constraints and capabilities of this storage medium.

```csharp
public StorageTraits Traits { get; }
```

#### Property Value

 [StorageTraits](Flowthru.Data.Capabilities.StorageTraits.md)

#### Remarks

Medium traits focus on WHERE data is stored and the access patterns it supports.
For composed adapters, these traits are merged with format and container traits.

## Methods

### <a id="Flowthru_Data_Storage_Medium_MemoryStorageMedium_Clear"></a> Clear\(\)

Clears the internal buffer, freeing memory.

```csharp
public void Clear()
```

### <a id="Flowthru_Data_Storage_Medium_MemoryStorageMedium_Exists"></a> Exists\(\)

Checks if data exists at this storage location.

```csharp
public FlowIO<bool> Exists()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Effect that produces true if data exists, false otherwise

#### Remarks

<p>
This is used to determine if a catalog entry is a "seed" (Layer 0 input)
or if it's produced by a node in the pipeline.
</p>

### <a id="Flowthru_Data_Storage_Medium_MemoryStorageMedium_ReadStream"></a> ReadStream\(\)

Reads raw bytes from storage as a stream.

```csharp
public FlowIO<Stream> ReadStream()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)\>

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

### <a id="Flowthru_Data_Storage_Medium_MemoryStorageMedium_WriteStream_System_IO_Stream_"></a> WriteStream\(Stream\)

Writes raw bytes to storage from a stream.

```csharp
public FlowIO<FlowUnit> WriteStream(Stream stream)
```

#### Parameters

`stream` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

Stream containing data to write

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Effects.FlowUnit.md)\>

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

