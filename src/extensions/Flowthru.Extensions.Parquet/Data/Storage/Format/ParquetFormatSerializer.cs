using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;
using Parquet;
using Parquet.Schema;
using Parquet.Serialization;

namespace Flowthru.Core.Data.Storage.Format;

/// <summary>
/// Format serializer for Parquet (columnar storage) files using adapter pattern.
/// </summary>
/// <typeparam name="TRow">The Flowthru schema type</typeparam>
/// <remarks>
/// <para>
/// <strong>Architecture:</strong>
/// </para>
/// <para>
/// Converts between Flowthru schemas (with required members) and Parquet-compatible DTOs:
/// </para>
/// <code>
/// Serialize:   TRow (required members) → DTO (parameterless ctor) → Parquet
/// Deserialize: Parquet → DTO (parameterless ctor) → TRow (required members)
/// </code>
/// <para>
/// <strong>Features:</strong>
/// </para>
/// <list type="bullet">
/// <item>SerializedLabel - Respects [SerializedLabel] attributes for property name mapping</item>
/// <item>Null Safety - Enforces non-nullable contracts during deserialization</item>
/// <item>Value Type Nullability - DTOs use nullable value types to match Parquet schema conventions</item>
/// <item>Enum Support - Automatically converts between Parquet's integer storage and enum types</item>
/// <item>Row group streaming - Writes in bounded batches (default 1M rows/group); peak write memory
/// is bounded to one row group regardless of total dataset size.</item>
/// </list>
/// <para>
/// <strong>Current Limitations:</strong>
/// </para>
/// <list type="bullet">
/// <item>SerializedEnum attributes are not used - enums stored/retrieved by underlying integer value</item>
/// <item>Per-column encoding hints require Parquet.Net v6 (not yet on NuGet); use
/// <see cref="ParquetItemOptions{TRow}.UseDictionaryEncoding"/> as a global flag in the meantime.</item>
/// </list>
/// </remarks>
public sealed class ParquetFormatSerializer<TRow> : IFormatSerializer<TRow>
  where TRow : notnull, IFlatSchema, IBinarySerializable
{
  private readonly ParquetItemOptions<TRow>? _options;

  /// <summary>
  /// Initializes a new instance with default production-ready options.
  /// </summary>
  public ParquetFormatSerializer() { }

  /// <summary>
  /// Initializes a new instance with caller-supplied tuning options.
  /// </summary>
  public ParquetFormatSerializer(ParquetItemOptions<TRow>? options) => _options = options;

  /// <inheritdoc/>
  /// <remarks>
  /// Parquet is a columnar format that supports row group streaming for efficient
  /// processing of large datasets.
  /// </remarks>
  public StorageTraits Traits => new StorageTraits { CanStream = true };

  /// <inheritdoc/>
  /// <remarks>
  /// Streams rows one row group at a time. Early-exit consumers (e.g. shallow inspection)
  /// will break after reading fewer than all row groups, avoiding full-file materialisation.
  /// Any <see cref="ParquetItemOptions{TRow}"/> supplied at construction time are threaded
  /// into the deserialiser (date type mapping, big-decimal, encoding settings).
  /// </remarks>
  public async IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
  {
    var readOptions = _options?.ToReadOptions();

    // Read schema and row-group count from the file footer (cheap seek-based metadata read)
    using var reader = await ParquetReader.CreateAsync(stream, leaveStreamOpen: true);
    var schema = reader.Schema;
    int rowGroupCount = reader.RowGroupCount;
    var adapter = new ParquetAdapter<TRow>(schema);

    // Yield one row group at a time so early-break consumers avoid reading the full file
    for (int rgi = 0; rgi < rowGroupCount; rgi++)
    {
      stream.Position = 0;
      var dtos = await adapter.DeserializeRowGroup(stream, rgi, readOptions);
      foreach (var dto in dtos)
      {
        yield return adapter.FromDto(dto);
      }
    }
  }

  /// <inheritdoc/>
  public async Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows)
  {
    // For serialization, create adapter based on TRow schema (no file to read)
    var adapter = new ParquetAdapter<TRow>(parquetSchema: null);
    await adapter.SerializeToParquetAsync(
      stream,
      rows,
      writeOptions: _options?.ToWriteOptions(),
      rowGroupSize: _options?.RowGroupSize ?? 1_000_000
    );
  }

  /// <inheritdoc/>
  public PropertyMappingConfiguration GetPropertyMappingConfiguration()
  {
    return PropertyMappingConfiguration.LibraryControlled(
      "Parquet.NET serialization respects [SerializedLabel] attributes for property name mapping."
    );
  }
}

