# <a id="Flowthru_Core_Abstractions_IStructuredSerializable"></a> Interface IStructuredSerializable

Namespace: [Flowthru.Core.Abstractions](Flowthru.Core.Abstractions.md)  
Assembly: Flowthru.Core.dll  

Marker interface for schema types that can be serialized to structured formats (JSON, XML).

```csharp
public interface IStructuredSerializable
```

## Examples

<pre><code class="lang-csharp">// ✅ Nested schema - structured serialization required
public sealed record CrossValidationResults(
    List&lt;FoldMetric&gt; FoldMetrics,
    double MeanScore
) : INestedSchema, IStructuredSerializable;

// ✅ Flat schema - structured serialization optional
public sealed record ModelMetrics(
    double Accuracy,
    double Precision,
    double Recall
) : IFlatSchema, IStructuredSerializable, ITextSerializable;

// ✅ Configuration object
public sealed record TrainingConfig(
    int Epochs,
    double LearningRate,
    Dictionary&lt;string, object&gt; Hyperparameters
) : INestedSchema, IStructuredSerializable;</code></pre>

## Remarks

<p>
<strong>Purpose:</strong> Indicates a schema is compatible with structured, hierarchical
formats that can represent nested objects and collections.
</p>
<p>
<strong>Compatible Formats:</strong>
</p>
<ul><li>JSON (JavaScript Object Notation)</li><li>XML (Extensible Markup Language)</li><li>YAML (YAML Ain't Markup Language)</li><li>TOML (Tom's Obvious Minimal Language)</li></ul>
<p>
<strong>Flexibility:</strong>
</p>
<p>
Unlike <xref href="Flowthru.Core.Abstractions.ITextSerializable" data-throw-if-not-resolved="false"></xref> which requires flat schemas, structured formats
can handle both:
</p>
<ul><li><xref href="Flowthru.Core.Abstractions.IFlatSchema" data-throw-if-not-resolved="false"></xref> - Simple, flat data structures</li><li><xref href="Flowthru.Core.Abstractions.INestedSchema" data-throw-if-not-resolved="false"></xref> - Complex, hierarchical data structures</li></ul>
<p>
<strong>Design Rationale:</strong>
</p>
<p>
Structured serialization is the most flexible format capability:
- Can represent any schema structure (flat or nested)
- Human-readable and widely supported
- Preserves type information and hierarchy
- Suitable for configuration, results, and complex data
</p>
<p>
<strong>Typical Usage Patterns:</strong>
</p>
<ul><li><strong>Nested schemas:</strong> Must use structured serialization (JSON/XML only option)</li><li><strong>Flat schemas:</strong> Can use structured serialization when human-readability matters</li><li><strong>Configuration objects:</strong> Often use JSON for flexibility</li><li><strong>Model metadata:</strong> JSON captures complex metrics and parameters</li></ul>

