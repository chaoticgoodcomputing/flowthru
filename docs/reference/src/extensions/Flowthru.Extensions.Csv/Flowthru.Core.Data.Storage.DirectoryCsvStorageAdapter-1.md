# <a id="Flowthru_Core_Data_Storage_DirectoryCsvStorageAdapter_1"></a> Class DirectoryCsvStorageAdapter<TRow\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Extensions.Csv.dll  

Storage adapter that reads all CSV files in a directory and concatenates
them into a single <xref href="System.Collections.Generic.IEnumerable%601" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class DirectoryCsvStorageAdapter<TRow> : ReadOnlyDirectoryStorageAdapter<TRow>, IStorageAdapter<IEnumerable<TRow>> where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Type Parameters

`TRow` 

Row schema type (must be flat and text-serializable)

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
ReadOnlyDirectoryStorageAdapter<TRow\> ← 
[DirectoryCsvStorageAdapter<TRow\>](Flowthru.Core.Data.Storage.DirectoryCsvStorageAdapter\-1.md)

#### Implements

IStorageAdapter<IEnumerable<TRow\>\>

#### Inherited Members

ReadOnlyDirectoryStorageAdapter<TRow\>.Traits, 
ReadOnlyDirectoryStorageAdapter<TRow\>.Load\(\), 
ReadOnlyDirectoryStorageAdapter<TRow\>.Save\(IEnumerable<TRow\>\), 
ReadOnlyDirectoryStorageAdapter<TRow\>.Exists\(\), 
ReadOnlyDirectoryStorageAdapter<TRow\>.InspectShallow\(int\), 
ReadOnlyDirectoryStorageAdapter<TRow\>.InspectDeep\(\), 
ReadOnlyDirectoryStorageAdapter<TRow\>.InspectTarget\(\), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
This adapter is <strong>read-only</strong>. It enumerates every <code>*.csv</code> file in the
given directory in lexicographic order, deserialises each with a shared
<xref href="Flowthru.Core.Data.Storage.Format.CsvFormatSerializer%601" data-throw-if-not-resolved="false"></xref>, and returns all rows concatenated.
</p>
<p>
All files must share the same schema (identical column headers). Files from
mixed schemas will cause deserialization errors at load time.
</p>
<p>
Typical use case: a raw ingest layer where data is delivered as one file per
day, one file per region, etc.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_DirectoryCsvStorageAdapter_1__ctor_System_String_"></a> DirectoryCsvStorageAdapter\(string\)

Creates a new directory CSV adapter.

```csharp
public DirectoryCsvStorageAdapter(string directoryPath)
```

#### Parameters

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory containing CSV files.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Thrown if <code class="paramref">directoryPath</code> is null or whitespace.

## Properties

### <a id="Flowthru_Core_Data_Storage_DirectoryCsvStorageAdapter_1_Traits"></a> Traits

Structural constraints and capabilities of this storage implementation.

```csharp
public override StorageTraits Traits { get; }
```

#### Property Value

 StorageTraits

#### Remarks

<p>
Adapter authors must declare what their storage can and cannot do.
These are intrinsic properties of the storage medium, not runtime state.
</p>
<p>
Pipeline validation uses these traits to fail fast when a pipeline attempts
invalid operations (e.g., writing to a read-only source).
</p>

## Methods

### <a id="Flowthru_Core_Data_Storage_DirectoryCsvStorageAdapter_1_LoadFile_System_String_System_Threading_CancellationToken_"></a> LoadFile\(string, CancellationToken\)

Deserializes one file into an async stream of <code class="typeparamref">TItem</code> values.

```csharp
protected override IAsyncEnumerable<TRow> LoadFile(string filePath, CancellationToken ct)
```

#### Parameters

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Absolute or relative path to the file.

`ct` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token.

#### Returns

 [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<TRow\>

### <a id="Flowthru_Core_Data_Storage_DirectoryCsvStorageAdapter_1_ValidateFileAsync_System_String_System_Int32_System_Threading_CancellationToken_"></a> ValidateFileAsync\(string, int, CancellationToken\)

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

