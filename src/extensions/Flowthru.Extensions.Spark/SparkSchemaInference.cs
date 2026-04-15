using System;
using System.Collections.Generic;
using System.Linq;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Spark.Sql;
using Flowthru.Spark.Sql.Types;

namespace Flowthru.Extensions.Spark;

/// <summary>
/// Derives Spark schema information from <see cref="IFlatSchema"/> types using
/// <c>[SerializedLabel]</c> metadata and a canonical CLR-to-Spark type mapping.
/// </summary>
/// <remarks>
/// Eliminates the need for manual <see cref="StructType"/> declarations and
/// <see cref="GenericRow"/> construction in preprocessing steps. The same metadata
/// that drives CSV/Parquet serialization (<see cref="PropertyMappingHelper"/>) is
/// reused here, so column names are always consistent with the rest of the pipeline.
/// </remarks>
public static class SparkSchemaInference
{
  // CLR → canonical Spark DataType
  // decimal maps to DoubleType: the .NET Spark bridge has no DecimalType primitive.
  private static readonly IReadOnlyDictionary<Type, Func<DataType>> s_clrToSpark = new Dictionary<
    Type,
    Func<DataType>
  >
  {
    [typeof(string)] = () => new StringType(),
    [typeof(bool)] = () => new BooleanType(),
    [typeof(byte)] = () => new ByteType(),
    [typeof(sbyte)] = () => new ByteType(),
    [typeof(short)] = () => new ShortType(),
    [typeof(int)] = () => new IntegerType(),
    [typeof(long)] = () => new LongType(),
    [typeof(float)] = () => new FloatType(),
    [typeof(double)] = () => new DoubleType(),
    [typeof(decimal)] = () => new DoubleType(),
    [typeof(byte[])] = () => new BinaryType(),
    [typeof(DateTime)] = () => new TimestampType(),
    [typeof(DateTimeOffset)] = () => new TimestampType(),
    [typeof(DateOnly)] = () => new DateType(),
  };

  /// <summary>
  /// Derives a <see cref="StructType"/> from the property metadata of
  /// <typeparamref name="T"/>.
  /// </summary>
  /// <typeparam name="T">A flat schema type decorated with <c>[FlowthruSchema]</c>.</typeparam>
  /// <returns>A <see cref="StructType"/> whose fields mirror the schema properties.</returns>
  /// <exception cref="NotSupportedException">
  /// Thrown when a property type has no known Spark equivalent.
  /// </exception>
  public static StructType InferStructType<T>()
    where T : notnull, IFlatSchema
  {
    var propertyMap = PropertyMappingHelper.BuildPropertyMap<T>();

    var fields = propertyMap
      .Select(kvp =>
      {
        var columnName = kvp.Key;
        var propertyType = kvp.Value.PropertyType;
        var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var nullable = underlying != propertyType;

        if (!s_clrToSpark.TryGetValue(underlying, out var factory))
        {
          throw new NotSupportedException(
            $"Property '{kvp.Value.Name}' on '{typeof(T).Name}' has type "
              + $"'{underlying.Name}' which has no known Spark counterpart. "
              + "Use a supported CLR type (string, bool, int, long, float, "
              + "double, decimal, byte[], DateTime, DateOnly)."
          );
        }

        return new StructField(columnName, factory(), nullable);
      })
      .ToArray();

    return new StructType(fields);
  }

  /// <summary>
  /// Projects an <see cref="IEnumerable{T}"/> into <see cref="GenericRow"/> instances
  /// whose values are ordered to match the field order of <see cref="InferStructType{T}"/>.
  /// </summary>
  /// <typeparam name="T">A flat schema type decorated with <c>[FlowthruSchema]</c>.</typeparam>
  /// <param name="source">The rows to project.</param>
  /// <returns>
  /// An enumerable of <see cref="GenericRow"/> objects ready for
  /// <c>SparkSession.CreateDataFrame</c>.
  /// </returns>
  public static IEnumerable<GenericRow> ToGenericRows<T>(IEnumerable<T> source)
    where T : notnull, IFlatSchema
  {
    var propertyMap = PropertyMappingHelper.BuildPropertyMap<T>();
    var orderedGetters = propertyMap.Values.Select(p => p.GetGetMethod()!).ToArray();

    foreach (var item in source)
    {
      var values = new object?[orderedGetters.Length];
      for (var i = 0; i < orderedGetters.Length; i++)
      {
        values[i] = orderedGetters[i].Invoke(item, null);
      }

      yield return new GenericRow(values);
    }
  }
}
