# <a id="Flowthru_Core_Data_ItemFactory"></a> Class ItemFactory

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Core.dll  

Static factory methods for creating catalog entries with common configurations.

```csharp
public static class ItemFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ItemFactory](Flowthru.Core.Data.ItemFactory.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">// Tier 1: No annotations - property names match external field names
public record SimpleSchema(
    int Id,           // Looks for "Id" in CSV/Excel/JSON
    string Name       // Looks for "Name" in CSV/Excel/JSON
) : IFlatSchema, ITextSerializable;

var simple = ItemFactory.Enumerable.Csv&lt;SimpleSchema&gt;("data", "data.csv");

// Tier 2: Explicit annotations - handle naming mismatches
public record ShuttleSchema(
    [SerializedLabel("id")]
    string Id,

    [SerializedLabel("shuttle_location")]        // snake_case in file
    string ShuttleLocation,

    [SerializedLabel("d_check_complete")]        // snake_case in file
    bool DCheckComplete,

    [SerializedLabel("Company ID")]              // space-separated in file
    int CompanyId
) : IFlatSchema, ITextSerializable;

var shuttles = ItemFactory.Enumerable.Excel&lt;ShuttleSchema&gt;(
    "shuttles",
    "data/shuttles.xlsx",
    "Sheet1"
);

// Same schema works across all formats
var csv = ItemFactory.Enumerable.Csv&lt;ShuttleSchema&gt;("shuttles", "data/shuttles.csv");
var json = ItemFactory.Enumerable.Json&lt;ShuttleSchema&gt;("shuttles", "data/shuttles.json");</code></pre>

## Remarks

<p>
<strong>Design Pattern:</strong> Static factory methods that compose storage adapters
from medium + format + container layers.
</p>
<p>
<strong>Discoverability:</strong> All factory methods are in one place with IntelliSense support.
</p>
<p>
<strong>Type Safety:</strong> Generic constraints enforce schema compatibility at compile-time.
</p>
<p>
<strong>Field Name Mapping with SerializedLabel:</strong>
</p>
<p>
Use the <xref href="Flowthru.Core.Abstractions.SerializedLabelAttribute" data-throw-if-not-resolved="false"></xref> to map C# property names to external field names
when they differ. This works uniformly across CSV, Excel, JSON, and most formats.
</p>

## Properties

### <a id="Flowthru_Core_Data_ItemFactory_Enumerable"></a> Enumerable

Factory methods for <xref href="System.Collections.Generic.IEnumerable%601" data-throw-if-not-resolved="false"></xref> catalog entries.

```csharp
public static EnumerableItemFactory Enumerable { get; }
```

#### Property Value

 [EnumerableItemFactory](Flowthru.Core.Data.EnumerableItemFactory.md)