/// <summary>
/// Adapter between Flowthru schema and Parquet-compatible DTO.
/// </summary>
internal sealed class ParquetAdapter<TRow>
  where TRow : notnull
{
  private readonly Type _dtoType;
  private readonly Func<TRow, object> _toDto;
  private readonly Func<object, TRow> _fromDto;
  private readonly MethodInfo _serializeMethod;
  private readonly MethodInfo _deserializeMethod;
  private MethodInfo _deserializeRowGroupMethod;
  private readonly Dictionary<string, string> _propertyNameMap; // Maps TRow property name -> DTO property name (serialized label)
  private readonly Dictionary<string, Type>? _parquetColumnTypes; // Maps column name -> actual Parquet type (for deserialization)

  /// <summary>
  /// Creates an adapter for Parquet serialization/deserialization.
  /// </summary>
  /// <param name="parquetSchema">Actual Parquet schema from file (for deserialization), or null (for serialization)</param>
  public ParquetAdapter(ParquetSchema? parquetSchema)
  {
    // Build property name mapping first (needed by CreateDtoType)
    _propertyNameMap = BuildPropertyNameMap();

    // Extract actual Parquet column types if schema provided
    if (parquetSchema != null)
    {
      _parquetColumnTypes = ExtractParquetColumnTypes(parquetSchema);
    }

    // Create DTO type dynamically based on Parquet schema (if available) or TRow schema
    _dtoType = CreateDtoType();

    // Compile conversion functions
    _toDto = CompileToDtoFunction();
    _fromDto = CompileFromDtoFunction();

    // Get Parquet.NET methods
    _serializeMethod = typeof(ParquetSerializer)
      .GetMethods()
      .First(m =>
        m.Name == nameof(ParquetSerializer.SerializeAsync) && m.GetParameters().Length == 4
      )
      .MakeGenericMethod(_dtoType);

    // Full deserialize (all row groups): (Stream, options, ct) — used by Load()
    _deserializeMethod = typeof(ParquetSerializer)
      .GetMethods()
      .First(m =>
        m.Name == nameof(ParquetSerializer.DeserializeAsync) && m.GetParameters().Length == 3
      )
      .MakeGenericMethod(_dtoType);

    // Single row-group deserialize: (Stream, int rowGroupIndex, options, ct) — used by DeserializeRows()
    _deserializeRowGroupMethod = typeof(ParquetSerializer)
      .GetMethods()
      .First(m =>
        m.Name == nameof(ParquetSerializer.DeserializeAsync)
        && m.GetParameters().Length == 4
        && m.GetParameters()[1].ParameterType == typeof(int)
      )
      .MakeGenericMethod(_dtoType);
  }

  public object ToDto(TRow row) => _toDto(row);

  public TRow FromDto(object dto) => _fromDto(dto);

  /// <summary>
  /// Serializes rows to Parquet format, flushing one row group per <paramref name="rowGroupSize"/>
  /// batch. Peak write-side memory is bounded to one row group regardless of total dataset size.
  /// </summary>
  /// <remarks>
  /// Each flush calls <see cref="ParquetSerializer.SerializeAsync"/> with <c>Append = true</c> after
  /// the first batch, producing one Parquet row group per batch. For 1–10 GB datasets this avoids
  /// materialising the entire dataset in memory and produces multi-row-group files that enable
  /// predicate pushdown and read parallelism in downstream query engines.
  /// </remarks>
  public async Task SerializeToParquetAsync(
    Stream stream,
    IAsyncEnumerable<TRow> rows,
    ParquetSerializerOptions? writeOptions,
    int rowGroupSize
  )
  {
    var listType = typeof(List<>).MakeGenericType(_dtoType);
    var batch = (System.Collections.IList)Activator.CreateInstance(listType)!;
    bool firstBatch = true;

    await foreach (var row in rows)
    {
      batch.Add(_toDto(row));

      if (batch.Count >= rowGroupSize)
      {
        await SerializeBatch(batch, stream, writeOptions, firstBatch);
        firstBatch = false;
        batch.Clear();
      }
    }

    // Write the final (possibly partial) batch — handles the common single-batch case too.
    if (batch.Count > 0)
    {
      await SerializeBatch(batch, stream, writeOptions, firstBatch);
    }
  }

  /// <summary>
  /// Writes one batch as a single Parquet row group. Stamps <c>Append = true</c> on
  /// subsequent batches so that each call appends a new row group rather than overwriting.
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
      // Clone the caller's options (or create minimal ones) with Append = true.
      // ParquetSerializerOptions is a plain class with no copy constructor, so we
      // construct a fresh instance and copy every relevant property.
      opts = writeOptions != null
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

    // Invoke: Task ParquetSerializer.SerializeAsync<TDto>(IEnumerable<TDto>, Stream, options, ct)
    var task = (Task)
      _serializeMethod.Invoke(null, [batch, stream, opts, CancellationToken.None])!;
    await task;
  }

  public async Task<System.Collections.IList> DeserializeFromParquet(Stream stream)
  {
    // Invoke: Task<IList<TDto>> ParquetSerializer.DeserializeAsync<TDto>(Stream, ...)
    var task = (Task)_deserializeMethod.Invoke(null, [stream, null, CancellationToken.None])!;
    await task;

    // Extract result using reflection - the Task<IList<TDto>> has a Result property
    var resultProperty = task.GetType().GetProperty("Result")!;
    return (System.Collections.IList)resultProperty.GetValue(task)!;
  }

  /// <summary>
  /// Deserializes a single row group identified by <paramref name="rowGroupIndex"/>,
  /// threading any caller-supplied <paramref name="readOptions"/> into Parquet.NET.
  /// This keeps I/O bounded to one row group when consumers break early.
  /// </summary>
  public async Task<System.Collections.IList> DeserializeRowGroup(
    Stream stream,
    int rowGroupIndex,
    ParquetSerializerOptions? readOptions
  )
  {
    // Invoke: Task<IList<TDto>> ParquetSerializer.DeserializeAsync<TDto>(Stream, int, options, ct)
    var task = (Task)_deserializeRowGroupMethod.Invoke(
      null,
      [stream, rowGroupIndex, readOptions, CancellationToken.None]
    )!;
    await task;

    var resultProperty = task.GetType().GetProperty("Result")!;
    return (System.Collections.IList)resultProperty.GetValue(task)!;
  }

  /// <summary>
  /// Extracts column types from the Parquet schema.
  /// Maps column names to their CLR types for DTO generation.
  /// </summary>
  private static Dictionary<string, Type> ExtractParquetColumnTypes(ParquetSchema schema)
  {
    var columnTypes = new Dictionary<string, Type>();

    foreach (var field in schema.GetDataFields())
    {
      var clrType = MapParquetTypeToClr(field);
      if (clrType != null)
      {
        columnTypes[field.Name] = clrType;
      }
    }

    return columnTypes;
  }

  /// <summary>
  /// Maps Parquet DataField to CLR type.
  /// Handles nullability and type conversions.
  /// </summary>
  private static Type? MapParquetTypeToClr(DataField field)
  {
    // Get the base CLR type from Parquet field
    Type baseType = field.ClrType;

    // Handle nullability - Parquet fields are often nullable
    // If field is nullable and type is value type, make it nullable
    if (field.IsNullable && baseType.IsValueType && Nullable.GetUnderlyingType(baseType) == null)
    {
      baseType = typeof(Nullable<>).MakeGenericType(baseType);
    }

    return baseType;
  }

  /// <summary>
  /// Builds a mapping from TRow property names to DTO property names (serialized labels).
  /// Respects [SerializedLabel] attributes for external data compatibility.
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

      // Get serialized name (respects [SerializedLabel] attribute)
      var serializedName = PropertyMappingHelper.GetFieldName(property);
      map[property.Name] = serializedName;
    }

    return map;
  }

  /// <summary>
  /// Creates a DTO type for Parquet serialization.
  /// Returns TRow directly if it already has a parameterless constructor AND no SerializedLabel attributes,
  /// otherwise generates a runtime DTO type with serialized property names.
  /// </summary>
  private Type CreateDtoType()
  {
    // Fast path: if TRow already has parameterless constructor AND no custom serialization labels
    if (HasParameterlessConstructor(typeof(TRow)) && !HasSerializedLabelAttributes())
    {
      return typeof(TRow);
    }

    // Slow path: generate runtime DTO type that mirrors TRow's structure with serialized names
    return GenerateRuntimeDtoType();
  }

  /// <summary>
  /// Checks if a type has a public parameterless constructor.
  /// </summary>
  private static bool HasParameterlessConstructor(Type type)
  {
    return type.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0);
  }

  /// <summary>
  /// Checks if TRow has any [SerializedLabel] attributes on its properties.
  /// </summary>
  private bool HasSerializedLabelAttributes()
  {
    // Check if any property has a different serialized name than its property name
    return _propertyNameMap.Any(kvp => kvp.Key != kvp.Value);
  }

  /// <summary>
  /// Generates a runtime type using Reflection.Emit that mirrors TRow's properties
  /// but includes a parameterless constructor for Parquet.NET compatibility.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This uses advanced Reflection.Emit APIs to dynamically create a new type at runtime.
  /// The generated type will have:
  /// </para>
  /// <list type="bullet">
  /// <item>A public parameterless constructor (required by Parquet.NET)</item>
  /// <item>All public properties from TRow with serialized names (respects [SerializedLabel])</item>
  /// <item>Auto-implemented property pattern (backing field + getter/setter)</item>
  /// </list>
  /// <para>
  /// The type is created in a dynamic assembly with a unique name to avoid collisions.
  /// Each property gets a compiler-style backing field name: &lt;PropertyName&gt;k__BackingField
  /// </para>
  /// </remarks>
  private Type GenerateRuntimeDtoType()
  {
    // Create a dynamic assembly and module to host the DTO type
    var typeBuilder = CreateDynamicTypeBuilder();

    // Emit IL for parameterless constructor: calls base object() constructor
    AddParameterlessConstructor(typeBuilder);

    // Copy all readable properties from TRow to DTO
    CopyPropertiesFromSourceType(typeBuilder);

    // Finalize and return the runtime type
    return typeBuilder.CreateType()!;
  }

  /// <summary>
  /// Creates a TypeBuilder for runtime type generation.
  /// Sets up a dynamic assembly with a unique name to avoid type collisions.
  /// </summary>
  private static TypeBuilder CreateDynamicTypeBuilder()
  {
    // Use GUID to ensure assembly name uniqueness across multiple DTO generations
    var assemblyName = new AssemblyName($"ParquetDto_{typeof(TRow).Name}_{Guid.NewGuid():N}");

    var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
      assemblyName,
      AssemblyBuilderAccess.Run
    );

    var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

    // Define a public sealed class (similar to C# records/classes)
    return moduleBuilder.DefineType(
      $"{typeof(TRow).Name}Dto",
      TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed
    );
  }

  /// <summary>
  /// Emits IL instructions for a parameterless constructor that calls object's constructor.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Equivalent C# code:
  /// </para>
  /// <code>
  /// public MyDto() : base() { }
  /// </code>
  /// <para>
  /// IL Instructions:
  /// </para>
  /// <list type="number">
  /// <item>ldarg.0 - Load 'this' pointer onto stack</item>
  /// <item>call object::.ctor() - Call base object constructor</item>
  /// <item>ret - Return from constructor</item>
  /// </list>
  /// </remarks>
  private static void AddParameterlessConstructor(TypeBuilder typeBuilder)
  {
    var constructorBuilder = typeBuilder.DefineConstructor(
      MethodAttributes.Public,
      CallingConventions.Standard,
      Type.EmptyTypes
    );

    var il = constructorBuilder.GetILGenerator();

    // Emit: this.base()
    il.Emit(OpCodes.Ldarg_0); // Load 'this'
    il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!); // Call base ctor
    il.Emit(OpCodes.Ret); // Return
  }

  /// <summary>
  /// Copies all readable public properties from TRow to the DTO type.
  /// Each property gets a backing field and auto-implemented getter/setter.
  /// Uses [SerializedLabel] attributes for property names if present.
  /// Makes value types nullable to match Parquet schema conventions.
  /// </summary>
  private void CopyPropertiesFromSourceType(TypeBuilder typeBuilder)
  {
    var properties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var property in properties)
    {
      if (!property.CanRead)
      {
        continue;
      }

      // Use serialized label from mapping (respects [SerializedLabel] attribute)
      var dtoPropertyName = _propertyNameMap[property.Name];

      // Determine DTO property type based on context:
      // 1. If we have actual Parquet schema (deserialization), use its type
      // 2. Otherwise (serialization), use TRow type with nullable wrappers
      Type dtoPropertyType;

      if (
        _parquetColumnTypes != null
        && _parquetColumnTypes.TryGetValue(dtoPropertyName, out var parquetType)
      )
      {
        // Use actual Parquet column type from file schema
        dtoPropertyType = parquetType;
      }
      else
      {
        // Fallback for serialization: make value types nullable
        // Parquet typically stores primitives as nullable fields
        dtoPropertyType = property.PropertyType;
        if (dtoPropertyType.IsValueType && Nullable.GetUnderlyingType(dtoPropertyType) == null)
        {
          dtoPropertyType = typeof(Nullable<>).MakeGenericType(dtoPropertyType);
        }
      }

      EmitAutoProperty(typeBuilder, dtoPropertyName, dtoPropertyType);
    }
  }

  /// <summary>
  /// Emits an auto-implemented property with backing field and getter/setter methods.
  /// Follows C# compiler conventions for auto-property naming and structure.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Equivalent C# code:
  /// </para>
  /// <code>
  /// public TPropertyType PropertyName { get; set; }
  /// </code>
  /// <para>
  /// This creates:
  /// </para>
  /// <list type="bullet">
  /// <item>Private backing field: &lt;PropertyName&gt;k__BackingField</item>
  /// <item>Public getter method: get_PropertyName()</item>
  /// <item>Public setter method: set_PropertyName(TPropertyType value)</item>
  /// </list>
  /// </remarks>
  private static void EmitAutoProperty(
    TypeBuilder typeBuilder,
    string propertyName,
    Type propertyType
  )
  {
    // Create backing field with compiler-style naming convention
    var backingField = typeBuilder.DefineField(
      $"<{propertyName}>k__BackingField",
      propertyType,
      FieldAttributes.Private
    );

    var propertyBuilder = typeBuilder.DefineProperty(
      propertyName,
      PropertyAttributes.HasDefault,
      propertyType,
      null
    );

    // Emit getter: public TPropertyType get_PropertyName() => backingField;
    EmitPropertyGetter(typeBuilder, propertyBuilder, backingField, propertyName, propertyType);

    // Emit setter: public void set_PropertyName(TPropertyType value) => backingField = value;
    EmitPropertySetter(typeBuilder, propertyBuilder, backingField, propertyName, propertyType);
  }

  /// <summary>
  /// Emits IL for a property getter method.
  /// </summary>
  /// <remarks>
  /// <para>
  /// IL Instructions:
  /// </para>
  /// <list type="number">
  /// <item>ldarg.0 - Load 'this' pointer</item>
  /// <item>ldfld backingField - Load backing field value</item>
  /// <item>ret - Return field value</item>
  /// </list>
  /// </remarks>
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
      propertyType,
      Type.EmptyTypes
    );

    var il = getterBuilder.GetILGenerator();
    il.Emit(OpCodes.Ldarg_0); // Load 'this'
    il.Emit(OpCodes.Ldfld, backingField); // Load backing field
    il.Emit(OpCodes.Ret); // Return field value

    propertyBuilder.SetGetMethod(getterBuilder);
  }

  /// <summary>
  /// Emits IL for a property setter method.
  /// </summary>
  /// <remarks>
  /// <para>
  /// IL Instructions:
  /// </para>
  /// <list type="number">
  /// <item>ldarg.0 - Load 'this' pointer</item>
  /// <item>ldarg.1 - Load 'value' parameter</item>
  /// <item>stfld backingField - Store value to backing field</item>
  /// <item>ret - Return</item>
  /// </list>
  /// </remarks>
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
      null,
      [propertyType]
    );

    var il = setterBuilder.GetILGenerator();
    il.Emit(OpCodes.Ldarg_0); // Load 'this'
    il.Emit(OpCodes.Ldarg_1); // Load 'value' parameter
    il.Emit(OpCodes.Stfld, backingField); // Store to backing field
    il.Emit(OpCodes.Ret); // Return

    propertyBuilder.SetSetMethod(setterBuilder);
  }

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
      if (!srcProperty.CanRead)
      {
        continue;
      }

      // Use serialized property name from mapping
      var dtoPropertyName = _propertyNameMap[srcProperty.Name];
      var dstProperty = _dtoType.GetProperty(dtoPropertyName);
      if (dstProperty != null && dstProperty.CanWrite)
      {
        Expression propertyValue = Expression.Property(rowParam, srcProperty);

        // Handle type conversion if DTO property is nullable but TRow property isn't
        if (srcProperty.PropertyType != dstProperty.PropertyType)
        {
          propertyValue = Expression.Convert(propertyValue, dstProperty.PropertyType);
        }

        expressions.Add(Expression.Assign(Expression.Property(dtoVar, dstProperty), propertyValue));
      }
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

    // Use reflection-based approach instead of expression trees
    // This allows us to use PropertyInfo.SetValue which handles init accessors
    // and perform null checking for contract enforcement
    return dto =>
    {
      var instance = SchemaActivator.CreateInstance<TRow>();
      var properties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

      foreach (var dstProperty in properties)
      {
        if (!dstProperty.CanWrite)
        {
          continue;
        }

        // Use property name mapping (respects [SerializedLabel])
        var dtoPropertyName = _propertyNameMap[dstProperty.Name];
        var srcProperty = _dtoType.GetProperty(dtoPropertyName);

        if (srcProperty != null && srcProperty.CanRead)
        {
          var value = srcProperty.GetValue(dto);

          // Null checking for contract enforcement:
          // If Parquet has null but TRow expects non-nullable, throw clear error
          if (value == null)
          {
            var isNullable =
              Nullable.GetUnderlyingType(dstProperty.PropertyType) != null
              || !dstProperty.PropertyType.IsValueType;

            if (!isNullable)
            {
              throw new InvalidDataException(
                $"Parquet deserialization failed: Field '{dtoPropertyName}' contains null value, "
                  + $"but schema property '{dstProperty.Name}' is non-nullable ({dstProperty.PropertyType.Name}). "
                  + $"Either make the schema property nullable ({dstProperty.PropertyType.Name}?) or ensure "
                  + $"the Parquet file contains no null values for this field."
              );
            }
          }
          else
          {
            // Handle type conversions between DTO and TRow
            // DTO type may differ from TRow type due to Parquet schema
            var underlyingSource = Nullable.GetUnderlyingType(srcProperty.PropertyType);
            var underlyingTarget = Nullable.GetUnderlyingType(dstProperty.PropertyType);
            var sourceType = underlyingSource ?? srcProperty.PropertyType;
            var targetType = underlyingTarget ?? dstProperty.PropertyType;

            // Convert if types don't match (comparing underlying non-nullable types)
            if (sourceType != targetType)
            {
              try
              {
                // Special handling for enum types
                if (targetType.IsEnum)
                {
                  // Parquet stores enums as their underlying integer type
                  // Use Enum.ToObject to convert from integer to enum
                  value = Enum.ToObject(targetType, value);
                }
                else
                {
                  value = Convert.ChangeType(value, targetType);
                }
              }
              catch (InvalidCastException ex)
              {
                throw new InvalidDataException(
                  $"Parquet deserialization failed: Cannot convert field '{dtoPropertyName}' "
                    + $"from {sourceType.Name} to {targetType.Name}. "
                    + $"The Parquet file has type {sourceType.Name} but schema expects {targetType.Name}.",
                  ex
                );
              }
            }
          }

          // PropertyInfo.SetValue handles init accessors correctly
          dstProperty.SetValue(instance, value);
        }
      }

      return instance;
    };
  }
}
