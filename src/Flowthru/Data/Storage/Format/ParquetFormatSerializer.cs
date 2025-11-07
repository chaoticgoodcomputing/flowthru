using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Flowthru.Abstractions;
using Parquet.Serialization;

namespace Flowthru.Data.Storage.Format;

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
/// <strong>Current Limitations:</strong>
/// </para>
/// <list type="bullet">
/// <item>SerializedLabel not yet supported - uses property names</item>
/// <item>SerializedEnum not yet supported - uses underlying values</item>
/// </list>
/// </remarks>
public sealed class ParquetFormatSerializer<TRow> : IFormatSerializer<TRow>
  where TRow : notnull, IFlatSchema, IBinarySerializable
{
  private static readonly ConcurrentDictionary<Type, object> _adapterCache = new();

  public ParquetFormatSerializer() { }

  public async IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
  {
    var adapter = GetOrCreateAdapter();
    var dtos = await adapter.DeserializeFromParquet(stream);

    foreach (var dto in dtos)
    {
      yield return adapter.FromDto(dto);
    }
  }

  public async Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows)
  {
    var adapter = GetOrCreateAdapter();
    await adapter.SerializeToParquetAsync(stream, rows);
  }

  private static ParquetAdapter<TRow> GetOrCreateAdapter()
  {
    return (ParquetAdapter<TRow>)
      _adapterCache.GetOrAdd(typeof(TRow), _ => new ParquetAdapter<TRow>());
  }

  public PropertyMappingConfiguration GetPropertyMappingConfiguration()
  {
    return PropertyMappingConfiguration.LibraryControlled(
      "Parquet.NET uses property names. SerializedLabel not yet supported."
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

  public ParquetAdapter()
  {
    // Create DTO type dynamically
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

    _deserializeMethod = typeof(ParquetSerializer)
      .GetMethods()
      .First(m =>
        m.Name == nameof(ParquetSerializer.DeserializeAsync) && m.GetParameters().Length == 3
      )
      .MakeGenericMethod(_dtoType);
  }

  public object ToDto(TRow row) => _toDto(row);

  public TRow FromDto(object dto) => _fromDto(dto);

  /// <summary>
  /// Serializes rows to Parquet format with proper type safety.
  /// Converts TRow instances to DTO instances and maintains type through serialization.
  /// </summary>
  public async Task SerializeToParquetAsync(Stream stream, IAsyncEnumerable<TRow> rows)
  {
    // Convert to strongly-typed list using reflection to create List<TDto>
    var listType = typeof(List<>).MakeGenericType(_dtoType);
    var dtosList = (System.Collections.IList)Activator.CreateInstance(listType)!;

    await foreach (var row in rows)
    {
      dtosList.Add(_toDto(row));
    }

    // Invoke: Task ParquetSerializer.SerializeAsync<TDto>(IEnumerable<TDto>, Stream, ...)
    var task = (Task)
      _serializeMethod.Invoke(null, [dtosList, stream, null, CancellationToken.None])!;
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
  /// Creates a DTO type for Parquet serialization.
  /// Returns TRow directly if it already has a parameterless constructor,
  /// otherwise generates a runtime DTO type with identical properties.
  /// </summary>
  private static Type CreateDtoType()
  {
    // Fast path: if TRow already has parameterless constructor, use it directly
    if (HasParameterlessConstructor(typeof(TRow)))
    {
      return typeof(TRow);
    }

    // Slow path: generate runtime DTO type that mirrors TRow's structure
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
  /// <item>All public properties from TRow with same names and types</item>
  /// <item>Auto-implemented property pattern (backing field + getter/setter)</item>
  /// </list>
  /// <para>
  /// The type is created in a dynamic assembly with a unique name to avoid collisions.
  /// Each property gets a compiler-style backing field name: &lt;PropertyName&gt;k__BackingField
  /// </para>
  /// </remarks>
  private static Type GenerateRuntimeDtoType()
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
  /// </summary>
  private static void CopyPropertiesFromSourceType(TypeBuilder typeBuilder)
  {
    var properties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var property in properties)
    {
      if (!property.CanRead)
      {
        continue;
      }

      EmitAutoProperty(typeBuilder, property.Name, property.PropertyType);
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

      var dstProperty = _dtoType.GetProperty(srcProperty.Name);
      if (dstProperty != null && dstProperty.CanWrite)
      {
        expressions.Add(
          Expression.Assign(
            Expression.Property(dtoVar, dstProperty),
            Expression.Property(rowParam, srcProperty)
          )
        );
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

    var dtoParam = Expression.Parameter(typeof(object), "dto");
    var typedDto = Expression.Variable(_dtoType, "typedDto");
    var rowVar = Expression.Variable(typeof(TRow), "row");

    var expressions = new List<Expression>
    {
      Expression.Assign(typedDto, Expression.Convert(dtoParam, _dtoType)),
      Expression.Assign(
        rowVar,
        Expression.Call(
          typeof(SchemaActivator)
            .GetMethod(nameof(SchemaActivator.CreateInstance))!
            .MakeGenericMethod(typeof(TRow))
        )
      ),
    };

    var properties = typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    foreach (var dstProperty in properties)
    {
      if (!dstProperty.CanWrite)
      {
        continue;
      }

      var srcProperty = _dtoType.GetProperty(dstProperty.Name);
      if (srcProperty != null && srcProperty.CanRead)
      {
        expressions.Add(
          Expression.Call(
            rowVar,
            dstProperty.SetMethod!,
            Expression.Property(typedDto, srcProperty)
          )
        );
      }
    }

    expressions.Add(rowVar);

    var block = Expression.Block(new[] { typedDto, rowVar }, expressions);
    return Expression.Lambda<Func<object, TRow>>(block, dtoParam).Compile();
  }
}
