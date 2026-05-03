using System.Reflection;
using Flowthru.Core.Abstractions;

namespace Flowthru.Core.Data.Serialization;

/// <summary>
/// Builds <see cref="PropertyMappingPlan{TRow}"/> instances by walking the public instance
/// properties of <typeparamref name="TRow"/> and classifying each according to the
/// universal Tier 1–5 cascade established in
/// <c>Flowthru.Core.SourceGenerators.SchemaAnalysis.SchemaPropertyClassifier</c>:
/// <list type="number">
/// <item>CLR primitives and BCL scalar structs</item>
/// <item>Enums</item>
/// <item><c>byte[]</c> opaque blobs</item>
/// <item>Known BCL scalar types not expressible via <see cref="IScalar"/></item>
/// <item>User-defined <see cref="IScalar"/> implementors (NewType wrappers)</item>
/// </list>
/// Anything not matching one of those tiers is classified as <see cref="PropertyKind.Nested"/>.
/// </summary>
/// <remarks>
/// <para>
/// The planner is the single point of truth for the cascade in the runtime layer; the
/// compile-time cascade in <c>SchemaPropertyClassifier</c> handles classifying schemas
/// for marker-interface emission. Both implement the same tiered logic against the same
/// type system; if you add a new tier here, mirror it in the classifier.
/// </para>
/// <para>
/// <strong>No caching.</strong> Each <see cref="Build{TRow}"/> call performs a fresh
/// reflection walk of <typeparamref name="TRow"/>. The cost is bounded by the number of
/// properties on the schema (a project-level constant), and serializers are constructed
/// at catalog wire-up time rather than per-row, so this fits Flowthru's "constant-time
/// overhead independent of data volume" performance principle. Memoization can be added
/// trivially as a wrapper if profiling later identifies it as a hot spot.
/// </para>
/// </remarks>
public static class PropertyMappingPlanner
{
  /// <summary>
  /// Builds a plan for <typeparamref name="TRow"/> with default options (empty cell
  /// treated as the only null sentinel for nullable properties).
  /// </summary>
  public static PropertyMappingPlan<TRow> Build<TRow>() => Build<TRow>(new PropertyMappingPlannerOptions());

  /// <summary>
  /// Builds a plan for <typeparamref name="TRow"/> with explicit options.
  /// </summary>
  /// <param name="options">
  /// Null-sentinel and other planner configuration. Pass a fresh instance per
  /// distinct configuration; the planner does not memoize across option variants.
  /// </param>
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

  // ── Per-property classification ──────────────────────────────────────────────

