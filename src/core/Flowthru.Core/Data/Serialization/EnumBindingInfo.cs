namespace Flowthru.Core.Data.Serialization;

/// <summary>
/// Kind-specific data for a <see cref="PropertyBinding"/> whose
/// <see cref="PropertyBinding.Kind"/> is <see cref="PropertyKind.Enum"/>.
/// </summary>
/// <param name="EnumType">
/// The enum type. Format converters typically pair this with
/// <see cref="Serialization.EnumMetadataRegistry"/> (Core-internal, accessible via
/// <c>InternalsVisibleTo</c> for first-party extensions) or
/// <see cref="Serialization.EnumSerializationHelper"/> for non-generic conversion.
/// </param>
/// <remarks>
/// The planner intentionally does not embed cached enum metadata here. The metadata
/// registry is accessed by-type at converter-construction time; storing a typed cache
/// reference in the binding would require leaking <c>EnumMetadataCache&lt;TEnum&gt;</c>
/// (an <c>internal</c> generic) into the public API surface.
/// </remarks>
public sealed record EnumBindingInfo(Type EnumType);
