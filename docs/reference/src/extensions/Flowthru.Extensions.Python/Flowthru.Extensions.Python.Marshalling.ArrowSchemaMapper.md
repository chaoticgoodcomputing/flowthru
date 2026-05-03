# <a id="Flowthru_Extensions_Python_Marshalling_ArrowSchemaMapper"></a> Class ArrowSchemaMapper

Namespace: [Flowthru.Extensions.Python.Marshalling](Flowthru.Extensions.Python.Marshalling.md)  
Assembly: Flowthru.Extensions.Python.dll  

Maps C# schema types to Apache Arrow schemas for tabular data interchange.

```csharp
public static class ArrowSchemaMapper
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ArrowSchemaMapper](Flowthru.Extensions.Python.Marshalling.ArrowSchemaMapper.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<strong>Purpose:</strong> Generates Arrow schemas from C# types annotated with [FlowthruSchema],
preserving field names (via [SerializedLabel]) and nullability for correct DataFrame marshalling.
</p>
<p>
<strong>Type Mapping (C# → Arrow):</strong>
</p>
<ul><li>int, int? → Int32Type</li><li>long, long? → Int64Type</li><li>float, float? → FloatType</li><li>double, double? → DoubleType</li><li>bool, bool? → BooleanType</li><li>string → StringType (always nullable in Arrow)</li><li>DateTime, DateTime? → TimestampType (microsecond, UTC)</li><li>DateTimeOffset, DateTimeOffset? → TimestampType (microsecond, UTC)</li><li>TimeSpan, TimeSpan? → DurationType (microsecond)</li><li>Guid, Guid? → StringType (serialized as string)</li><li>byte[] → BinaryType</li></ul>
<p>
<strong>Field Naming:</strong> Resolves <code>[SerializedLabel]</code> attributes via
<xref href="Flowthru.Extensions.Python.Marshalling.ArrowSchemaMapper.GetFieldName(System.Reflection.PropertyInfo)" data-throw-if-not-resolved="false"></xref>, the same resolution Core's
<xref href="Flowthru.Core.Data.Serialization.PropertyMappingPlanner" data-throw-if-not-resolved="false"></xref> applies for
CSV/Parquet/JSON serializers.
</p>

## Methods

### <a id="Flowthru_Extensions_Python_Marshalling_ArrowSchemaMapper_BuildArrowSchema__1"></a> BuildArrowSchema<T\>\(\)

Generates an Apache Arrow schema from a C# schema type.

```csharp
public static Schema BuildArrowSchema<T>() where T : notnull
```

#### Returns

 Schema

Arrow schema with fields matching the C# type's properties

#### Type Parameters

`T` 

The C# schema type to map

#### Remarks

The schema includes all public instance properties, with field names determined by
[SerializedLabel] attributes or property names. Nullability is preserved from the C# type.

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

Thrown when a property type cannot be mapped to Arrow.

### <a id="Flowthru_Extensions_Python_Marshalling_ArrowSchemaMapper_BuildArrowSchema_System_Type_"></a> BuildArrowSchema\(Type\)

Generates an Apache Arrow schema from a C# type.

```csharp
public static Schema BuildArrowSchema(Type type)
```

#### Parameters

`type` [Type](https://learn.microsoft.com/dotnet/api/system.type)

The C# type to map

#### Returns

 Schema

Arrow schema with fields matching the type's properties

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

Thrown when a property type cannot be mapped to Arrow.

### <a id="Flowthru_Extensions_Python_Marshalling_ArrowSchemaMapper_BuildDtypeSpec__1"></a> BuildDtypeSpec<T\>\(\)

Builds a Python dictionary (PyDict) mapping column names to pandas dtype strings.

```csharp
public static object BuildDtypeSpec<T>() where T : notnull
```

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

PyDict with dtype specifications for df_to_ipc

#### Type Parameters

`T` 

The C# schema type

#### Remarks

Maps Arrow types to pandas dtype strings:
- Int32Type → 'int32'
- Int64Type → 'int64'
- FloatType → 'float32'
- DoubleType → 'float64'
- BooleanType → 'bool'
- StringType → 'object'

### <a id="Flowthru_Extensions_Python_Marshalling_ArrowSchemaMapper_BuildDtypeSpecDictionary__1"></a> BuildDtypeSpecDictionary<T\>\(\)

Builds a C# dictionary mapping column names to pandas dtype strings,
suitable for JSON serialization in subprocess protocol messages.

```csharp
public static Dictionary<string, string> BuildDtypeSpecDictionary<T>() where T : notnull
```

#### Returns

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

Dictionary with dtype specifications for the subprocess worker's df_to_ipc

#### Type Parameters

`T` 

The C# schema type

