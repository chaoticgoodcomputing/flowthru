using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Flowthru.Data.Schema;
using Parquet;
using Parquet.Schema;
using Parquet.Serialization;

namespace Flowthru.Data.Storage.Parquet.Internal;

/// <summary>
/// Bridge between Flowthru schemas (with <c>required</c> init-only
/// members) and Parquet.NET's serialiser (which requires a
/// parameterless constructor with mutable properties). Generates a
/// runtime DTO type via <see cref="System.Reflection.Emit"/> that
/// mirrors <typeparamref name="TRow"/>'s public properties, copies
/// values across the boundary via compiled expression-tree delegates,
/// and routes through Parquet.NET's reflection-driven reader/writer.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why a runtime DTO?</strong> Parquet.NET's
/// <see cref="ParquetSerializer"/> uses reflection to set property
/// values and demands a parameterless constructor on the target type.
/// Flowthru schemas with <c>required</c> init-only members can't be
/// instantiated that way. Compiling a fresh DTO type at runtime gives
/// us a parameterless-ctor surface Parquet.NET is happy with while
/// keeping the user-facing schema's invariants intact.
/// </para>
/// <para>
/// <strong>Fast path.</strong> If <typeparamref name="TRow"/> already
/// has a public parameterless constructor and no <c>[SerializedLabel]</c>
/// remappings, the DTO type <em>is</em> <typeparamref name="TRow"/> —
/// no IL emission, no copy.
/// </para>
/// <para>
/// <strong>Slow path.</strong> Otherwise, the runtime DTO has the same
/// property names as the user-facing schema (with
/// <c>[SerializedLabel]</c> applied) and value-type properties widened
/// to their nullable form (matching Parquet's wire-level convention
/// of nullable columns).
/// </para>
/// </remarks>
internal sealed class ParquetAdapter<TRow>
  where TRow : notnull
{
  private readonly Type _dtoType;
  private readonly Func<TRow, object> _toDto;
  private readonly Func<object, TRow> _fromDto;
  private readonly MethodInfo _serializeMethod;
  private readonly MethodInfo _deserializeRowGroupMethod;
  private readonly Dictionary<string, string> _propertyNameMap;
  private readonly Dictionary<string, Type>? _parquetColumnTypes;

  public ParquetAdapter(ParquetSchema? parquetSchema)
  {
    _propertyNameMap = BuildPropertyNameMap();

    if (parquetSchema is not null)
    {
      _parquetColumnTypes = ExtractParquetColumnTypes(parquetSchema);
    }

    _dtoType = CreateDtoType();
    _toDto = CompileToDtoFunction();
    _fromDto = CompileFromDtoFunction();

    _serializeMethod = typeof(ParquetSerializer)
      .GetMethods()
      .First(m =>
        m.Name == nameof(ParquetSerializer.SerializeAsync) && m.GetParameters().Length == 4
      )
      .MakeGenericMethod(_dtoType);

    _deserializeRowGroupMethod = typeof(ParquetSerializer)
      .GetMethods()
      .First(m =>
        m.Name == nameof(ParquetSerializer.DeserializeAsync)
        && m.GetParameters().Length == 4
        && m.GetParameters()[1].ParameterType == typeof(int)
      )
      .MakeGenericMethod(_dtoType);
  }

  public TRow FromDto(object dto) => _fromDto(dto);

  // ── Write path ────────────────────────────────────────────────────────

  /// <summary>
  /// Serialise rows to Parquet, flushing one row group per
  /// <paramref name="rowGroupSize"/> batch. Peak memory bounded to
  /// one row group regardless of total dataset size.
  /// </summary>
  public async Task SerializeToParquetAsync(
    Stream stream,
    IAsyncEnumerable<TRow> rows,
    ParquetSerializerOptions? writeOptions,
    int rowGroupSize
  )
  {
    var listType = typeof(List<>).MakeGenericType(_dtoType);
    var batch = (System.Collections.IList)Activator.CreateInstance(listType)!;
    var firstBatch = true;

    await foreach (var row in rows.ConfigureAwait(false))
    {
      batch.Add(_toDto(row));
      if (batch.Count >= rowGroupSize)
      {
        await SerializeBatch(batch, stream, writeOptions, firstBatch).ConfigureAwait(false);
        firstBatch = false;
        batch.Clear();
      }
    }

    // Final flush — handles a partial last batch in the streaming case
    // and forces a schema-only write when the source produced no rows.
    // Parquet.Net's SerializeAsync<T> derives the column schema from
    // T's reflection even for an empty list, so an empty round-trip
    // still produces a valid Parquet file with a footer.
    if (batch.Count > 0 || firstBatch)
    {
      await SerializeBatch(batch, stream, writeOptions, firstBatch).ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Write one batch as a single Parquet row group. After the first
  /// batch we stamp <c>Append = true</c> on the options so subsequent
  /// flushes append a row group rather than overwriting the file.
  /// </summary>
  private async Task SerializeBatch(
    System.Collections.IList batch,
    Stream stream,
    ParquetSerializerOptions? writeOptions,
    bool isFirstBatch
  )
  {
    ParquetSerializerOptions? opts;
    if (!isFirstBatch)
    {
      opts =
        writeOptions is not null
          ? new ParquetSerializerOptions
          {
            Append = true,
            CompressionMethod = writeOptions.CompressionMethod,
            CompressionLevel = writeOptions.CompressionLevel,
            RowGroupSize = writeOptions.RowGroupSize,
            PropertyNameCaseInsensitive = writeOptions.PropertyNameCaseInsensitive,
            ParquetOptions = writeOptions.ParquetOptions,
          }
          : new ParquetSerializerOptions { Append = true };
    }
    else
    {
      opts = writeOptions;
    }

    var task = (Task)_serializeMethod.Invoke(null, [batch, stream, opts, CancellationToken.None])!;
    await task.ConfigureAwait(false);
  }

  // ── Read path ─────────────────────────────────────────────────────────

  /// <summary>
  /// Deserialize one row group identified by <paramref name="rowGroupIndex"/>.
  /// Threading caller-supplied <paramref name="readOptions"/> into
  /// Parquet.NET keeps date/time mapping and big-decimal preferences
  /// honoured.
  /// </summary>
  public async Task<System.Collections.IList> DeserializeRowGroup(
    Stream stream,
    int rowGroupIndex,
    ParquetSerializerOptions? readOptions
  )
  {
    var task = (Task)_deserializeRowGroupMethod.Invoke(
      null,
      [stream, rowGroupIndex, readOptions, CancellationToken.None]
    )!;
    await task.ConfigureAwait(false);

    var resultProperty = task.GetType().GetProperty("Result")!;
    return (System.Collections.IList)resultProperty.GetValue(task)!;
  }

  // ── DTO type synthesis ────────────────────────────────────────────────

  private static Dictionary<string, Type> ExtractParquetColumnTypes(ParquetSchema schema)
  {
    var columnTypes = new Dictionary<string, Type>();
    foreach (var field in schema.GetDataFields())
    {
      var clrType = MapParquetTypeToClr(field);
      if (clrType is not null)
      {
        columnTypes[field.Name] = clrType;
      }
    }
    return columnTypes;
  }

  private static Type? MapParquetTypeToClr(DataField field)
  {
    var baseType = field.ClrType;
    // Parquet fields are often nullable on disk; widen value types
    // accordingly so the DTO holds nullable wrappers.
    if (field.IsNullable && baseType.IsValueType && Nullable.GetUnderlyingType(baseType) is null)
    {
      baseType = typeof(Nullable<>).MakeGenericType(baseType);
    }
    return baseType;
  }

  /// <summary>
  /// Build the <c>TRow.Property</c> → <c>DTO.Property</c> name map.
  /// Honours <see cref="SerializedLabelAttribute"/>.
  /// </summary>
  private static Dictionary<string, string> BuildPropertyNameMap()
  {
    var properties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var map = new Dictionary<string, string>();
    foreach (var property in properties)
    {
      if (!property.CanRead)
      {
        continue;
      }
      var label = property.GetCustomAttribute<SerializedLabelAttribute>();
      map[property.Name] = label?.Label ?? property.Name;
    }
    return map;
  }

  /// <summary>
  /// If <typeparamref name="TRow"/> already has a public parameterless
  /// constructor, no <c>[SerializedLabel]</c> remappings, and (on the
  /// read path) property types matching the file's column types, use it
  /// directly as the DTO type. Otherwise generate a runtime DTO via
  /// <see cref="System.Reflection.Emit"/>.
  /// </summary>
  private Type CreateDtoType()
  {
    if (
      HasParameterlessConstructor(typeof(TRow))
      && !HasSerializedLabelAttributes()
      && MatchesParquetColumnTypes()
    )
    {
      return typeof(TRow);
    }
    return GenerateRuntimeDtoType();
  }

  private static bool HasParameterlessConstructor(Type type) =>
    type.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0);

  private bool HasSerializedLabelAttributes() =>
    _propertyNameMap.Any(kvp => kvp.Key != kvp.Value);

  /// <summary>
  /// True when every on-disk column type (nullability-widened per
  /// <see cref="MapParquetTypeToClr"/>) matches the corresponding
  /// <typeparamref name="TRow"/> property type exactly, so Parquet.NET
  /// can deserialize into <typeparamref name="TRow"/> directly. Always
  /// true on the write path (no file schema to mirror). External
  /// writers (DuckDB, Spark, pandas) conventionally mark every column
  /// optional even for never-null data; deserializing such a file into
  /// a non-nullable property is a definition-level mismatch inside
  /// Parquet.NET, so those files must route through the runtime DTO,
  /// which mirrors the file's types and converts (with null-contract
  /// enforcement) in <see cref="FromDto"/>.
  /// </summary>
  private bool MatchesParquetColumnTypes()
  {
    if (_parquetColumnTypes is null)
    {
      return true;
    }

    foreach (var property in typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
      if (!property.CanRead)
      {
        continue;
      }
      if (
        _parquetColumnTypes.TryGetValue(_propertyNameMap[property.Name], out var fileType)
        && fileType != property.PropertyType
      )
      {
        return false;
      }
    }
    return true;
  }

  /// <summary>
  /// Generate a runtime type that mirrors <typeparamref name="TRow"/>'s
  /// public properties, with a parameterless constructor and mutable
  /// auto-properties. Each DTO instance is created in a unique
  /// dynamic assembly to avoid collisions across schema types.
  /// </summary>
  private Type GenerateRuntimeDtoType()
  {
    var typeBuilder = CreateDynamicTypeBuilder();
    AddParameterlessConstructor(typeBuilder);
    CopyPropertiesFromSourceType(typeBuilder);
    return typeBuilder.CreateType()!;
  }

  private static TypeBuilder CreateDynamicTypeBuilder()
  {
    var assemblyName = new AssemblyName($"ParquetDto_{typeof(TRow).Name}_{Guid.NewGuid():N}");
    var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
      assemblyName, AssemblyBuilderAccess.Run
    );
    var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
    return moduleBuilder.DefineType(
      $"{typeof(TRow).Name}Dto",
      TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed
    );
  }

  private static void AddParameterlessConstructor(TypeBuilder typeBuilder)
  {
    var constructorBuilder = typeBuilder.DefineConstructor(
      MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes
    );
    var il = constructorBuilder.GetILGenerator();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
    il.Emit(OpCodes.Ret);
  }

  private void CopyPropertiesFromSourceType(TypeBuilder typeBuilder)
  {
    var properties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var property in properties)
    {
      if (!property.CanRead)
      {
        continue;
      }

      var dtoPropertyName = _propertyNameMap[property.Name];

      Type dtoPropertyType;
      if (
        _parquetColumnTypes is not null
        && _parquetColumnTypes.TryGetValue(dtoPropertyName, out var parquetType)
      )
      {
        // Deserialisation path: mirror the on-disk column type so
        // Parquet.NET hands us values it can produce.
        dtoPropertyType = parquetType;
      }
      else
      {
        // Serialisation path: widen value types to nullable since
        // Parquet column nullability is the wire-level default.
        dtoPropertyType = property.PropertyType;
        if (dtoPropertyType.IsValueType && Nullable.GetUnderlyingType(dtoPropertyType) is null)
        {
          dtoPropertyType = typeof(Nullable<>).MakeGenericType(dtoPropertyType);
        }
      }

      EmitAutoProperty(typeBuilder, dtoPropertyName, dtoPropertyType);
    }
  }

  private static void EmitAutoProperty(
    TypeBuilder typeBuilder, string propertyName, Type propertyType
  )
  {
    var backingField = typeBuilder.DefineField(
      $"<{propertyName}>k__BackingField", propertyType, FieldAttributes.Private
    );

    var propertyBuilder = typeBuilder.DefineProperty(
      propertyName, PropertyAttributes.HasDefault, propertyType, null
    );

    EmitPropertyGetter(typeBuilder, propertyBuilder, backingField, propertyName, propertyType);
    EmitPropertySetter(typeBuilder, propertyBuilder, backingField, propertyName, propertyType);
  }

  private static void EmitPropertyGetter(
    TypeBuilder typeBuilder,
    PropertyBuilder propertyBuilder,
    FieldBuilder backingField,
    string propertyName,
    Type propertyType
  )
  {
    var getterBuilder = typeBuilder.DefineMethod(
      $"get_{propertyName}",
      MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
      propertyType, Type.EmptyTypes
    );
    var il = getterBuilder.GetILGenerator();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldfld, backingField);
    il.Emit(OpCodes.Ret);
    propertyBuilder.SetGetMethod(getterBuilder);
  }

  private static void EmitPropertySetter(
    TypeBuilder typeBuilder,
    PropertyBuilder propertyBuilder,
    FieldBuilder backingField,
    string propertyName,
    Type propertyType
  )
  {
    var setterBuilder = typeBuilder.DefineMethod(
      $"set_{propertyName}",
      MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
      null, [propertyType]
    );
    var il = setterBuilder.GetILGenerator();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldarg_1);
    il.Emit(OpCodes.Stfld, backingField);
    il.Emit(OpCodes.Ret);
    propertyBuilder.SetSetMethod(setterBuilder);
  }

  // ── Conversion delegates (TRow ↔ DTO) ─────────────────────────────────

  private Func<TRow, object> CompileToDtoFunction()
  {
    if (_dtoType == typeof(TRow))
    {
      return row => row;
    }

    var rowParam = Expression.Parameter(typeof(TRow), "row");
    var dtoVar = Expression.Variable(_dtoType, "dto");
    var dtoConstructor = _dtoType.GetConstructor(Type.EmptyTypes)!;
    var expressions = new List<Expression>
    {
      Expression.Assign(dtoVar, Expression.New(dtoConstructor)),
    };

    var properties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    foreach (var srcProperty in properties)
    {
      if (!srcProperty.CanRead) continue;

      var dtoPropertyName = _propertyNameMap[srcProperty.Name];
      var dstProperty = _dtoType.GetProperty(dtoPropertyName);
      if (dstProperty is null || !dstProperty.CanWrite) continue;

      Expression propertyValue = Expression.Property(rowParam, srcProperty);
      if (srcProperty.PropertyType != dstProperty.PropertyType)
      {
        propertyValue = Expression.Convert(propertyValue, dstProperty.PropertyType);
      }
      expressions.Add(Expression.Assign(Expression.Property(dtoVar, dstProperty), propertyValue));
    }

    expressions.Add(Expression.Convert(dtoVar, typeof(object)));

    var block = Expression.Block(new[] { dtoVar }, expressions);
    return Expression.Lambda<Func<TRow, object>>(block, rowParam).Compile();
  }

  private Func<object, TRow> CompileFromDtoFunction()
  {
    if (_dtoType == typeof(TRow))
    {
      return dto => (TRow)dto;
    }

    return dto =>
    {
      var instance = SchemaActivator.CreateInstance<TRow>();
      var properties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

      foreach (var dstProperty in properties)
      {
        if (!dstProperty.CanWrite) continue;

        var dtoPropertyName = _propertyNameMap[dstProperty.Name];
        var srcProperty = _dtoType.GetProperty(dtoPropertyName);
        if (srcProperty is null || !srcProperty.CanRead) continue;

        var value = srcProperty.GetValue(dto);

        if (value is null)
        {
          // Null-contract enforcement: Parquet has null but TRow's
          // schema declared the property non-nullable. Surface as
          // SchemaMismatchException so the composed adapter's
          // boundary lifts to the typed RuntimeError.SchemaMismatch
          // variant — same path as missing-column detection.
          var isNullable =
            Nullable.GetUnderlyingType(dstProperty.PropertyType) is not null
            || !dstProperty.PropertyType.IsValueType;

          if (!isNullable)
          {
            throw new SchemaMismatchException(
              $"Parquet field '{dtoPropertyName}' contains null but schema property "
              + $"'{dstProperty.Name}' is non-nullable ({dstProperty.PropertyType.Name}). "
              + "Either widen the schema property to nullable or ensure the file "
              + "carries no nulls for this column."
            );
          }
        }
        else
        {
          var underlyingSource = Nullable.GetUnderlyingType(srcProperty.PropertyType);
          var underlyingTarget = Nullable.GetUnderlyingType(dstProperty.PropertyType);
          var sourceType = underlyingSource ?? srcProperty.PropertyType;
          var targetType = underlyingTarget ?? dstProperty.PropertyType;

          if (sourceType != targetType)
          {
            try
            {
              if (targetType.IsEnum)
              {
                // Parquet stores enums as their underlying integer type
                // by default in this initial migration. (Honoring
                // [SerializedEnum] for cross-format string consistency
                // is a scoped follow-up — see §4.8 progress notes.)
                value = Enum.ToObject(targetType, value);
              }
              else
              {
                value = Convert.ChangeType(value, targetType);
              }
            }
            catch (InvalidCastException ex)
            {
              throw new SchemaMismatchException(
                $"Parquet field '{dtoPropertyName}' is type {sourceType.Name} but schema "
                + $"property '{dstProperty.Name}' expects {targetType.Name}.",
                ex
              );
            }
          }
        }

        dstProperty.SetValue(instance, value);
      }

      return instance;
    };
  }
}
