namespace Flowthru.Step;

/// <summary>
/// The container shape a step extension declares it can ingest or emit
/// for an <c>IItem&lt;TContainer&lt;TRow&gt;&gt;</c>. Container concerns
/// (enumerability, materialization strategy) live here; row concerns
/// (primitives, nested types, serializability) remain governed by
/// <c>[FlowthruSchema]</c> on the row type.
/// </summary>
/// <remarks>
/// <para>
/// Step extensions declare which container kinds they support via
/// <see cref="StepExtensionCapabilitiesAttribute"/>. Per the Phase 9
/// RFC, <c>Singleton | Enumerable</c> is the enforced minimum for
/// extensions that ship to NuGet — anything below that fires
/// <c>FT1301</c>.
/// </para>
/// <para>
/// The flag values themselves are arranged so that more-specific
/// containers carry higher bits than less-specific ones. Resolution
/// at the catalog-side introspection layer
/// (<c>Flowthru.Data.Catalog.Item.ContainerKindOf</c>) walks
/// container shapes in the order
/// <see cref="Source"/> → <see cref="Queryable"/> →
/// <see cref="Enumerable"/> → <see cref="Singleton"/>, picking the
/// most specific match. <see cref="System.Linq.IQueryable{T}"/> is
/// itself <see cref="System.Collections.Generic.IEnumerable{T}"/>-
/// assignable, so this order matters.
/// </para>
/// </remarks>
[Flags]
public enum StepContainerKind
{
  /// <summary>Sentinel — no container kind declared.</summary>
  None = 0,

  /// <summary>
  /// A bare <c>T</c> — single value, neither enumerable nor queryable.
  /// Required for any extension consuming
  /// <c>Flowthru.Data.Catalog.Configuration.ConfigurationItem&lt;T&gt;</c>
  /// (scalar options records) or other singleton-shaped items.
  /// </summary>
  Singleton = 1 << 0,

  /// <summary>
  /// <c>IEnumerable&lt;T&gt;</c> or any type assignable to it that
  /// isn't already a more-specific container. The bread-and-butter
  /// tabular shape.
  /// </summary>
  Enumerable = 1 << 1,

  /// <summary>
  /// <c>IQueryable&lt;T&gt;</c> — extensions that can push computation
  /// into the data source (EF Core, GraphQL providers, future SQL
  /// extensions). Nice-to-have; not part of the Phase 9 minimum floor.
  /// </summary>
  Queryable = 1 << 2,

  /// <summary>
  /// A <c>Flowthru.Prelude.FlowSource&lt;T&gt;</c> — the lazy,
  /// resource-safe streaming catalog payload produced by
  /// <c>.AsStream()</c>, whose sole consumption path is
  /// compile-to-<c>FlowIO</c>. Supersedes the removed bare-
  /// <c>IAsyncEnumerable</c> <c>AsyncStream</c> kind (see ADR-0023):
  /// <c>FlowSource</c> keeps enumeration inside the effect envelope,
  /// so errors-as-values, disposal, and cancellation are preserved by
  /// construction. Nice-to-have; not part of the Phase 9 minimum floor.
  /// </summary>
  Source = 1 << 3,
}