  private static PropertyBinding ClassifyProperty(
    PropertyInfo property,
    PropertyMappingPlannerOptions options,
    NullabilityInfoContext nullabilityContext
  )
  {
    var fieldName = GetFieldName(property);

    // Determine effective type and nullability up front. Nullable<T> wrappers are
    // unwrapped; reference-type nullability is read from the C# 8 annotation.
    var declaredType = property.PropertyType;
    var underlyingNullable = Nullable.GetUnderlyingType(declaredType);
    var isValueTypeNullable = underlyingNullable is not null;
    var isReferenceTypeNullable =
      !declaredType.IsValueType && nullabilityContext.Create(property).ReadState == NullabilityState.Nullable;

    var effectiveType = underlyingNullable ?? declaredType;
    var isNullable = isValueTypeNullable || isReferenceTypeNullable;

    var nullSentinels = isNullable ? options.NullSentinels : Array.Empty<string>();

    // Tier 1: CLR primitives, plus DateTime which the source-side classifier treats as
    // a primitive. Note that string is a reference type but primitive-typed.
    if (IsClrPrimitive(effectiveType))
    {
      return new PropertyBinding(
        property,
        fieldName,
        PropertyKind.Primitive,
        isNullable,
        effectiveType,
        nullSentinels,
        @enum: null,
        iScalar: null
      );
    }

    // Tier 2: enums
    if (effectiveType.IsEnum)
    {
      return new PropertyBinding(
        property,
        fieldName,
        PropertyKind.Enum,
        isNullable,
        effectiveType,
        nullSentinels,
        @enum: new EnumBindingInfo(effectiveType),
        iScalar: null
      );
    }

    // Tier 3: byte[] (opaque blob — structurally an array but semantically a single value)
    if (effectiveType.IsArray && effectiveType.GetElementType() == typeof(byte))
    {
      return new PropertyBinding(
        property,
        fieldName,
        PropertyKind.Primitive,
        isNullable,
        effectiveType,
        nullSentinels,
        @enum: null,
        iScalar: null
      );
    }

    // Tier 4: known BCL scalar structs — types defined outside this library that cannot
    // self-declare IScalar. Mirrors the classifier's bounded list.
    if (IsKnownBclScalarStruct(effectiveType))
    {
      return new PropertyBinding(
        property,
        fieldName,
        PropertyKind.Primitive,
        isNullable,
        effectiveType,
        nullSentinels,
        @enum: null,
        iScalar: null
      );
    }

    // Tier 5: IScalar implementors — user-defined NewType wrappers around a single
    // primitive. The wrapper must expose exactly one public readable instance property
    // (the value accessor) and a constructor taking that property's type.
    if (TryGetIScalarBinding(effectiveType, out var iScalarInfo))
    {
      return new PropertyBinding(
        property,
        fieldName,
        PropertyKind.IScalar,
        isNullable,
        effectiveType,
        nullSentinels,
        @enum: null,
        iScalar: iScalarInfo
      );
    }

    // Anything else → Nested. Format extensions that support nested data recurse;
    // flat-only formats reject (though their generic constraints already prevent the
    // call site from compiling).
    return new PropertyBinding(
      property,
      fieldName,
      PropertyKind.Nested,
      isNullable,
      effectiveType,
      nullSentinels,
      @enum: null,
      iScalar: null
    );
  }

  // ── Tier-specific helpers ────────────────────────────────────────────────────

  /// <summary>
  /// Resolves a property's external field name. Returns the
  /// <see cref="SerializedLabelAttribute.Label"/> when present, otherwise the property
  /// name. Inlined into the planner during Phase B5 — previously lived as a public
  /// helper at <c>Flowthru.Core.Data.Storage.Format.PropertyMappingHelper.GetFieldName</c>.
  /// </summary>
  private static string GetFieldName(PropertyInfo property)
  {
    var label = property.GetCustomAttribute<SerializedLabelAttribute>();
    return label?.Label ?? property.Name;
  }

  private static bool IsClrPrimitive(Type type)
  {
    // Mirrors the SpecialType cases recognized by the source-side classifier.
    if (type.IsPrimitive)
    {
      // bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, char, IntPtr/UIntPtr.
      return true;
    }
    return type == typeof(string)
      || type == typeof(decimal)
      || type == typeof(DateTime);
  }

  private static bool IsKnownBclScalarStruct(Type type)
  {
    // Mirrors the bounded BCL list in the source-side classifier. These are types
    // defined outside Flowthru that cannot self-declare IScalar.
    return type == typeof(Guid)
      || type == typeof(TimeSpan)
      || type == typeof(DateTimeOffset)
      || type == typeof(DateOnly)
      || type == typeof(TimeOnly)
      || type == typeof(Half)
      || type == typeof(Int128)
      || type == typeof(UInt128);
  }

  private static bool TryGetIScalarBinding(Type type, out IScalarBindingInfo? info)
  {
    info = null;

    if (!typeof(IScalar).IsAssignableFrom(type))
    {
      return false;
    }

    // Accept any IScalar implementor that exposes exactly one public readable instance
    // property. Multi-property structs that erroneously declare IScalar are rejected
    // here — the public IScalar XML doc warns against that pattern.
    var publicReadableProps = type
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
      .Where(p => p.DeclaringType == type) // exclude inherited members
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
      // No matching wrapping constructor — the type doesn't fit the expected NewType
      // shape, so we don't classify it as IScalar. Falls through to Nested.
      return false;
    }

    info = new IScalarBindingInfo(type, backingType, valueProperty, ctor);
    return true;
  }
}
