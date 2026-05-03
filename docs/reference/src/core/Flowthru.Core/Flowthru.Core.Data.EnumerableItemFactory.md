# <a id="Flowthru_Core_Data_EnumerableItemFactory"></a> Class EnumerableItemFactory

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Core.dll  

Extension point for <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref> factory methods.

```csharp
public sealed class EnumerableItemFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EnumerableItemFactory](Flowthru.Core.Data.EnumerableItemFactory.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
IEnumerable&lt;T&gt; is the standard .NET collection interface.
</p>
<p>
<strong>Characteristics:</strong>
</p>
<ul><li><strong>Lazy evaluation:</strong> LINQ queries deferred until enumeration</li><li><strong>Re-enumerable:</strong> Can cause side effects (multiple DB hits, file reads)</li><li><strong>Mutable:</strong> Underlying collection can be modified</li><li><strong>Standard .NET:</strong> Works with all .NET libraries</li></ul>
<p>
<strong>Use Cases:</strong>
</p>
<ul><li>Standard data processing flows (90% of cases)</li><li>Interop with .NET libraries expecting IEnumerable</li><li>LINQ query composition</li><li>Large datasets where you'll enumerate only once</li></ul>
<p>
Format-specific factory methods (CSV, Parquet, Excel) are provided as extension
methods by their respective packages. Add extension methods to this type to
register new formats.
</p>

## Methods

### <a id="Flowthru_Core_Data_EnumerableItemFactory_BinaryDirectory_System_String_System_String_System_String_"></a> BinaryDirectory\(string, string, string\)

Creates a catalog entry over a directory of binary files (one blob per file). Read
produces a <xref href="Flowthru.Core.Data.Directory%601" data-throw-if-not-resolved="false"></xref> keyed by full file path with <code>byte[]</code>
values; Save writes one file per entry, deleting any existing files matching the
pattern first so re-runs are deterministic.

```csharp
public Item<Directory<byte[]>> BinaryDirectory(string label, string directoryPath, string filePattern = "*")
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory.

`filePattern` [string](https://learn.microsoft.com/dotnet/api/system.string)

Glob for matching files (default <code>"*"</code> — every file in the directory). Pass
e.g. <code>"*.png"</code> when the directory hosts a single binary format alongside other
content that should be ignored.

#### Returns

 [Item](Flowthru.Core.Data.Item\-1.md)<[Directory](Flowthru.Core.Data.Directory\-1.md)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]\>\>

#### Remarks

This is intentionally not a partitioning primitive — each file represents an
independent binary unit (a PNG, a PDF, a serialised model). If you need to chunk a
single logical artifact across files, do that in a step before write and reassemble
in a step after read.

### <a id="Flowthru_Core_Data_EnumerableItemFactory_Json__1_System_String_System_String_Flowthru_Core_Data_Storage_IStorageMediumResolver_Flowthru_Core_Data_Storage_IStorageMedium_"></a> Json<TRow\>\(string, string, IStorageMediumResolver?, IStorageMedium?\)

Creates a JSON file catalog item with IEnumerable container for collections.

