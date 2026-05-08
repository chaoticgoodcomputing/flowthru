namespace Flowthru.Data.Schema;

/// <summary>
/// Capability marker declaring that a format serializer round-trips
/// <see cref="IScalar"/> NewType wrappers — reads and writes the cell
/// as the wrapper's backing primitive type and constructs/extracts the
/// wrapper across the boundary.
/// </summary>
/// <remarks>
/// <para>
/// Lifted from a runtime <c>SupportsIScalar = true</c> bool flag to a
/// type-level marker per §2.11. Format-author declares support by
/// implementing this interface; consumers requiring the capability take
/// a <c>where TFormat : ISupportsIScalar</c> constraint, and the
/// compiler rejects unsupported pairings at the smart-constructor call
/// site.
/// </para>
/// <para>
/// CSV, Excel, and JSON format serializers all support
/// <see cref="IScalar"/> via Core's
/// <c>Flowthru.Data.Schema.Mapping.PropertyMappingPlanner</c>. A format
/// that opts out of the planner via
/// <c>[OptOutOfPropertyPlanner(reason)]</c> may not support
/// <see cref="IScalar"/> automatically — its author chooses whether to
/// implement this marker.
/// </para>
/// </remarks>
public interface ISupportsIScalar
{
}
