# <a id="Flowthru_Core_Data_Storage_SingletonXmlStorageAdapter_1"></a> Class SingletonXmlStorageAdapter<T\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Extensions.Xml.dll  

Storage adapter for a single XML file deserialized to a singleton object.

```csharp
public sealed class SingletonXmlStorageAdapter<T> : IStorageAdapter<T> where T : IStructuredSerializable
```

#### Type Parameters

`T` 

The document type. Must be XML-serializable and structured-serializable.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SingletonXmlStorageAdapter<T\>](Flowthru.Core.Data.Storage.SingletonXmlStorageAdapter\-1.md)

#### Implements

IStorageAdapter<T\>

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Mirrors <xref href="Flowthru.Core.Data.Storage.SingletonJsonStorageAdapter%601" data-throw-if-not-resolved="false"></xref>: direct single-object serialization
that bypasses the medium/format/container composition, since singleton XML documents
do not stream rows.
</p>
<p>
<strong>Serialization:</strong> Uses <xref href="System.Xml.Serialization.XmlSerializer" data-throw-if-not-resolved="false"></xref>. Decorate <code class="typeparamref">T</code>
with <code>[XmlRoot]</code>, <code>[XmlElement]</code>, and <code>[XmlAttribute]</code> as needed.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_SingletonXmlStorageAdapter_1__ctor_System_String_"></a> SingletonXmlStorageAdapter\(string\)

Creates a new singleton XML storage adapter.

```csharp
public SingletonXmlStorageAdapter(string filePath)
```

#### Parameters

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the XML file.

## Properties

### <a id="Flowthru_Core_Data_Storage_SingletonXmlStorageAdapter_1_Traits"></a> Traits

Structural constraints and capabilities of this storage implementation.

```csharp
public StorageTraits Traits { get; }
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

### <a id="Flowthru_Core_Data_Storage_SingletonXmlStorageAdapter_1_Exists"></a> Exists\(\)

Checks if data exists at this storage location.

```csharp
public FlowIO<bool> Exists()
```

#### Returns

 FlowIO<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Effect that produces true if data exists, false otherwise

#### Remarks

<p>
Delegates to the underlying medium's Exists check.
Used to determine if a catalog entry is a seed (Layer 0 input).
</p>

### <a id="Flowthru_Core_Data_Storage_SingletonXmlStorageAdapter_1_InspectDeep"></a> InspectDeep\(\)

Performs deep validation by examining the entire dataset.

```csharp
public FlowIO<ValidationResult> InspectDeep()
```

#### Returns

 FlowIO<ValidationResult\>

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

### <a id="Flowthru_Core_Data_Storage_SingletonXmlStorageAdapter_1_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

Performs shallow validation by checking data availability and sampling a subset of data.

```csharp
public FlowIO<ValidationResult> InspectShallow(int sampleSize)
```

#### Parameters

`sampleSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of rows/records to sample for validation

#### Returns

 FlowIO<ValidationResult\>

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

### <a id="Flowthru_Core_Data_Storage_SingletonXmlStorageAdapter_1_InspectTarget"></a> InspectTarget\(\)

Validates that this storage location is accessible as a write destination.

```csharp
public FlowIO<ValidationResult> InspectTarget()
```

#### Returns

 FlowIO<ValidationResult\>

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

### <a id="Flowthru_Core_Data_Storage_SingletonXmlStorageAdapter_1_Load"></a> Load\(\)

Loads data from storage.

```csharp
public FlowIO<T> Load()
```

#### Returns

 FlowIO<T\>

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

### <a id="Flowthru_Core_Data_Storage_SingletonXmlStorageAdapter_1_Save__0_"></a> Save\(T\)

Saves data to storage.

```csharp
public FlowIO<FlowUnit> Save(T data)
```

#### Parameters

`data` T

The data to save

#### Returns

 FlowIO<FlowUnit\>

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

