namespace Flowthru.Core.Abstractions;

/// <summary>
/// Marker interface for schema types that can be serialized to structured formats (JSON, XML).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Indicates a schema is compatible with structured, hierarchical
/// formats that can represent nested objects and collections.
/// </para>
/// <para>
/// <strong>Compatible Formats:</strong>
/// </para>
/// <list type="bullet">
/// <item>JSON (JavaScript Object Notation)</item>
/// <item>XML (Extensible Markup Language)</item>
/// <item>YAML (YAML Ain't Markup Language)</item>
/// <item>TOML (Tom's Obvious Minimal Language)</item>
/// </list>
/// <para>
/// <strong>Flexibility:</strong>
/// </para>
/// <para>
/// Unlike <see cref="ITextSerializable"/> which requires flat schemas, structured formats
/// can handle both:
/// </para>
/// <list type="bullet">
/// <item><see cref="IFlatSchema"/> - Simple, flat data structures</item>
/// <item><see cref="INestedSchema"/> - Complex, hierarchical data structures</item>
/// </list>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// Structured serialization is the most flexible format capability:
/// - Can represent any schema structure (flat or nested)
/// - Human-readable and widely supported
/// - Preserves type information and hierarchy
/// - Suitable for configuration, results, and complex data
/// </para>
/// <para>
/// <strong>Typical Usage Patterns:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>Nested schemas:</strong> Must use structured serialization (JSON/XML only option)</item>
/// <item><strong>Flat schemas:</strong> Can use structured serialization when human-readability matters</item>
/// <item><strong>Configuration objects:</strong> Often use JSON for flexibility</item>
/// <item><strong>Model metadata:</strong> JSON captures complex metrics and parameters</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // ✅ Nested schema - structured serialization required
/// public sealed record CrossValidationResults(
///     List&lt;FoldMetric&gt; FoldMetrics,
///     double MeanScore
/// ) : INestedSchema, IStructuredSerializable;
///
/// // ✅ Flat schema - structured serialization optional
/// public sealed record ModelMetrics(
///     double Accuracy,
///     double Precision,
///     double Recall
/// ) : IFlatSchema, IStructuredSerializable, ITextSerializable;
///
/// // ✅ Configuration object
/// public sealed record TrainingConfig(
///     int Epochs,
///     double LearningRate,
///     Dictionary&lt;string, object&gt; Hyperparameters
/// ) : INestedSchema, IStructuredSerializable;
/// </code>
/// </example>
public interface IStructuredSerializable
{
  // Marker interface - no members required
}
