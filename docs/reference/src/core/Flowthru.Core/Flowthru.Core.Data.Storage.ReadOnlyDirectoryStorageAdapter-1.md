# <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1"></a> Class ReadOnlyDirectoryStorageAdapter<TItem\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Abstract base for read-only storage adapters that aggregate all files of a given
pattern within a directory into a single item sequence.

```csharp
public abstract class ReadOnlyDirectoryStorageAdapter<TItem> : IStorageAdapter<IEnumerable<TItem>>
```

#### Type Parameters

`TItem` 

The item type yielded per file (or per row within a file).

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ReadOnlyDirectoryStorageAdapter<TItem\>](Flowthru.Core.Data.Storage.ReadOnlyDirectoryStorageAdapter\-1.md)

#### Implements

[IStorageAdapter<IEnumerable<TItem\>\>](Flowthru.Core.Data.Storage.IStorageAdapter\-1.md)

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
Owns all directory-as-medium concerns: existence checks, lexicographic file
enumeration, <xref href="Flowthru.Core.Data.Storage.ReadOnlyDirectoryStorageAdapter%601.Save(System.Collections.Generic.IEnumerable%7b%600%7d)" data-throw-if-not-resolved="false"></xref> refusal, and pre-flight validation scaffolding.
Subclasses implement two abstract members that encode the format-specific behavior:
</p>
<ul><li>
  <xref href="Flowthru.Core.Data.Storage.ReadOnlyDirectoryStorageAdapter%601.LoadFile(System.String%2cSystem.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> — deserialize one file into an async stream of
  <code class="typeparamref">TItem</code> values.
</li><li>
  <xref href="Flowthru.Core.Data.Storage.ReadOnlyDirectoryStorageAdapter%601.ValidateFileAsync(System.String%2cSystem.Int32%2cSystem.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> — probe one file at a given sample depth and
  return a <xref href="Flowthru.Core.Data.Validation.ValidationResult" data-throw-if-not-resolved="false"></xref>.
</li></ul>
<p>
<strong>Inspection semantics:</strong>
<xref href="Flowthru.Core.Data.Storage.ReadOnlyDirectoryStorageAdapter%601.InspectShallow(System.Int32)" data-throw-if-not-resolved="false"></xref> applies a per-file sample to <em>every</em> file in
the directory, returning the first failure encountered.
<xref href="Flowthru.Core.Data.Storage.ReadOnlyDirectoryStorageAdapter%601.InspectDeep" data-throw-if-not-resolved="false"></xref> applies an unbounded scan to every file.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1__ctor_System_String_System_String_System_String_"></a> ReadOnlyDirectoryStorageAdapter\(string, string, string\)

Initializes the adapter.

```csharp
protected ReadOnlyDirectoryStorageAdapter(string directoryPath, string filePattern, string catalogKey)
```

#### Parameters

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory.

