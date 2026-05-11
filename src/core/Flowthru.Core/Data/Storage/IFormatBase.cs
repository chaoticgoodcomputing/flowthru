namespace Flowthru.Data.Storage;

/// <summary>
/// Common surface shared by <see cref="IFormatRowReader{TRow}"/> and
/// <see cref="IFormatRowWriter{TRow}"/>. Carries the runtime traits every
/// format extension declares regardless of read or write capability.
/// Row-shape capability claims are expressed at the type level via marker
/// interfaces (<c>ISupportsIScalar</c>, <c>ISupportsNested</c>) on the
/// concrete format type rather than as runtime bool flags.
/// </summary>
/// <typeparam name="TRow">The row type the format handles.</typeparam>
/// <remarks>
/// <para>
/// Capability-segmented interfaces (Phase D pattern, preserved in the FP
/// rewrite): read-only formats implement <see cref="IFormatRowReader{TRow}"/>
/// only; write-capable formats implement
/// <see cref="IFormatSerializer{TRow}"/> which composes both segments.
/// Streaming formats additionally mark themselves with
/// <see cref="IFormatStreamReader{TRow}"/>.
/// </para>
/// <para>
/// The property-mapping configuration hook (consuming
/// <c>PropertyMappingPlanner</c>) lands in this interface in Phase 2B-3
/// when the planner is ported.
/// </para>
/// </remarks>
public interface IFormatBase<TRow>
  where TRow : notnull
{
  /// <summary>
  /// Structural capabilities of this format serializer. Composed with the
  /// medium's traits to produce the adapter-level <see cref="StorageTraits"/>.
  /// </summary>
  StorageTraits Traits { get; }
}
