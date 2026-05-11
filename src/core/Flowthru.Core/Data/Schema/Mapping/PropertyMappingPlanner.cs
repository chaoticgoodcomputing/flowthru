using System.Reflection;

namespace Flowthru.Data.Schema.Mapping;

/// <summary>
/// Pure type-indexed function: builds <see cref="PropertyMappingPlan{TRow}"/>
/// instances by walking <c>TRow</c>'s public properties
/// and classifying each under the universal Tier 1–5 cascade
/// (CLR primitives, enums, byte-blobs, BCL scalar structs, IScalar NewType
/// wrappers, anything else as nested).
/// </summary>
/// <remarks>
/// <para>
/// The planner is the single source of truth for the cascade in the
/// runtime layer; the compile-time cascade in
/// <c>Flowthru.Core.SourceGenerators.Schema.SchemaPropertyClassifier</c>
/// implements the same logic for marker emission. Adding a new tier means
/// adding it to both — Core's source-gen tests guard against drift.
/// </para>
/// <para>
/// No caching: each <see cref="Build{TRow}()"/> call performs a fresh
/// reflection walk. The cost is bounded by the number of properties on
/// the schema (a project-level constant), and serializers are constructed
/// at catalog wire-up time rather than per-row, so the cost fits
/// Flowthru's "constant-time overhead independent of data volume"
/// principle.
/// </para>
/// </remarks>
public static class PropertyMappingPlanner
{
  /// <summary>Builds a plan for <typeparamref name="TRow"/> with default options.</summary>
  public static PropertyMappingPlan<TRow> Build<TRow>() =>
    Build<TRow>(new PropertyMappingPlannerOptions());

  /// <summary>Builds a plan for <typeparamref name="TRow"/> with explicit options.</summary>
  public static PropertyMappingPlan<TRow> Build<TRow>(PropertyMappingPlannerOptions options)
  {
    if (options is null)
    {
      throw new ArgumentNullException(nameof(options));
    }

    var properties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var nullabilityContext = new NullabilityInfoContext();

    var bindings = new List<PropertyBinding>(properties.Length);
    var byFieldName = new Dictionary<string, PropertyBinding>(StringComparer.OrdinalIgnoreCase);

    foreach (var property in properties)
    {
      var binding = ClassifyProperty(property, options, nullabilityContext);
      bindings.Add(binding);
      byFieldName[binding.FieldName] = binding;
    }

    return new PropertyMappingPlan<TRow>(bindings, byFieldName);
  }

  // ── Per-property classification ─────────────────────────────────────────

  private static PropertyBinding ClassifyProperty(
    PropertyInfo property,
    PropertyMappingPlannerOptions options,
    NullabilityInfoContext nullabilityContext
  )
  {
    var fieldName = GetFieldName(property);

    var declaredType = property.PropertyType;
    var underlyingNullable = Nullable.GetUnderlyingType(declaredType);
    var isValueTypeNullable = underlyingNullable is not null;
    var isReferenceTypeNullable =
      !declaredType.IsValueType
      && nullabilityContext.Create(property).ReadState == NullabilityState.Nullable;

    var effectiveType = underlyingNullable ?? declaredType;
    var isNullable = isValueTypeNullable || isReferenceTypeNullable;
    var nullSentinels = isNullable ? options.NullSentinels : Array.Empty<string>();

    // Tier 1: CLR primitives + DateTime + decimal + string
    if (IsClrPrimitive(effectiveType))
    {
      return new PropertyBinding(
        property, fieldName, PropertyKind.Primitive,
        isNullable, effectiveType, nullSentinels, @enum: null, iScalar: null
      );
    }

    // Tier 2: enums
    if (effectiveType.IsEnum)
    {
      var (forward, reverse) = SerializedEnumMappings.Build(effectiveType);
      return new PropertyBinding(
        property, fieldName, PropertyKind.Enum,
        isNullable, effectiveType, nullSentinels,
        @enum: new EnumBindingInfo(effectiveType, forward, reverse), iScalar: null
      );
    }

    // Tier 3: byte[] opaque blob
    if (effectiveType.IsArray && effectiveType.GetElementType() == typeof(byte))
    {
      return new PropertyBinding(
        property, fieldName, PropertyKind.Primitive,
        isNullable, effectiveType, nullSentinels, @enum: null, iScalar: null
      );
    }

    // Tier 4: known BCL scalar structs (Guid, TimeSpan, DateTimeOffset, etc.)
    if (IsKnownBclScalarStruct(effectiveType))
    {
      return new PropertyBinding(
        property, fieldName, PropertyKind.Primitive,
        isNullable, effectiveType, nullSentinels, @enum: null, iScalar: null
      );
    }

    // Tier 5: IScalar NewType wrappers
    if (TryGetIScalarBinding(effectiveType, out var iScalarInfo))
    {
      return new PropertyBinding(
        property, fieldName, PropertyKind.IScalar,
        isNullable, effectiveType, nullSentinels, @enum: null, iScalar: iScalarInfo
      );
    }

    // Anything else: nested.
    return new PropertyBinding(
      property, fieldName, PropertyKind.Nested,
      isNullable, effectiveType, nullSentinels, @enum: null, iScalar: null
    );
  }

  private static string GetFieldName(PropertyInfo property)
  {
    var label = property.GetCustomAttribute<SerializedLabelAttribute>();
    return label?.Label ?? property.Name;
  }

  private static bool IsClrPrimitive(Type type)
  {
    if (type.IsPrimitive)
    {
      return true;
    }
    return type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime);
  }

  private static bool IsKnownBclScalarStruct(Type type) =>
    type == typeof(Guid)
    || type == typeof(TimeSpan)
    || type == typeof(DateTimeOffset)
    || type == typeof(DateOnly)
    || type == typeof(TimeOnly)
    || type == typeof(Half)
    || type == typeof(Int128)
    || type == typeof(UInt128);

  private static bool TryGetIScalarBinding(Type type, out IScalarBindingInfo? info)
  {
    info = null;
    if (!typeof(IScalar).IsAssignableFrom(type))
    {
      return false;
    }

    // Single public readable instance property — the value accessor.
    var publicReadableProps = type
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
      .Where(p => p.DeclaringType == type)
      .ToList();

    if (publicReadableProps.Count != 1)
    {
      return false;
    }

    var valueProperty = publicReadableProps[0];
    var backingType = valueProperty.PropertyType;

    var ctor = type.GetConstructor(new[] { backingType });
    if (ctor is null)
    {
      return false;
    }

    info = new IScalarBindingInfo(type, backingType, valueProperty, ctor);
    return true;
  }
}
