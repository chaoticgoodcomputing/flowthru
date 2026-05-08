namespace Flowthru.Data.Schema;

/// <summary>
/// Marker interface for schema types with nested structure — collections,
/// nested objects, dictionaries, or other hierarchical shapes.
/// </summary>
/// <remarks>
/// <para>
/// Source-gen-emitted automatically for <c>[FlowthruSchema]</c>-attributed
/// types whose properties contain anything classified as
/// <c>PropertyKind.Nested</c> by the Tier 1–5 cascade. Mutually exclusive
/// with <see cref="IFlatSchema"/>: a schema is either entirely flat or
/// contains at least one nested property.
/// </para>
/// <para>
/// Format serializers that handle nested data (JSON, Parquet, XML) accept
/// schemas marking either <see cref="IFlatSchema"/> or <see cref="INestedSchema"/>;
/// flat-only formats (CSV, Excel) accept only <see cref="IFlatSchema"/>.
/// </para>
/// </remarks>
public interface INestedSchema
{
}
