# <a id="Flowthru_Data_Capabilities_StorageTraits"></a> Class StorageTraits

Namespace: [Flowthru.Data.Capabilities](Flowthru.Data.Capabilities.md)  
Assembly: Flowthru.Core.dll  

Describes the structural constraints and capabilities of a storage implementation.
Defaults represent filesystem-file baseline behavior.

```csharp
public record StorageTraits : IEquatable<StorageTraits>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StorageTraits](Flowthru.Data.Capabilities.StorageTraits.md)

#### Implements

[IEquatable<StorageTraits\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

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
<strong>Design Philosophy:</strong>
</p>
<p>
A filesystem file is the median storage mechanism — the "zero" from which we measure deviations.
A <strong>constraint</strong> narrows from this baseline (e.g., read-only, non-persistent, non-inspectable).
A <strong>capability</strong> widens beyond it (e.g., streamable, appendable, transactional).
</p>
<p>
<strong>Constraint Examples:</strong>
</p>
<ul><li><strong>Read-only sources:</strong> HTTP GET endpoints, Excel files, database views — set <code>CanWrite = false</code></li><li><strong>Write-only sinks:</strong> Logging endpoints, append-only audit tables — set <code>CanRead = false</code></li><li><strong>Non-inspectable:</strong> Remote sources that can't be sampled cheaply — set <code>CanInspect = false</code></li><li><strong>Non-persistent:</strong> In-memory caches, temporary buffers — set <code>IsPersistent = false</code></li><li><strong>Network-dependent:</strong> Remote databases, S3, HTTP — set <code>RequiresNetwork = true</code></li></ul>
<p>
<strong>Capability Examples:</strong>
</p>
<ul><li><strong>Streamable:</strong> CSV files, database queries, Parquet — set <code>CanStream = true</code></li><li><strong>Appendable:</strong> Log files, Spark SaveMode.Append, append-only tables — set <code>CanAppend = true</code></li><li><strong>Transactional:</strong> Database writes, ACID-compliant stores — set <code>IsTransactional = true</code></li></ul>
<p>
<strong>Two-Level Constraint Model:</strong>
</p>
<p>
Traits are declared at two levels:
</p>
<ul><li><strong>Adapter level:</strong> The adapter author declares what the storage medium intrinsically supports.
These are structural truths (e.g., an HTTP GET endpoint cannot write).</li><li><strong>Catalog level:</strong> The pipeline author can further constrain an adapter using <code>Item.Constrain()</code>.
Constraints can only tighten, never loosen (one-way ratchet).</li></ul>
<p>
<strong>Usage in Adapters:</strong>
</p>
<pre><code class="lang-csharp">public sealed class EFCoreStorageAdapter&lt;T&gt; : IStorageAdapter&lt;IEnumerable&lt;T&gt;&gt;
{
    public StorageTraits Traits { get; }

    public EFCoreStorageAdapter(DbContext context, bool readOnly = false)
    {
        Traits = new StorageTraits
        {
            CanWrite = !readOnly,
            RequiresNetwork = true,
            IsTransactional = true,
            CanStream = true,
        };
    }
}</code></pre>
<p>
<strong>Usage in Catalogs:</strong>
</p>
<pre><code class="lang-csharp">public IItem&lt;IEnumerable&lt;Company&gt;&gt; ReferenceData =&gt;
    GetOrCreateEntry(() =&gt; Items.Enumerable.Csv&lt;Company&gt;(
        "ref_data", $"{_basePath}/reference.csv")
        .Constrain(t =&gt; t with { CanWrite = false }));</code></pre>

## Properties

### <a id="Flowthru_Data_Capabilities_StorageTraits_CanAppend"></a> CanAppend

Can data be appended without replacing existing data?

```csharp
public bool CanAppend { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: <code>false</code> (filesystem file writes typically overwrite).
Set to <code>true</code> for append-only logs, Spark SaveMode.Append, incremental tables.

### <a id="Flowthru_Data_Capabilities_StorageTraits_CanInspect"></a> CanInspect

Can the source be inspected for pre-flight validation?

```csharp
public bool CanInspect { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: <code>true</code> (filesystem files can be sampled).
Set to <code>false</code> for sources that are expensive to validate (remote HTTP, distributed Spark).

### <a id="Flowthru_Data_Capabilities_StorageTraits_CanRead"></a> CanRead

Can data be read from this source?

```csharp
public bool CanRead { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: <code>true</code> (filesystem files are readable).
Set to <code>false</code> for write-only sinks (logging endpoints, audit tables).

### <a id="Flowthru_Data_Capabilities_StorageTraits_CanStream"></a> CanStream

Can data be lazily streamed without full materialization?

```csharp
public bool CanStream { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: <code>false</code> (filesystem file I/O typically buffers).
Set to <code>true</code> for CSV streaming, database cursors, Parquet row groups.
Enables memory-efficient processing of large datasets.

### <a id="Flowthru_Data_Capabilities_StorageTraits_CanWrite"></a> CanWrite

Can data be written to this source?

```csharp
public bool CanWrite { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: <code>true</code> (filesystem files are writable).
Set to <code>false</code> for read-only sources (HTTP GET, Excel files, database views).

### <a id="Flowthru_Data_Capabilities_StorageTraits_IsPersistent"></a> IsPersistent

Does data survive across pipeline runs?

```csharp
public bool IsPersistent { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: <code>true</code> (filesystem files persist).
Set to <code>false</code> for in-memory caches, temporary buffers, or transient state.

### <a id="Flowthru_Data_Capabilities_StorageTraits_IsTransactional"></a> IsTransactional

Are writes atomic (all-or-nothing)?

```csharp
public bool IsTransactional { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: <code>false</code> (filesystem file writes are not ACID).
Set to <code>true</code> for database transactions, ACID-compliant stores.

### <a id="Flowthru_Data_Capabilities_StorageTraits_RequiresNetwork"></a> RequiresNetwork

Does this storage require network access?

```csharp
public bool RequiresNetwork { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Default: <code>false</code> (filesystem files are local).
Set to <code>true</code> for remote databases, S3, HTTP endpoints.
Used for pre-flight validation in offline/CI environments.

