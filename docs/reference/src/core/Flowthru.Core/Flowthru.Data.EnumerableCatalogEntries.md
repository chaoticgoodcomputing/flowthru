# <a id="Flowthru_Data_EnumerableItems"></a> Class EnumerableItems

Namespace: [Flowthru.Data](Flowthru.Data.md)  
Assembly: Flowthru.Core.dll  

Extension point for <xref href="Flowthru.Data.Items.Enumerable" data-throw-if-not-resolved="false"></xref> factory methods.

```csharp
public sealed class EnumerableItems
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EnumerableItems](Flowthru.Data.EnumerableItems.md)

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
<ul><li>Standard data processing pipelines (90% of cases)</li><li>Interop with .NET libraries expecting IEnumerable</li><li>LINQ query composition</li><li>Large datasets where you'll enumerate only once</li></ul>
<p>
Format-specific factory methods (CSV, Parquet, Excel) are provided as extension
methods by their respective packages. Add extension methods to this type to
register new formats.
</p>

## Methods

### <a id="Flowthru_Data_EnumerableItems_Json__1_System_String_System_String_"></a> Json<TRow\>\(string, string\)

Creates a JSON file catalog entry with IEnumerable container for collections.

```csharp
public Item<IEnumerable<TRow>> Json<TRow>(string label, string filePath) where TRow : notnull, IStructuredSerializable
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to JSON file

#### Returns

 [Item](Flowthru.Data.Item\-1.md)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>

Catalog entry with file + JSON + IEnumerable composition

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

### <a id="Flowthru_Data_EnumerableItems_Memory__1_System_String_"></a> Memory<TRow\>\(string\)

Creates an in-memory transient catalog entry with IEnumerable container.

```csharp
public Item<IEnumerable<TRow>> Memory<TRow>(string label)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

#### Returns

 [Item](Flowthru.Data.Item\-1.md)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<TRow\>\>

Catalog entry with memory storage (no serialization)

#### Type Parameters

`TRow` 

Row schema type

#### Remarks

<p>
<strong>Use Case:</strong> Intermediate pipeline data that doesn't need persistence
</p>
<p>
<strong>Storage Traits:</strong>
</p>
<ul><li>IsPersistent: false (data lost when process ends)</li></ul>

