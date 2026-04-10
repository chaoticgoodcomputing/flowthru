# <a id="Flowthru_Core_Data_Validation_InspectionLevel"></a> Enum InspectionLevel

Namespace: [Flowthru.Core.Data.Validation](Flowthru.Core.Data.Validation.md)  
Assembly: Flowthru.Core.dll  

Defines the level of inspection to perform on catalog entries before pipeline execution.

```csharp
public enum InspectionLevel
```

## Fields

`Deep = 2` 

Perform deep inspection: all checks from Shallow plus validation of ALL rows.

<p>
<strong>What Deep Inspection Adds:</strong>
</p>
<ul><li>All checks from Shallow inspection</li><li>Validates EVERY row deserializes successfully</li><li>Checks for data quality issues throughout entire dataset</li></ul>
<p>
Performance: Potentially significant overhead (seconds to minutes for large datasets)
</p>
<p>
<strong>Use Cases:</strong>
</p>
<ul><li>Critical production deployments</li><li>After external data updates</li><li>CI/CD regression testing</li><li>When data corruption is suspected</li></ul>
<p>
<strong>Must be explicitly opted-in by the pipeline creator.</strong>
</p>

`None = 0` 

Skip inspection entirely.

Use when:
- Data source is trusted and validated externally
- Validation overhead is prohibitive for large datasets
- You're explicitly opting out of safety checks

`Shallow = 1` 

Perform shallow inspection: existence, format, headers, and sample rows.

<p>
<strong>What Shallow Inspection Checks:</strong>
</p>
<ul><li>File/resource exists</li><li>Format is valid (parseable as CSV/Excel/Parquet/etc.)</li><li>Headers match expected schema (column names, property mappings)</li><li>First N rows (default: 100) deserialize successfully</li><li>Data types are compatible with schema</li></ul>
<p>
Performance: Minimal overhead (~10-100ms for typical files)
</p>
<p>
<strong>This is the default for all Layer 0 inputs.</strong>
</p>

## Remarks

<p>
Inspection levels are used to validate external data sources (Layer 0 inputs) before
the pipeline begins execution, following the "fail-fast" principle.
</p>
<p>
<strong>Default Behavior:</strong>
- External inputs (Layer 0): Shallow inspection (all storage adapters support inspection)
- Intermediate outputs (Layer 1+): Always None (not inspected, they're created by the pipeline)
</p>
<p>
<strong>When to Use Each Level:</strong>
</p>
<ul><li><span class="term">None</span>
Skip validation entirely. Use for trusted data sources or when validation overhead is prohibitive.
</li><li><span class="term">Shallow</span>
Validate existence, format, headers, and a sample of rows. Fast and catches most common issues.
<strong>This is the default for Layer 0 inputs.</strong>
</li><li><span class="term">Deep</span>
Validate all rows in the dataset. Thorough but expensive. Use for critical data or after
external updates. Must be explicitly opted-in.
</li></ul>