```csharp
public Item<IEnumerable<TRow>> Json<TRow>(string label, string filePath, IStorageMediumResolver? resolver = null, IStorageMedium? medium = null) where TRow : notnull, IStructuredSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path or URI to JSON file

`resolver` [IStorageMediumResolver](Flowthru.Core.Data.Storage.IStorageMediumResolver.md)?

Optional resolver for remote URIs (e.g., <code>https://</code>, <code>sftp://</code>).
Falls back to <xref href="Flowthru.Core.Data.Storage.Medium.FileStorageMedium" data-throw-if-not-resolved="false"></xref> when <code>null</code>.

`medium` [IStorageMedium](Flowthru.Core.Data.Storage.IStorageMedium.md)?

Explicit medium override. Takes precedence over <code class="paramref">resolver</code> when both
are supplied. Use for per-entry customisation or direct injection in tests.

#### Returns

 [Item](Flowthru.Core.Data.Item\-1.md)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>

Catalog item with file + JSON + IEnumerable composition

#### Type Parameters

`TRow` 

Row schema type (must be structured-serializable)

#### Remarks

<p>
<strong>Requirements:</strong>
</p>
<ul><li>TRow must implement IStructuredSerializable</li><li>TRow supports both flat and nested schemas</li></ul>
<p>
<strong>Supports:</strong>
</p>
<ul><li>Traditional schemas with parameterless constructors</li><li>Modern schemas with required properties (C# 11+)</li><li>Positional records with primary constructors</li></ul>
<p>
<strong>Serialization:</strong> JSON array format for collections
</p>

### <a id="Flowthru_Core_Data_EnumerableItemFactory_JsonDirectory__1_System_String_System_String_"></a> JsonDirectory<TRow\>\(string, string\)

Creates a catalog entry over a directory of JSON files where each file is a JSON
array of <code class="typeparamref">TRow</code> (mirrors the <xref href="Flowthru.Core.Data.EnumerableItemFactory.Json%60%601(System.String%2cSystem.String%2cFlowthru.Core.Data.Storage.IStorageMediumResolver%2cFlowthru.Core.Data.Storage.IStorageMedium)" data-throw-if-not-resolved="false"></xref> single-file
shape). Read produces a <xref href="Flowthru.Core.Data.Directory%601" data-throw-if-not-resolved="false"></xref> keyed by full file path with
<code>IEnumerable&lt;TRow&gt;</code> values; Save writes one JSON file per entry, deleting
existing <code>*.json</code> in the directory first so re-runs are deterministic.

```csharp
public Item<Directory<IEnumerable<TRow>>> JsonDirectory<TRow>(string label, string directoryPath) where TRow : notnull, IStructuredSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory containing the JSON array files

#### Returns

 [Item](Flowthru.Core.Data.Item\-1.md)<[Directory](Flowthru.Core.Data.Directory\-1.md)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>\>

#### Type Parameters

`TRow` 

Row schema type (must be structured-serializable)

#### Remarks

All files must share the same schema. This is intentionally not a partitioning
primitive — each file represents an independent unit. Use <xref href="Flowthru.Core.Data.EnumerableItemFactory.JsonDocuments%60%601(System.String%2cSystem.String)" data-throw-if-not-resolved="false"></xref>
for the singleton-document-per-file shape (one JSON object per file).

### <a id="Flowthru_Core_Data_EnumerableItemFactory_JsonDocuments__1_System_String_System_String_"></a> JsonDocuments<T\>\(string, string\)

Creates a catalog entry over a directory of singleton-JSON-document files (one JSON
object per file). Read produces a <xref href="Flowthru.Core.Data.Directory%601" data-throw-if-not-resolved="false"></xref> keyed by full file path
with deserialised <code class="typeparamref">T</code> values; Save writes one JSON file per
entry, deleting existing <code>*.json</code> in the directory first so re-runs are
deterministic.

```csharp
public Item<Directory<T>> JsonDocuments<T>(string label, string directoryPath) where T : IStructuredSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`directoryPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the directory containing the JSON document files

#### Returns

 [Item](Flowthru.Core.Data.Item\-1.md)<[Directory](Flowthru.Core.Data.Directory\-1.md)<T\>\>

#### Type Parameters

`T` 

Document type (must be structured-serializable)

#### Remarks

Use <xref href="Flowthru.Core.Data.EnumerableItemFactory.JsonDirectory%60%601(System.String%2cSystem.String)" data-throw-if-not-resolved="false"></xref> for the row-collection-per-file shape (each file
is a JSON array). This entry's per-file contract is one JSON object per file —
parallel to <xref href="Flowthru.Core.Data.ItemFactory.Single.Json%60%601(System.String%2cSystem.String)" data-throw-if-not-resolved="false"></xref>.

### <a id="Flowthru_Core_Data_EnumerableItemFactory_Memory__1_System_String_"></a> Memory<TRow\>\(string\)

Creates an in-memory transient catalog item with IEnumerable container.

```csharp
public Item<IEnumerable<TRow>> Memory<TRow>(string label)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

#### Returns

 [Item](Flowthru.Core.Data.Item\-1.md)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>

Catalog item with memory storage (no serialization)

#### Type Parameters

`TRow` 

Row schema type

#### Remarks

<p>
<strong>Use Case:</strong> Intermediate Flow data that doesn't need persistence
</p>
<p>
<strong>Storage Traits:</strong>
</p>
<ul><li>IsPersistent: false (data lost when process ends)</li></ul>