`filePattern` [string](https://learn.microsoft.com/dotnet/api/system.string)

Glob pattern for eligible files (e.g. <code>"*.csv"</code>).

`catalogKey` [string](https://learn.microsoft.com/dotnet/api/system.string)

Key used in directory-level validation error reports.

## Fields

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_DirectoryPath"></a> DirectoryPath

The path to the directory managed by this adapter.

```csharp
protected readonly string DirectoryPath
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_FilePattern"></a> FilePattern

The glob pattern used to select eligible files (e.g. <code>*.csv</code>).

```csharp
protected readonly string FilePattern
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_Traits"></a> Traits

Structural constraints and capabilities of this storage implementation.

```csharp
public virtual StorageTraits Traits { get; }
```

#### Property Value

 [StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md)

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

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_Exists"></a> Exists\(\)

Checks if data exists at this storage location.

```csharp
public FlowIO<bool> Exists()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Effect that produces true if data exists, false otherwise

#### Remarks

<p>
Delegates to the underlying medium's Exists check.
Used to determine if a catalog entry is a seed (Layer 0 input).
</p>

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_GetFiles"></a> GetFiles\(\)

Enumerates eligible files in the directory in lexicographic order.
Returns an empty sequence if the directory does not exist.

```csharp
protected IEnumerable<string> GetFiles()
```

#### Returns

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_InspectDeep"></a> InspectDeep\(\)

Performs deep validation by examining the entire dataset.

```csharp
public FlowIO<ValidationResult> InspectDeep()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

#### Remarks

<p>
<strong>Semantic Intent:</strong> Validate that all data is available, accessible, and valid.
</p>
<p>
<strong>Additional Checks Beyond Shallow:</strong>
</p>
<ul><li>Validate ALL rows can be deserialized (not just sample)</li><li>Check data quality constraints across entire dataset</li><li>Detect corruption or inconsistencies throughout data</li></ul>
<p>
<strong>Implementation Guidelines:</strong>
</p>
<ul><li>File adapters: Read and validate entire file</li><li>Memory adapters: Validate all stored data</li><li>Database adapters: Full table scan with validation</li><li>Null adapters: Always return success (no data required)</li></ul>
<p>
<strong>Performance:</strong> Potentially expensive - only use when data integrity is critical.
</p>

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

Performs shallow validation by checking data availability and sampling a subset of data.

```csharp
public FlowIO<ValidationResult> InspectShallow(int sampleSize)
```

#### Parameters

`sampleSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of rows/records to sample for validation

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

#### Remarks

<p>
<strong>Semantic Intent:</strong> Validate that data is available and accessible.
</p>
<p>
<strong>Typical Checks:</strong>
</p>
<ul><li>Data source exists (file, table, etc.)</li><li>Data source is accessible (permissions, connectivity)</li><li>Sample rows can be read and deserialized successfully</li><li>Schema matches expected structure</li></ul>
<p>
<strong>Implementation Guidelines:</strong>
</p>
<ul><li>File adapters: Check file exists, read and validate sample rows</li><li>Memory adapters: Check if data has been initialized</li><li>Database adapters: Check table exists, query sample rows</li><li>Null adapters: Always return success (no data required)</li></ul>
<p>
<strong>Performance:</strong> Should be fast (~10-100ms) - suitable for pre-flight validation.
</p>

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_InspectTarget"></a> InspectTarget\(\)

Validates that this storage location is accessible as a write destination.

```csharp
public FlowIO<ValidationResult> InspectTarget()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

#### Remarks

<p>
<strong>Semantic Intent:</strong> Validate that the destination can accept writes
before any pipeline step executes. This is distinct from <xref href="Flowthru.Core.Data.Storage.IStorageAdapter%601.InspectShallow(System.Int32)" data-throw-if-not-resolved="false"></xref>,
which validates that readable data exists.
</p>
<p>
<strong>Typical Checks:</strong>
</p>
<ul><li>File adapters: Parent directory exists and process has write permission</li><li>Database adapters: Target table exists, schema is compatible, connection is valid</li><li>Read-only adapters (<code>CanWrite = false</code>): Return success trivially</li><li>Memory / null adapters: Return success trivially</li></ul>
<p>
<strong>When Called:</strong> During pre-flight validation, after external inputs are
inspected and before any step executes. Skipped if <code>Traits.CanInspect = false</code>
or if explicitly disabled via <code>ValidationOptions.SkipTargetInspection()</code>.
</p>

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_Load"></a> Load\(\)

Loads data from storage.

```csharp
public FlowIO<IEnumerable<TItem>> Load()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TItem\>\>

Effect that produces data on success

#### Remarks

<p>
<strong>Execution Flow:</strong>
</p>
<p>
For composed adapters, this orchestrates:
</p>
<pre><code class="lang-csharp">1. medium.ReadStream()           → Stream
2. format.DeserializeRows()      → IAsyncEnumerable&lt;TRow&gt;
3. container.FromRows()          → TContainer</code></pre>
<p>
<strong>Error Handling:</strong>
</p>
<p>
Errors from any layer are propagated:
- Medium errors (file not found, access denied)
- Format errors (parse failures, schema mismatches)
- Container errors (memory allocation, type conversion)
</p>

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_LoadFile_System_String_System_Threading_CancellationToken_"></a> LoadFile\(string, CancellationToken\)

Deserializes one file into an async stream of <code class="typeparamref">TItem</code> values.

```csharp
protected abstract IAsyncEnumerable<TItem> LoadFile(string filePath, CancellationToken ct)
```

#### Parameters

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Absolute or relative path to the file.

`ct` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token.

#### Returns

 [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<TItem\>

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_Save_System_Collections_Generic_IEnumerable__0__"></a> Save\(IEnumerable<TItem\>\)

Saves data to storage.

```csharp
public FlowIO<FlowUnit> Save(IEnumerable<TItem> data)
```

#### Parameters

`data` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TItem\>

The data to save

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Core.Effects.FlowUnit.md)\>

Effect that completes on successful save

#### Remarks

<p>
<strong>Execution Flow:</strong>
</p>
<p>
For composed adapters, this orchestrates:
</p>
<pre><code class="lang-csharp">1. container.ToRows()            → IAsyncEnumerable&lt;TRow&gt;
2. format.SerializeRows()        → Stream
3. medium.WriteStream()          → FlowUnit</code></pre>
<p>
<strong>Atomicity:</strong>
</p>
<p>
Implementations should strive for atomic saves to avoid partial writes on failure.
</p>

### <a id="Flowthru_Core_Data_Storage_ReadOnlyDirectoryStorageAdapter_1_ValidateFileAsync_System_String_System_Int32_System_Threading_CancellationToken_"></a> ValidateFileAsync\(string, int, CancellationToken\)

Validates one file at the given sample depth.

```csharp
protected abstract Task<ValidationResult> ValidateFileAsync(string filePath, int sampleSize, CancellationToken ct)
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

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

