namespace Flowthru.Data.Schema;

/// <summary>
/// Capability marker declaring that a format serializer round-trips
/// <see cref="INestedSchema"/> shapes — schemas with object-typed
/// sub-properties or collection columns.
/// </summary>
/// <remarks>
/// <para>
/// Lifted from a runtime <c>SupportsNested = true</c> bool flag to a
/// type-level marker per §2.11. Flat-only formats (CSV, Excel) leave
/// this absent; their generic constraint
/// <c>where TRow : IFlatSchema</c> already structurally excludes nested
/// schemas at the call site — this marker is the explicit declaration
/// for formats that <em>do</em> support nested data (JSON, Parquet, XML).
/// </para>
/// </remarks>
public interface ISupportsNested
{
}
