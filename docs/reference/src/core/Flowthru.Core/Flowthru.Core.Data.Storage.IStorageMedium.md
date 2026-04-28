# <a id="Flowthru_Core_Data_Storage_IStorageMedium"></a> Interface IStorageMedium

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Interface for storage medium - handles raw byte stream I/O.

```csharp
public interface IStorageMedium
```

## Examples

<pre><code class="lang-csharp">// File storage medium
var fileMedium = new FileStorageMedium("data/file.csv");
var readResult = await fileMedium.ReadStream().Run();</code></pre>

## Remarks

<p>
<strong>Responsibility:</strong> Abstract WHERE data is stored (file system, memory, network, database).
</p>
<p>
<strong>Separation of Concerns:</strong>
</p>
<p>
The storage medium layer is isolated from:
- Format serialization (CSV, JSON, Parquet) - handled by <xref href="Flowthru.Core.Data.Storage.IFormatSerializer%601" data-throw-if-not-resolved="false"></xref>
- Container representation (IEnumerable, IDataView) - handled by <xref href="Flowthru.Core.Data.Storage.IContainerAdapter%602" data-throw-if-not-resolved="false"></xref>
</p>
<p>
<strong>Design Pattern:</strong>
</p>
<p>
This is the lowest layer in the composition pattern:
</p>
<pre><code class="lang-csharp">Medium (bytes) → Format (rows) → Container (in-memory)
File/Memory    → CSV/JSON      → IEnumerable/IDataView</code></pre>
<p>
<strong>Effect Types:</strong>
</p>
<p>
All operations return <xref href="Flowthru.Core.Effects.FlowIO%601" data-throw-if-not-resolved="false"></xref> effects to represent:
- I/O operations that can fail
- Async execution
- Cancellation support
- Functional composition
</p>

## Properties

### <a id="Flowthru_Core_Data_Storage_IStorageMedium_Traits"></a> Traits

Structural constraints and capabilities of this storage medium.

```csharp
StorageTraits Traits { get; }
```

#### Property Value

 [StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md)

#### Remarks

Medium traits focus on WHERE data is stored and the access patterns it supports.
For composed adapters, these traits are merged with format and container traits.

## Methods

### <a id="Flowthru_Core_Data_Storage_IStorageMedium_Exists"></a> Exists\(\)

Checks if data exists at this storage location.

```csharp
FlowIO<bool> Exists()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Effect that produces true if data exists, false otherwise

#### Remarks

<p>
This is used to determine if a catalog entry is a "seed" (Layer 0 input)
or if it's produced by a step in the pipeline.
</p>

### <a id="Flowthru_Core_Data_Storage_IStorageMedium_InspectTarget"></a> InspectTarget\(\)

Validates that this storage location is accessible as a write destination.

```csharp
FlowIO<ValidationResult> InspectTarget()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

#### Remarks

Default implementation returns success — override in medium implementations that
can meaningfully probe write access before execution (e.g., filesystem path checks).

### <a id="Flowthru_Core_Data_Storage_IStorageMedium_ReadStream"></a> ReadStream\(\)

Reads raw bytes from storage as a stream.

```csharp
FlowIO<Stream> ReadStream()
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

### <a id="Flowthru_Core_Data_Storage_IStorageMedium_WriteStream_System_IO_Stream_"></a> WriteStream\(Stream\)

Writes raw bytes to storage from a stream.

```csharp
FlowIO<FlowUnit> WriteStream(Stream stream)
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

