using System.Reflection;
using Apache.Arrow;
using Apache.Arrow.Arrays;
using Apache.Arrow.Types;
using Flowthru.Step.Python;

namespace Flowthru.Step.Python.Internal;

internal static class ArrowMarshallingRegistry
{
  private static readonly Dictionary<Type, IArrowMarshallingRule> _rules = BuildRules();

  static ArrowMarshallingRegistry()
  {
    // Register the canonical arrow.uuid extension definition with Arrow's
    // process-wide registry so IPC round-trips deserialize FixedSizeBinary(16)
    // columns tagged "arrow.uuid" back into GuidArray instances instead of
    // surfacing as raw FixedSizeBinaryArray. Idempotent — Register overwrites
    // the same definition under the same name.
    GuidExtensionDefinition.Instance.AddToDefaultRegistry();

    // Fail-fast drift guard: the FT2008 / FT2009 analyzer consults
    // PythonMarshallableTypeNames.All to decide which leaf property types
    // are allowed. If the analyzer ever advertises a name the registry
    // can't actually encode, every passing build is a runtime trap —
    // the analyzer's promise ("if it compiles, Arrow can encode it")
    // silently breaks. Assert the shared list is a subset of the
    // registry so that direction is impossible. The reverse (registry
    // has rules the shared list doesn't list yet) is a missed-coverage
    // case the analyzer reconciles in a follow-up.
    var registryNames = new HashSet<string>(
      _rules.Keys.Select(t => t.FullName ?? t.Name),
      StringComparer.Ordinal
    );
    var missing = PythonMarshallableTypeNames.All
      .Where(n => !registryNames.Contains(n))
      .ToArray();
    if (missing.Length > 0)
    {
      throw new InvalidOperationException(
        "PythonMarshallableTypeNames.All advertises type(s) the ArrowMarshallingRegistry "
        + $"has no rule for: [{string.Join(", ", missing)}]. "
        + "Add the matching IArrowMarshallingRule or remove the name from the shared list."
      );
    }
  }

  public static IArrowMarshallingRule? TryGet(Type clrType) =>
    _rules.TryGetValue(clrType, out var rule) ? rule : null;

  public static IReadOnlyCollection<Type> SupportedClrTypes => _rules.Keys;

  private static Dictionary<Type, IArrowMarshallingRule> BuildRules()
  {
    var rules = new IArrowMarshallingRule[]
    {
      new Int32Rule(),
      new Int64Rule(),
      new FloatRule(),
      new DoubleRule(),
      new BooleanRule(),
      new StringRule(),
      new DateTimeRule(),
      new DateTimeOffsetRule(),
      new TimeSpanRule(),
      new GuidRule(),
      new BinaryRule(),
      new DecimalRule(),
    };
    return rules.ToDictionary(r => r.ClrType);
  }

  private sealed class Int32Rule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(int);
    public string CanonicalTypeName => "int";
    public string PandasDtype => "int32";

    public IArrowType CreateArrowType(PropertyInfo? property) => Int32Type.Default;

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new Int32Array.Builder();
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append((int)value);
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is Int32Array;

