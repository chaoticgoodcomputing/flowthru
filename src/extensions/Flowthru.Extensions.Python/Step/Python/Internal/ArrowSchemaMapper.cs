using System.Reflection;
using Apache.Arrow;
using Apache.Arrow.Types;
using Flowthru.Data.Schema;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Maps C# schema types to Apache Arrow schemas for tabular data interchange.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Generates Arrow schemas from C# types annotated with [FlowthruSchema],
/// preserving field names (via [SerializedLabel]) and nullability for correct DataFrame marshalling.
/// </para>
/// <para>
/// <strong>Type Mapping (C# → Arrow):</strong>
/// </para>
/// <list type="bullet">
/// <item>int, int? → Int32Type</item>
/// <item>long, long? → Int64Type</item>
/// <item>float, float? → FloatType</item>
/// <item>double, double? → DoubleType</item>
/// <item>bool, bool? → BooleanType</item>
/// <item>string → StringType (always nullable in Arrow)</item>
/// <item>DateTime, DateTime? → TimestampType (microsecond, UTC)</item>
/// <item>DateTimeOffset, DateTimeOffset? → TimestampType (microsecond, UTC)</item>
/// <item>TimeSpan, TimeSpan? → DurationType (microsecond)</item>
/// <item>Guid, Guid? → StringType (serialized as string)</item>
/// <item>byte[] → BinaryType</item>
/// </list>
/// <para>
/// <strong>Field Naming:</strong> Resolves <c>[SerializedLabel]</c> attributes via
/// <see cref="GetFieldName"/>, the same resolution Core's
/// <see cref="Flowthru.Data.Schema.Mapping.PropertyMappingPlanner"/> applies for
/// CSV/Parquet/JSON serializers.
/// </para>
/// </remarks>
public static class ArrowSchemaMapper
{
  /// <summary>
  /// Generates an Apache Arrow schema from a C# schema type.
  /// </summary>
  /// <typeparam name="T">The C# schema type to map</typeparam>
  /// <returns>Arrow schema with fields matching the C# type's properties</returns>
  /// <exception cref="NotSupportedException">
  /// Thrown when a property type cannot be mapped to Arrow.
  /// </exception>
  /// <remarks>
  /// The schema includes all public instance properties, with field names determined by
  /// [SerializedLabel] attributes or property names. Nullability is preserved from the C# type.
  /// </remarks>
  public static Schema BuildArrowSchema<T>()
    where T : notnull
  {
    return BuildArrowSchema(typeof(T));
  }

  /// <summary>
  /// Generates an Apache Arrow schema from a C# type.
  /// </summary>
  /// <param name="type">The C# type to map</param>
  /// <returns>Arrow schema with fields matching the type's properties</returns>
  /// <exception cref="NotSupportedException">
  /// Thrown when a property type cannot be mapped to Arrow.
  /// </exception>
  public static Schema BuildArrowSchema(Type type)
  {
    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var fields = new List<Field>();

    foreach (var property in properties)
    {
      // Skip indexed properties (this[int index])
      if (property.GetIndexParameters().Length > 0)
      {
        continue;
      }

      // Get external field name (respects SerializedLabel)
      var fieldName = GetFieldName(property);

      // Map C# type to Arrow type. Re-throw with the property name so a
      // user staring at "Type 'System.Decimal' cannot be mapped..." can
      // immediately see *which* property they need to fix; without this
      // the diagnostic only names the type.
      IArrowType arrowType;
      bool nullable;
      try
      {
        arrowType = MapToArrowType(property.PropertyType, property, out nullable);
      }
      catch (NotSupportedException ex)
      {
        throw new NotSupportedException(
          $"Property '{property.Name}' on '{type.Name}': {ex.Message}",
          ex
        );
      }

      // Create Arrow field with nullability
      var field = new Field(fieldName, arrowType, nullable);
      fields.Add(field);
    }

    return new Schema(fields, metadata: null);
  }

  /// <summary>
  /// Maps a C# CLR type to an Apache Arrow type.
  /// </summary>
  /// <param name="clrType">The C# type to map</param>
  /// <param name="property">The owning property, threaded through so rules
  /// with attribute-driven parameters (e.g. a future Decimal128 rule reading
  /// <c>[ArrowDecimal]</c>) can read them. May be null for recursive list-
  /// element mapping where no owning property exists.</param>
  /// <param name="nullable">Output parameter indicating if the field is nullable</param>
  /// <returns>The corresponding Arrow type</returns>
  /// <exception cref="NotSupportedException">
  /// Thrown when the CLR type cannot be mapped to Arrow.
  /// </exception>
  private static IArrowType MapToArrowType(Type clrType, PropertyInfo? property, out bool nullable)
  {
    // Handle nullable value types (int?, double?, etc.)
    var underlyingType = Nullable.GetUnderlyingType(clrType);
    if (underlyingType != null)
    {
      nullable = true;
      return MapNonNullableType(underlyingType, property);
    }

    // Reference types are nullable by default in Arrow
    nullable = !clrType.IsValueType;

    return MapNonNullableType(clrType, property);
  }

