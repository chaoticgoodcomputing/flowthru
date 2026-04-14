using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.DataFrames;
using Flowthru.Spark.Sql;
using Flowthru.Spark.Sql.Types;

namespace Flowthru.Extensions.Spark;

/// <summary>
/// Represents a schema mismatch detected during pre-flight validation of a Spark
/// <see cref="DataFrame"/> against a typed schema <typeparamref name="T"/>.
/// </summary>
public sealed record SchemaValidationError(string ColumnName, string Reason);

/// <summary>
/// Materializes a <see cref="TypedFrame{T}"/> into an <see cref="IEnumerable{T}"/> by
/// compiling the expression tree, validating the live Spark schema, and collecting rows.
/// </summary>
/// <typeparam name="T">
/// The target schema type. Must be a flat schema — non-flat schemas contain
/// nested or collection properties that cannot be expressed as scalar Spark columns.
/// </typeparam>
/// <remarks>
/// <para>
/// This is the materialization boundary in a Spark flow. When a step inputs a
/// <see cref="TypedFrame{T}"/> and outputs an <see cref="IEnumerable{T}"/>, call
/// <see cref="Collect"/> to trigger the Spark action and hydrate typed rows.
/// </para>
/// <para>
/// The <see cref="PropertyMappingHelper"/> is used to resolve column names, so
/// <c>[SerializedLabel]</c> attributes are honoured the same way as CSV and Parquet.
/// </para>
/// <para>
/// Register via <c>UseSpark()</c>; inject into flow delegates by type parameter.
/// </para>
/// </remarks>
public sealed class SparkRowHydrator<T>
    where T : notnull, IFlatSchema
{
    private readonly SparkFrameProvider _provider;

    // Built once per T — reflection cost is paid at construction time.
    private readonly IReadOnlyDictionary<string, PropertyInfo> _propertyMap;

    // Maps each Spark DataType to the C# types that can safely receive it.
    private static readonly IReadOnlyDictionary<
        Type,
        IReadOnlyList<Type>
    > s_sparkToClrCompatibility = BuildCompatibilityMap();

    /// <summary>
    /// Initializes a new <see cref="SparkRowHydrator{T}"/>.
    /// </summary>
    public SparkRowHydrator(SparkFrameProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _propertyMap = PropertyMappingHelper.BuildPropertyMap<T>();
    }

    // ──────────────────────────────────────────────────────────────────
    //  Pre-flight validation
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates that the <see cref="DataFrame"/>'s schema covers every property on
    /// <typeparamref name="T"/> with a compatible type.
    /// </summary>
    /// <remarks>
    /// Call this before <see cref="Collect"/> to surface schema drift as a structured
    /// pre-flight error rather than a runtime exception deep inside <c>GetAs&lt;T&gt;</c>.
    /// </remarks>
    /// <param name="dataFrame">The compiled native DataFrame to inspect.</param>
    /// <returns>
    /// An empty list when the schema is compatible; otherwise, one entry per problem column.
    /// </returns>
    public IReadOnlyList<SchemaValidationError> ValidateSchema(DataFrame dataFrame)
    {
        if (dataFrame == null)
            throw new ArgumentNullException(nameof(dataFrame));

        var errors = new List<SchemaValidationError>();
        var schemaFieldsByName = dataFrame.Schema().Fields.ToDictionary(
            f => f.Name,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var (externalName, property) in _propertyMap)
        {
            if (!schemaFieldsByName.TryGetValue(externalName, out var field))
            {
                errors.Add(
                    new SchemaValidationError(
                        externalName,
                        $"Column '{externalName}' (mapped from '{property.Name}') "
                            + "is not present in the DataFrame schema."
                    )
                );
                continue;
            }

            if (!IsCompatible(field.DataType, property.PropertyType))
            {
                errors.Add(
                    new SchemaValidationError(
                        externalName,
                        $"Column '{externalName}' has Spark type '{field.DataType.SimpleString}' "
                            + $"which is not compatible with C# property '{property.Name}' "
                            + $"of type '{property.PropertyType.Name}'."
                    )
                );
            }
        }

        return errors;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Materialization
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Compiles the expression tree, validates the live DataFrame schema, collects
    /// all rows, and hydrates them into <typeparamref name="T"/> instances.
    /// </summary>
    /// <param name="frame">The typed frame to materialize.</param>
    /// <returns>The materialized rows as an <see cref="IEnumerable{T}"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the DataFrame schema is incompatible with <typeparamref name="T"/>.
    /// The exception message lists all detected mismatches.
    /// </exception>
    public IEnumerable<T> Collect(TypedFrame<T> frame)
    {
        if (frame == null)
            throw new ArgumentNullException(nameof(frame));

        var dataFrame = _provider.CompileToNative(frame);

        var errors = ValidateSchema(dataFrame);
        if (errors.Count > 0)
        {
            var details = string.Join(
                Environment.NewLine,
                errors.Select(e => $"  • {e.ColumnName}: {e.Reason}")
            );
            throw new InvalidOperationException(
                $"DataFrame schema is incompatible with '{typeof(T).Name}':"
                    + Environment.NewLine
                    + details
            );
        }

        return dataFrame.Collect().Select(HydrateRow);
    }

    // ──────────────────────────────────────────────────────────────────
    //  Row → T hydration
    // ──────────────────────────────────────────────────────────────────

    private T HydrateRow(Row row)
    {
        // Records with required init properties cannot be set via reflection after construction,
        // so we build a Dictionary<string, object> and use the ObjectFactory pattern:
        // construct via the primary constructor by matching parameter names.
        //
        // If T has a parameterless constructor (unusual for schema records), fall back to
        // property-setter hydration.
        var ctor = FindRecordConstructor();
        if (ctor != null)
            return HydrateViaConstructor(row, ctor);

        return HydrateViaSetters(row);
    }

    private T HydrateViaConstructor(Row row, ConstructorInfo ctor)
    {
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            // Constructor parameter names match property names (C# record convention).
            // Look up the external field name via the property map.
            if (
                _propertyMap.TryGetValue(param.Name!, out var property)
                || _propertyMap.TryGetValue(
                    _propertyMap.Keys.FirstOrDefault(k =>
                        string.Equals(
                            k,
                            param.Name,
                            StringComparison.OrdinalIgnoreCase
                        )
                    ) ?? "",
                    out property
                )
            )
            {
                var externalName = PropertyMappingHelper.GetFieldName(property);
                args[i] = ConvertValue(row.Get(externalName), property.PropertyType);
            }
            else
            {
                args[i] = param.HasDefaultValue ? param.DefaultValue : null;
            }
        }

        return (T)ctor.Invoke(args);
    }

    private T HydrateViaSetters(Row row)
    {
        var instance = Activator.CreateInstance<T>();
        foreach (var (externalName, property) in _propertyMap)
        {
            if (!property.CanWrite)
                continue;

            var rawValue = row.Get(externalName);
            property.SetValue(instance, ConvertValue(rawValue, property.PropertyType));
        }
        return instance;
    }

    private ConstructorInfo? FindRecordConstructor()
    {
        // C# records emit a primary constructor whose parameter count matches property count.
        // Pick the constructor whose parameter names collectively match the property map keys
        // (by property name, not external label).
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return typeof(T)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Where(c =>
            {
                var ps = c.GetParameters();
                return ps.Length > 0
                    && ps.All(p => properties.Contains(p.Name ?? ""));
            })
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
    }

    // ──────────────────────────────────────────────────────────────────
    //  Type compatibility and conversion
    // ──────────────────────────────────────────────────────────────────

    private static object? ConvertValue(object? raw, Type targetType)
    {
        if (raw == null)
            return null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsEnum)
            return Enum.Parse(underlying, raw.ToString()!);

        try
        {
            return Convert.ChangeType(raw, underlying);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot convert Spark value '{raw}' ({raw.GetType().Name}) "
                    + $"to target type '{underlying.Name}'.",
                ex
            );
        }
    }

    private static bool IsCompatible(DataType sparkType, Type clrType)
    {
        var underlying = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (underlying.IsEnum)
            return sparkType is StringType or IntegerType or LongType;

        if (
            s_sparkToClrCompatibility.TryGetValue(sparkType.GetType(), out var compatibleClrTypes)
        )
            return compatibleClrTypes.Contains(underlying);

        // Unknown Spark type — allow through; runtime will report the real error.
        return true;
    }

    private static IReadOnlyDictionary<Type, IReadOnlyList<Type>> BuildCompatibilityMap()
    {
        return new Dictionary<Type, IReadOnlyList<Type>>
        {
            [typeof(StringType)] = [typeof(string)],
            [typeof(BooleanType)] = [typeof(bool)],
            [typeof(ByteType)] = [typeof(byte), typeof(sbyte), typeof(short), typeof(int), typeof(long)],
            [typeof(ShortType)] = [typeof(short), typeof(int), typeof(long)],
            [typeof(IntegerType)] = [typeof(int), typeof(long)],
            [typeof(LongType)] = [typeof(long)],
            [typeof(FloatType)] = [typeof(float), typeof(double)],
            [typeof(DoubleType)] = [typeof(double)],
            [typeof(DecimalType)] = [typeof(decimal), typeof(double)],
            [typeof(BinaryType)] = [typeof(byte[])],
            [typeof(DateType)] = [typeof(DateTime), typeof(DateOnly)],
            [typeof(TimestampType)] = [typeof(DateTime), typeof(DateTimeOffset)],
        };
    }
}
