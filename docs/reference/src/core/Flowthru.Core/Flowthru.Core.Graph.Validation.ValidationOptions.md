# <a id="Flowthru_Core_Graph_Validation_ValidationOptions"></a> Class ValidationOptions

Namespace: [Flowthru.Core.Graph.Validation](Flowthru.Core.Graph.Validation.md)  
Assembly: Flowthru.Core.dll  

Configuration for pipeline validation behavior.

```csharp
public class ValidationOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ValidationOptions](Flowthru.Core.Graph.Validation.ValidationOptions.md)

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
ValidationOptions provides pipeline-level overrides for validation configuration.
The primary mechanism for validation configuration is catalog-level via the
<xref href="Flowthru.Core.Data.IItem.PreferredInspectionLevel" data-throw-if-not-resolved="false"></xref> property and the fluent
<code>.WithInspectionLevel()</code> API.
</p>
<p>
<strong>Default Behavior (if not configured):</strong>
</p>
<ul><li>Catalog entry has PreferredInspectionLevel set → use that level</li><li>Layer 0 inputs → Shallow inspection (all storage adapters support inspection)</li><li>All intermediate outputs (Layer 1+) → None (never inspected)</li></ul>
<p>
<strong>Catalog-Level Configuration (Recommended):</strong>
</p>
<pre><code class="lang-csharp">public ICatalogDataset&lt;Company&gt; Companies =&gt;
  CreateDataset(() =&gt; new CsvCatalogDataset&lt;Company&gt;("companies", "data/companies.csv")
    .WithInspectionLevel(InspectionLevel.Deep));</code></pre>
<p>
<strong>Pipeline-Level Override (Advanced):</strong>
</p>
<pre><code class="lang-csharp">builder
  .RegisterPipeline&lt;MyCatalog&gt;("data_processing", MyPipeline.Create)
  .WithValidation(validation =&gt; {
    // Override catalog-level setting for this specific pipeline
    validation.Inspect(catalog.Companies, InspectionLevel.Shallow); // Temporarily use shallow
  });</code></pre>
<p>
<strong>Design Rationale:</strong>
</p>
<p>
Validation configuration is primarily a property of the data source itself, not the pipeline
consuming it. Critical external datasets should always be deeply validated, regardless of
which pipeline uses them. Pipeline-level overrides exist for rare cases where different
validation is needed temporarily (e.g., performance testing, debugging).
</p>

## Methods

### <a id="Flowthru_Core_Graph_Validation_ValidationOptions_Default"></a> Default\(\)

Creates a new ValidationOptions instance with default settings.

```csharp
public static ValidationOptions Default()
```

#### Returns

 [ValidationOptions](Flowthru.Core.Graph.Validation.ValidationOptions.md)

### <a id="Flowthru_Core_Graph_Validation_ValidationOptions_Inspect_Flowthru_Core_Graph_INode_Flowthru_Core_Data_Validation_InspectionLevel_"></a> Inspect\(INode, InspectionLevel\)

Specifies the inspection level for a specific catalog entry.

```csharp
public ValidationOptions Inspect(INode catalogEntry, InspectionLevel level)
```

#### Parameters

`catalogEntry` [INode](Flowthru.Core.Graph.INode.md)

The catalog entry to configure

`level` [InspectionLevel](Flowthru.Core.Data.Validation.InspectionLevel.md)

The inspection level to use for this entry

#### Returns

 [ValidationOptions](Flowthru.Core.Graph.Validation.ValidationOptions.md)

This ValidationOptions instance for fluent chaining

#### Remarks

This configuration only applies to Layer 0 inputs (external data).
Intermediate outputs are never inspected regardless of this setting.

