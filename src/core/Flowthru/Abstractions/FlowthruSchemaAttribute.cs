namespace Flowthru.Abstractions;

/// <summary>
/// Marks a schema type for automatic interface generation. The source generator
/// will analyze the type's properties and emit the appropriate marker interfaces:
/// <list type="bullet">
/// <item><see cref="IFlatSchema"/> or <see cref="INestedSchema"/> based on property types</item>
/// <item><see cref="ITextSerializable"/> for flat schemas (CSV/TSV compatible)</item>
/// <item><see cref="IBinarySerializable"/> for flat schemas (Parquet compatible)</item>
/// <item><see cref="IStructuredSerializable"/> for all schemas (JSON/XML compatible)</item>
/// </list>
/// </summary>
/// <remarks>
/// The annotated type must be declared as <c>partial</c>. The generator inspects
/// public instance properties to determine structural classification.
/// </remarks>
[AttributeUsage(
  AttributeTargets.Class | AttributeTargets.Struct,
  Inherited = false,
  AllowMultiple = false
)]
public sealed class FlowthruSchemaAttribute : Attribute { }
