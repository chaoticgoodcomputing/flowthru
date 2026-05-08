namespace Flowthru.Data.Schema.Mapping;

/// <summary>
/// Kind-specific data for a <see cref="PropertyBinding"/> whose
/// <see cref="PropertyBinding.Kind"/> is <see cref="PropertyKind.Enum"/>.
/// Carries the enum type and the bidirectional mapping between enum
/// values and their <see cref="SerializedEnumAttribute"/>-declared
/// string representations.
/// </summary>
/// <param name="EnumType">The enum type (non-nullable form).</param>
/// <param name="Forward">
/// Boxed enum value → serialized string. Format extensions consume
/// this on the write path.
/// </param>
/// <param name="Reverse">
/// Serialized string → boxed enum value. Format extensions consume
/// this on the read path. Lookup is ordinal-case-sensitive.
/// </param>
/// <remarks>
/// The mappings are populated once per enum type by
/// <see cref="PropertyMappingPlanner.Build{TRow}()"/> via
/// <see cref="SerializedEnumMappings.Build"/>; format extensions read
/// them off the binding instead of reflecting over the enum
/// themselves. Boxing is intentional — the binding is non-generic so
/// the dictionaries are keyed at <see cref="object"/>; the per-row
/// allocation cost is bounded by the number of enum properties (a
/// schema-level constant) and bounded again by the format extension's
/// own caching strategy (e.g., CsvHelper's per-converter instance is
/// constructed once per <typeparamref name="TRow"/>).
/// </remarks>
public sealed record EnumBindingInfo(
  Type EnumType,
  IReadOnlyDictionary<object, string> Forward,
  IReadOnlyDictionary<string, object> Reverse
);