  /// <summary>
  /// Maps a non-nullable C# type to an Arrow type. This dispatcher handles
  /// the recursive shapes (enum, list/array); leaf types are looked up in
  /// <see cref="ArrowMarshallingRegistry"/>.
  /// </summary>
  private static IArrowType MapNonNullableType(Type type, PropertyInfo? property)
  {
    var rule = ArrowMarshallingRegistry.TryGet(type);
    if (rule is not null)
    {
      return rule.CreateArrowType(property);
    }

    // Enum types: serialize as string (name), matching the Python-side string representation
    if (type.IsEnum)
    {
      return StringType.Default;
    }

    // List / array types: map to Arrow ListType recursively. byte[] is handled
    // by the registry as BinaryType, and string (which is IEnumerable<char>)
    // is handled as StringType — both checks live before the list-element
    // resolver below.
    var listElementType = TryGetListElementType(type);
    if (listElementType is not null)
    {
      // Recursively map element type. Element nullability is preserved through
      // the recursive Nullable<T> unwrap inside MapToArrowType, but element-list
      // fields are conventionally nullable so each inner element can be null.
      var elementArrowType = MapToArrowType(listElementType, property: null, out var elementNullable);
      var elementField = new Field("item", elementArrowType, elementNullable);
      return new ListType(elementField);
    }

    // Unsupported type
    throw new NotSupportedException(
      $"Type '{type.FullName}' cannot be mapped to Apache Arrow. "
        + "Supported types: int, long, float, double, bool, string, DateTime, "
        + "DateTimeOffset, TimeSpan, Guid, byte[], enum, list/array of any "
        + "supported type, and their nullable variants."
    );
  }

  /// <summary>
  /// Returns the element type if <paramref name="type"/> represents a list-like
  /// collection eligible for Arrow <c>ListType</c> mapping, otherwise
  /// <c>null</c>. <c>string</c> and <c>byte[]</c> are excluded because they
  /// have first-class scalar/binary mappings — they would otherwise match via
  /// <see cref="IEnumerable{T}"/>.
  /// </summary>
  internal static Type? TryGetListElementType(Type type)
  {
    if (type == typeof(string) || type == typeof(byte[])) return null;

    if (type.IsArray && type.GetArrayRank() == 1)
    {
      return type.GetElementType();
    }

    // Match List<T>, IList<T>, IEnumerable<T>, IReadOnlyList<T>, etc. — anything
    // that implements IEnumerable<T> with a concrete element type. The first
    // match wins; nested IEnumerable<T> (e.g. List<List<int>>) is handled by
    // the recursive call in MapNonNullableType.
    var ienum = type.GetInterfaces()
      .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    if (ienum is not null)
    {
      return ienum.GetGenericArguments()[0];
    }

    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
    {
      return type.GetGenericArguments()[0];
    }

    return null;
  }

  /// <summary>
  /// Gets the external field name for a property, respecting [SerializedLabel].
  /// </summary>
  /// <remarks>
  /// Inlined during Phase B5 — previously delegated to <c>PropertyMappingHelper.GetFieldName</c>
  /// in <c>Flowthru.Core.Data.Storage.Format</c>, which has been deleted.
  /// </remarks>
  internal static string GetFieldName(PropertyInfo property)
  {
    var label = property.GetCustomAttribute<SerializedLabelAttribute>();
    return label?.Label ?? property.Name;
  }

  /// <summary>
  /// Builds a C# dictionary mapping column names to pandas dtype strings,
  /// suitable for JSON serialization in subprocess protocol messages.
  /// </summary>
  /// <typeparam name="T">The C# schema type</typeparam>
  /// <returns>Dictionary with dtype specifications for the subprocess worker's df_to_ipc</returns>
  public static Dictionary<string, string> BuildDtypeSpecDictionary<T>()
    where T : notnull
  {
    var type = typeof(T);
    var schema = BuildArrowSchema(type);
    var properties = type
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.GetIndexParameters().Length == 0)
      .ToList();

    var dict = new Dictionary<string, string>();
    foreach (var field in schema.FieldsList)
    {
      var property = properties.First(p => GetFieldName(p) == field.Name);
      var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
      var rule = ArrowMarshallingRegistry.TryGet(propertyType);

      // Enums and ListType columns live in pandas as object-dtype Series.
      // We deliberately skip dtype coercion for lists — pyarrow infers the
      // inner Arrow type from the list contents on pa.Table.from_pandas,
      // and the C# side has already declared the canonical element type
      // via the Arrow schema field.
      dict[field.Name] = rule?.PandasDtype ?? "object";
    }
    return dict;
  }

}
