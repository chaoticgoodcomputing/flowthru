namespace Flowthru.Data.Schema;

/// <summary>
/// Marker interface for property types that serialize to a single primitive
/// value — i.e., they produce <c>"key": value</c>, not <c>"key": {…}</c>
/// or <c>"key": […]</c>. Used to classify NewType / value-object wrappers
/// as flat scalars rather than nested objects.
/// </summary>
/// <remarks>
/// <para>
/// Without this interface, the schema classifier cannot distinguish a
/// <c>CustomerId</c> wrapping a <c>string</c> from a multi-property
/// nested record. Implementing <see cref="IScalar"/> is the user's
/// declaration that "this type is a single value, treat it as a flat
/// scalar column."
/// </para>
/// <para>
/// The contract is that the type must serialize to a single JSON value
/// (number, string, boolean, null). Multi-property structs and
/// collections must NOT implement this interface — doing so would
/// misrepresent their structure and cause silent data loss in flat
/// formats.
/// </para>
/// <para>
/// Format extensions that round-trip <see cref="IScalar"/>-marked
/// properties declare <c>ISupportsIScalar</c> on their serializer; format
/// extensions that don't (e.g., a Parquet variant that drops the wrapper
/// silently) leave it absent so the compile-time gate catches the bad
/// pairing at the smart-constructor call site.
/// </para>
/// </remarks>
public interface IScalar
{
}
