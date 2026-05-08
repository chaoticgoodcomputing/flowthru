namespace Flowthru.Data.Schema;

/// <summary>
/// Marker interface for schema types with flat (non-nested) structure —
/// only primitive properties, no collections or nested objects.
/// </summary>
/// <remarks>
/// <para>
/// Source-gen-emitted automatically for <c>[FlowthruSchema]</c>-attributed
/// types whose properties classify as flat under the Tier 1–5 cascade
/// (primitives, enums, byte-blobs, BCL scalars, <c>IScalar</c> NewType
/// wrappers). Manual implementation is permitted but discouraged — the
/// generator is the source of truth.
/// </para>
/// <para>
/// Format serializers that constrain their generic parameter
/// <c>where TRow : IFlatSchema</c> structurally exclude nested schemas at
/// the call site (CSV, Excel — see §3.3). Nested schemas mark
/// <see cref="INestedSchema"/> instead; the two are mutually exclusive.
/// </para>
/// </remarks>
public interface IFlatSchema
{
}
