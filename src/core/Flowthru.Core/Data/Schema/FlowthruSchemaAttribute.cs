using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Data.Schema;

/// <summary>
/// Marks a schema type for automatic interface-marker generation. The
/// source generator inspects the type's public instance properties under
/// the Tier 1–5 cascade (primitive, enum, byte-blob, BCL scalar,
/// <see cref="IScalar"/> NewType, nested) and emits the appropriate
/// schema markers:
/// <list type="bullet">
///   <item><see cref="IFlatSchema"/> or <see cref="INestedSchema"/> based on property kinds.</item>
///   <item><see cref="ITextSerializable"/> for flat schemas (CSV/TSV).</item>
///   <item><see cref="IBinarySerializable"/> for flat schemas (Parquet/Avro).</item>
///   <item><see cref="IStructuredSerializable"/> for all schemas (JSON/XML).</item>
/// </list>
/// </summary>
/// <remarks>
/// The annotated type must be declared as <c>partial</c>. The generator
/// emits a separate partial declaration adding the marker interfaces.
/// </remarks>
[ExcludeFromCodeCoverage]
[AttributeUsage(
  AttributeTargets.Class | AttributeTargets.Struct,
  Inherited = false,
  AllowMultiple = false
)]
public sealed class FlowthruSchemaAttribute : Attribute
{
}
