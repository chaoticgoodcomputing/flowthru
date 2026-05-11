using System.Reflection;
using CsvHelper.Configuration;
using Flowthru.Data.Schema.Mapping;

namespace Flowthru.Data.Storage.Csv.Internal;

/// <summary>
/// CsvHelper class map that consumes <see cref="PropertyMappingPlanner"/>
/// to drive field-name mapping (<c>[SerializedLabel]</c>), enum
/// conversion (<c>[SerializedEnum]</c>),
/// <see cref="Flowthru.Data.Schema.IScalar"/> NewType wrapping, and
/// per-property null-sentinel handling.
/// </summary>
/// <typeparam name="T">The row type being mapped.</typeparam>
/// <remarks>
/// Translates the planner's per-property bindings into CsvHelper's
/// <c>ClassMap</c> abstractions. For types with a parameterless
/// constructor we register Member maps (used for both reads and
/// writes); for positional records (no parameterless constructor) we
/// additionally register Parameter maps so the reader can bind cells
/// to the primary constructor's parameters.
/// </remarks>
internal sealed class SerializedLabelClassMap<T> : ClassMap<T>
{
  /// <summary>Default constructor — only empty cells are treated as null.</summary>
  public SerializedLabelClassMap()
    : this(CsvFormatSerializerDefaults.NullValues) { }

  /// <summary>Custom null-sentinel list applied to nullable properties on read.</summary>
  public SerializedLabelClassMap(IReadOnlyList<string> nullValues)
  {
    var plan = PropertyMappingPlanner.Build<T>(
      new PropertyMappingPlannerOptions { NullSentinels = nullValues }
    );

    // Member maps cover the writer path for both parameterless-ctor types
    // and positional records (record's compiler-synthesised properties
    // expose getters), and cover the reader path for types with a
    // parameterless constructor.
    foreach (var binding in plan.Bindings)
    {
      var memberMap = Map(typeof(T), binding.Property).Name(binding.FieldName);
      ApplyConverter(memberMap, binding);
      ApplyNullSentinels(memberMap, binding);
    }

    // For positional records without a parameterless constructor,
    // additionally register Parameter maps so the reader can bind cells
    // to the primary constructor's parameters and instantiate the type.
    var hasParameterlessCtor = typeof(T).GetConstructor(Type.EmptyTypes) is not null;
    if (!hasParameterlessCtor)
    {
      var primaryCtor = typeof(T)
        .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
        .OrderByDescending(c => c.GetParameters().Length)
        .First();

      foreach (var param in primaryCtor.GetParameters())
      {
        var binding = plan.Bindings.FirstOrDefault(b =>
          string.Equals(b.Property.Name, param.Name, StringComparison.Ordinal)
        );
        if (binding is null)
        {
          continue;
        }

        var parameterMap = Parameter(() => primaryCtor, param.Name!).Name(binding.FieldName);
        ApplyConverter(parameterMap, binding);
        ApplyNullSentinels(parameterMap, binding);
      }
    }
  }

  private static void ApplyConverter(MemberMap memberMap, PropertyBinding binding)
  {
    var converter = BuildConverter(binding);
    if (converter is not null)
    {
      memberMap.TypeConverter(converter);
    }
  }

  private static void ApplyConverter(ParameterMap parameterMap, PropertyBinding binding)
  {
    var converter = BuildConverter(binding);
    if (converter is not null)
    {
      parameterMap.TypeConverter(converter);
    }
  }

  private static void ApplyNullSentinels(MemberMap memberMap, PropertyBinding binding)
  {
    if (binding.IsNullable && binding.NullSentinels.Count > 0)
    {
      memberMap.TypeConverterOption.NullValues(binding.NullSentinels.ToArray());
    }
  }

  private static void ApplyNullSentinels(ParameterMap parameterMap, PropertyBinding binding)
  {
    if (binding.IsNullable && binding.NullSentinels.Count > 0)
    {
      parameterMap.TypeConverterOption.NullValues(binding.NullSentinels.ToArray());
    }
  }

  private static CsvHelper.TypeConversion.ITypeConverter? BuildConverter(PropertyBinding binding)
  {
    switch (binding.Kind)
    {
      case PropertyKind.Enum:
      {
        var converterType = typeof(SerializedEnumCsvConverter<>).MakeGenericType(
          binding.EffectiveType
        );
        return (CsvHelper.TypeConversion.ITypeConverter)
          Activator.CreateInstance(converterType, binding.Enum!)!;
      }
      case PropertyKind.IScalar:
      {
        var info = binding.IScalar!;
        var converterType = typeof(IScalarCsvConverter<,>).MakeGenericType(
          info.ScalarType,
          info.BackingType
        );
        return (CsvHelper.TypeConversion.ITypeConverter)
          Activator.CreateInstance(converterType, info.ValueProperty.Name)!;
      }
      case PropertyKind.Primitive:
        // Primitives, BCL scalar structs, byte[] — CsvHelper's built-in
        // TypeConverterCache handles these natively.
        return null;
      case PropertyKind.Nested:
        // The IFlatSchema constraint on CsvFormatSerializer<TRow> rules
        // out nested-bearing schemas at the call site, so a Nested
        // binding here would indicate an upstream bug. Fall through to
        // CsvHelper's default; if a nested value really did sneak in,
        // CsvHelper will produce a clear error.
        return null;
      default:
        return null;
    }
  }
}