    public object? Decode(IArrowArray array, int index) =>
      ((Int32Array)array).GetValue(index);
  }

  private sealed class Int64Rule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(long);
    public string CanonicalTypeName => "long";
    public string PandasDtype => "int64";

    public IArrowType CreateArrowType(PropertyInfo? property) => Int64Type.Default;

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new Int64Array.Builder();
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append((long)value);
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is Int64Array;

    public object? Decode(IArrowArray array, int index) =>
      ((Int64Array)array).GetValue(index);
  }

  private sealed class FloatRule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(float);
    public string CanonicalTypeName => "float";
    public string PandasDtype => "float32";

    public IArrowType CreateArrowType(PropertyInfo? property) => FloatType.Default;

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new FloatArray.Builder();
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append((float)value);
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is FloatArray;

    public object? Decode(IArrowArray array, int index) =>
      ((FloatArray)array).GetValue(index);
  }

  private sealed class DoubleRule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(double);
    public string CanonicalTypeName => "double";
    public string PandasDtype => "float64";

    public IArrowType CreateArrowType(PropertyInfo? property) => DoubleType.Default;

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new DoubleArray.Builder();
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append((double)value);
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is DoubleArray;

    public object? Decode(IArrowArray array, int index) =>
      ((DoubleArray)array).GetValue(index);
  }

  private sealed class BooleanRule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(bool);
    public string CanonicalTypeName => "bool";
    public string PandasDtype => "bool";

    public IArrowType CreateArrowType(PropertyInfo? property) => BooleanType.Default;

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new BooleanArray.Builder();
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append((bool)value);
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is BooleanArray;

    public object? Decode(IArrowArray array, int index) =>
      ((BooleanArray)array).GetValue(index);
  }

  private sealed class StringRule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(string);
    public string CanonicalTypeName => "string";
    public string PandasDtype => "object";

    public IArrowType CreateArrowType(PropertyInfo? property) => StringType.Default;

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new StringArray.Builder();
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append((string)value);
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is StringArray or LargeStringArray;

    public object? Decode(IArrowArray array, int index) => array switch
    {
      StringArray s => s.GetString(index),
      LargeStringArray ls => ls.GetString(index),
      _ => throw new NotSupportedException(
        $"Cannot convert Arrow array of type '{array.Data.DataType.Name}' to C# type 'String'."
      ),
    };
  }

  private sealed class DateTimeRule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(DateTime);
    public string CanonicalTypeName => "DateTime";
    public string PandasDtype => "datetime64[ns]";

    public IArrowType CreateArrowType(PropertyInfo? property) =>
      new TimestampType(TimeUnit.Microsecond, (string?)null);

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new TimestampArray.Builder((TimestampType)arrowType);
      foreach (var value in values)
      {
        if (value is null)
        {
          builder.AppendNull();
        }
        else
        {
          var dt = (DateTime)value;
          var utcDt = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
          builder.Append(new DateTimeOffset(utcDt, TimeSpan.Zero));
        }
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is TimestampArray;

    public object? Decode(IArrowArray array, int index)
    {
      var dto = ((TimestampArray)array).GetTimestamp(index);
      return dto?.UtcDateTime;
    }
  }

  private sealed class DateTimeOffsetRule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(DateTimeOffset);
    public string CanonicalTypeName => "DateTimeOffset";
    public string PandasDtype => "datetime64[ns, UTC]";

    public IArrowType CreateArrowType(PropertyInfo? property) =>
      new TimestampType(TimeUnit.Microsecond, timezone: "UTC");

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new TimestampArray.Builder((TimestampType)arrowType);
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append(((DateTimeOffset)value).ToUniversalTime());
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is TimestampArray;

    public object? Decode(IArrowArray array, int index) =>
      ((TimestampArray)array).GetTimestamp(index);
  }

  private sealed class TimeSpanRule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(TimeSpan);
    public string CanonicalTypeName => "TimeSpan";
    public string PandasDtype => "timedelta64[ns]";

    public IArrowType CreateArrowType(PropertyInfo? property) => DurationType.Microsecond;

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new DurationArray.Builder(DurationType.Microsecond);
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append((TimeSpan)value);
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is DurationArray;

    public object? Decode(IArrowArray array, int index) =>
      ((DurationArray)array).GetTimeSpan(index);
  }

  private sealed class GuidRule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(Guid);
    public string CanonicalTypeName => "Guid";
    public string PandasDtype => "object";

    public IArrowType CreateArrowType(PropertyInfo? property) => GuidType.Default;

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new GuidArray.Builder();
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append((Guid)value);
      }
      return builder.Build();
    }

    // Accept the canonical arrow.uuid extension AND the storage shapes a
    // Python round-trip can land us in: pa.Table.from_pandas drops the
    // extension wrapper, yielding plain Binary / FixedSizeBinary(16) /
    // LargeBinary. The dispatcher only delegates here when the declared
    // CLR type is Guid, so we can safely interpret 16-byte cells as UUIDs.
    public bool Matches(IArrowArray array) =>
      array is GuidArray or FixedSizeBinaryArray or BinaryArray or LargeBinaryArray;

    public object? Decode(IArrowArray array, int index)
    {
      switch (array)
      {
        case GuidArray g:
          return g.GetGuid(index);
        case FixedSizeBinaryArray fsb:
          return GuidArray.RFC4122ToGuid(fsb.GetBytes(index));
        case BinaryArray b:
          {
            var bytes = b.GetBytes(index);
            if (bytes.Length != 16)
              throw new InvalidOperationException(
                $"Binary column declared as Guid has cell of {bytes.Length} bytes; expected 16."
              );
            return GuidArray.RFC4122ToGuid(bytes);
          }
        case LargeBinaryArray lb:
          {
            var bytes = lb.GetBytes(index);
            if (bytes.Length != 16)
              throw new InvalidOperationException(
                $"LargeBinary column declared as Guid has cell of {bytes.Length} bytes; expected 16."
              );
            return GuidArray.RFC4122ToGuid(bytes);
          }
        default:
          throw new NotSupportedException(
            $"Cannot convert Arrow array of type '{array.Data.DataType.Name}' to C# type 'Guid'."
          );
      }
    }
  }

  private sealed class DecimalRule : IArrowMarshallingRule
  {
    // Default precision/scale used when [ArrowDecimal] is absent. Chosen to
    // cover every System.Decimal value the CLR can losslessly represent
    // (29 significant digits, scale up to 28) while leaving headroom for a
    // typical monetary scale.
    private const int DefaultPrecision = 28;
    private const int DefaultScale = 9;

    public Type ClrType => typeof(decimal);
    public string CanonicalTypeName => "decimal";
    public string PandasDtype => "object";

    public IArrowType CreateArrowType(PropertyInfo? property)
    {
      var attr = property?.GetCustomAttribute<ArrowDecimalAttribute>();
      return attr is null
        ? new Decimal128Type(DefaultPrecision, DefaultScale)
        : new Decimal128Type(attr.Precision, attr.Scale);
    }

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new Decimal128Array.Builder((Decimal128Type)arrowType);
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append((decimal)value);
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is Decimal128Array;

    public object? Decode(IArrowArray array, int index)
    {
      var sql = ((Decimal128Array)array).GetSqlDecimal(index);
      return sql is null ? null : (decimal)sql.Value;
    }
  }

  private sealed class BinaryRule : IArrowMarshallingRule
  {
    public Type ClrType => typeof(byte[]);
    public string CanonicalTypeName => "byte[]";
    public string PandasDtype => "object";

    public IArrowType CreateArrowType(PropertyInfo? property) => BinaryType.Default;

    public IArrowArray Encode(IArrowType arrowType, List<object?> values)
    {
      var builder = new BinaryArray.Builder();
      foreach (var value in values)
      {
        if (value is null) builder.AppendNull();
        else builder.Append((byte[])value);
      }
      return builder.Build();
    }

    public bool Matches(IArrowArray array) => array is BinaryArray or LargeBinaryArray;

    public object? Decode(IArrowArray array, int index) => array switch
    {
      BinaryArray b => b.GetBytes(index).ToArray(),
      LargeBinaryArray lb => lb.GetBytes(index).ToArray(),
      _ => null,
    };
  }
}
